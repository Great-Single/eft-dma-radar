﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace eft_dma_radar
{
    public class RegisteredPlayers
    {
        private readonly Memory _mem;
        private readonly ulong _base;
        private readonly ulong _listBase;
        private readonly HashSet<uint> _registered;
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
                return _mem.ReadInt(_base + 0x18);
            }
        }

        public RegisteredPlayers(Memory mem, ulong baseAddr)
        {
            _mem = mem;
            _base = baseAddr;
            _listBase = _mem.ReadPtr(_base + 0x0010);
            _registered = new HashSet<uint>();
            _players = new ConcurrentDictionary<string, Player>();
        }

        /// <summary>
        /// Updates the ConcurrentDictionary of 'Players'
        /// </summary>
        public void UpdateList()
        {
            int count = this.PlayerCount; // cache count
            for (uint i = 0; i < count; i++) // Add new players
            {
                try
                {
                    if (_registered.Contains(i)) continue;
                    var playerBase = _mem.ReadPtr(_listBase + 0x20 + (i * 0x8));
                    var playerProfile = _mem.ReadPtr(playerBase + 0x4B8);
                    var playerId = _mem.ReadPtr(playerProfile + 0x10);
                    var playerIdString = _mem.ReadUnityString(playerId); // Player's Personal ID ToDo Testing
                    var player = new Player(_mem, playerBase, playerProfile); // allocate player object
                    if (_players.TryAdd(playerIdString, player)) // Add to collection
                    {
                        Console.WriteLine($"Added new player from index {i + 1} of {count}" +
                            $"\nBase: 0x{playerBase.ToString("X")}" +
                            $"\nID: {playerIdString}");
                        _registered.Add(i);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ERROR iterating registered player {i + 1} of {count}: {ex}");
                }
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
