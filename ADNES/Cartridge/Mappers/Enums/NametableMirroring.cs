namespace ADNES.Cartridge.Mappers.Enums
{
    /// <summary>
    ///     Nametable Mirroring
    ///
    ///     More Information: https://wiki.nesdev.com/w/index.php/Mirroring#Nametable_Mirroring
    /// </summary>
    internal enum NametableMirroring
    {
        /// <summary>
        ///     Horizontal VRAM mirroring (vertical arrangement of the nametables)
        /// </summary>
        Horizontal,
        /// <summary>
        ///     Vertical VRAM mirroring (horizontal arrangement of the nametables)
        /// </summary>
        Vertical,
        /// <summary>
        ///     Single Screen mirroring of the lower nametable.
        /// </summary>
        SingleLower,
        /// <summary>
        ///     Single Screen mirroring of the upper nametable.
        /// </summary>
        SingleUpper,
    }
}