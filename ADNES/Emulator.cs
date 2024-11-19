using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ADNES.Cartridge;
using ADNES.Controller;
using ADNES.CPU;
using ADNES.Enums;

namespace ADNES
{
    public class Emulator(
        Emulator.ProcessFrameDelegate processFrameDelegate,
        EmulatorSpeed emulatorSpeed = EmulatorSpeed.Normal)
    {
        /// <summary>
        ///    Delegate used to process a frame of data from the PPU
        /// </summary>
        /// <param name="outputFrame">Raw 8bpp Bitmap data from the NES PPU<param>
        public delegate void ProcessFrameDelegate(byte[] outputFrame);

        //NES System Components
        private Core _cpu;
        private PPU.Core _ppu;
        private NESCartridge _cartridge;

        /// <summary>
        ///    Task which will contain the Emulator rendering loop while the Emulator is running
        /// </summary>
        private Task _emulatorTask;

        /// <summary>
        ///     iNES ROM Data that is loaded into the Emulator
        /// </summary>
        private byte[] _romData;

        /// <summary>
        ///     Player 1 Controller for the Emulator
        /// </summary>
        public readonly IController Controller1 = new NESController();

        /// <summary>
        ///     Flag to determine if the Emulator is currently running
        /// </summary>
        public bool IsRunning;

        //Public Statistics

        /// <summary>
        ///     Total CPU Cycles since the Emulator was started
        /// </summary>
        public long TotalCPUCycles => _cpu.Cycles;

        /// <summary>
        ///     Total PPU Cycles since the Emulator was started
        /// </summary>
        public long TotalPPUCycles => _ppu.Cycles;

        /// <summary>
        ///     Height of Frames Rendered by the NES Emulator
        /// </summary>
        public const int Height = 240;

        /// <summary>
        ///     Width of Frames Rendered by the NES Emulator
        /// </summary>
        public const int Width = 256;

        //Internal Statistics
        private int _cpuIdleCycles;

        public Emulator(byte[] rom, ProcessFrameDelegate processFrameDelegate, EmulatorSpeed emulatorSpeed = EmulatorSpeed.Normal) : this(processFrameDelegate, emulatorSpeed)
        {
            _romData = rom;
            _cartridge = new NESCartridge(rom);
        }

        //Setup Emulator Components

        /// <summary>
        ///     Load new ROM into memory
        /// </summary>
        /// <param name="romData"></param>
        public void LoadRom(byte[] romData)
        {
            //Make sure we're not currently running
            if (IsRunning)
                throw new Exception("Cannot Load ROM while Emulator is Running");

            _romData = romData;
            _cartridge = new NESCartridge(_romData);
        }

        /// <summary>
        ///     News up and Starts the Emulator Task
        /// </summary>
        public void Start()
        {
            //Verify there's a delegate assigned to process frames
            if (processFrameDelegate == null)
                throw new Exception("No Frame Processing Delegate Assigned");

            if (_romData == null)
                throw new Exception("No ROM Data Loaded");

            _cartridge.LoadROM(_romData);
            _ppu = new PPU.Core(_cartridge.MemoryMapper, DMATransfer);
            _cpu = new Core(_cartridge.MemoryMapper, Controller1);

            _cpu.Reset();
            _ppu.Reset();
            IsRunning = true;
            _emulatorTask = new TaskFactory().StartNew(Run, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        ///     Signals the Emulator Task to stop
        /// </summary>
        public void Stop() => IsRunning = false;

        /// <summary>
        ///     Delegate used to transfer information between CPU memory (typically CPU RAM) and the PPU OAM buffer
        ///
        ///     https://wiki.nesdev.com/w/index.php/PPU_registers#OAMDMA
        /// </summary>
        /// <param name="oam">OAM Memory -- always 256 bytes</param>
        /// <param name="oamOffset"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private byte[] DMATransfer(byte[] oam, int oamOffset, int offset)
        {
            for(var i = 0; i < 256; i++)
            {
                oam[(oamOffset + i) % 256] = _cpu.CPUMemory.ReadByte(offset + i);
            }

            /*
             *DMA Transfer consumes 513 cycles of the CPU, so we mark those cycles
             *as idle cycles, which basically means we'll keep ticking the PPU 3 times
             *for each idle CPU cycle, and the CPU will tick once the idle cycles are
             *passed
             *
             *If DMA occurs on an odd CPU cycle, it takes an extra cycle
             *Not even joking: https://wiki.nesdev.com/w/index.php/PPU_registers#OAMDMA
             */
            _cpuIdleCycles = 513;
            if (_cpu.Cycles % 2 == 1) _cpuIdleCycles++; 

            return oam;
        }

        /// <summary>
        ///     Method used to Run the Emulator Task
        ///
        ///     Task will run until the _powerOn value is set to false
        /// </summary>
        private void Run()
        {
            //Frame Timing Stopwatch
            var sw = Stopwatch.StartNew();

            //CPU startup state is always at 4 cycles
            _cpu.Cycles = 4;

            int cpuTicks;
            while (IsRunning)
            {
                //If we're not idling (DMA), tick the CPU
                if (_cpuIdleCycles == 0)
                {
                    cpuTicks = _cpu.Tick();
                }
                else
                {
                    //Otherwise, mark it as an idle cycle and carry on
                    _cpuIdleCycles--;
                    _cpu.Instruction.Cycles = 1;
                    _cpu.Cycles++;
                    cpuTicks = 1;
                }

                //Count how many cycles that instruction took and
                //execute that number of instruction * 3 for the PPU
                //We do ceiling since it's ok for the PPU to overshoot at this point
                for (var i = 0; i < cpuTicks * 3; i++)
                {
                    _ppu.Tick();
                }

                //If the PPU has signaled NMI, reset its status and signal the CPU
                if (_ppu.NMI)
                {
                    _ppu.NMI = false;
                    _cpu.NMI = true;
                }

                //Check to see if there's a frame in the PPU Frame Buffer
                //If there is, let's render it
                if (_ppu.FrameReady)
                {
                    processFrameDelegate(_ppu.FrameBuffer);
                    _ppu.FrameReady = false;

                    //Throttle our frame rate here to the desired rate (if required)
                    switch (emulatorSpeed)
                    {
                        case EmulatorSpeed.Turbo:
                            continue;
                        case EmulatorSpeed.Normal when sw.ElapsedMilliseconds < 17:
                            Thread.Sleep((int)(17 - sw.ElapsedMilliseconds));
                            break;
                        case EmulatorSpeed.Half when sw.ElapsedMilliseconds < 32:
                            Thread.Sleep((int)(32 - sw.ElapsedMilliseconds));
                            break;
                    }
                    sw.Restart();
                }
            }
        }
    }
}