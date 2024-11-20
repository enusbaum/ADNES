using ADNES.Cartridge.Mappers.Enums;

namespace ADNES.Cartridge.Mappers
{
    /// <summary>
    ///     Public Interface for ADNES.Cartridge Mappers
    /// </summary>
    internal interface IMapper
    {
        byte ReadByte(int offset);

        void WriteByte(int offset, byte data);

        void RegisterReadInterceptor(MapperBase.ReadInterceptor readInterceptor, int offset);

        void RegisterReadInterceptor(MapperBase.ReadInterceptor readInterceptor, int offsetStart, int offsetEnd);

        void RegisterWriteInterceptor(MapperBase.WriteInterceptor writeInterceptor, int offset);

        void RegisterWriteInterceptor(MapperBase.WriteInterceptor writeInterceptor, int offsetStart, int offsetEnd);

        NametableMirroring NametableMirroring { get; set; }
    }
}
