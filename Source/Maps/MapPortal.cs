using Microsoft.Xna.Framework;

namespace Terrascent.Maps;

/// <summary>
/// State of a map portal.
/// </summary>
public enum PortalState
{
    /// <summary>Portal is active and can be entered.</summary>
    Active,

    /// <summary>Portal is closing (animation).</summary>
    Closing,

    /// <summary>Portal is closed and cannot be entered.</summary>
    Closed
}

/// <summary>
/// A portal that leads to or from a map zone.
/// </summary>
public class MapPortal
{
    /// <summary>Unique identifier for this portal.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>World position of the portal.</summary>
    public Vector2 Position { get; set; }

    /// <summary>The zone this portal leads to.</summary>
    public Guid? TargetZoneId { get; set; }

    /// <summary>Whether this is an entrance (into zone) or exit (back to world).</summary>
    public bool IsEntrance { get; set; }

    /// <summary>Current state of the portal.</summary>
    public PortalState State { get; private set; } = PortalState.Active;

    /// <summary>Interaction radius in pixels.</summary>
    public float InteractionRadius { get; set; } = 32f;

    /// <summary>Portal color based on map tier.</summary>
    public Color PortalColor { get; set; } = Color.Purple;

    /// <summary>Animation timer for visual effects.</summary>
    public float AnimationTime { get; private set; }

    /// <summary>Time remaining until portal closes (for exit portals).</summary>
    public float TimeRemaining { get; private set; } = -1f; // -1 = no timer

    // Portal dimensions
    public const float WIDTH = 48f;
    public const float HEIGHT = 64f;

    // Closing animation duration
    private const float CLOSING_DURATION = 0.5f;
    private float _closingTimer;

    public MapPortal(Vector2 position, bool isEntrance = true)
    {
        Position = position;
        IsEntrance = isEntrance;
    }

    /// <summary>
    /// Create a portal for a map zone.
    /// </summary>
    public static MapPortal CreateZonePortal(Vector2 position, Guid zoneId, int mapTier)
    {
        var portal = new MapPortal(position, true)
        {
            TargetZoneId = zoneId,
            PortalColor = MapTier.GetTierColor(mapTier)
        };
        return portal;
    }

    /// <summary>
    /// Create an exit portal to return to the overworld.
    /// </summary>
    public static MapPortal CreateExitPortal(Vector2 position)
    {
        return new MapPortal(position, false)
        {
            TargetZoneId = null,
            PortalColor = Color.LightBlue
        };
    }

    /// <summary>
    /// Update portal state and animation.
    /// </summary>
    public void Update(float deltaTime)
    {
        AnimationTime += deltaTime;

        // Update timer if set
        if (TimeRemaining > 0)
        {
            TimeRemaining -= deltaTime;
            if (TimeRemaining <= 0)
            {
                Close();
            }
        }

        // Handle closing animation
        if (State == PortalState.Closing)
        {
            _closingTimer += deltaTime;
            if (_closingTimer >= CLOSING_DURATION)
            {
                State = PortalState.Closed;
            }
        }
    }

    /// <summary>
    /// Check if a position is within interaction range.
    /// </summary>
    public bool IsInRange(Vector2 position)
    {
        if (State != PortalState.Active) return false;

        float distance = Vector2.Distance(position, Center);
        return distance <= InteractionRadius;
    }

    /// <summary>
    /// Check if an entity's bounding box overlaps with the portal.
    /// </summary>
    public bool Overlaps(Rectangle bounds)
    {
        if (State != PortalState.Active) return false;

        var portalBounds = GetBounds();
        return bounds.Intersects(portalBounds);
    }

    /// <summary>
    /// Get the bounding rectangle of the portal.
    /// </summary>
    public Rectangle GetBounds()
    {
        return new Rectangle(
            (int)(Position.X - WIDTH / 2),
            (int)(Position.Y - HEIGHT / 2),
            (int)WIDTH,
            (int)HEIGHT
        );
    }

    /// <summary>
    /// Get the center position of the portal.
    /// </summary>
    public Vector2 Center => new(Position.X, Position.Y);

    /// <summary>
    /// Start closing the portal.
    /// </summary>
    public void Close()
    {
        if (State == PortalState.Active)
        {
            State = PortalState.Closing;
            _closingTimer = 0f;
        }
    }

    /// <summary>
    /// Reopen a closed portal.
    /// </summary>
    public void Reopen()
    {
        State = PortalState.Active;
        _closingTimer = 0f;
    }

    /// <summary>
    /// Set a timer for the portal to automatically close.
    /// </summary>
    public void SetTimer(float seconds)
    {
        TimeRemaining = seconds;
    }

    /// <summary>
    /// Get the current animation scale (for visual pulsing).
    /// </summary>
    public float GetPulseScale()
    {
        return 1f + MathF.Sin(AnimationTime * 3f) * 0.05f;
    }

    /// <summary>
    /// Get the current animation alpha (for closing effect).
    /// </summary>
    public float GetAlpha()
    {
        if (State == PortalState.Closing)
        {
            return 1f - (_closingTimer / CLOSING_DURATION);
        }
        return State == PortalState.Active ? 1f : 0f;
    }

    /// <summary>
    /// Get portal color with animation applied.
    /// </summary>
    public Color GetAnimatedColor()
    {
        float alpha = GetAlpha();
        float pulse = 0.8f + MathF.Sin(AnimationTime * 4f) * 0.2f;

        return new Color(
            (int)(PortalColor.R * pulse),
            (int)(PortalColor.G * pulse),
            (int)(PortalColor.B * pulse),
            (int)(255 * alpha)
        );
    }

    #region Serialization

    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(Id.ToByteArray());
        writer.Write(Position.X);
        writer.Write(Position.Y);
        writer.Write(TargetZoneId.HasValue);
        if (TargetZoneId.HasValue)
            writer.Write(TargetZoneId.Value.ToByteArray());
        writer.Write(IsEntrance);
        writer.Write((int)State);
        writer.Write(TimeRemaining);
    }

    public static MapPortal LoadFrom(BinaryReader reader)
    {
        var id = new Guid(reader.ReadBytes(16));
        var posX = reader.ReadSingle();
        var posY = reader.ReadSingle();
        var hasTarget = reader.ReadBoolean();
        Guid? targetZoneId = hasTarget ? new Guid(reader.ReadBytes(16)) : null;
        var isEntrance = reader.ReadBoolean();
        var state = (PortalState)reader.ReadInt32();
        var timeRemaining = reader.ReadSingle();

        return new MapPortal(new Vector2(posX, posY), isEntrance)
        {
            TargetZoneId = targetZoneId,
            State = state,
            TimeRemaining = timeRemaining
        };
    }

    #endregion
}