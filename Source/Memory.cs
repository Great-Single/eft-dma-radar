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
            try
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
            } catch (DMAShutdown) { return; } // Shutdown Thread Gracefully
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
            catch (DMAShutdown) { throw; }
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
            catch (DMAShutdown) { throw; }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting module base: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Performs multiple reads in one sequence, significantly faster than single reads.
        /// </summary>
        // Credit to asmfreak https://www.unknowncheats.me/forum/3345474-post27.html
        public static object[] ReadScatter(ScatterReadEntry[] entries)
        {
            ThrowIfDMAShutdown();
            uint dwMemScatters = 0;
            List<ulong> toScatter = new List<ulong>();
            for (int i = 0; i < entries.Length; i++)
            {
                ulong dwAddress = entries[i].addr;
                uint size;
                if (entries[i].type == typeof(IntPtr) || entries[i].type == typeof(string))
                    size = (uint)entries[i].size;
                else size = (uint)Marshal.SizeOf(entries[i].type);

                //get the number of pages
                uint dwNumPages = GetNumberOfPages(dwAddress, size);

                //loop all the pages we would need
                for (int p = 0; p < dwNumPages; p++)
                {
                    toScatter.Add(PageAlign(dwAddress));
                    dwMemScatters++;
                }
            }
            var scatters = vmm.MemReadScatter(_pid, vmm.FLAG_NOCACHE, toScatter.ToArray());
            object[] results = new object[entries.Length];

            dwMemScatters = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                ulong dwAdd = entries[i].addr;

                uint dwPageOffset = PAGE_OFFSET(dwAdd);

                uint size;
                if (entries[i].type == typeof(IntPtr) || entries[i].type == typeof(string))
                    size = (uint)entries[i].size;
                else size = (uint)Marshal.SizeOf(entries[i].type);
                byte[] buffer = new byte[size];
                int bufferOffset = 0;
                uint cb = Math.Min(size, (uint)Environment.SystemPageSize - dwPageOffset);

                uint dwNumPages = GetNumberOfPages(dwAdd, size);

                for (int p = 0; p < dwNumPages; p++)
                {
                    if (scatters[dwMemScatters].f)
                    {
                        Buffer.BlockCopy(scatters[dwMemScatters].pb, (int)dwPageOffset, buffer, bufferOffset, (int)cb);
                        bufferOffset += (int)cb;
                    }
                    else
                        for (int p2 = 0; p2 < cb; p2++)
                        {
                            Buffer.BlockCopy(new byte[] { 0 }, 0, buffer, bufferOffset, 1);
                            bufferOffset++;
                        }

                    cb = (uint)Environment.SystemPageSize;
                    if (((dwPageOffset + size) & 0xfff) != 0)
                        cb = ((dwPageOffset + size) & 0xfff);

                    dwPageOffset = 0;
                    dwMemScatters++;
                }
                try
                {
                    if (entries[i].type == typeof(ulong))
                    {
                        results[i] = BitConverter.ToUInt64(buffer);
                    }
                    else if (entries[i].type == typeof(float))
                    {
                        results[i] = BitConverter.ToSingle(buffer);
                    }
                    else if (entries[i].type == typeof(int))
                    {
                        results[i] = BitConverter.ToInt32(buffer);
                    }
                    else if (entries[i].type == typeof(IntPtr))
                    {
                        if (buffer.Length != size) throw new DMAException("Incomplete buffer read!");
                        var memBuf = Marshal.AllocHGlobal((int)size); // alloc memory (must free later)
                        Marshal.Copy(buffer, 0, memBuf, (int)size); // Copy to mem buffer
                        results[i] = memBuf; // Store ref to mem buffer
                    }
                    else if (entries[i].type == typeof(string)) // Assumed Unity String
                    {
                        results[i] = Encoding.Unicode.GetString(buffer);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error parsing result from Scatter Read: {ex}");
                }
            }
            return results;
        }

        private static uint PAGE_OFFSET(ulong addr)
        {
            return (uint)(addr - PageAlign(addr));
        }


        /// <summary>
        /// Copy 'n' bytes to unmanaged memory. Caller is responsible for freeing memory.
        /// </summary>
        public static void ReadBuffer(ulong addr, IntPtr bufPtr, int size)
        {
            ThrowIfDMAShutdown();
            var readBuf = vmm.MemRead(_pid, addr, (uint)size, vmm.FLAG_NOCACHE);
            if (readBuf.Length != size) throw new DMAException("Incomplete buffer read!");
            Marshal.Copy(readBuf
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
                var length = (uint)ReadInt(addr + Offsets.UnityString_Len);
                return Encoding.Unicode.GetString(
                    vmm.MemRead(_pid, addr + Offsets.UnityString_Value, length * 2, vmm.FLAG_NOCACHE));
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
            if (!_running) throw new DMAShutdown("DMA Device is no longer initialized!");
        }

        // Helper DLL to perform memory alignment macros

        /// <summary>
        /// Returns memory address of aligned page.
        /// </summary>
        [DllImport("memalign.dll")]
        private static extern ulong PageAlign(ulong addr);
        /// <summary>
        /// Returns the number of pages the address/size is within.
        /// </summary>
        [DllImport("memalign.dll")]
        private static extern uint GetNumberOfPages(ulong addr, uint size);
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

    public class DMAShutdown : Exception
    {
        public DMAShutdown()
        {
        }

        public DMAShutdown(string message)
            : base(message)
        {
        }

        public DMAShutdown(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

}
