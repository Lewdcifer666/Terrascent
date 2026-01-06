using System.Runtime.InteropServices;

namespace Terrascent.World;

/// <summary>
/// Core tile data structure. Kept small (8 bytes) for cache efficiency.
/// A 4200x1200 world = ~40MB of tile data at 8 bytes per tile.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Tile
{
    /// <summary>
    /// The type of block (dirt, stone, ore, etc). 0 = Air.
    /// </summary>
    public TileType Type;

    /// <summary>
    /// The background wall type. 0 = No wall.
    /// </summary>
    public WallType Wall;

    /// <summary>
    /// Sprite frame X coordinate for auto-tiling (which variant to show).
    /// </summary>
    public byte FrameX;

    /// <summary>
    /// Sprite frame Y coordinate for auto-tiling.
    /// </summary>
    public byte FrameY;

    /// <summary>
    /// Light level from 0 (pitch black) to 255 (full bright).
    /// </summary>
    public byte Light;

    /// <summary>
    /// Bit flags for tile state (active, wired, etc).
    /// </summary>
    public TileFlags Flags;

    #region Properties

    /// <summary>
    /// Returns true if this tile has a solid block (not air).
    /// </summary>
    public readonly bool IsActive => Flags.HasFlag(TileFlags.Active);

    /// <summary>
    /// Returns true if this tile is empty (air).
    /// </summary>
    public readonly bool IsAir => Type == TileType.Air || !IsActive;

    /// <summary>
    /// Returns true if this tile has a background wall.
    /// </summary>
    public readonly bool HasWall => Wall != WallType.None;

    /// <summary>
    /// Returns true if tile frame needs to be recalculated.
    /// </summary>
    public readonly bool IsFrameDirty => Flags.HasFlag(TileFlags.FrameDirty);

    #endregion

    #region Constructors

    /// <summary>
    /// Create a new tile with the specified type.
    /// </summary>
    public Tile(TileType type)
    {
        Type = type;
        Wall = WallType.None;
        FrameX = 0;
        FrameY = 0;
        Light = 0;
        Flags = type != TileType.Air ? TileFlags.Active | TileFlags.FrameDirty : TileFlags.None;
    }

    /// <summary>
    /// Create a new tile with type and wall.
    /// </summary>
    public Tile(TileType type, WallType wall) : this(type)
    {
        Wall = wall;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Set the tile type and mark as active.
    /// </summary>
    public void SetType(TileType type)
    {
        Type = type;
        if (type != TileType.Air)
        {
            Flags |= TileFlags.Active | TileFlags.FrameDirty;
        }
        else
        {
            Flags &= ~TileFlags.Active;
            Flags |= TileFlags.FrameDirty;
        }
    }

    /// <summary>
    /// Clear the tile (make it air).
    /// </summary>
    public void Clear()
    {
        Type = TileType.Air;
        Flags &= ~TileFlags.Active;
        Flags |= TileFlags.FrameDirty;
        FrameX = 0;
        FrameY = 0;
    }

    /// <summary>
    /// Set the background wall.
    /// </summary>
    public void SetWall(WallType wall)
    {
        Wall = wall;
    }

    /// <summary>
    /// Clear the background wall.
    /// </summary>
    public void ClearWall()
    {
        Wall = WallType.None;
    }

    /// <summary>
    /// Set the sprite frame coordinates.
    /// </summary>
    public void SetFrame(byte frameX, byte frameY)
    {
        FrameX = frameX;
        FrameY = frameY;
        Flags &= ~TileFlags.FrameDirty;
    }

    /// <summary>
    /// Mark the frame as needing recalculation.
    /// </summary>
    public void MarkFrameDirty()
    {
        Flags |= TileFlags.FrameDirty;
    }

    #endregion

    #region Static Helpers

    /// <summary>
    /// Create an empty air tile.
    /// </summary>
    public static Tile Empty => new(TileType.Air);

    #endregion

    public override readonly string ToString()
    {
        return $"Tile({Type}, Wall={Wall}, Light={Light}, Flags={Flags})";
    }
}