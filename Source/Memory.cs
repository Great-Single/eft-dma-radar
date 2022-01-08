using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using vmmsharp;

namespace eft_dma_radar
{
    public class Memory : IDisposable
    {
        private readonly Thread _worker;
        private Game _game;
        private uint _pid;
        public ulong BaseModule { get; private set; }
        public bool InGame
        {
            get
            {
                return _game?.InGame ?? false;
            }
        }
        public ConcurrentDictionary<string, Player> Players
        {
            get
            {
                return _game?.Players;
            }
        }

        public Memory()
        {
            Console.WriteLine("Loading memory module...");
            vmm.Initialize("-printf", "-v", "-device", "FPGA"); // Initialize DMA device
            Console.WriteLine("Starting Memory worker thread...");
            _worker = new Thread(() => Worker()) { IsBackground = true };
            _worker.Start(); // Start new background thread to do memory operations on
        }

        private void Worker()
        {
            while (true)
            {
                while (true) // Startup loop
                {
                    if (GetPid()
                    && GetModuleBase()
                    )
                    {
                        Console.WriteLine($"EFT is running at PID {_pid}, and found module base entry for UnityPlayer.dll at {BaseModule.ToString("X")}");
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Unable to find EFT process, trying again in 15 seconds...");
                        Thread.Sleep(15000);
                    }
                }
                while (Heartbeat())
                {
                    _game = new Game(this);
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
                Console.WriteLine("Escape From Tarkov is no longer running!");
            }
        }

        private bool GetPid()
        {
            if (_disposed) throw new ObjectDisposedException("Memory Module has been disposed!");
            try
            {
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

        private bool GetModuleBase()
        {
            if (_disposed) throw new ObjectDisposedException("Memory Module has been disposed!");
            try
            {
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
        public unsafe void ReadBuffer(ulong addr, IntPtr bufPtr, int size)
        {
            if (_disposed) throw new ObjectDisposedException("Memory Module has been disposed!");
            Marshal.Copy(vmm.MemRead(_pid, addr, (uint)size, vmm.FLAG_NOCACHE)
                , 0, bufPtr, size);
        }

        /// <summary>
        /// Read a chain of pointers.
        /// </summary>
        public ulong ReadPtrChain(ulong ptr, uint[] offsets)
        {
            if (_disposed) throw new ObjectDisposedException("Memory Module has been disposed!");
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
        public ulong ReadPtr(ulong ptr) => ReadUlong(ptr);


        public ulong ReadUlong(ulong addr)
        {
            if (_disposed) throw new ObjectDisposedException("Memory Module has been disposed!");
            try
            {
                return BitConverter.ToUInt64(vmm.MemRead(_pid, addr, 8, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading UInt64 at 0x{addr.ToString("X")}", ex);
            }
        }

        public long ReadLong(ulong addr) // read 8 bytes (int64)
        {
            if (_disposed) throw new ObjectDisposedException("Memory Module has been disposed!");
            try
            {
                return BitConverter.ToInt64(vmm.MemRead(_pid, addr, 8, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading Int64 at 0x{addr.ToString("X")}", ex);
            }
        }
        public int ReadInt(ulong addr) // read 4 bytes (int32)
        {
            if (_disposed) throw new ObjectDisposedException("Memory Module has been disposed!");
            try
            {
                return BitConverter.ToInt32(vmm.MemRead(_pid, addr, 4, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading Int32 at 0x{addr.ToString("X")}", ex);
            }
        }
        public uint ReadUint(ulong addr) // read 4 bytes (uint32)
        {
            if (_disposed) throw new ObjectDisposedException("Memory Module has been disposed!");
            try
            {
                return BitConverter.ToUInt32(vmm.MemRead(_pid, addr, 4, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading Uint32 at 0x{addr.ToString("X")}", ex);
            }
        }
        public float ReadFloat(ulong addr) // read 4 bytes (float)
        {
            if (_disposed) throw new ObjectDisposedException("Memory Module has been disposed!");
            try
            {
                return BitConverter.ToSingle(vmm.MemRead(_pid, addr, 4, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading float at 0x{addr.ToString("X")}", ex);
            }
        }
        public double ReadDouble(ulong addr) // read 8 bytes (double)
        {
            if (_disposed) throw new ObjectDisposedException("Memory Module has been disposed!");
            try
            {
                return BitConverter.ToDouble(vmm.MemRead(_pid, addr, 8, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading double at 0x{addr.ToString("X")}", ex);
            }
        }
        public bool ReadBool(ulong addr) // read 1 byte (bool)
        {
            if (_disposed) throw new ObjectDisposedException("Memory Module has been disposed!");
            try
            {
                return BitConverter.ToBoolean(vmm.MemRead(_pid, addr, 1, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading boolean at 0x{addr.ToString("X")}", ex);
            }
        }

        public T ReadStruct<T>(ulong addr) // Read structure from memory location
        {
            if (_disposed) throw new ObjectDisposedException("Memory Module has been disposed!");
            int size = Marshal.SizeOf(typeof(T));
            var mem = Marshal.AllocHGlobal(size); // alloc mem
            try
            {
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
        public string ReadString(ulong addr, uint size) // read n bytes (string)
        {
            if (_disposed) throw new ObjectDisposedException("Memory Module has been disposed!");
            try
            {
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
        public string ReadUnityString(ulong addr)
        {
            if (_disposed) throw new ObjectDisposedException("Memory Module has been disposed!");
            try
            {
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
        public bool Heartbeat() // Make sure game is still there
        {
            vmm.PidGetFromName("EscapeFromTarkov.exe", out uint pid);
            if (pid == 0) return false;
            else return true;
        }

        // Public implementation of Dispose pattern callable by consumers.
        private volatile bool _disposed = false;
        /// <summary>
        /// Calls vmm.Close() and cleans up DMA Unmanaged Resources.
        /// </summary>
        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _disposed = true;
                vmm.Close(); // Cleanup vmmsharp resources
            }
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
