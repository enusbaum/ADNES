using System;
using ADNES.Cartridge.Mappers;

namespace ADNES.Cartridge
{
    /// <summary>
    ///     Public Interface for an ADNES Cartridge
    /// </summary>
    internal interface ICartridge
    {
        bool LoadROM(ReadOnlySpan<byte> ROM);
        IMapper MemoryMapper { get; set; }
    }
}
