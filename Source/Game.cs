using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace eft_dma_radar
{

    /// <summary>
    /// Class containing Game (Raid) instance.
    /// </summary>
    public class Game
    {
        private GameObjectManager _gom;
        private ulong _localGameWorld;
        private RegisteredPlayers _rgtPlayers;
        public volatile bool InGame = false;
        public ConcurrentDictionary<string, Player> Players
        {
            get
            {
                return _rgtPlayers?.Players;
            }
        }

        public Game()
        {
        }

        /// <summary>
        /// Waits until Raid has started before returning to caller.
        /// </summary>
        public void WaitForGame()
        {
            while (true)
            {
                if (!Memory.Heartbeat()) throw new Exception("Game is not running!");
                if (GetGOM() && GetLGW()) break;
                else Thread.Sleep(1500);
            }
            Console.WriteLine("Raid has started!");
            InGame = true;
        }

        /// <summary>
        /// Gets Game Object Manager structure.
        /// </summary>
        private bool GetGOM()
        {
            try
            {
                var addr = Memory.ReadPtr(Memory.BaseModule + 0x17F8D28);
                _gom = Memory.ReadStruct<GameObjectManager>(addr);
                Debug.WriteLine($"Found Game Object Manager at 0x{addr.ToString("X")}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting Game Object Manager: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Helper method to locate Game World object.
        /// </summary>
        private ulong GetObjectFromList(ulong listPtr, ulong lastObjectPtr, string objectName)
        {
            var activeObject = Memory.ReadStruct<BaseObject>(Memory.ReadPtr(listPtr));
            var lastObject = Memory.ReadStruct<BaseObject>(Memory.ReadPtr(lastObjectPtr));

            if (activeObject.obj != 0x0)
            {
                while (activeObject.obj != 0x0 && activeObject.obj != lastObject.obj)
                {
                    try
                    {
                        var objectNamePtr = Memory.ReadPtr(activeObject.obj + 0x60);
                        var objectNameStr = Memory.ReadString(objectNamePtr, 24);
                        if (objectNameStr.Contains(objectName, StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.WriteLine($"Found object {objectNameStr}");
                            return activeObject.obj;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"ERROR parsing object name, moving onto next: {ex}");
                    }

                    activeObject = Memory.ReadStruct<BaseObject>(activeObject.nextObjectLink); // Read next object
                }
            }
            Debug.WriteLine($"Couldn't find object {objectName}");
            return 0;
        }

        /// <summary>
        /// Gets Local Game World address.
        /// </summary>
        private bool GetLGW()
        {
            try
            {
                var gameWorld = GetObjectFromList(
                    Memory.ReadPtr(_gom.ActiveNodes),
                    Memory.ReadPtr(_gom.LastActiveNode),
                    "GameWorld");
                if (gameWorld == 0) throw new DMAException("Unable to find GameWorld Object, likely not in raid.");
                _localGameWorld = Memory.ReadPtrChain(gameWorld, new uint[] { 0x30, 0x18, 0x28 }); // Game world >> Local Game World
                var rgtPlayers = new RegisteredPlayers(Memory.ReadPtr(_localGameWorld + 0x80));
                if (rgtPlayers.PlayerCount > 1) // Make sure not in hideout,etc.
                {
                    _rgtPlayers = rgtPlayers;
                    return true;
                }
                else
                {
                    Debug.WriteLine("ERROR - Local Game World does not contain players (hideout?)");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting Local Game World: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Main Game Loop executed by Memory Worker Thread. Updates player list, and updates all player values.
        /// </summary>
        public void GameLoop()
        {
            int playerCount = _rgtPlayers.PlayerCount;
            if (playerCount < 1 || playerCount > 1024)
            {
                Console.WriteLine("Raid has ended!");
                InGame = false;
                return;
            }
            _rgtPlayers.UpdateList(); // Check for new players, add to list
            _rgtPlayers.UpdateAllPlayers(); // Update all player locations,etc.
        }
    }
}
