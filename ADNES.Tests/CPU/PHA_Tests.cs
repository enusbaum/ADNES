using ADNES.Cartridge.Mappers;
using ADNES.CPU;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ADNES.Tests.CPU
{
    [TestClass]
    public class PHA_Tests
    {
        [TestMethod]
        public void PHA_Implied()
        {
            var mapper = new NROM([0x48], null);
            var cpu = new Core(mapper) { A = 0x01 };

            cpu.Tick();

            //Verify Registers
            //Not Modified

            //Verify Cycles
            Assert.AreEqual(3u, cpu.Cycles);

            //Verify Flags
            //Not Modified

            //Verify Stack
            Assert.AreEqual(0x01, cpu.CPUMemory.ReadByte(Core.STACK_BASE + cpu.SP + 1));
        }
    }
}