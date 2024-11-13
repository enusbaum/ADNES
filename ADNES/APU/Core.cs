using ADNES.APU.Channels;
using ADNES.Cartridge.Mappers;

namespace ADNES.APU
{
    internal class Core
    {
        /// <summary>
        ///     APU Memory Space
        /// </summary>
        public IMapper SystemMemory;

        /// <summary>
        ///     NES APU Pulse Channel 1
        /// </summary>
        private readonly Pulse PulseRegister1;

        /// <summary>
        ///     NES APU Pulse Channel 2
        /// </summary>
        private readonly Pulse PulseRegister2;

        /// <summary>
        ///     NES APU Triangle Channel
        /// </summary>
        private readonly Triangle TriangleRegister;

        /// <summary>
        ///     NES APU Noise Channel
        /// </summary>
        private readonly Noise NoiseRegister;

        /// <summary>
        ///     NES APU DMC Channel
        /// </summary>
        private readonly DMC DMCRegister;

        public Core(IMapper memoryMapper)
        {
            SystemMemory = memoryMapper;

            PulseRegister1 = new Pulse();
            PulseRegister2 = new Pulse();
            TriangleRegister = new Triangle();
            NoiseRegister = new Noise();
            DMCRegister = new DMC();

            //Register Memory Interceptors for APU Pulse Channel 1 and 2
            SystemMemory.RegisterWriteInterceptor(PulseRegister1.Write, 0x4000, 0x4003);
            SystemMemory.RegisterWriteInterceptor(PulseRegister2.Write, 0x4004, 0x4007);

            //Register Memory Interceptors for APU Triangle Channel
            SystemMemory.RegisterWriteInterceptor(TriangleRegister.Write, 0x4008, 0x400B);

            //Register Memory Interceptors for APU Noise Channel
            SystemMemory.RegisterWriteInterceptor(NoiseRegister.Write, 0x400C, 0x400F);

            //Register Memory Interceptors for APU DMC Channel
            SystemMemory.RegisterWriteInterceptor(DMCRegister.Write, 0x4010, 0x4013);

        }
    }
}
