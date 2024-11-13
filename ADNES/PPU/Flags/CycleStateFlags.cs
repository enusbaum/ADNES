namespace ADNES.PPU.Flags
{
    /// <summary>
    ///     Defined Flags for current cycle we're in the PPU rendering pipeline
    ///
    ///     https://wiki.nesdev.com/w/index.php/PPU_rendering
    /// </summary>
    internal static class CycleStateFlags
    {
        public const int Visible = 1 << 0;
        public const int Prefetch = 1 << 1;
        public const int Fetch = 1 << 2;
        public const int Default = 1 << 3;
    }
}
