using ADNES.Cartridge.Mappers.impl;
using ADNES.CPU;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ADNES.Tests.CPU
{
    [TestClass]
    public class CLI_Tests
    {
        [TestMethod]
        public void CLI_Interrupt()
        {
            var mapper = new NROM([0x58], null);
            var cpu = new Core(mapper);
            cpu.Status.InterruptDisable = true;

            cpu.Tick();

            //Verify Cycles
            Assert.AreEqual(2u, cpu.Cycles);

            //Verify Flags
            Assert.AreEqual(false, cpu.Status.InterruptDisable);
        }
    }
}