using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using vmmsharp;

namespace eft_dma_radar
{
    internal static class Memory
    {
        private static volatile bool _running = false;
        private static readonly Thread _worker;
        private static Game _game;
        private static uint _pid;
        public static ulong BaseModule { get; private set; }
        public static bool InGame
        {
            get
            {
                return _game?.InGame ?? false;
            }
        }
        public static ConcurrentDictionary<string, Player> Players
        {
            get
            {
                return _game?.Players;
            }
        }

        static Memory()
        {
            try
            {
                Debug.WriteLine("Loading memory module...");
                if (!vmm.Initialize("-printf", "-v", "-device", "FPGA", "-memmap", "mmap.txt")) // Initialize DMA device
                    throw new DMAException("ERROR initializing DMA Device! If you do not have a memory map (mmap.txt) edit line 37 in Memory.cs");
                Debug.WriteLine("Starting Memory worker thread...");
                _worker = new Thread(() => Worker()) { IsBackground = true };
                _worker.Start(); // Start new background thread to do memory operations on
                _running = true;
                Program.ShowWindow(Program.GetConsoleWindow(), 0); // Hide console if successful
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "DMA Init", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
            }
        }

        private static void Worker()
        {
            while (true)
            {
                while (true) // Startup loop
                {
                    if (GetPid()
                    && GetModuleBase()
                    )
                    {
                        Debug.WriteLine($"EFT is running at PID {_pid}, and found module base entry for UnityPlayer.dll at {BaseModule.ToString("X")}");
                        break;
                    }
                    else
                    {
                        Debug.WriteLine("Unable to find EFT process, trying again in 15 seconds...");
                        Thread.Sleep(15000);
                    }
                }
                while (Heartbeat())
                {
                    _game = new Game();
                    try
                    {
                        _game.WaitForGame();
                        while (_game.InGame)
                        {
                            _game.GameLoop();
                        }
                    }
                    catch
                    {
                        Debug.WriteLine("Unhandled exception in Game Loop, restarting...");
                    }
                }
                Debug.WriteLine("Escape From Tarkov is no longer running!");
            }
        }

        private static bool GetPid()
        {
            try
            {
                ThrowIfDMAShutdown();
                vmm.PidGetFromName("EscapeFromTarkov.exe", out _pid);
                if (_pid == 0) throw new DMAException("Unable to obtain PID. Game may not be running.");
                else
                {
                    Debug.WriteLine($"EscapeFromTarkov.exe is running at PID {_pid}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting PID: {ex}");
                return false;
            }
        }

        private static bool GetModuleBase()
        {
            try
            {
                ThrowIfDMAShutdown();
                BaseModule = vmm.ProcessGetModuleBase(_pid, "UnityPlayer.dll");
                if (BaseModule == 0) throw new DMAException("Unable to obtain Base Module Address. Game may not be running");
                else
                {
                    Debug.WriteLine($"Found UnityPlayer.dll at 0x{BaseModule.ToString("x")}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting module base: {ex}");
                return false;
            }
        }
 

        /// <summary>
        /// Copy 'n' bytes to unmanaged memory. Caller is responsible for freeing memory.
        /// </summary>
        public static void ReadBuffer(ulong addr, IntPtr bufPtr, int size)
        {
            ThrowIfDMAShutdown();
            Marshal.Copy(vmm.MemRead(_pid, addr, (uint)size, vmm.FLAG_NOCACHE)
                , 0, bufPtr, size);
        }

        /// <summary>
        /// Read a chain of pointers.
        /// </summary>
        public static ulong ReadPtrChain(ulong ptr, uint[] offsets)
        {
            ulong addr = 0;
            try { addr = ReadPtr(ptr + offsets[0]); }
            catch (Exception ex) { throw new DMAException($"ERROR reading pointer chain at index 0, addr 0x{ptr.ToString("X")} + 0x{offsets[0].ToString("X")}", ex); }
            for (int i = 1; i < offsets.Length; i++)
            {
                try { addr = ReadPtr(addr + offsets[i]); }
                catch (Exception ex) { throw new DMAException($"ERROR reading pointer chain at index {i}, addr 0x{addr.ToString("X")} + 0x{offsets[i].ToString("X")}", ex); }
            }
            return addr;
        }
        /// <summary>
        /// Resolves a pointer and returns the memory address it points to.
        /// </summary>
        public static ulong ReadPtr(ulong ptr) => ReadUlong(ptr);


        public static ulong ReadUlong(ulong addr)
        {
            try
            {
                ThrowIfDMAShutdown();
                return BitConverter.ToUInt64(vmm.MemRead(_pid, addr, 8, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading UInt64 at 0x{addr.ToString("X")}", ex);
            }
        }

        public static long ReadLong(ulong addr) // read 8 bytes (int64)
        {
            try
            {
                ThrowIfDMAShutdown();
                return BitConverter.ToInt64(vmm.MemRead(_pid, addr, 8, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading Int64 at 0x{addr.ToString("X")}", ex);
            }
        }
        public static int ReadInt(ulong addr) // read 4 bytes (int32)
        {
            try
            {
                ThrowIfDMAShutdown();
                return BitConverter.ToInt32(vmm.MemRead(_pid, addr, 4, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading Int32 at 0x{addr.ToString("X")}", ex);
            }
        }
        public static uint ReadUint(ulong addr) // read 4 bytes (uint32)
        {
            try
            {
                ThrowIfDMAShutdown();
                return BitConverter.ToUInt32(vmm.MemRead(_pid, addr, 4, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading Uint32 at 0x{addr.ToString("X")}", ex);
            }
        }
        public static float ReadFloat(ulong addr) // read 4 bytes (float)
        {
            try
            {
                ThrowIfDMAShutdown();
                return BitConverter.ToSingle(vmm.MemRead(_pid, addr, 4, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading float at 0x{addr.ToString("X")}", ex);
            }
        }
        public static double ReadDouble(ulong addr) // read 8 bytes (double)
        {
            try
            {
                ThrowIfDMAShutdown();
                return BitConverter.ToDouble(vmm.MemRead(_pid, addr, 8, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading double at 0x{addr.ToString("X")}", ex);
            }
        }
        public static bool ReadBool(ulong addr) // read 1 byte (bool)
        {
            try
            {
                ThrowIfDMAShutdown();
                return BitConverter.ToBoolean(vmm.MemRead(_pid, addr, 1, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading boolean at 0x{addr.ToString("X")}", ex);
            }
        }

        public static T ReadStruct<T>(ulong addr) // Read structure from memory location
        {
            int size = Marshal.SizeOf(typeof(T));
            var mem = Marshal.AllocHGlobal(size); // alloc mem
            try
            {
                ThrowIfDMAShutdown();
                Marshal.Copy(
                    vmm.MemRead(_pid, addr, (uint)size, vmm.FLAG_NOCACHE), 
                    0, mem, size); // Read to pointer location

                return (T)Marshal.PtrToStructure(mem, typeof(T)); // Convert bytes to struct
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading struct at 0x{addr.ToString("X")}", ex);
            }
            finally
            {
                Marshal.FreeHGlobal(mem); // free mem
            }
        }
        /// <summary>
        /// Read 'n' bytes at specified address and convert directly to a string.
        /// </summary>
        public static string ReadString(ulong addr, uint size) // read n bytes (string)
        {
            try
            {
                ThrowIfDMAShutdown();
                return Encoding.Default.GetString(
                    vmm.MemRead(_pid, addr, size, vmm.FLAG_NOCACHE));
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading string at 0x{addr.ToString("X")}", ex);
            }
        }

        /// <summary>
        /// Read UnityEngineString structure
        /// </summary>
        public static string ReadUnityString(ulong addr)
        {
            try
            {
                ThrowIfDMAShutdown();
                var length = (uint)ReadInt(addr + 0x10);
                return Encoding.Unicode.GetString(
                    vmm.MemRead(_pid, addr + 0x14, length * 2, vmm.FLAG_NOCACHE));
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading UnityString at 0x{addr.ToString("X")}", ex);
            }
        }

        /// <summary>
        /// ToDo - Not sure if this is a good way to keep track if the process is still open
        /// </summary>
        public static bool Heartbeat() // Make sure game is still there
        {
            ThrowIfDMAShutdown();
            vmm.PidGetFromName("EscapeFromTarkov.exe", out uint pid);
            if (pid == 0) return false;
            else return true;
        }

        /// <summary>
        /// Close down DMA Device Connection.
        /// </summary>
        public static void Shutdown()
        {
            if (_running)
            {
                Debug.WriteLine("Closing down DMA Connection...");
                _running = false;
                vmm.Close();
            }
        }

        private static void ThrowIfDMAShutdown()
        {
            if (!_running) throw new DMAException("DMA Device is no longer initialized!");
        }

    }

    public class DMAException : Exception
    {
        public DMAException()
        {
        }

        public DMAException(string message)
            : base(message)
        {
        }

        public DMAException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

}
