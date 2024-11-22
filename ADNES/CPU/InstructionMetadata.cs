using System;
using ADNES.CPU.Enums;

namespace ADNES.CPU
{
    /// <summary>
    ///     We use the InstructionMetadata class to set specific details for an Opcode
    ///     such as Cycles, Addressing Mode, etc.
    /// </summary>
    internal record InstructionMetadata
    {
        /// <summary>
        ///     Enumeration of the actual Operation to be performed
        /// </summary>
        public Opcode Opcode = Opcode.NONE;

        /// <summary>
        ///     Operand of Instruction being Executed
        /// </summary>
        public ushort? Operand;

        /// <summary>
        ///     Addressing Mode of the specified operation
        /// </summary>
        public AddressingMode AddressingMode;

        /// <summary>
        ///     Specified if the requested operation costs an extra cycle
        ///     if it crosses page boundaries
        /// </summary>
        public bool PageBoundaryCheck;

        /// <summary>
        ///     Base number of cycles for the instruction
        /// </summary>
        public int Cycles;

        /// <summary>
        ///     Total number of bytes for the instruction (including op and operand)
        /// </summary>
        public int Length;

        /// <summary>
        ///     Method to execute for this OpCode
        /// </summary>
        public Action OpCodeExecution;

        /// <summary>
        ///     Override of ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Enum.GetName(typeof(Opcode), Opcode) ?? throw new InvalidOperationException();
    }
}
