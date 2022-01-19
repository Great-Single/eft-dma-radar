using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
                ScatterReadEntry[] toScatter = new ScatterReadEntry[count];
                for (uint i = 0; i < count; i++)
                {
                    toScatter[i] = new ScatterReadEntry()
                    {
                        addr = _listBase + 0x20 + (i * 0x8),
                        type = typeof(ulong)
                    };
                }
                var playerBase = Memory.ReadScatter(toScatter);
                for (uint i = 0; i < count; i++)
                {
                    toScatter[i] = new ScatterReadEntry()
                    {
                        addr = (ulong)playerBase[i] + 0x4B8,
                        type = typeof(ulong)
                    };
                }
                var playerProfile = Memory.ReadScatter(toScatter);
                for (uint i = 0; i < count; i++)
                {
                    toScatter[i] = new ScatterReadEntry()
                    {
                        addr = (ulong)playerProfile[i] + 0x10,
                        type = typeof(ulong)
                    };
                }
                var playerId = Memory.ReadScatter(toScatter);
                for (uint i = 0; i < count; i++)
                {
                    toScatter[i] = new ScatterReadEntry()
                    {
                        addr = (ulong)playerId[i] + 0x10,
                        type = typeof(int)
                    };
                }
                var playerIdLen = Memory.ReadScatter(toScatter);
                for (uint i = 0; i < count; i++)
                {
                    toScatter[i] = new ScatterReadEntry()
                    {
                        addr = (ulong)playerId[i] + 0x14,
                        type = typeof(string),
                        size = (int)playerIdLen[i] * 2
                    };
                }
                var playerIdStr = Memory.ReadScatter(toScatter);
                for (uint i = 0; i < count; i++)
                {
                    try
                    {
                        var id = (string)playerIdStr[i];
                        if (id is null || id == String.Empty) throw new Exception("Player ID is blank/invalid.");
                        if (!_players.ContainsKey(id))
                        {
                            var player = new Player((ulong)playerBase[i], (ulong)playerProfile[i]); // allocate player object
                            _players.TryAdd(id, player);
                        }
                        else
                        {
                            lock (_players[id])
                            {
                                _players[id].IsActive = true;
                            }
                        }
                        _registered.Add(id);
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
                ScatterReadEntry[] toScatter = new ScatterReadEntry[(players.Length * 2) + (players.Length * 7)];
                int index = 0;
                for (int i = 0; i < players.Length; i++)
                {
                    toScatter[index++] = new ScatterReadEntry()
                    {
                        addr = players[i].Value.MovementContext + 0x22C,
                        type = typeof(float)
                    };
                    for (int p = 0; p < 7; p++)
                    {
                        toScatter[index++] = new ScatterReadEntry()
                        {
                            addr = players[i].Value.BodyParts[p] + 0x10,
                            type = typeof(float)
                        };
                    }
                    toScatter[index++] = new ScatterReadEntry()
                    {
                        addr = players[i].Value.PlayerTransformInternal + 0x40,
                        type = typeof(int)
                    };
                }
                var scatterRead = Memory.ReadScatter(toScatter);
                index = 0;
                int[] posIndexes = new int[players.Length]; // used for next scatter read
                for (int i = 0; i < players.Length; i++)
                {
                    lock (players[i].Value)
                    {
                        players[i].Value.SetDirection((float)scatterRead[index++]);
                        float[] bodyParts = new float[7];
                        for (int p = 0; p < 7; p++)
                        {
                            bodyParts[p] = (float)scatterRead[index++];
                        }
                        posIndexes[i] = (int)scatterRead[index++];
                        players[i].Value.SetHealth(bodyParts);
                    }
                }
                toScatter = new ScatterReadEntry[players.Length * 2];
                index = 0;
                for (int i = 0; i < players.Length; i++)
                {
                    toScatter[index++] = new ScatterReadEntry()
                    {
                        addr = players[i].Value.PlayerTransformMatrixListBase,
                        type = typeof(IntPtr),
                        size = Marshal.SizeOf(typeof(Matrix34)) * posIndexes[i] + Marshal.SizeOf(typeof(Matrix34))
                    };
                    toScatter[index++] = new ScatterReadEntry()
                    {
                        addr = players[i].Value.PlayerTransformDependencyIndexTableBase,
                        type = typeof(IntPtr),
                        size = Marshal.SizeOf(typeof(int)) * posIndexes[i] + Marshal.SizeOf(typeof(int))
                    };
                }
                scatterRead = Memory.ReadScatter(toScatter);
                index = 0;
                for (int i = 0; i < players.Length; i++)
                {
                    object[] ptrs = new object[2]
                    {
                        scatterRead[index++],
                        scatterRead[index++]
                    };
                    lock (players[i].Value)
                    {
                        players[i].Value.SetPosition(ptrs, posIndexes[i]);
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
