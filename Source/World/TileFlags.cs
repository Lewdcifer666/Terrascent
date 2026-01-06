namespace Terrascent.World;

/// <summary>
/// Bit flags for tile state. Packed into a single byte.
/// </summary>
[Flags]
public enum TileFlags : byte
{
    None = 0,

    /// <summary>
    /// Tile is "active" (solid/visible). Air tiles have this unset.
    /// </summary>
    Active = 1 << 0,

    /// <summary>
    /// Tile has been modified by player (for world gen tracking).
    /// </summary>
    Modified = 1 << 1,

    /// <summary>
    /// Tile is actuated (made non-solid by actuator).
    /// </summary>
    Actuated = 1 << 2,

    /// <summary>
    /// Tile has red wire.
    /// </summary>
    WireRed = 1 << 3,

    /// <summary>
    /// Tile has blue wire.
    /// </summary>
    WireBlue = 1 << 4,

    /// <summary>
    /// Tile has green wire.
    /// </summary>
    WireGreen = 1 << 5,

    /// <summary>
    /// Tile frame needs recalculation (dirty flag for auto-tiling).
    /// </summary>
    FrameDirty = 1 << 6,

    /// <summary>
    /// Reserved for future use.
    /// </summary>
    Reserved = 1 << 7,
}