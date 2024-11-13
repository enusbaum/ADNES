using System;
using ADNES.Cartridge.Flags;
using ADNES.Cartridge.Mappers;
using ADNES.Cartridge.Mappers.Enums;
using ADNES.Cartridge.Mappers.impl;
using ADNES.Common.Extensions;
using Microsoft.Extensions.Logging;

namespace ADNES.Cartridge
{
    /// <summary>
    ///     Class that Represents a NES cartridge by loading an iNES format ROM
    ///
    ///     This class/project will contain the PGR/CHR memory as well as mapper functionality.
    ///     Access to this class is abstracted through the ICartridge interface, which is referenced
    ///     directly by both the CPU and PPU (as in the actual NES)
    ///
    ///     ROM Format: https://wiki.nesdev.com/w/index.php/INES
    /// </summary>
    internal class NESCartridge : ICartridge
    {
        private byte Flags6;
        private byte Flags7;
        private byte[] _prgRom;
        private byte _prgRomBanks;
        private byte[] _chrRom;
        private byte _chrRomBanks;
        private byte[] _prgRam;
        private bool UsesCHRRAM;
        private NametableMirroring _nametableMirroring;

        public IMapper MemoryMapper { get; set; }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="ROM">Byte Array containing the desired iNES ROM to load</param>
        public NESCartridge(ReadOnlySpan<byte> ROM)
        {
            LoadROM(ROM);
        }

        /// <summary>
        ///     Loads the specified iNES ROM
        /// </summary>
        /// <param name="ROM">Byte Array containing the desired iNES ROM to load</param>
        /// <returns>TRUE if load was successful</returns>
        public bool LoadROM(ReadOnlySpan<byte> ROM)
        {
            //Header is 16 bytes

            //PRG Rom starts right after, unless there's a 512 byte trainer (indicated by flags)
            var prgROMOffset = 16;

            //_header == "NES<EOF>"
            if (BitConverter.ToInt32(ROM) != 0x1A53454E)
                throw new Exception("Invalid ROM Header");

            //Setup Memory
            _prgRomBanks = ROM[4];
            var prgROMSize = _prgRomBanks * 16384;
            _prgRom = new byte[prgROMSize];

            _chrRomBanks = ROM[5];
            var chrROMSize = _chrRomBanks * 8192; //0 denotes default 8k
            if (ROM[5] == 0)
            {
                _chrRom = new byte[8192];
                UsesCHRRAM = true;
            }
            else
            {
                _chrRom = new byte[chrROMSize];
            }

            //Set Flags6
            Flags6 = ROM[6];

            //Move PGR ROM Start if Trainer Present
            if (Flags6.IsFlagSet(Byte6Flags.TrainerPresent))
                prgROMOffset += 512;

            //Set Initial Mirroring Mode
            _nametableMirroring = Flags6.IsFlagSet(Byte6Flags.VerticalMirroring) ? NametableMirroring.Vertical : NametableMirroring.Horizontal;

            //Set Flags7
            Flags7 = ROM[7];

            var prgRAMSize = ROM[8] == 0 ? 8192 : ROM[8] * 8192; //0 denotes default 8k
            _prgRam = new byte[prgRAMSize];

            //Load PRG ROM
            _prgRom = ROM.Slice(prgROMOffset, prgROMSize).ToArray();

            //Load CHR ROM
            _chrRom = ROM.Slice(prgROMOffset + prgROMSize, chrROMSize).ToArray();

            //Load Proper Mapper
            var mapperNumber = Flags7 & 0xF0 | (Flags6 >> 4 & 0xF);
            MemoryMapper = mapperNumber switch
            {
                0 => new NROM(_prgRom, _chrRom, _nametableMirroring),
                1 => new MMC1(_prgRomBanks, _chrRom, _prgRom, UsesCHRRAM, false, _nametableMirroring),
                2 => new UxROM(_prgRom, _prgRomBanks, _chrRom, _nametableMirroring),
                3 => new CNROM(_prgRom, _prgRomBanks, _chrRom, _nametableMirroring),
                _ => throw new Exception($"Unsupported Mapper: {mapperNumber}")
            };

            return true;
        }
    }
}
