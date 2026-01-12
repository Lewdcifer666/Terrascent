using Microsoft.Xna.Framework;
//using Terrascent.Source.Maps;

namespace Terrascent.Maps;

/// <summary>
/// State of the map device.
/// </summary>
public enum MapDeviceState
{
    /// <summary>Device is empty, ready to accept a map.</summary>
    Empty,

    /// <summary>Device has a map inserted.</summary>
    MapInserted,

    /// <summary>Device is activating (generating zone).</summary>
    Activating,

    /// <summary>Device is active with portals open.</summary>
    Active,

    /// <summary>Device is on cooldown after zone closed.</summary>
    Cooldown
}

/// <summary>
/// The Map Device opens portals to endgame map zones.
/// Similar to Path of Exile's Map Device.
/// </summary>
public class MapDevice
{
    /// <summary>Unique identifier for this device.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>World position of the device (center).</summary>
    public Vector2 Position { get; set; }

    /// <summary>Current state of the device.</summary>
    public MapDeviceState State { get; private set; } = MapDeviceState.Empty;

    /// <summary>The currently inserted map (if any).</summary>
    public MapItem? InsertedMap { get; private set; }

    /// <summary>The active zone (if any).</summary>
    public MapZone? ActiveZone { get; private set; }

    /// <summary>List of portals around the device.</summary>
    public List<MapPortal> Portals { get; } = new();

    /// <summary>Interaction radius for the device.</summary>
    public float InteractionRadius { get; set; } = 64f;

    /// <summary>Activation time in seconds.</summary>
    public float ActivationTime { get; set; } = 2f;

    /// <summary>Cooldown time after zone closes.</summary>
    public float CooldownTime { get; set; } = 5f;

    // Device dimensions
    public const float WIDTH = 96f;
    public const float HEIGHT = 64f;

    // Portal configuration
    private const int PORTAL_COUNT = 6;
    private const float PORTAL_DISTANCE = 80f;

    // Timers
    private float _activationTimer;
    private float _cooldownTimer;

    // Animation
    public float AnimationTime { get; private set; }

    /// <summary>Event fired when a zone is created.</summary>
    public event Action<MapZone>? OnZoneCreated;

    /// <summary>Event fired when a zone is closed.</summary>
    public event Action<MapZone>? OnZoneClosed;

    public MapDevice(Vector2 position)
    {
        Position = position;
    }

    /// <summary>
    /// Update device state.
    /// </summary>
    public void Update(float deltaTime)
    {
        AnimationTime += deltaTime;

        switch (State)
        {
            case MapDeviceState.Activating:
                UpdateActivating(deltaTime);
                break;

            case MapDeviceState.Active:
                UpdateActive(deltaTime);
                break;

            case MapDeviceState.Cooldown:
                UpdateCooldown(deltaTime);
                break;
        }
    }

    private void UpdateActivating(float deltaTime)
    {
        _activationTimer += deltaTime;

        if (_activationTimer >= ActivationTime)
        {
            FinishActivation();
        }
    }

    private void UpdateActive(float deltaTime)
    {
        // Update zone
        ActiveZone?.Update(deltaTime);

        // Update portals
        foreach (var portal in Portals)
        {
            portal.Update(deltaTime);
        }

        // Check if zone should close
        if (ActiveZone != null)
        {
            if (ActiveZone.PortalsRemaining <= 0 || ActiveZone.State == MapZoneState.Closed)
            {
                CloseZone();
            }
        }
    }

    private void UpdateCooldown(float deltaTime)
    {
        _cooldownTimer += deltaTime;

        if (_cooldownTimer >= CooldownTime)
        {
            State = MapDeviceState.Empty;
            _cooldownTimer = 0f;
        }
    }

    /// <summary>
    /// Insert a map into the device.
    /// </summary>
    public bool InsertMap(MapItem map)
    {
        if (State != MapDeviceState.Empty)
            return false;

        InsertedMap = map;
        State = MapDeviceState.MapInserted;
        return true;
    }

    /// <summary>
    /// Remove the inserted map without activating.
    /// </summary>
    public MapItem? RemoveMap()
    {
        if (State != MapDeviceState.MapInserted)
            return null;

        var map = InsertedMap;
        InsertedMap = null;
        State = MapDeviceState.Empty;
        return map;
    }

    /// <summary>
    /// Activate the device, starting zone generation.
    /// </summary>
    public bool Activate()
    {
        if (State != MapDeviceState.MapInserted || InsertedMap == null)
            return false;

        State = MapDeviceState.Activating;
        _activationTimer = 0f;
        return true;
    }

    /// <summary>
    /// Finish activation and create portals.
    /// </summary>
    private void FinishActivation()
    {
        if (InsertedMap == null) return;

        // Create the zone
        ActiveZone = new MapZone(InsertedMap);

        // Generate portals in a circle around the device
        GeneratePortals();

        // Activate the zone
        ActiveZone.Activate();

        State = MapDeviceState.Active;

        OnZoneCreated?.Invoke(ActiveZone);

        System.Diagnostics.Debug.WriteLine($"Map Device activated: {InsertedMap.GetDisplayName()}");
    }

    /// <summary>
    /// Generate portals around the device.
    /// </summary>
    private void GeneratePortals()
    {
        Portals.Clear();

        if (ActiveZone == null) return;

        for (int i = 0; i < PORTAL_COUNT; i++)
        {
            // Calculate position in circle
            float angle = (float)(i * 2 * Math.PI / PORTAL_COUNT) - MathF.PI / 2;
            float x = Position.X + MathF.Cos(angle) * PORTAL_DISTANCE;
            float y = Position.Y + MathF.Sin(angle) * PORTAL_DISTANCE;

            var portal = MapPortal.CreateZonePortal(
                new Vector2(x, y),
                ActiveZone.Id,
                ActiveZone.Tier
            );

            Portals.Add(portal);
            ActiveZone.Portals.Add(portal);
        }
    }

    /// <summary>
    /// Close the active zone.
    /// </summary>
    public void CloseZone()
    {
        if (ActiveZone == null) return;

        ActiveZone.Close();

        // Close all portals
        foreach (var portal in Portals)
        {
            portal.Close();
        }

        OnZoneClosed?.Invoke(ActiveZone);

        // Clear state
        InsertedMap = null;
        ActiveZone = null;
        Portals.Clear();

        State = MapDeviceState.Cooldown;
        _cooldownTimer = 0f;

        System.Diagnostics.Debug.WriteLine("Map zone closed");
    }

    /// <summary>
    /// Force close the zone (admin/debug).
    /// </summary>
    public void ForceClose()
    {
        if (State == MapDeviceState.Active)
        {
            CloseZone();
        }
        else if (State == MapDeviceState.MapInserted)
        {
            InsertedMap = null;
            State = MapDeviceState.Empty;
        }
        else if (State == MapDeviceState.Activating)
        {
            InsertedMap = null;
            State = MapDeviceState.Empty;
            _activationTimer = 0f;
        }
    }

    /// <summary>
    /// Check if a position is within interaction range.
    /// </summary>
    public bool IsInRange(Vector2 position)
    {
        float distance = Vector2.Distance(position, Position);
        return distance <= InteractionRadius;
    }

    /// <summary>
    /// Get the bounding rectangle of the device.
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
    /// Get activation progress (0-1).
    /// </summary>
    public float GetActivationProgress()
    {
        if (State != MapDeviceState.Activating) return 0f;
        return Math.Clamp(_activationTimer / ActivationTime, 0f, 1f);
    }

    /// <summary>
    /// Get cooldown progress (0-1).
    /// </summary>
    public float GetCooldownProgress()
    {
        if (State != MapDeviceState.Cooldown) return 0f;
        return Math.Clamp(_cooldownTimer / CooldownTime, 0f, 1f);
    }

    /// <summary>
    /// Get the display color based on state.
    /// </summary>
    public Color GetDisplayColor()
    {
        return State switch
        {
            MapDeviceState.Empty => Color.Gray,
            MapDeviceState.MapInserted => Color.Yellow,
            MapDeviceState.Activating => Color.Orange,
            MapDeviceState.Active => Color.Green,
            MapDeviceState.Cooldown => Color.DarkGray,
            _ => Color.White
        };
    }

    /// <summary>
    /// Get status text for UI.
    /// </summary>
    public string GetStatusText()
    {
        return State switch
        {
            MapDeviceState.Empty => "Insert Map",
            MapDeviceState.MapInserted => $"Ready: {InsertedMap?.GetDisplayName()}",
            MapDeviceState.Activating => $"Activating... {GetActivationProgress() * 100:F0}%",
            MapDeviceState.Active => $"Active: {ActiveZone?.PortalsRemaining ?? 0} portals",
            MapDeviceState.Cooldown => $"Cooldown... {(1 - GetCooldownProgress()) * CooldownTime:F1}s",
            _ => "Unknown"
        };
    }

    #region Serialization

    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(Id.ToByteArray());
        writer.Write(Position.X);
        writer.Write(Position.Y);
        writer.Write((int)State);

        // Save inserted map
        writer.Write(InsertedMap != null);
        InsertedMap?.SaveTo(writer);

        // Save active zone
        writer.Write(ActiveZone != null);
        ActiveZone?.SaveTo(writer);

        // Save timers
        writer.Write(_activationTimer);
        writer.Write(_cooldownTimer);
    }

    public static MapDevice LoadFrom(BinaryReader reader)
    {
        var id = new Guid(reader.ReadBytes(16));
        var posX = reader.ReadSingle();
        var posY = reader.ReadSingle();
        var state = (MapDeviceState)reader.ReadInt32();

        var device = new MapDevice(new Vector2(posX, posY))
        {
            State = state
        };

        // Load inserted map
        if (reader.ReadBoolean())
        {
            device.InsertedMap = MapItem.LoadFrom(reader);
        }

        // Load active zone
        if (reader.ReadBoolean())
        {
            device.ActiveZone = MapZone.LoadFrom(reader);
            // Rebuild portals reference
            device.Portals.AddRange(device.ActiveZone.Portals);
        }

        // Load timers
        device._activationTimer = reader.ReadSingle();
        device._cooldownTimer = reader.ReadSingle();

        return device;
    }

    #endregion
}