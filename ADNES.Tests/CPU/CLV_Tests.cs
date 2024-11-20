using ADNES.Cartridge.Mappers;
using ADNES.CPU;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ADNES.Tests.CPU
{
    [TestClass]
    public class CLV_Tests
    {
        [TestMethod]
        public void CLV_Overflow()
        {
            var mapper = new NROM([0xB8], null);
            var cpu = new Core(mapper);
            cpu.Status.Overflow = true;

            cpu.Tick();

            //Verify Cycles
            Assert.AreEqual(2u, cpu.Cycles);

            //Verify Flags
            Assert.AreEqual(false, cpu.Status.Overflow);
        }
    }
}