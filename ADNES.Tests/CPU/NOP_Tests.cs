using ADNES.Cartridge.Mappers.impl;
using ADNES.CPU;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ADNES.Tests.CPU
{
    [TestClass]
    public class NOP_Tests
    {
        [TestMethod]
        public void NOP_Implied()
        {
            var mapper = new NROM([0xEA], null);
            var cpu = new Core(mapper);

            cpu.Tick();

            //Verify Cycles
            Assert.AreEqual(2u, cpu.Cycles);
        }
    }
}
