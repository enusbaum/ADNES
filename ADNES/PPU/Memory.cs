﻿using System;
using System.Runtime.CompilerServices;
using ADNES.Cartridge.Mappers;
using ADNES.Cartridge.Mappers.Enums;

namespace ADNES.PPU
{
    /// <summary>
    ///     PPU Internal Memory
    ///
    ///     In addition to accessing the CHR ROM via the mapper, the PPU itself has 2K RAM
    ///     and 32 bytes of Palette RAM
    /// </summary>
    internal class Memory
    {
        private readonly IMapper _memoryMapper;
        private readonly byte[] _ppuVram;
        private readonly byte[] _paletteMemory;

        internal Memory(IMapper memoryMapper)
        {
            _memoryMapper = memoryMapper;
            _ppuVram = new byte[2048];
            _paletteMemory = new byte[32];
        }

        /// <summary>
        ///     Resets the memory back to startup state
        /// </summary>
        public void Reset()
        {
            Array.Clear(_ppuVram, 0, _ppuVram.Length);
            Array.Clear(_paletteMemory, 0, _paletteMemory.Length);
        }

        public byte ReadByte(int offset)
        {
            return offset switch
            {
                // Palette RAM
                >= 0x3F00 and <= 0x3FFF => _paletteMemory[GetPaletteRamOffsetIndex(offset)],
                // CHR (ROM or RAM) pattern tables
                < 0x2000 => _memoryMapper.ReadByte(offset),
                // Internal _vRam
                <= 0x3EFF => _ppuVram[VramOffsetToOffsetIndex(offset)],
                _ => throw new Exception($"Invalid PPU Memory Read at address: {offset:X4}")
            };
        }

        /// <summary>
        ///     Write a byte of memory to the specified address.
        /// </summary>
        /// <param name="offset">the address to write to</param>
        /// <param name="data">the byte to write to the specified address</param>
        public void WriteByte(int offset, byte data)
        {
            switch (offset)
            {
                case < 0x2000:
                    _memoryMapper.WriteByte(offset, data);
                    return;
                // Internal VRAM
                case <= 0x3EFF:
                    _ppuVram[VramOffsetToOffsetIndex(offset)] = data;
                    return;
                // Palette RAM addresses
                case <= 0x3FFF:
                    _paletteMemory[GetPaletteRamOffsetIndex(offset)] = data;
                    return;
                default:
                    throw new Exception($"Invalid PPU Memory Write at address: {offset:X4}");
            }
        }

        /// <summary>
        ///     Given a palette RAM address ($3F00-$3FFF), return the index in
        ///     palette RAM that it corresponds to.
        /// </summary>
        /// <returns>Offset in our 32-byte non-mirrored Palette RAM</returns>
        /// <param name="offset">Offset in Palette RAM</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetPaletteRamOffsetIndex(int offset)
        {
            var index = (offset - 0x3F00) % 32;

            // Mirror $3F10, $3F14, $3F18, $3F1C to $3F00, $3F14, $3F08 $3F0C
            if (index >= 16 && (index - 16) % 4 == 0) return index - 16;

            return index;
        }

        /// <summary>
        ///     Given a address $2000-$3EFFF, returns the index in the VRAM array
        ///     that the address points to depending on the current VRAM mirroring
        ///     mode.
        ///
        ///     We get the mirroring mode from the memory mapper, which loaded it
        ///     from the cartridge/ROM
        /// </summary>
        /// <returns>Offset of the Index</returns>
        /// <param name="offset">Index Offset</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int VramOffsetToOffsetIndex(int offset)
        {
            // Address in VRAM indexed with 0 at 0x2000
            var index = (offset - 0x2000) % 0x1000;
            switch (_memoryMapper.NametableMirroring)
            {
                case NametableMirroring.Vertical:
                    // If in one of the mirrored regions, subtract 0x800 to get index
                    if (index >= 0x800) index -= 0x800;
                    break;
                case NametableMirroring.Horizontal:
                    if (index > 0x800) index = (index - 0x800) % 0x400 + 0x400; // In the 2 B regions
                    else index %= 0x400; // In one of the 2 A regions
                    break;
                case NametableMirroring.SingleLower:
                    index %= 0x400;
                    break;
                case NametableMirroring.SingleUpper:
                    index = index % 400 + 0x400;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return index;
        }
    }
}
