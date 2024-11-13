namespace ADNES.APU.Channels
{
    /// <summary>
    ///     The NES APU triangle channel generates a pseudo-triangle wave. This class can be used to represent the triangle channel.
    ///
    ///     Because the NES Triangle Channel Registers are write only, we accept data written to the registers and then expose the parsed values
    ///     of those registers to be read by ADNES and emulated as audio.
    /// </summary>
    internal class Triangle
    {

        /// <summary>
        ///     Represents the 4 bytes of a Triangle Channel within the NES APU.
        ///
        ///     Address $4009 (byte 1) is not used by the Triangle Channel.
        /// </summary>
        private byte[] registers = new byte[4];

        //Expose parsed Triangle Channel register values
        //$4008 Values
        public bool LengthCounterHalt { get { return (registers[0] & 0x80) != 0; } }
        public byte LinearCounter { get { return (byte)(registers[0] & 0x7F); } }

        //$400A Values (Timer Low)
        public byte TimerLow { get { return registers[2]; } }

        //$400B Values (Length Counter and Timer High)
        public byte LengthCounter { get { return (byte)((registers[3] & 0xF8) >> 3); } }
        public byte TimerHigh { get { return (byte)(registers[3] & 0x07); } }

        //Combined Timer Vaules
        public ushort Timer { get { return (ushort)((TimerHigh << 8) | TimerLow); } }

        /// <summary>
        ///    Writes a value to the Triangle Channel registers.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="value"></param>
        public void Write(int address, byte value)
        {
            registers[address - 0x4008] = value;
        }
    }
}
