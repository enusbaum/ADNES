﻿using ADNES.Cartridge.Mappers;
using ADNES.CPU;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ADNES.Tests.CPU
{
    [TestClass]
    public class BRK_Tests
    {
        [TestMethod]
        public void BRK()
        {
            var mapper = new NROM([0x00], null);
            var cpu = new Core(mapper);
            cpu.CPUMemory.WriteByte(0xFFFE, 0xFE);
            cpu.CPUMemory.WriteByte(0xFFFF, 0xC0);
            cpu.Status.InterruptDisable = false;

            cpu.Tick();

            //Verify Memory Values
            Assert.AreNotEqual(0xC000, cpu.PC);
            Assert.AreEqual(0xC0FF, cpu.PC);

            //Verify Cycles
            Assert.AreEqual(7u, cpu.Cycles);

            //Verify Flags
            Assert.AreEqual(true, cpu.Status.InterruptDisable);
        }
    }
}