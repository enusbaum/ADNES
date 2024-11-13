using ADNES.Cartridge.Mappers.impl;
using ADNES.CPU;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ADNES.Tests.CPU
{
    [TestClass]
    public class CLC_Tests
    {
        [TestMethod]
        public void CLC_Carry()
        {
            var mapper = new NROM([0x18], null);
            var cpu = new Core(mapper);
            cpu.Status.Carry = true;

            cpu.Tick();

            //Verify Cycles
            Assert.AreEqual(2u, cpu.Cycles);

            //Verify Flags
            Assert.AreEqual(false, cpu.Status.Carry);
        }
    }
}