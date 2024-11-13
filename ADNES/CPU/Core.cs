using System;
using System.Net;
using System.Runtime.CompilerServices;
using ADNES.Cartridge.Mappers;
using ADNES.Common.Extensions;
using ADNES.Controller;
using ADNES.CPU.Enums;

namespace ADNES.CPU
{
    /// <summary>
    ///     Emulated MOS 6502 8-bit CPU Core
    ///     
    ///     In-Depth documentation: http://www.obelisk.me.uk/6502/reference.html
    /// </summary>
    internal class Core
    {
        /// <summary>
        ///     X Index Register
        /// </summary>
        public byte X { get; set;}

        /// <summary>
        ///     Y Index Register
        /// </summary>
        public byte Y { get; set; }

        /// <summary>
        ///     Accumulator
        /// </summary>
        public byte A { get; set; }

        /// <summary>
        ///     Program Counter
        /// </summary>
        public int PC { get; set; }

        /// <summary>
        ///     Stack Pointer
        /// </summary>
        public byte SP { get; set; }

        /// <summary>
        ///     CPU Status Flags
        /// </summary>
        public CPUStatus Status { get; set; }

        /// <summary>
        ///     CPU Memory Space
        /// </summary>
        public Memory CPUMemory { get; set; }

        /// <summary>
        ///     Total Cycles the core has executed since starting
        /// </summary>
        public long Cycles { get; set; }

        /// <summary>
        ///     Current Instruction being executed
        /// </summary>
        public InstructionMetadata Instruction { get; set; }

        /// <summary>
        ///     Used to signal the CPU that an NMI has occurred
        /// </summary>
        public bool NMI { get; set; }

        /// <summary>
        ///     Stack Base Offset
        /// </summary>
        public const ushort STACK_BASE = 0x100;

        /// <summary>
        ///     Array containing all the instructions supported by the CPU with their metadata
        ///
        ///     The Instructions are stored by their OpCode byte as the array index
        /// </summary>
        private readonly InstructionMetadata[] _instructions = new InstructionMetadata[0x100];

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="memoryMapper">Cartridge Memory Mapper</param>
        /// <param name="controller">NES Controller (Player 1)</param>
        public Core(IMapper memoryMapper, IController controller = null)
        {
            Status = new CPUStatus();
            Instruction = new InstructionMetadata();
            CPUMemory = new Memory(memoryMapper, controller);

            //Setup CPU Instructions
            //We use the Instructions Array to contain Metadata for each instruction (# of Cycles, OpCode, Addressing Mode, etc), including the Method to call for execution
            //The Index in the array for the OpCode is the OpCode itself
            _instructions[0x69] = new InstructionMetadata { Opcode = Opcode.ADC, AddressingMode = AddressingMode.Immediate, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = ADC };
            _instructions[0x65] = new InstructionMetadata { Opcode = Opcode.ADC, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 3, OpCodeExecution = ADC };
            _instructions[0x75] = new InstructionMetadata { Opcode = Opcode.ADC, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 4, OpCodeExecution = ADC };
            _instructions[0x6D] = new InstructionMetadata { Opcode = Opcode.ADC, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = ADC };
            _instructions[0x7D] = new InstructionMetadata { Opcode = Opcode.ADC, AddressingMode = AddressingMode.AbsoluteX, PageBoundaryCheck = true, Length = 3, Cycles = 4, OpCodeExecution = ADC };
            _instructions[0x79] = new InstructionMetadata { Opcode = Opcode.ADC, AddressingMode = AddressingMode.AbsoluteY, PageBoundaryCheck = true, Length = 3, Cycles = 4, OpCodeExecution = ADC };
            _instructions[0x61] = new InstructionMetadata { Opcode = Opcode.ADC, AddressingMode = AddressingMode.IndexedIndirect, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = ADC };
            _instructions[0x71] = new InstructionMetadata { Opcode = Opcode.ADC, AddressingMode = AddressingMode.IndirectIndexed, PageBoundaryCheck = true, Length = 2, Cycles = 5, OpCodeExecution = ADC };
            _instructions[0x29] = new InstructionMetadata { Opcode = Opcode.AND, AddressingMode = AddressingMode.Immediate, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = AND };
            _instructions[0x25] = new InstructionMetadata { Opcode = Opcode.AND, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 3, OpCodeExecution = AND };
            _instructions[0x35] = new InstructionMetadata { Opcode = Opcode.AND, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 4, OpCodeExecution = AND };
            _instructions[0x2D] = new InstructionMetadata { Opcode = Opcode.AND, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = AND };
            _instructions[0x3D] = new InstructionMetadata { Opcode = Opcode.AND, AddressingMode = AddressingMode.AbsoluteX, PageBoundaryCheck = true, Length = 3, Cycles = 4, OpCodeExecution = AND };
            _instructions[0x39] = new InstructionMetadata { Opcode = Opcode.AND, AddressingMode = AddressingMode.AbsoluteY, PageBoundaryCheck = true, Length = 3, Cycles = 4, OpCodeExecution = AND };
            _instructions[0x21] = new InstructionMetadata { Opcode = Opcode.AND, AddressingMode = AddressingMode.IndexedIndirect, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = AND };
            _instructions[0x31] = new InstructionMetadata { Opcode = Opcode.AND, AddressingMode = AddressingMode.IndirectIndexed, PageBoundaryCheck = true, Length = 2, Cycles = 5, OpCodeExecution = AND };
            _instructions[0x0A] = new InstructionMetadata { Opcode = Opcode.ASL, AddressingMode = AddressingMode.Accumulator, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = ASL };
            _instructions[0x06] = new InstructionMetadata { Opcode = Opcode.ASL, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 5, OpCodeExecution = ASL };
            _instructions[0x16] = new InstructionMetadata { Opcode = Opcode.ASL, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = ASL };
            _instructions[0x0E] = new InstructionMetadata { Opcode = Opcode.ASL, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 6, OpCodeExecution = ASL };
            _instructions[0x1E] = new InstructionMetadata { Opcode = Opcode.ASL, AddressingMode = AddressingMode.AbsoluteX, PageBoundaryCheck = false, Length = 3, Cycles = 7, OpCodeExecution = ASL };
            _instructions[0x90] = new InstructionMetadata { Opcode = Opcode.BCC, AddressingMode = AddressingMode.Relative, PageBoundaryCheck = true, Length = 2, Cycles = 2, OpCodeExecution = BCC };
            _instructions[0xB0] = new InstructionMetadata { Opcode = Opcode.BCS, AddressingMode = AddressingMode.Relative, PageBoundaryCheck = true, Length = 2, Cycles = 2, OpCodeExecution = BCS };
            _instructions[0xF0] = new InstructionMetadata { Opcode = Opcode.BEQ, AddressingMode = AddressingMode.Relative, PageBoundaryCheck = true, Length = 2, Cycles = 2, OpCodeExecution = BEQ };
            _instructions[0x24] = new InstructionMetadata { Opcode = Opcode.BIT, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 3, OpCodeExecution = BIT };
            _instructions[0x2C] = new InstructionMetadata { Opcode = Opcode.BIT, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = BIT };
            _instructions[0x30] = new InstructionMetadata { Opcode = Opcode.BMI, AddressingMode = AddressingMode.Relative, PageBoundaryCheck = true, Length = 2, Cycles = 2, OpCodeExecution = BMI };
            _instructions[0xD0] = new InstructionMetadata { Opcode = Opcode.BNE, AddressingMode = AddressingMode.Relative, PageBoundaryCheck = true, Length = 2, Cycles = 2, OpCodeExecution = BNE };
            _instructions[0x10] = new InstructionMetadata { Opcode = Opcode.BPL, AddressingMode = AddressingMode.Relative, PageBoundaryCheck = true, Length = 2, Cycles = 2, OpCodeExecution = BPL };
            _instructions[0x00] = new InstructionMetadata { Opcode = Opcode.BRK, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 7, OpCodeExecution = BRK };
            _instructions[0x50] = new InstructionMetadata { Opcode = Opcode.BVC, AddressingMode = AddressingMode.Relative, PageBoundaryCheck = true, Length = 2, Cycles = 2, OpCodeExecution = BVC };
            _instructions[0x70] = new InstructionMetadata { Opcode = Opcode.BVS, AddressingMode = AddressingMode.Relative, PageBoundaryCheck = true, Length = 2, Cycles = 2, OpCodeExecution = BVS };
            _instructions[0x18] = new InstructionMetadata { Opcode = Opcode.CLC, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = CLC };
            _instructions[0xD8] = new InstructionMetadata { Opcode = Opcode.CLD, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = CLD };
            _instructions[0x58] = new InstructionMetadata { Opcode = Opcode.CLI, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = CLI };
            _instructions[0xB8] = new InstructionMetadata { Opcode = Opcode.CLV, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = CLV };
            _instructions[0xC9] = new InstructionMetadata { Opcode = Opcode.CMP, AddressingMode = AddressingMode.Immediate, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = CMP };
            _instructions[0xC5] = new InstructionMetadata { Opcode = Opcode.CMP, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 3, OpCodeExecution = CMP };
            _instructions[0xD5] = new InstructionMetadata { Opcode = Opcode.CMP, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 4, OpCodeExecution = CMP };
            _instructions[0xCD] = new InstructionMetadata { Opcode = Opcode.CMP, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = CMP };
            _instructions[0xDD] = new InstructionMetadata { Opcode = Opcode.CMP, AddressingMode = AddressingMode.AbsoluteX, PageBoundaryCheck = true, Length = 3, Cycles = 4, OpCodeExecution = CMP };
            _instructions[0xD9] = new InstructionMetadata { Opcode = Opcode.CMP, AddressingMode = AddressingMode.AbsoluteY, PageBoundaryCheck = true, Length = 3, Cycles = 4, OpCodeExecution = CMP };
            _instructions[0xC1] = new InstructionMetadata { Opcode = Opcode.CMP, AddressingMode = AddressingMode.IndexedIndirect, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = CMP };
            _instructions[0xD1] = new InstructionMetadata { Opcode = Opcode.CMP, AddressingMode = AddressingMode.IndirectIndexed, PageBoundaryCheck = true, Length = 2, Cycles = 5, OpCodeExecution = CMP };
            _instructions[0xE0] = new InstructionMetadata { Opcode = Opcode.CPX, AddressingMode = AddressingMode.Immediate, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = CPX };
            _instructions[0xE4] = new InstructionMetadata { Opcode = Opcode.CPX, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 3, OpCodeExecution = CPX };
            _instructions[0xEC] = new InstructionMetadata { Opcode = Opcode.CPX, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = CPX };
            _instructions[0xC0] = new InstructionMetadata { Opcode = Opcode.CPY, AddressingMode = AddressingMode.Immediate, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = CPY };
            _instructions[0xC4] = new InstructionMetadata { Opcode = Opcode.CPY, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 3, OpCodeExecution = CPY };
            _instructions[0xCC] = new InstructionMetadata { Opcode = Opcode.CPY, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = CPY };
            _instructions[0xC3] = new InstructionMetadata { Opcode = Opcode.DCP, AddressingMode = AddressingMode.IndexedIndirect, PageBoundaryCheck = false, Length = 2, Cycles = 8, OpCodeExecution = DCP };
            _instructions[0xC7] = new InstructionMetadata { Opcode = Opcode.DCP, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 5, OpCodeExecution = DCP };
            _instructions[0xCF] = new InstructionMetadata { Opcode = Opcode.DCP, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 8, OpCodeExecution = DCP };
            _instructions[0xD3] = new InstructionMetadata { Opcode = Opcode.DCP, AddressingMode = AddressingMode.IndirectIndexed, PageBoundaryCheck = false, Length = 2, Cycles = 8, OpCodeExecution = DCP };
            _instructions[0xD7] = new InstructionMetadata { Opcode = Opcode.DCP, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = DCP };
            _instructions[0xDB] = new InstructionMetadata { Opcode = Opcode.DCP, AddressingMode = AddressingMode.AbsoluteY, PageBoundaryCheck = false, Length = 3, Cycles = 7, OpCodeExecution = DCP };
            _instructions[0xDF] = new InstructionMetadata { Opcode = Opcode.DCP, AddressingMode = AddressingMode.AbsoluteX, PageBoundaryCheck = false, Length = 3, Cycles = 7, OpCodeExecution = DCP };
            _instructions[0xC6] = new InstructionMetadata { Opcode = Opcode.DEC, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 5, OpCodeExecution = DEC };
            _instructions[0xD6] = new InstructionMetadata { Opcode = Opcode.DEC, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = DEC };
            _instructions[0xCE] = new InstructionMetadata { Opcode = Opcode.DEC, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 6, OpCodeExecution = DEC };
            _instructions[0xDE] = new InstructionMetadata { Opcode = Opcode.DEC, AddressingMode = AddressingMode.AbsoluteX, PageBoundaryCheck = false, Length = 3, Cycles = 7, OpCodeExecution = DEC };
            _instructions[0xCA] = new InstructionMetadata { Opcode = Opcode.DEX, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = DEX };
            _instructions[0x88] = new InstructionMetadata { Opcode = Opcode.DEY, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = DEY };
            _instructions[0x49] = new InstructionMetadata { Opcode = Opcode.EOR, AddressingMode = AddressingMode.Immediate, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = EOR };
            _instructions[0x45] = new InstructionMetadata { Opcode = Opcode.EOR, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 3, OpCodeExecution = EOR };
            _instructions[0x55] = new InstructionMetadata { Opcode = Opcode.EOR, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 4, OpCodeExecution = EOR };
            _instructions[0x4D] = new InstructionMetadata { Opcode = Opcode.EOR, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = EOR };
            _instructions[0x5D] = new InstructionMetadata { Opcode = Opcode.EOR, AddressingMode = AddressingMode.AbsoluteX, PageBoundaryCheck = true, Length = 3, Cycles = 4, OpCodeExecution = EOR };
            _instructions[0x59] = new InstructionMetadata { Opcode = Opcode.EOR, AddressingMode = AddressingMode.AbsoluteY, PageBoundaryCheck = true, Length = 3, Cycles = 4, OpCodeExecution = EOR };
            _instructions[0x41] = new InstructionMetadata { Opcode = Opcode.EOR, AddressingMode = AddressingMode.IndexedIndirect, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = EOR };
            _instructions[0x51] = new InstructionMetadata { Opcode = Opcode.EOR, AddressingMode = AddressingMode.IndirectIndexed, PageBoundaryCheck = true, Length = 2, Cycles = 5, OpCodeExecution = EOR };
            _instructions[0xE6] = new InstructionMetadata { Opcode = Opcode.INC, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 5, OpCodeExecution = INC };
            _instructions[0xF6] = new InstructionMetadata { Opcode = Opcode.INC, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = INC };
            _instructions[0xEE] = new InstructionMetadata { Opcode = Opcode.INC, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 6, OpCodeExecution = INC };
            _instructions[0xFE] = new InstructionMetadata { Opcode = Opcode.INC, AddressingMode = AddressingMode.AbsoluteX, PageBoundaryCheck = false, Length = 3, Cycles = 7, OpCodeExecution = INC };
            _instructions[0xE8] = new InstructionMetadata { Opcode = Opcode.INX, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = INX };
            _instructions[0xC8] = new InstructionMetadata { Opcode = Opcode.INY, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = INY };
            _instructions[0xE3] = new InstructionMetadata { Opcode = Opcode.ISB, AddressingMode = AddressingMode.IndexedIndirect, PageBoundaryCheck = false, Length = 2, Cycles = 8, OpCodeExecution = ISB };
            _instructions[0xE7] = new InstructionMetadata { Opcode = Opcode.ISB, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 5, OpCodeExecution = ISB };
            _instructions[0xEF] = new InstructionMetadata { Opcode = Opcode.ISB, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 6, OpCodeExecution = ISB };
            _instructions[0xF3] = new InstructionMetadata { Opcode = Opcode.ISB, AddressingMode = AddressingMode.IndirectIndexed, PageBoundaryCheck = false, Length = 2, Cycles = 8, OpCodeExecution = ISB };
            _instructions[0xF7] = new InstructionMetadata { Opcode = Opcode.ISB, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = ISB };
            _instructions[0xFB] = new InstructionMetadata { Opcode = Opcode.ISB, AddressingMode = AddressingMode.AbsoluteY, PageBoundaryCheck = false, Length = 3, Cycles = 7, OpCodeExecution = ISB };
            _instructions[0xFF] = new InstructionMetadata { Opcode = Opcode.ISB, AddressingMode = AddressingMode.AbsoluteX, PageBoundaryCheck = false, Length = 3, Cycles = 7, OpCodeExecution = ISB };
            _instructions[0x4C] = new InstructionMetadata { Opcode = Opcode.JMP, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 0, Cycles = 3, OpCodeExecution = JMP };
            _instructions[0x6C] = new InstructionMetadata { Opcode = Opcode.JMP, AddressingMode = AddressingMode.Indirect, PageBoundaryCheck = false, Length = 0, Cycles = 5, OpCodeExecution = JMP };
            _instructions[0x20] = new InstructionMetadata { Opcode = Opcode.JSR, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 0, Cycles = 6, OpCodeExecution = JSR };
            _instructions[0xA3] = new InstructionMetadata { Opcode = Opcode.LAX, AddressingMode = AddressingMode.IndexedIndirect, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = LAX };
            _instructions[0xA7] = new InstructionMetadata { Opcode = Opcode.LAX, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 3, OpCodeExecution = LAX };
            _instructions[0xAF] = new InstructionMetadata { Opcode = Opcode.LAX, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = LAX };
            _instructions[0xB3] = new InstructionMetadata { Opcode = Opcode.LAX, AddressingMode = AddressingMode.IndirectIndexed, PageBoundaryCheck = false, Length = 2, Cycles = 5, OpCodeExecution = LAX };
            _instructions[0xB7] = new InstructionMetadata { Opcode = Opcode.LAX, AddressingMode = AddressingMode.ZeroPageY, PageBoundaryCheck = false, Length = 2, Cycles = 5, OpCodeExecution = LAX };
            _instructions[0xBF] = new InstructionMetadata { Opcode = Opcode.LAX, AddressingMode = AddressingMode.AbsoluteY, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = LAX };
            _instructions[0xA9] = new InstructionMetadata { Opcode = Opcode.LDA, AddressingMode = AddressingMode.Immediate, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = LDA };
            _instructions[0xA5] = new InstructionMetadata { Opcode = Opcode.LDA, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 3, OpCodeExecution = LDA };
            _instructions[0xB5] = new InstructionMetadata { Opcode = Opcode.LDA, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 4, OpCodeExecution = LDA };
            _instructions[0xAD] = new InstructionMetadata { Opcode = Opcode.LDA, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = LDA };
            _instructions[0xBD] = new InstructionMetadata { Opcode = Opcode.LDA, AddressingMode = AddressingMode.AbsoluteX, PageBoundaryCheck = true, Length = 3, Cycles = 4, OpCodeExecution = LDA };
            _instructions[0xB9] = new InstructionMetadata { Opcode = Opcode.LDA, AddressingMode = AddressingMode.AbsoluteY, PageBoundaryCheck = true, Length = 3, Cycles = 4, OpCodeExecution = LDA };
            _instructions[0xA1] = new InstructionMetadata { Opcode = Opcode.LDA, AddressingMode = AddressingMode.IndexedIndirect, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = LDA };
            _instructions[0xB1] = new InstructionMetadata { Opcode = Opcode.LDA, AddressingMode = AddressingMode.IndirectIndexed, PageBoundaryCheck = true, Length = 2, Cycles = 5, OpCodeExecution = LDA };
            _instructions[0xA2] = new InstructionMetadata { Opcode = Opcode.LDX, AddressingMode = AddressingMode.Immediate, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = LDX };
            _instructions[0xA6] = new InstructionMetadata { Opcode = Opcode.LDX, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 3, OpCodeExecution = LDX };
            _instructions[0xB6] = new InstructionMetadata { Opcode = Opcode.LDX, AddressingMode = AddressingMode.ZeroPageY, PageBoundaryCheck = false, Length = 2, Cycles = 4, OpCodeExecution = LDX };
            _instructions[0xAE] = new InstructionMetadata { Opcode = Opcode.LDX, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = LDX };
            _instructions[0xBE] = new InstructionMetadata { Opcode = Opcode.LDX, AddressingMode = AddressingMode.AbsoluteY, PageBoundaryCheck = true, Length = 3, Cycles = 4, OpCodeExecution = LDX };
            _instructions[0xA0] = new InstructionMetadata { Opcode = Opcode.LDY, AddressingMode = AddressingMode.Immediate, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = LDY };
            _instructions[0xA4] = new InstructionMetadata { Opcode = Opcode.LDY, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 3, OpCodeExecution = LDY };
            _instructions[0xB4] = new InstructionMetadata { Opcode = Opcode.LDY, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 4, OpCodeExecution = LDY };
            _instructions[0xAC] = new InstructionMetadata { Opcode = Opcode.LDY, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = LDY };
            _instructions[0xBC] = new InstructionMetadata { Opcode = Opcode.LDY, AddressingMode = AddressingMode.AbsoluteX, PageBoundaryCheck = true, Length = 3, Cycles = 4, OpCodeExecution = LDY };
            _instructions[0x4A] = new InstructionMetadata { Opcode = Opcode.LSR, AddressingMode = AddressingMode.Accumulator, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = LSR };
            _instructions[0x46] = new InstructionMetadata { Opcode = Opcode.LSR, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 5, OpCodeExecution = LSR };
            _instructions[0x56] = new InstructionMetadata { Opcode = Opcode.LSR, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = LSR };
            _instructions[0x4E] = new InstructionMetadata { Opcode = Opcode.LSR, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 6, OpCodeExecution = LSR };
            _instructions[0x5E] = new InstructionMetadata { Opcode = Opcode.LSR, AddressingMode = AddressingMode.AbsoluteX, PageBoundaryCheck = false, Length = 3, Cycles = 7, OpCodeExecution = LSR };
            _instructions[0xEA] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = () => { } };
            _instructions[0x1A] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = () => { } };
            _instructions[0x3A] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = () => { } };
            _instructions[0x5A] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = () => { } };
            _instructions[0x7A] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = () => { } };
            _instructions[0xDA] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = () => { } };
            _instructions[0xFA] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = () => { } };
            _instructions[0x04] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = () => { } };
            _instructions[0x44] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = () => { } };
            _instructions[0x64] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = () => { } };
            _instructions[0x14] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = () => { } };
            _instructions[0x34] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = () => { } };
            _instructions[0x54] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = () => { } };
            _instructions[0x74] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = () => { } };
            _instructions[0xD4] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = () => { } };
            _instructions[0xF4] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = () => { } };
            _instructions[0x80] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = () => { } };
            _instructions[0x82] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = () => { } };
            _instructions[0x89] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = () => { } };
            _instructions[0xC2] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = () => { } };
            _instructions[0xE2] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = () => { } };
            _instructions[0x0C] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = () => { } };
            _instructions[0x1C] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = () => { } };
            _instructions[0x3C] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = () => { } };
            _instructions[0x5C] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = () => { } };
            _instructions[0x7C] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = () => { } };
            _instructions[0xDC] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = () => { } };
            _instructions[0xFC] = new InstructionMetadata { Opcode = Opcode.NOP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = () => { } };
            _instructions[0x09] = new InstructionMetadata { Opcode = Opcode.ORA, AddressingMode = AddressingMode.Immediate, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = ORA };
            _instructions[0x05] = new InstructionMetadata { Opcode = Opcode.ORA, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 3, OpCodeExecution = ORA };
            _instructions[0x15] = new InstructionMetadata { Opcode = Opcode.ORA, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 4, OpCodeExecution = ORA };
            _instructions[0x0D] = new InstructionMetadata { Opcode = Opcode.ORA, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = ORA };
            _instructions[0x1D] = new InstructionMetadata { Opcode = Opcode.ORA, AddressingMode = AddressingMode.AbsoluteX, PageBoundaryCheck = true, Length = 3, Cycles = 4, OpCodeExecution = ORA };
            _instructions[0x19] = new InstructionMetadata { Opcode = Opcode.ORA, AddressingMode = AddressingMode.AbsoluteY, PageBoundaryCheck = true, Length = 3, Cycles = 4, OpCodeExecution = ORA };
            _instructions[0x01] = new InstructionMetadata { Opcode = Opcode.ORA, AddressingMode = AddressingMode.IndexedIndirect, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = ORA };
            _instructions[0x11] = new InstructionMetadata { Opcode = Opcode.ORA, AddressingMode = AddressingMode.IndirectIndexed, PageBoundaryCheck = true, Length = 2, Cycles = 5, OpCodeExecution = ORA };
            _instructions[0x48] = new InstructionMetadata { Opcode = Opcode.PHA, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 3, OpCodeExecution = PHA };
            _instructions[0x08] = new InstructionMetadata { Opcode = Opcode.PHP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 3, OpCodeExecution = PHP };
            _instructions[0x68] = new InstructionMetadata { Opcode = Opcode.PLA, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 4, OpCodeExecution = PLA };
            _instructions[0x28] = new InstructionMetadata { Opcode = Opcode.PLP, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 4, OpCodeExecution = PLP };
            _instructions[0x23] = new InstructionMetadata { Opcode = Opcode.RLA, AddressingMode = AddressingMode.IndexedIndirect, PageBoundaryCheck = false, Length = 2, Cycles = 8, OpCodeExecution = RLA };
            _instructions[0x27] = new InstructionMetadata { Opcode = Opcode.RLA, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 5, OpCodeExecution = RLA };
            _instructions[0x2F] = new InstructionMetadata { Opcode = Opcode.RLA, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 6, OpCodeExecution = RLA };
            _instructions[0x33] = new InstructionMetadata { Opcode = Opcode.RLA, AddressingMode = AddressingMode.IndirectIndexed, PageBoundaryCheck = false, Length = 2, Cycles = 8, OpCodeExecution = RLA };
            _instructions[0x37] = new InstructionMetadata { Opcode = Opcode.RLA, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = RLA };
            _instructions[0x3B] = new InstructionMetadata { Opcode = Opcode.RLA, AddressingMode = AddressingMode.AbsoluteY, PageBoundaryCheck = false, Length = 3, Cycles = 7, OpCodeExecution = RLA };
            _instructions[0x3F] = new InstructionMetadata { Opcode = Opcode.RLA, AddressingMode = AddressingMode.AbsoluteX, PageBoundaryCheck = false, Length = 3, Cycles = 7, OpCodeExecution = RLA };
            _instructions[0x2A] = new InstructionMetadata { Opcode = Opcode.ROL, AddressingMode = AddressingMode.Accumulator, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = ROL };
            _instructions[0x26] = new InstructionMetadata { Opcode = Opcode.ROL, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 5, OpCodeExecution = ROL };
            _instructions[0x36] = new InstructionMetadata { Opcode = Opcode.ROL, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = ROL };
            _instructions[0x2E] = new InstructionMetadata { Opcode = Opcode.ROL, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 6, OpCodeExecution = ROL };
            _instructions[0x3E] = new InstructionMetadata { Opcode = Opcode.ROL, AddressingMode = AddressingMode.AbsoluteX, PageBoundaryCheck = false, Length = 3, Cycles = 7, OpCodeExecution = ROL };
            _instructions[0x6A] = new InstructionMetadata { Opcode = Opcode.ROR, AddressingMode = AddressingMode.Accumulator, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = ROR };
            _instructions[0x66] = new InstructionMetadata { Opcode = Opcode.ROR, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 5, OpCodeExecution = ROR };
            _instructions[0x76] = new InstructionMetadata { Opcode = Opcode.ROR, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = ROR };
            _instructions[0x6E] = new InstructionMetadata { Opcode = Opcode.ROR, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 6, OpCodeExecution = ROR };
            _instructions[0x7E] = new InstructionMetadata { Opcode = Opcode.ROR, AddressingMode = AddressingMode.AbsoluteX, PageBoundaryCheck = false, Length = 3, Cycles = 7, OpCodeExecution = ROR };
            _instructions[0x63] = new InstructionMetadata { Opcode = Opcode.RRA, AddressingMode = AddressingMode.IndexedIndirect, PageBoundaryCheck = false, Length = 2, Cycles = 8, OpCodeExecution = RRA };
            _instructions[0x67] = new InstructionMetadata { Opcode = Opcode.RRA, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 5, OpCodeExecution = RRA };
            _instructions[0x6F] = new InstructionMetadata { Opcode = Opcode.RRA, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 6, OpCodeExecution = RRA };
            _instructions[0x73] = new InstructionMetadata { Opcode = Opcode.RRA, AddressingMode = AddressingMode.IndirectIndexed, PageBoundaryCheck = false, Length = 2, Cycles = 8, OpCodeExecution = RRA };
            _instructions[0x77] = new InstructionMetadata { Opcode = Opcode.RRA, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = RRA };
            _instructions[0x7B] = new InstructionMetadata { Opcode = Opcode.RRA, AddressingMode = AddressingMode.AbsoluteY, PageBoundaryCheck = false, Length = 3, Cycles = 7, OpCodeExecution = RRA };
            _instructions[0x7F] = new InstructionMetadata { Opcode = Opcode.RRA, AddressingMode = AddressingMode.AbsoluteX, PageBoundaryCheck = false, Length = 3, Cycles = 7, OpCodeExecution = RRA };
            _instructions[0x40] = new InstructionMetadata { Opcode = Opcode.RTI, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 0, Cycles = 6, OpCodeExecution = RTI };
            _instructions[0x60] = new InstructionMetadata { Opcode = Opcode.RTS, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 0, Cycles = 6, OpCodeExecution = RTS };
            _instructions[0x83] = new InstructionMetadata { Opcode = Opcode.SAX, AddressingMode = AddressingMode.IndexedIndirect, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = SAX };
            _instructions[0x87] = new InstructionMetadata { Opcode = Opcode.SAX, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 3, OpCodeExecution = SAX };
            _instructions[0x8F] = new InstructionMetadata { Opcode = Opcode.SAX, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = SAX };
            _instructions[0x97] = new InstructionMetadata { Opcode = Opcode.SAX, AddressingMode = AddressingMode.ZeroPageY, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = SAX };
            _instructions[0xE9] = new InstructionMetadata { Opcode = Opcode.SBC, AddressingMode = AddressingMode.Immediate, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = SBC };
            _instructions[0xEB] = new InstructionMetadata { Opcode = Opcode.SBC, AddressingMode = AddressingMode.Immediate, PageBoundaryCheck = false, Length = 2, Cycles = 2, OpCodeExecution = SBC };
            _instructions[0xE5] = new InstructionMetadata { Opcode = Opcode.SBC, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 3, OpCodeExecution = SBC };
            _instructions[0xF5] = new InstructionMetadata { Opcode = Opcode.SBC, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 4, OpCodeExecution = SBC };
            _instructions[0xED] = new InstructionMetadata { Opcode = Opcode.SBC, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = SBC };
            _instructions[0xFD] = new InstructionMetadata { Opcode = Opcode.SBC, AddressingMode = AddressingMode.AbsoluteX, PageBoundaryCheck = true, Length = 3, Cycles = 4, OpCodeExecution = SBC };
            _instructions[0xF9] = new InstructionMetadata { Opcode = Opcode.SBC, AddressingMode = AddressingMode.AbsoluteY, PageBoundaryCheck = true, Length = 3, Cycles = 4, OpCodeExecution = SBC };
            _instructions[0xE1] = new InstructionMetadata { Opcode = Opcode.SBC, AddressingMode = AddressingMode.IndexedIndirect, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = SBC };
            _instructions[0xF1] = new InstructionMetadata { Opcode = Opcode.SBC, AddressingMode = AddressingMode.IndirectIndexed, PageBoundaryCheck = true, Length = 2, Cycles = 5, OpCodeExecution = SBC };
            _instructions[0x38] = new InstructionMetadata { Opcode = Opcode.SEC, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = SEC };
            _instructions[0xF8] = new InstructionMetadata { Opcode = Opcode.SED, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = SED };
            _instructions[0x78] = new InstructionMetadata { Opcode = Opcode.SEI, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = SEI };
            _instructions[0x03] = new InstructionMetadata { Opcode = Opcode.SLO, AddressingMode = AddressingMode.IndexedIndirect, PageBoundaryCheck = false, Length = 2, Cycles = 8, OpCodeExecution = SLO };
            _instructions[0x07] = new InstructionMetadata { Opcode = Opcode.SLO, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 5, OpCodeExecution = SLO };
            _instructions[0x0F] = new InstructionMetadata { Opcode = Opcode.SLO, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 6, OpCodeExecution = SLO };
            _instructions[0x13] = new InstructionMetadata { Opcode = Opcode.SLO, AddressingMode = AddressingMode.IndirectIndexed, PageBoundaryCheck = false, Length = 2, Cycles = 8, OpCodeExecution = SLO };
            _instructions[0x17] = new InstructionMetadata { Opcode = Opcode.SLO, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = SLO };
            _instructions[0x1B] = new InstructionMetadata { Opcode = Opcode.SLO, AddressingMode = AddressingMode.AbsoluteY, PageBoundaryCheck = false, Length = 3, Cycles = 7, OpCodeExecution = SLO };
            _instructions[0x1F] = new InstructionMetadata { Opcode = Opcode.SLO, AddressingMode = AddressingMode.AbsoluteX, PageBoundaryCheck = false, Length = 3, Cycles = 7, OpCodeExecution = SLO };
            _instructions[0x43] = new InstructionMetadata { Opcode = Opcode.SRE, AddressingMode = AddressingMode.IndexedIndirect, PageBoundaryCheck = false, Length = 2, Cycles = 8, OpCodeExecution = SRE };
            _instructions[0x47] = new InstructionMetadata { Opcode = Opcode.SRE, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 5, OpCodeExecution = SRE };
            _instructions[0x4F] = new InstructionMetadata { Opcode = Opcode.SRE, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 6, OpCodeExecution = SRE };
            _instructions[0x53] = new InstructionMetadata { Opcode = Opcode.SRE, AddressingMode = AddressingMode.IndirectIndexed, PageBoundaryCheck = false, Length = 2, Cycles = 8, OpCodeExecution = SRE };
            _instructions[0x57] = new InstructionMetadata { Opcode = Opcode.SRE, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = SRE };
            _instructions[0x5B] = new InstructionMetadata { Opcode = Opcode.SRE, AddressingMode = AddressingMode.AbsoluteY, PageBoundaryCheck = false, Length = 3, Cycles = 7, OpCodeExecution = SRE };
            _instructions[0x5F] = new InstructionMetadata { Opcode = Opcode.SRE, AddressingMode = AddressingMode.AbsoluteX, PageBoundaryCheck = false, Length = 3, Cycles = 7, OpCodeExecution = SRE };
            _instructions[0x85] = new InstructionMetadata { Opcode = Opcode.STA, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 3, OpCodeExecution = STA };
            _instructions[0x95] = new InstructionMetadata { Opcode = Opcode.STA, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 4, OpCodeExecution = STA };
            _instructions[0x8D] = new InstructionMetadata { Opcode = Opcode.STA, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = STA };
            _instructions[0x9D] = new InstructionMetadata { Opcode = Opcode.STA, AddressingMode = AddressingMode.AbsoluteX, PageBoundaryCheck = false, Length = 3, Cycles = 5, OpCodeExecution = STA };
            _instructions[0x99] = new InstructionMetadata { Opcode = Opcode.STA, AddressingMode = AddressingMode.AbsoluteY, PageBoundaryCheck = false, Length = 3, Cycles = 5, OpCodeExecution = STA };
            _instructions[0x81] = new InstructionMetadata { Opcode = Opcode.STA, AddressingMode = AddressingMode.IndexedIndirect, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = STA };
            _instructions[0x91] = new InstructionMetadata { Opcode = Opcode.STA, AddressingMode = AddressingMode.IndirectIndexed, PageBoundaryCheck = false, Length = 2, Cycles = 6, OpCodeExecution = STA };
            _instructions[0x86] = new InstructionMetadata { Opcode = Opcode.STX, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 3, OpCodeExecution = STX };
            _instructions[0x96] = new InstructionMetadata { Opcode = Opcode.STX, AddressingMode = AddressingMode.ZeroPageY, PageBoundaryCheck = false, Length = 2, Cycles = 4, OpCodeExecution = STX };
            _instructions[0x8E] = new InstructionMetadata { Opcode = Opcode.STX, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = STX };
            _instructions[0x84] = new InstructionMetadata { Opcode = Opcode.STY, AddressingMode = AddressingMode.ZeroPage, PageBoundaryCheck = false, Length = 2, Cycles = 3, OpCodeExecution = STY };
            _instructions[0x94] = new InstructionMetadata { Opcode = Opcode.STY, AddressingMode = AddressingMode.ZeroPageX, PageBoundaryCheck = false, Length = 2, Cycles = 4, OpCodeExecution = STY };
            _instructions[0x8C] = new InstructionMetadata { Opcode = Opcode.STY, AddressingMode = AddressingMode.Absolute, PageBoundaryCheck = false, Length = 3, Cycles = 4, OpCodeExecution = STY };
            _instructions[0xAA] = new InstructionMetadata { Opcode = Opcode.TAX, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = TAX };
            _instructions[0xA8] = new InstructionMetadata { Opcode = Opcode.TAY, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = TAY };
            _instructions[0xBA] = new InstructionMetadata { Opcode = Opcode.TSX, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = TSX };
            _instructions[0x8A] = new InstructionMetadata { Opcode = Opcode.TXA, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = TXA };
            _instructions[0x9A] = new InstructionMetadata { Opcode = Opcode.TXS, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = TXS };
            _instructions[0x98] = new InstructionMetadata { Opcode = Opcode.TYA, AddressingMode = AddressingMode.Implicit, PageBoundaryCheck = false, Length = 1, Cycles = 2, OpCodeExecution = TYA };

            //Reset Internal Counters and Values
            Reset();
        }

        //Resets the CPU to a startup state
        //Requires the ROM to be loaded into memory first
        public void Reset()
        {
            //Set Startup States for Registers
            SP = 0xFD;
            PC = 0xC000; //This is for unit testing
            A = 0;
            X = 0;
            Y = 0;
            Status.FromByte(0x24);
            Cycles = 0;

            //Zero out memory
            for (int i = 0; i < 0x2000; i++)
            {
                CPUMemory.WriteByte(i, 0x0);
            }

            //We check if the loaded ROM has starting address
            var newProgramCounter = BitConverter.ToUInt16([CPUMemory.ReadByte(0xFFFC), CPUMemory.ReadByte(0xFFFD)], 0);
            if (newProgramCounter != 0)
                PC = newProgramCounter;
        }


        /// <summary>
        ///     Ticks the CPU for the specified number of instructions
        /// </summary>
        /// <param name="count"></param>
        public int Tick(int count)
        {
            var totalTicks = 0;
            for (var i = 0; i < count; i++)
                totalTicks += Tick();

            return totalTicks;
        }

        /// <summary>
        ///     Ticks the CPU one instruction
        /// </summary>
        public int Tick()
        {
            //Check for NMI Interrupt
            if (NMI)
            {
                Push((ushort)PC);
                Push(Status.ToByte());
                PC = BitConverter.ToUInt16([CPUMemory.ReadByte(0xFFFA), CPUMemory.ReadByte(0xFFFB)], 0);
                Status.InterruptDisable = true;
                NMI = false;
            }

            //Decode
            Instruction = _instructions[CPUMemory.ReadByte(PC)];

            //Execute
            Instruction.OpCodeExecution.Invoke();

            //Increment the things
            PC += Instruction.Length;
            Cycles += Instruction.Cycles;

            return Instruction.Cycles;
        }

        /// <summary>
        ///     ADC - Add with Carry
        /// 
        ///     A,Z,C,N = A+M+C
        /// 
        ///     This instruction adds the contents of a memory location to the accumulator together with the carry bit.
        ///     If overflow occurs the carry bit is set, this enables multiple byte addition to be performed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ADC()
        {
            var value = Instruction.AddressingMode == AddressingMode.Immediate ? GetOperandByte() : CPUMemory.ReadByte(ResolveAddress());

            unchecked
            {
                var newA = (sbyte)A + (sbyte)value + (Status.Carry ? 1 : 0);
                Status.Zero = (byte)newA == 0;
                Status.Negative = ((byte)newA).IsNegative();
                Status.Carry = A + value + (Status.Carry ? 1 : 0) > byte.MaxValue;
                Status.Overflow = newA > 127 || newA < -128;
                A = (byte)newA;
            }
        }

        /// <summary>
        ///     AND - Logical AND
        /// 
        ///     A,Z,N = A &amp; M
        /// 
        ///     A logical AND is performed, bit by bit, on the accumulator contents using the contents of a byte of memory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void AND()
        {
            A &= Instruction.AddressingMode == AddressingMode.Immediate ? GetOperandByte() : CPUMemory.ReadByte(ResolveAddress());
            Status.Negative = A.IsNegative();
            Status.Zero = A == 0;
        }

        /// <summary>
        ///     ASL - Arithmetic Shift Left
        ///
        ///     A,Z,C,N = M*2 or M, Z, C, N = M * 2
        /// 
        ///     This operation shifts all the bits of the accumulator or memory contents one bit left.Bit 0 is set to 0 and bit 7 is placed in the
        ///     carry flag. The effect of this operation is to multiply the memory contents by 2 (ignoring 2's complement considerations), setting
        ///     the carry if the result will not fit in 8 bits.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ASL()
        {
            var address = ResolveAddress();
            var value = Instruction.AddressingMode == AddressingMode.Accumulator ? A : CPUMemory.ReadByte(address);

            Status.Carry = value >> 7 == 1;

            //Shift Left by 1
            value <<= 1;

            if (Instruction.AddressingMode != AddressingMode.Accumulator)
            {
                CPUMemory.WriteByte(address, value);
            }
            else
            {
                A = value;
            }

            Status.Zero = value == 0;
            Status.Negative = value.IsNegative();
        }

        /// <summary>
        ///     BCC - Branch if Carry Clear
        /// 
        ///     If the carry flag is clear then add the relative displacement to the program counter to cause a branch to a new location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void BCC()
        {
            if (Status.Carry)
                return;

            //+1 cycle for success
            Cycles++;

            PC = ResolveAddress();
        }

        /// <summary>
        ///     BCS - Branch if Carry Set
        ///     
        ///     If the carry flag is set then add the relative displacement to the program counter to cause a branch to a new location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void BCS()
        {
            if (!Status.Carry)
                return;

            //+1 cycle for success
            Cycles++;

            PC = ResolveAddress();
        }

        /// <summary>
        ///     BEQ - Branch if Equal
        /// 
        ///     If the zero flag is set then add the relative displacement to the program counter to cause a branch to a new location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void BEQ()
        {
            if (!Status.Zero)
                return;

            //+1 cycle for success
            Cycles++;

            PC = ResolveAddress();
        }

        /// <summary>
        ///     BIT - Bit Test
        /// 
        ///     A &amp; M, N = M7, V = M6
        ///     
        ///     This instructions is used to test if one or more bits are set in a target memory location.The mask pattern in A is ANDed with the 
        ///     value in memory to set or clear the zero flag, but the result is not kept. Bits 7 and 6 of the value from memory are copied 
        ///     into the N and V flags.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void BIT()
        {
            var value = CPUMemory.ReadByte(ResolveAddress());

            Status.Negative = value.IsNegative();
            Status.Overflow = (value & (1 << 6)) != 0;
            Status.Zero = (A & value) == 0;
        }

        /// <summary>
        ///     BMI - Branch if Minus
        /// 
        ///     If the negative flag is set then add the relative displacement to the program counter to cause a branch to a new location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void BMI()
        {
            if (!Status.Negative)
                return;

            //+1 cycle for success
            Cycles++;

            PC = ResolveAddress();
        }

        /// <summary>
        ///     BNE - Branch if Not Equal
        ///     
        ///     If the zero flag is clear then add the relative displacement to the program counter to cause a branch to a new location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void BNE()
        {
            if (Status.Zero)
                return;

            //+1 cycle for success
            Cycles++;

            PC = ResolveAddress(); 
        }

        /// <summary>
        ///     BPL - Branch if Positive
        /// 
        ///     If the negative flag is clear then add the relative displacement to the program counter to cause a branch to a new location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void BPL()
        {
            if (Status.Negative)
                return;

            //+1 cycle for success
            Cycles++;

            PC = ResolveAddress();
        }

        /// <summary>
        ///     BRK - Force Interrupt
        /// 
        ///     The BRK instruction forces the generation of an interrupt request. The program counter and processor status are pushed 
        ///     on the stack then the IRQ interrupt vector at $FFFE/F is loaded into the PC and the break flag in the status set to one.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void BRK()
        {
            Push((ushort)PC);
            Push((byte) (Status.ToByte() | 0b00010000));
            Status.InterruptDisable = true;
            PC = GetWord(0xFFFE);
        }

        /// <summary>
        ///     BVC - Branch if Overflow Clear
        /// 
        ///     If the overflow flag is clear then add the relative displacement to the program counter to cause a branch to a new 
        ///     location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void BVC()
        {
            if (Status.Overflow)
                return;

            //+1 cycle for success
            Cycles++;

            PC = ResolveAddress();
        }

        /// <summary>
        ///     BVS - Branch if Overflow Set
        ///     
        ///     If the overflow flag is set then add the relative displacement to the program counter to cause a branch to a new 
        ///     location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void BVS()
        {
            if (!Status.Overflow)
                return;

            //+1 cycle for success
            Cycles++;

            PC = ResolveAddress();
        }

        /// <summary>
        ///     CLC - Clear Carry Flag
        /// 
        ///     C = 0
        /// 
        ///     Set the carry flag to zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void CLC() => Status.Carry = false;

        /// <summary>
        ///     CLD - Clear Decimal Flag
        /// 
        ///     D = 0
        /// 
        ///     Set the decimal flag to zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void CLD() => Status.DecimalMode = false;

        /// <summary>
        ///     CLI - Clear Interrupt Flag
        /// 
        ///     I = 0
        /// 
        ///     Set the Interrupt flag to zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void CLI() => Status.InterruptDisable = false;

        /// <summary>
        ///     CLV - Clear Overflow Flag
        /// 
        ///     V = 0
        /// 
        ///     Set the Overflow flag to zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void CLV() => Status.Overflow = false;

        /// <summary>
        ///     CMP - Compare
        /// 
        ///     Z,C,N = A-M
        /// 
        ///     This instruction compares the contents of the accumulator with another memory held value and sets the zero and 
        ///     carry flags as appropriate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void CMP()
        {
            var value = Instruction.AddressingMode == AddressingMode.Immediate ? GetOperandByte() : CPUMemory.ReadByte(ResolveAddress());

            Status.Carry = (A >= value);
            Status.Zero = (A == value);
            unchecked
            {
                Status.Negative = ((byte)(A - value)).IsNegative();
            }   
        }

        /// <summary>
        ///     CPX - Compare X Register
        /// 
        ///     Z,C,N = X-M
        /// 
        ///     This instruction compares the contents of the X register with another memory held value and sets the zero and carry flags as appropriate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void CPX()
        {
            var value = Instruction.AddressingMode == AddressingMode.Immediate ? GetOperandByte() : CPUMemory.ReadByte(ResolveAddress());

            Status.Carry = X >= value;
            Status.Zero = X == value;
            unchecked
            {
                Status.Negative = ((byte) (X - value)).IsNegative();
            }
        }

        /// <summary>
        ///     CPY - Compare Y Register
        /// 
        ///     Z,C,N = Y-M
        /// 
        ///     This instruction compares the contents of the Y register with another memory held value and sets the zero and carry flags as appropriate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void CPY()
        {
            var value = Instruction.AddressingMode == AddressingMode.Immediate ? GetOperandByte() : CPUMemory.ReadByte(ResolveAddress());

            Status.Carry = Y >= value;
            Status.Zero = Y == value;
            unchecked
            {
                Status.Negative = ((byte)(Y - value)).IsNegative();
            }
        }

        /// <summary>
        ///     DCP - Undocumented OpCode
        ///
        ///     The read-modify-write instructions (INC, DEC, ASL, LSR, ROL, ROR) have few valid addressing modes, but these instructions have three more: (d,X),
        ///     (d),Y, and a,Y. In some cases, it could be worth it to use these and ignore the side effect on the accumulator.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void DCP()
        {
            DEC();
            CMP();
        }

        /// <summary>
        ///     DEC - Decrement Memory
        /// 
        ///     M,Z,N = M-1
        /// 
        ///     Subtracts one from the value held at a specified memory location setting the zero and negative flags as appropriate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void DEC()
        {
            var address = ResolveAddress();
            var value = Instruction.AddressingMode == AddressingMode.Immediate ? GetOperandByte() : CPUMemory.ReadByte(address);

            unchecked
            {
                value--;
            }

            Status.Zero = value == 0;
            Status.Negative = value.IsNegative();
            CPUMemory.WriteByte(address, value);
        }

        /// <summary>
        ///     DEX - Decrement X Register
        /// 
        ///     X,Z,N = X-1
        /// 
        ///     Subtracts one from the X register setting the zero and negative flags as appropriate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void DEX()
        {
            unchecked
            {
                X--;
            }

            Status.Zero = X == 0;
            Status.Negative = X.IsNegative();
        }

        /// <summary>
        ///     DEY - Decrement Y Register
        /// 
        ///     Y,Z,N = Y-1
        /// 
        ///     Subtracts one from the Y register setting the zero and negative flags as appropriate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void DEY()
        {
            unchecked
            {
                Y--;
            }

            Status.Zero = Y == 0;
            Status.Negative = Y.IsNegative();
        }

        /// <summary>
        ///     EOR - Exclusive OR
        /// 
        ///     A,Z,N = A^M
        /// 
        ///     An exclusive OR is performed, bit by bit, on the accumulator contents using the contents of a byte of memory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void EOR()
        {
            A ^= Instruction.AddressingMode == AddressingMode.Immediate ? GetOperandByte() : CPUMemory.ReadByte(ResolveAddress());
            Status.Zero = A == 0;
            Status.Negative = A.IsNegative();
        }

        /// <summary>
        ///     INC - Increment Memory
        /// 
        ///     M,Z,N = M+1
        /// 
        ///     Adds one to the value held at a specified memory location setting the zero and negative flags as appropriate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void INC()
        {
            var address = ResolveAddress();
            var value = Instruction.AddressingMode == AddressingMode.Immediate ? GetOperandByte() : CPUMemory.ReadByte(address);

            unchecked
            {
                value++;
            }

            Status.Zero = value == 0;
            Status.Negative = value.IsNegative();
            CPUMemory.WriteByte(address, value);
        }

        /// <summary>
        ///     INX - Increment X Register
        ///     
        ///     X,Z,N = X+1
        /// 
        ///     Adds one to the X register setting the zero and negative flags as appropriate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void INX()
        {
            unchecked
            {
                X++;
            }

            Status.Zero = X == 0;
            Status.Negative = X.IsNegative();
        }

        /// <summary>
        ///     INY - Increment Y Register
        /// 
        ///      Y,Z,N = Y+1
        /// 
        ///     Adds one to the Y register setting the zero and negative flags as appropriate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void INY()
        {
            unchecked
            {
                Y++;
            }

            Status.Zero = Y == 0;
            Status.Negative = Y.IsNegative();
        }

        /// <summary>
        ///     ISB - Undocumented Opcode
        ///
        ///     Equivalent to INC value then SBC value, except supporting more addressing modes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ISB()
        {
            INC();
            SBC();
        }

        /// <summary>
        ///     JMP - Jump
        /// 
        ///     Sets the program counter to the address specified by the operand.
        /// 
        ///     NOTE: An original 6502 has does not correctly fetch the target address if the indirect vector falls on a page 
        ///     boundary (e.g. $xxFF where xx is any value from $00 to $FF). In this case fetches the LSB from $xxFF as expected 
        ///     but takes the MSB from $xx00. This is fixed in some later chips like the 65SC02 so for compatibility always ensure 
        ///     the indirect vector is not at the end of the page.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void JMP()
        {
            //Properly set address if it falls on a page boundary
            if (Instruction.AddressingMode == AddressingMode.Indirect && (GetOperandWord() & 0xFF) == 0xFF)
            {
                PC = GetWord(GetOperandWord(), true);
            }
            else
            {
                PC = ResolveAddress();
            }
        }

        /// <summary>
        ///     JSR - Jump to Subroutine
        /// 
        ///     The JSR instruction pushes the address(minus one) of the return point on to the stack and then sets the program counter 
        ///     to the target memory address.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void JSR()
        {
            Push((ushort) (PC+2));
            PC = ResolveAddress();
        }

        /// <summary>
        ///     LAX - Undocumented Opcode
        ///
        ///     Shortcut for LDA value then TAX. Saves a byte and two cycles and allows use of the X register
        ///     with the (d),Y addressing mode. Notice that the immediate is missing; the opcode that would
        ///     have been LAX is affected by line noise on the data bus. MOS 6502: even the bugs have bugs.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void LAX()
        {
            LDA();
            TAX();
        }

        /// <summary>
        ///     LDA - Load Accumulator
        /// 
        ///     A,Z,N = M
        /// 
        ///     Loads a byte of memory into the accumulator setting the zero and negative flags as appropriate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void LDA()
        {
            A = Instruction.AddressingMode == AddressingMode.Immediate ? GetOperandByte() : CPUMemory.ReadByte(ResolveAddress());

            Status.Zero = A == 0;
            Status.Negative = A.IsNegative();
        }

        /// <summary>
        ///     LDX - Load X Register
        /// 
        ///     X,Z,N = M
        /// 
        ///     Loads a byte of memory into the X register setting the zero and negative flags as appropriate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void LDX()
        {
            X = Instruction.AddressingMode == AddressingMode.Immediate ? GetOperandByte() : CPUMemory.ReadByte(ResolveAddress());

            Status.Zero = X == 0;
            Status.Negative = X.IsNegative();
        }

        /// <summary>
        ///     LDY - Load Y Register
        /// 
        ///     Y,Z,N = M
        /// 
        ///     Loads a byte of memory into the Y register setting the zero and negative flags as appropriate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void LDY()
        {
            Y = Instruction.AddressingMode == AddressingMode.Immediate ? GetOperandByte() : CPUMemory.ReadByte(ResolveAddress());

            Status.Zero = (Y == 0);
            Status.Negative = Y.IsNegative();
        }

        /// <summary>
        ///     LSR - Logical Shift Right
        /// 
        ///     A,C,Z,N = A/2 or M, C, Z, N = M / 2
        /// 
        ///     Each of the bits in A or M is shift one place to the right.
        ///     The bit that was in bit 0 is shifted into the carry flag.
        ///     Bit 7 is set to zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void LSR()
        {
            if (Instruction.AddressingMode == AddressingMode.Accumulator)
            {
                Status.Carry = (A & 0x01) > 0;
                A >>= 1;
                Status.Zero = A == 0;
                Status.Negative = A.IsNegative();
                return;
            }

            var address = ResolveAddress();
            var value = CPUMemory.ReadByte(address);

            Status.Carry = (value & 0x01) == 1;
            value >>= 1;
            Status.Zero = value == 0;
            Status.Negative = value.IsNegative();

            CPUMemory.WriteByte(address, value);
        }

        /// <summary>
        ///     ORA - Logical Inclusive OR
        ///     
        ///     A,Z,N = A|M
        ///     An inclusive OR is performed, bit by bit, on the accumulator contents using the contents of a byte of memory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ORA()
        {
            A |= Instruction.AddressingMode == AddressingMode.Immediate ? GetOperandByte() : CPUMemory.ReadByte(ResolveAddress());
            Status.Negative = A.IsNegative();
            Status.Zero = A == 0;
        }

        /// <summary>
        ///     PHA - Push Accumulator
        /// 
        ///     Pushes a copy of the accumulator on to the stack.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void PHA() => Push(A);

        /// <summary>
        ///     PHP - Push Processor Status
        /// 
        ///     Pushes a copy of the status flags on to the stack.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void PHP() => Push((byte) (Status.ToByte() | 0b00010000));

        /// <summary>
        ///     PLA - Pull Accumulator
        /// 
        ///     Pulls an 8 bit value from the stack and into the accumulator.
        ///     The zero and negative flags are set as appropriate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void PLA()
        {
            A = PopByte();
            Status.Zero = A == 0;
            Status.Negative = A.IsNegative();
        }

        /// <summary>
        ///     PLP - Pull Processor Status
        ///     
        ///     Pulls an 8 bit value from the stack and into the processor flags.
        ///     The flags will take on new states as determined by the value pulled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void PLP() => Status.FromByte((byte) (PopByte() & ~0b00010000));

        /// <summary>
        ///     RLA - Undocumented Opcode
        ///
        ///     Equivalent to ROL value then AND value, except supporting more addressing modes.
        ///     LDA #$FF followed by RLA is an efficient way to rotate a variable while also loading it in A.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void RLA()
        {
            ROL();
            AND();
        }

        /// <summary>
        ///     ROL - Rotate Left
        /// 
        ///     Move each of the bits in either A or M one place to the left. Bit 0 is filled with the current value of the carry flag
        ///     whilst the old bit 7 becomes the new carry flag value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ROL()
        {
            var value = Instruction.AddressingMode == AddressingMode.Accumulator
                ? A
                : CPUMemory.ReadByte(ResolveAddress());

            //Shift Bits to new value
            var newValue = (byte)(value << 1);

            //New Bit 0 filled with current value of Carry Flag
            if (Status.Carry)
                newValue |= 0x01;

            //Old Bit 7 becomes new carry flag
            Status.Carry = value >> 7 == 1;
            
            //Set Negative Flag on new Bit 7 value
            Status.Negative = newValue.IsNegative();

            //Set Zero Flag on new value
            Status.Zero = newValue == 0;

            //Save New Value
            if (Instruction.AddressingMode == AddressingMode.Accumulator)
            {
                A = newValue;
            }
            else
            {
                CPUMemory.WriteByte(ResolveAddress(), newValue);
            }
        }

        /// <summary>
        ///     ROR - Rotate Right
        /// 
        ///     Move each of the bits in either A or M one place to the right. Bit 7 is filled with the current value
        ///     of the carry flag whilst the old bit 0 becomes the new carry flag value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ROR()
        {
            var value = Instruction.AddressingMode == AddressingMode.Accumulator
                ? A
                : CPUMemory.ReadByte(ResolveAddress());

            //Shift Bits to new value
            var newValue = (byte)(value >> 1);

            //New Bit 0 filled with current value of Carry Flag
            if (Status.Carry)
                newValue |= 0x80;

            //Old Bit 0 becomes new carry flag
            Status.Carry = (byte)(value << 7) == 0x80;

            //Set Negative Flag on new Bit 7 value
            Status.Negative = newValue.IsNegative();

            //Set Zero Flag on new value
            Status.Zero = newValue == 0;

            //Save New Value
            if (Instruction.AddressingMode == AddressingMode.Accumulator)
            {
                A = newValue;
            }
            else
            {
                CPUMemory.WriteByte(ResolveAddress(), newValue);
            }
        }

        /// <summary>
        ///     RRA - Undocumented Opcode
        ///
        ///     Equivalent to ROR value then ADC value, except supporting more addressing modes. Essentially
        ///     this computes A + value / 2, where value is 9-bit and the division is rounded up.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void RRA()
        {
            ROR();
            ADC();
        }

        /// <summary>
        ///     RTI - Return from Interrupt
        ///
        ///     The RTI instruction is used at the end of an interrupt processing routine.It pulls the processor flags
        ///     from the stack followed by the program counter.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void RTI()
        {
            Status.FromByte(PopByte());
            PC = PopWord();
        }

        /// <summary>
        ///     RTS - Return from Subroutine
        ///
        ///     The RTS instruction is used at the end of a subroutine to return to the calling routine. It pulls the
        ///     program counter (minus one) from the stack.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void RTS() => PC = PopWord() + 1;

        /// <summary>
        ///     SAX - Undocumented Opcode
        ///
        ///     Stores the bitwise AND of A and X. As with STA and STX, no flags are affected.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void SAX() =>
            CPUMemory.WriteByte(ResolveAddress(), (byte) (A & X));

        /// <summary>
        ///     SBC - Subtract with Carry
        ///
        ///     A,Z,C,N = A-M-(1-C)
        ///
        ///     This instruction subtracts the contents of a memory location to the accumulator together with the not
        ///     of the carry bit. If overflow occurs the carry bit is clear, this enables multiple byte subtraction to be performed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void SBC()
        {
            var value = Instruction.AddressingMode == AddressingMode.Immediate ? GetOperandByte() : CPUMemory.ReadByte(ResolveAddress());

            unchecked
            {
                var newA = (sbyte)A - (sbyte)value - (1- (Status.Carry ? 1 : 0));

                Status.Zero = (byte)newA == 0;
                Status.Negative = ((byte)newA).IsNegative();
                Status.Carry = A - value - (1 - (Status.Carry ? 1 : 0)) >=  byte.MinValue && A - value - (1 - (Status.Carry ? 1 : 0)) <= byte.MaxValue ;
                Status.Overflow = newA > 127 || newA < -128;
                A = (byte)newA;
            }
        }

        /// <summary>
        ///     SEC - Set Carry Flag
        ///
        ///     Set the carry flag to one.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void SEC() => Status.Carry = true;

        /// <summary>
        ///     SED - Set Decimal Flag
        ///
        ///     Set the decimal mode flag to one.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void SED() => Status.DecimalMode = true;

        /// <summary>
        ///     SEI - Set Interrupt Disable
        ///
        ///     Set the interrupt disable flag to one.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void SEI() => Status.InterruptDisable = true;

        /// <summary>
        ///     LSO - Undocumented Opcode
        /// 
        ///     Equivalent to ASL value then ORA value, except supporting more addressing modes.
        ///     LDA #0 followed by SLO is an efficient way to shift a variable while also loading it in A.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void SLO()
        {
            ASL();
            ORA();
        }

        /// <summary>
        ///     SRE - Undocumented Opcode
        ///
        ///     Equivalent to LSR value then EOR value, except supporting more addressing modes. LDA #0 followed
        ///     by SRE is an efficient way to shift a variable while also loading it in A.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void SRE()
        {
            LSR();
            EOR();
        }

        /// <summary>
        ///     STA - Store Accumulator
        ///
        ///     M = A
        /// 
        ///     Stores the contents of the accumulator into memory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void STA() => CPUMemory.WriteByte(ResolveAddress(), A);


        /// <summary>
        ///     STX - Store Accumulator
        ///
        ///     M = X
        /// 
        ///     Stores the contents of the X register into memory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void STX() => CPUMemory.WriteByte(ResolveAddress(), X);


        /// <summary>
        ///     STY - Store Accumulator
        ///
        ///     M = Y
        /// 
        ///     Stores the contents of the Y register into memory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void STY() => CPUMemory.WriteByte(ResolveAddress(), Y);


        /// <summary>
        ///     TAX - Transfer Accumulator to X
        ///
        ///     X = A
        /// 
        ///     Copies the current contents of the accumulator into the X register and sets the zero
        ///     and negative flags as appropriate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void TAX()
        {
            X = A;
            Status.Zero = X == 0;
            Status.Negative = X.IsNegative();
        }

        /// <summary>
        ///     TAY - Transfer Accumulator to Y
        ///
        ///     Y = A
        /// 
        ///     Copies the current contents of the accumulator into the Y register and sets the zero
        ///     and negative flags as appropriate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void TAY()
        {
            Y = A;
            Status.Zero = Y == 0;
            Status.Negative = Y.IsNegative();
        }

        /// <summary>
        ///     TSX - Stack Pointer to X
        ///
        ///     X = SP
        /// 
        ///     Copies the current contents of the Stack Pointer into the X register and sets the zero
        ///     and negative flags as appropriate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void TSX()
        {
            X = SP;
            Status.Zero = X == 0;
            Status.Negative = X.IsNegative();
        }

        /// <summary>
        ///     TXA - Transfer X Register to Accumulator
        ///
        ///     A = X
        /// 
        ///     Copies the current contents of the X register into the accumulator and sets the zero
        ///     and negative flags as appropriate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void TXA()
        {
            A = X;
            Status.Zero = A == 0;
            Status.Negative = A.IsNegative();
        }

        /// <summary>
        ///     TXS - Transfer X to Stack Pointer
        ///
        ///     SP = X
        /// 
        ///     Copies the current contents of the X register into the stack register.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void TXS() => SP = X;

        /// <summary>
        ///     TYA - Transfer Y Register to Accumulator
        ///
        ///     A = Y
        /// 
        ///     Copies the current contents of the Y register into the accumulator and sets the zero
        ///     and negative flags as appropriate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void TYA()
        {
            A = Y;
            Status.Zero = A == 0;
            Status.Negative = A.IsNegative();
        }

        /// <summary>
        ///     Pushes a byte value to the stack location and decrements the stack pointer
        /// </summary>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void Push(byte value)
        {
            CPUMemory.WriteByte( STACK_BASE + SP, value);
            SP--;
        }

        /// <summary>
        ///  Pushes a word value to the stack location and decrements the stack pointer
        /// </summary>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void Push(ushort value)
        {
            CPUMemory.WriteByte(STACK_BASE + SP, (byte)(value >> 8));
            SP--;
            CPUMemory.WriteByte(STACK_BASE + SP, (byte)(value & 0xFF));
            SP--;
        }

        /// <summary>
        ///     Pops a byte value from the stack location and increments the stack pointer
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private byte PopByte()
        {
            SP++;
            return CPUMemory.ReadByte(STACK_BASE + SP);
        }

        /// <summary>
        ///     Pops a word value from the stack location and increments the stack pointer
        ///
        ///     Overload for PopByte, called twice and return value is cast into a ushort
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private ushort PopWord() => BitConverter.ToUInt16(new[] {PopByte(), PopByte()}, 0);

        /// <summary>
        ///     Helper method to get the byte operand for an opcode
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private byte GetOperandByte()
        {
            Instruction.Operand = CPUMemory.ReadByte(PC + 1);
            return (byte) Instruction.Operand;
        }

        /// <summary>
        ///     Helper method to get the sbyte operand for an opcode
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private sbyte GetOperandSByte()
        {
            var result = (sbyte)CPUMemory.ReadByte(PC + 1);
            Instruction.Operand = (ushort)result;
            return result;
        }

        /// <summary>
        ///     Helper method to get the word operand for an opcode
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private ushort GetOperandWord()
        {
            Instruction.Operand = GetWord((ushort)(PC + 1));
            return (ushort)Instruction.Operand;
        } 

        /// <summary>
        ///     Returns the address based on the specified Addressing Mode and the operand
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private int ResolveAddress()
        {
            switch (Instruction.AddressingMode)
            {
                case AddressingMode.Absolute:
                    return GetOperandWord();
                case AddressingMode.Immediate:
                    return PC + 1;
                case AddressingMode.Accumulator:
                    return A;
                case AddressingMode.ZeroPage:
                    return GetOperandByte();
                case AddressingMode.ZeroPageX:
                    byte zeroPageXAddress;
                    unchecked
                    {
                        zeroPageXAddress = GetOperandByte();
                        zeroPageXAddress += X;
                    }
                    return zeroPageXAddress;
                case AddressingMode.ZeroPageY:
                    byte zeroPageYAddress;
                    unchecked
                    {
                        zeroPageYAddress = GetOperandByte();
                        zeroPageYAddress += Y;
                    }
                    return zeroPageYAddress;
                case AddressingMode.Relative:
                    if (Instruction.PageBoundaryCheck)
                        CheckBoundary(GetOperandSByte() + PC, PC);
                    return  GetOperandSByte() + PC;
                case AddressingMode.AbsoluteX:
                    if (Instruction.PageBoundaryCheck)
                        CheckBoundary(GetOperandWord() + X, GetOperandWord());
                    return  GetOperandWord() + X;
                case AddressingMode.AbsoluteY:
                    if (Instruction.PageBoundaryCheck)
                        CheckBoundary(GetOperandWord() + Y, GetOperandWord());

                    var targetAbsoluteYOffset = GetOperandWord();

                    //Check special case where 0xFFFF wraps back to 0x0000
                    if (targetAbsoluteYOffset == 0xFFFF)
                        return Y - 1;

                    return targetAbsoluteYOffset + Y;
                case AddressingMode.Indirect:
                    return GetWord(GetOperandWord());
                case AddressingMode.IndexedIndirect:
                    byte address;
                    unchecked
                    {
                        address = GetOperandByte();
                        address += X;
                    }
                    return GetWord(address, true);
                case AddressingMode.IndirectIndexed:
                    if (Instruction.PageBoundaryCheck)
                        CheckBoundary( GetWord(GetOperandByte(), true) + Y, GetWord(GetOperandByte(), true));

                    var targetOffset = GetWord(GetOperandByte(), true);

                    //Check special case where 0xFFFF wraps back to 0x0000
                    if (targetOffset == 0xFFFF)
                        return Y-1;

                    return targetOffset + Y;
                    
                default:
                    throw new Exception("Unknown Addressing Mode");
            }
        }

        /// <summary>
        ///     Checks two values to see if the most significant bit is different denoting a Page Boundary will be crossed
        /// </summary>
        /// <param name="address1"></param>
        /// <param name="address2"></param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void CheckBoundary(int address1, int address2)
        {
            if (address1 >> 8 != address2 >> 8)
                Cycles++;
        }

        /// <summary>
        ///     Helper Method that returns an unsigned word
        /// </summary>
        /// <param name="address"></param>
        /// <param name="pageWrap"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private ushort GetWord(int address, bool pageWrap = false)
        {
            if (pageWrap && (address | 0xFF) == address)
            {
                return BitConverter.ToUInt16(
                    [CPUMemory.ReadByte(address), CPUMemory.ReadByte(address & ~0xFF)],
                    0);
            }

            return BitConverter.ToUInt16([CPUMemory.ReadByte(address), CPUMemory.ReadByte(address +1)], 0);
        }
    }
}