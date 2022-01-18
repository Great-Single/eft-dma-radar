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
                int count = this.PlayerCount; // cache count
                ulong[] addr = new ulong[count];
                for (uint i = 0; i < count; i++)
                {
                    addr[i] = _listBase + 0x20 + (i * 0x8);
                }
                var playerBase = Memory.ReadScatter(addr);
                for (uint i = 0; i < count; i++)
                {
                    addr[i] = playerBase[i] + 0x4B8;
                }
                var playerProfile = Memory.ReadScatter(addr);
                for (uint i = 0; i < count; i++)
                {
                    addr[i] = playerProfile[i] + 0x10;
                }
                var playerId = Memory.ReadScatter(addr);
                for (uint i = 0; i < count; i++)
                {
                    try
                    {
                        var playerIdString = Memory.ReadUnityString(playerId[i]); // Player's Personal ID ToDo Testing
                        if (!_players.ContainsKey(playerIdString))
                        {
                            var player = new Player(playerBase[i], playerProfile[i]); // allocate player object
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
            foreach (var player in _players) // Update all players
            {
                lock (player.Value) // Obtain object lock
                {
                    player.Value.Update();
                }
            }
        }
    }
}
