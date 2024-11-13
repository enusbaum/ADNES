using System;

namespace ADNES.Controller.Enums
{
    /// <summary>
    ///     Mapped Values for the NES Standard Controller
    ///
    ///     https://wiki.nesdev.com/w/index.php/Standard_controller
    /// </summary>
    [Flags]
    public enum Buttons : byte
    {
        A = 1,
        B = 1 << 1,
        Select = 1 << 2,
        Start = 1 << 3,
        Up = 1 << 4,
        Down = 1 << 5,
        Left = 1 << 6,
        Right = 1 << 7
    }
}
