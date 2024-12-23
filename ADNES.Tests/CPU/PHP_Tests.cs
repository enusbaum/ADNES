﻿using ADNES.Cartridge.Mappers;
using ADNES.CPU;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ADNES.Tests.CPU
{
    [TestClass]
    public class PHP_Tests
    {
        [TestMethod]
        public void PHP_Implied()
        {
            var mapper = new NROM([0x08], null);
            var cpu = new Core(mapper);

            //Set all statuses to true
            cpu.Status.Zero = true;
            cpu.Status.Carry = true;
            cpu.Status.DecimalMode = true;
            cpu.Status.InterruptDisable = true;
            cpu.Status.Negative = true;
            cpu.Status.Overflow = true;

            cpu.Tick();

            //Verify Registers
            //Not Modified

            //Verify Cycles
            Assert.AreEqual(3u, cpu.Cycles);

            //Verify Flags
            //Not Modified

            //Verify Stack
            Assert.AreEqual(0xFF, cpu.CPUMemory.ReadByte(Core.STACK_BASE + cpu.SP + 1));
        }
    }
}