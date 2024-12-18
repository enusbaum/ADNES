﻿using ADNES.Cartridge.Mappers;
using ADNES.CPU;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ADNES.Tests.CPU
{
    [TestClass]
    public class CPX_Tests
    {
        [TestMethod]
        public void CPX_Immediate_Carry()
        {
            var mapper = new NROM([0xE0, 0x7E], null);
            var cpu = new Core(mapper) { X = 0x7F };

            cpu.Tick();

            //Verify Cycles
            Assert.AreEqual(2u, cpu.Cycles);

            //Verify Flags
            Assert.AreEqual(false, cpu.Status.Negative);
            Assert.AreEqual(true, cpu.Status.Carry);
            Assert.AreEqual(false, cpu.Status.Zero);
        }

        [TestMethod]
        public void CPX_Immediate_Carry_Zero()
        {
            var mapper = new NROM([0xE0, 0x7F], null);
            var cpu = new Core(mapper) { X = 0x7F };

            cpu.Tick();

            //Verify Cycles
            Assert.AreEqual(2u, cpu.Cycles);

            //Verify Flags
            Assert.AreEqual(false, cpu.Status.Negative);
            Assert.AreEqual(true, cpu.Status.Carry);
            Assert.AreEqual(true, cpu.Status.Zero);
        }

        [TestMethod]
        public void CPX_Immediate_Negative()
        {
            var mapper = new NROM([0xE0, 0x01], null);
            var cpu = new Core(mapper) { X = 0x00 };

            cpu.Tick();

            //Verify Cycles
            Assert.AreEqual(2u, cpu.Cycles);

            //Verify Flags
            Assert.AreEqual(true, cpu.Status.Negative);
            Assert.AreEqual(false, cpu.Status.Carry);
            Assert.AreEqual(false, cpu.Status.Zero);
        }

        [TestMethod]
        public void CPX_ZeroPage_Carry()
        {
            var mapper = new NROM([0xE4, 0x00], null);
            var cpu = new Core(mapper) { X = 0x7F };
            cpu.CPUMemory.WriteByte(0x00, 0x7E);

            cpu.Tick();

            //Verify Cycles
            Assert.AreEqual(3u, cpu.Cycles);

            //Verify Flags
            Assert.AreEqual(false, cpu.Status.Negative);
            Assert.AreEqual(true, cpu.Status.Carry);
            Assert.AreEqual(false, cpu.Status.Zero);
        }
        [TestMethod]
        public void CPX_Absolute_Carry()
        {
            var mapper = new NROM([0xEC, 0x00, 0x00], null);
            var cpu = new Core(mapper) { X = 0x7F };
            cpu.CPUMemory.WriteByte(0x00, 0x7E);

            cpu.Tick();

            //Verify Cycles
            Assert.AreEqual(4u, cpu.Cycles);

            //Verify Flags
            Assert.AreEqual(false, cpu.Status.Negative);
            Assert.AreEqual(true, cpu.Status.Carry);
            Assert.AreEqual(false, cpu.Status.Zero);
        }

    }
}
