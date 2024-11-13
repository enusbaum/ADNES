namespace ADNES.CPU.Enums
{
    /// <summary>
    ///     Enumerator where we list the available addressing modes in ADNES
    /// </summary>
    internal enum AddressingMode
    {
        Implicit,
        Accumulator,
        Immediate,
        ZeroPage,
        ZeroPageX,
        ZeroPageY,
        Relative,
        Absolute,
        AbsoluteX,
        AbsoluteY,
        Indirect,
        IndexedIndirect,
        IndirectIndexed,
        NONE
    }
}