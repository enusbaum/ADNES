using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADNES.APU.Channels
{
    /// <summary>
    ///     The NES APU's delta modulation channel (DMC) can output 1-bit delta-encoded samples or can have its 7-bit counter directly loaded, allowing flexible manual sample playback.
    ///
    ///     This class can be used to represent the DMC channel.
    /// </summary>
    internal class DMC
    {
        /// <summary>
        ///     Represents the 4 bytes of a DMC Channel within the NES APU.
        /// </summary>
        private byte[] registers = new byte[4];

        /// <summary>
        ///    The NTSC Frequency of the NES APU represented in Hz.
        /// </summary>
        private const int NTSCFrequency = 1789773;

        //Expose parsed DMC Channel register values
        //$4010 Values
        public bool IRQEnabled { get { return (registers[0] & 0x80) != 0; } }
        public bool LoopSample { get { return (registers[0] & 0x40) != 0; } }
        public byte RateIndex { get { return (byte)(registers[0] & 0x0F); } }

        //$4011 Values (Direct Load)
        public byte DirectLoad { get { return registers[1]; } }

        //$4012 Values (Sample Address)
        public byte SampleAddress { get { return registers[2]; } }

        //$4013 Values (Sample Length)
        public byte SampleLength { get { return registers[3]; } }

        //Establish NTSC Rate Index Table for DMC Channel
        //The rate determines for how many CPU cycles happen between changes in the output level during automatic delta-encoded sample playback.
        private static readonly ushort[] NTSCRateIndexTable = new ushort[]
        {
            428, 380, 340, 320, 286, 254, 226, 214,
            190, 160, 142, 128, 106, 84, 72, 54
        };

        //Calculate the DMC Channel's output frequency based on the specified rate index in the rate Index Table
        public float GetOutputFrequency() => (float)NTSCFrequency / NTSCRateIndexTable[RateIndex];

        /// <summary>
        ///    Writes a value to the DMC Channel registers.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="value"></param>
        public void Write(int address, byte value)
        {
            registers[address - 0x4010] = value;
        }
    }
}
