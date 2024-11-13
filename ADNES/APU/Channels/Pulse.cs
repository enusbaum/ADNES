namespace ADNES.APU.Channels
{
    /// <summary>
    ///     The NES APU has two Pulse Channels, each with 4 bytes of memory. This class can be used to represent one of those channels.
    ///
    ///     Because the NES Pulse Channel Registers are write only, we accept data written to the registers and then expose the parsed values
    ///     of those registers to be read by ADNES and emulated as audio.
    /// </summary>
    internal class Pulse
    {
        /// <summary>
        ///     Represents the 4 bytes of a Pulse Channel within the NES APU.
        /// </summary>
        private byte[] registers = new byte[4];

        //Expose parsed Pulse Channel register values

        //$4000 Values
        public byte DutyCycle { get { return (byte)(registers[0] >> 6); } }
        public bool LengthCounterHalt { get { return (registers[0] & 0x20) != 0; } }
        public bool ConstantVolume { get { return (registers[0] & 0x10) != 0; } }
        public byte Volume { get { return (byte)(registers[0] & 0x0F); } }

        //$4001 Values (Sweep Unit)
        public bool SweepEnabled { get { return (registers[1] & 0x80) != 0; } }
        public byte SweepPeriod { get { return (byte)((registers[1] & 0x70) >> 4); } }
        public bool SweepNegate { get { return (registers[1] & 0x08) != 0; } }
        public byte SweepShift { get { return (byte)(registers[1] & 0x07); } }

        //$4002 Values (Timer Low)
        public byte TimerLow { get { return registers[2]; } }

        //$4003 Values (Length Counter and Timer High)
        public byte LengthCounter { get { return (byte)((registers[3] & 0xF8) >> 3); } }
        public byte TimerHigh { get { return (byte)(registers[3] & 0x07); } }

        //Combined Timer Vaules
        public ushort Timer { get { return (ushort)((TimerHigh << 8) | TimerLow); } }

        /// <summary>
        ///    Writes a value to the Pulse Channel registers.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="value"></param>
        public void Write(int address, byte value)
        {
            registers[address - 0x4000] = value;
        }

    }
}
