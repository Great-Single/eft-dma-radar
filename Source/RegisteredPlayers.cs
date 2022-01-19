using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace eft_dma_radar
{
    public class RegisteredPlayers
    {
        private readonly ulong _base;
        private readonly ulong _listBase;
        private readonly HashSet<string> _registered;
        private readonly ConcurrentDictionary<string, Player> _players; // backing field
        public ConcurrentDictionary<string, Player> Players
        {
            get
            {
                return _players;
            }
        }

        public int PlayerCount
        {
            get
            {
                return Memory.ReadInt(_base + 0x18);
            }
        }

        public RegisteredPlayers(ulong baseAddr)
        {
            _base = baseAddr;
            _listBase = Memory.ReadPtr(_base + 0x0010);
            _registered = new HashSet<string>();
            _players = new ConcurrentDictionary<string, Player>();
        }

        /// <summary>
        /// Updates the ConcurrentDictionary of 'Players'
        /// </summary>
        public void UpdateList()
        {
            try
            {
                _registered.Clear();
                var count = this.PlayerCount; // cache count
                ulong[] addr = new ulong[count];
                Type[] types = new Type[count];
                for (uint i = 0; i < count; i++)
                {
                    addr[i] = _listBase + 0x20 + (i * 0x8);
                    types[i] = typeof(ulong);
                }
                var playerBase = Memory.ReadScatter(addr, types);
                for (uint i = 0; i < count; i++)
                {
                    addr[i] = (ulong)playerBase[i] + 0x4B8;
                }
                var playerProfile = Memory.ReadScatter(addr, types);
                for (uint i = 0; i < count; i++)
                {
                    addr[i] = (ulong)playerProfile[i] + 0x10;
                }
                var playerId = Memory.ReadScatter(addr, types);
                for (uint i = 0; i < count; i++)
                {
                    try
                    {
                        var playerIdString = Memory.ReadUnityString((ulong)playerId[i]); // Player's Personal ID ToDo Testing
                        if (!_players.ContainsKey(playerIdString))
                        {
                            var player = new Player((ulong)playerBase[i], (ulong)playerProfile[i]); // allocate player object
                            _players.TryAdd(playerIdString, player);
                        }
                        else
                        {
                            lock (_players[playerIdString])
                            {
                                _players[playerIdString].IsActive = true;
                            }
                        }
                        _registered.Add(playerIdString);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"ERROR iterating registered player {i + 1} of {count}: {ex}");
                    }
                }
                var inactivePlayers = _players.Where(x => !_registered.Contains(x.Key));
                foreach (KeyValuePair<string, Player> player in inactivePlayers)
                {
                    lock (player.Value)
                    {
                        player.Value.Update(); // update one last time
                        player.Value.IsActive = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR iterating registered players: {ex}");
            }
        }

        /// <summary>
        /// Updates all 'Player' values (Position,health,direction,etc.)
        /// </summary>
        public void UpdateAllPlayers()
        {
            try
            {
                var players = _players.Where(x => x.Value.IsActive && x.Value.IsAlive).ToArray();
                ulong[] toScatter = new ulong[players.Length + (players.Length * 7)];
                Type[] types = new Type[players.Length + (players.Length * 7)];
                int index = 0;
                for (int i = 0; i < players.Length; i++)
                {
                    toScatter[index] = players[i].Value.MovementContext + 0x22C;
                    types[index] = typeof(float);
                    index++;
                    for (int p = 0; p < 7; p++)
                    {
                        toScatter[index] = players[i].Value.BodyParts[p] + 0x10;
                        types[index] = typeof(float);
                        index++;
                    }
                }
                var scatterRead = Memory.ReadScatter(toScatter, types);
                index = 0;
                for (int i = 0; i < players.Length; i++)
                {
                    lock (players[i].Value)
                    {
                        players[i].Value.GetDirection((float)scatterRead[index]);
                        index++;
                        float[] bodyParts = new float[7];
                        for (int p = 0; p < 7; p++)
                        {
                            bodyParts[p] = (float)scatterRead[index];
                            index++;
                        }
                        players[i].Value.GetHealth(bodyParts);
                        players[i].Value.GetPosition(); // non scatter
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR updating All Players: {ex}");
            }
        }
    }
}
