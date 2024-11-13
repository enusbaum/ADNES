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

3. Create an instance of the Emulator

```csharp
var emularor = new Emulator(byte[] rom, ProcessFrameDelegate processFrameDelegate);
```

As the emulator is running, Frames are processed and passed to the delegate as they become available. The delegate can then be used to display the frames in your own project.

4. Start the Emulator

```csharp
public void Start()
```