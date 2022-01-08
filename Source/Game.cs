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
        private readonly Memory _mem;
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

        public Game(Memory mem)
        {
            _mem = mem;
        }

        /// <summary>
        /// Waits until Raid has started before returning to caller.
        /// </summary>
        public void WaitForGame()
        {
            while (true)
            {
                if (!_mem.Heartbeat()) throw new Exception("Game is not running!");
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
                var addr = _mem.ReadPtr(_mem.BaseModule + 0x17F8D28);
                _gom = _mem.ReadStruct<GameObjectManager>(addr);
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
            var activeObject = _mem.ReadStruct<BaseObject>(_mem.ReadPtr(listPtr));
            var lastObject = _mem.ReadStruct<BaseObject>(_mem.ReadPtr(lastObjectPtr));

            if (activeObject.obj != 0x0)
            {
                while (activeObject.obj != 0x0 && activeObject.obj != lastObject.obj)
                {
                    try
                    {
                        var objectNamePtr = _mem.ReadPtr(activeObject.obj + 0x60);
                        var objectNameStr = _mem.ReadString(objectNamePtr, 24);
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

                    activeObject = _mem.ReadStruct<BaseObject>(activeObject.nextObjectLink); // Read next object
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
                    _mem.ReadPtr(_gom.ActiveNodes),
                    _mem.ReadPtr(_gom.LastActiveNode),
                    "GameWorld");
                if (gameWorld == 0) throw new DMAException("Unable to find GameWorld Object, likely not in raid.");
                _localGameWorld = _mem.ReadPtrChain(gameWorld, new uint[] { 0x30, 0x18, 0x28 }); // Game world >> Local Game World
                var rgtPlayers = new RegisteredPlayers(_mem, _mem.ReadPtr(_localGameWorld + 0x80));
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
