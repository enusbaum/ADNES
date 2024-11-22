# ADNES - Another dotnet NES Emulator

ADNES is a Nintendo Emulator wrapped in a NuGet package for easy implementation in any of your projects. The Emulator is written in C# and is designed to be as modular as possible.

The goal of the project first started as my personal journey to learn more about the NES and how it works. I wanted to create a project that would allow me to learn more about the NES and the underlying hardware, while also providing a platform for others to learn from as well.

The code itself has been written in a way to balance readability and performance. 

Hardware components such as the CPU, PPU, Controller, and Cartridge are all wired to/through the Emulator. The Emulator handles the timings both for CPU->PPU ratio, as well as frame timing on how fast ADNES renders frames (Half, Normal or **TURBO**).

# Overview

Implementing ADNES into your own project is designed to be as simple as possible. The Emulator is wrapped in a NuGet package, and can be installed via NuGet Package Manager.

Implementing the Emulator itself can be done in a few simple steps:

1. Install the NuGet package

```
Install-Package ADNES
```

2. Create Delegate in your application to handle 8-bit bitmap frames as they become available

```csharp
public delegate void ProcessFrameDelegate(byte[] outputFrame);
```

Frames returned from ADNES are 8bpp bitmaps that are 256x240 pixels in size and mapped to the 2C02 NTSC Color Palette. There are helper methods in ADNES `ADNES.Helpers` to assist you in converting the data from
ADNES to 32bpp bitmap data (or 32bpp Bitmap files).

3. Create an instance of the Emulator

```csharp
var emulator = new Emulator(byte[] rom, ProcessFrameDelegate processFrameDelegate);
```

ADNES supports iNES format ROMs. The ROM is passed as a byte array to the Emulator. The delegate is used to pass frames to your application as they become available.

4. Start the Emulator

```csharp
public void Start()
```

As the emulator is running, Frames are processed and passed to the delegate as they become available. The delegate can then be used to display the frames in your own project.

Player 1 Controller can be accessed via `public readonly IController Controller1` on the instance of `ADNES.Emulator`.

There are several properties that are updated during runtime to report the status of the Emulator:

- `public bool IsRunning` - Returns true if the Emulator is currently running
- `public long TotalCPUCycles` - The total number of CPU cycles that have been processed since the Emulator was started
- `public long TotalPPUCycles` - The total number of PPU cycles that have been processed since the Emulator was started
- `public long TotalFrames` - The total number of frames that have been rendered since the Emulator was started
