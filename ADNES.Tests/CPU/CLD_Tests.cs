using ADNES.Cartridge.Mappers;
using ADNES.CPU;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ADNES.Tests.CPU
{
    [TestClass]
    public class CLD_Tests
    {
        [TestMethod]
        public void CLD_Decimal()
        {
            var mapper = new NROM([0xD8], null);
            var cpu = new Core(mapper);
            cpu.Status.DecimalMode = true;

            cpu.Tick();

            //Verify Cycles
            Assert.AreEqual(2u, cpu.Cycles);

            //Verify Flags
            Assert.AreEqual(false, cpu.Status.DecimalMode);
        }
    }
}