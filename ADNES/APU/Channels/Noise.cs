namespace ADNES.APU.Channels
{
    /// <summary>
    ///     The NES APU has a Noise Channel, which generates a pseudo-random sequence of bits. This class can be used to represent the noise channel.
    ///
    ///     Because the NES Noise Channel Registers are write only, we accept data written to the registers and then expose the parsed values
    ///     of those registers to be read by ADNES and emulated as audio.
    /// </summary>
    internal class Noise
    {
        /// <summary>
        ///     Represents the 4 bytes of a Noise Channel within the NES APU.
        ///
        ///     $400D (byte 1) is not used by the Noise Channel.
        /// </summary>
        private byte[] registers = new byte[4];

        //Expose parsed Noise Channel register values
        //$400C Values
        public bool LengthCounterHalt { get { return (registers[0] & 0x20) != 0; } }
        public bool ConstantVolume { get { return (registers[0] & 0x10) != 0; } }
        public byte Volume { get { return (byte)(registers[0] & 0x0F); } }

        //$400E Values (Envelope)
        public bool LoopNoise { get { return (registers[2] & 0x20) != 0; } }
        public byte NoisePeriod { get { return (byte)(registers[2] & 0x0F); } }

        //$400F Values (Length Counter Load)
        public byte LengthCounter { get { return (byte)((registers[3] & 0xF8) >> 3); } }


        /// <summary>
        ///    Writes a value to the Noise Channel registers.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="value"></param>
        public void Write(int address, byte value)
        {
            registers[address - 0x400C] = value;
        }
    }
}
