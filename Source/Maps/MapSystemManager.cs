using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terrascent.Items;

namespace Terrascent.Maps;

/// <summary>
/// Manages the overall map system including devices, active zones, and UI.
/// </summary>
public class MapSystemManager
{
    // Map Devices in the world
    private readonly List<MapDevice> _devices = new();

    // Fusion Devices in the world
    private readonly List<MapFusionDevice> _fusionDevices = new();

    // Currently active zones
    private readonly List<MapZone> _activeZones = new();

    // Map item storage for inventory integration
    public MapItemStorage MapStorage { get; }

    // UI components
    public MapDeviceUI? DeviceUI { get; private set; }

    // Player's current zone (null if in overworld)
    public MapZone? CurrentZone { get; private set; }

    // Player position when entering zone (for return)
    private Vector2 _overworldReturnPosition;

    // Dependencies
    private readonly Inventory _playerInventory;

    // Events
    public event Action<MapZone>? OnZoneEntered;
    public event Action<MapZone>? OnZoneExited;
    public event Action<MapItem>? OnMapDropped;
    public event Action<MapCurrency>? OnCurrencyDropped;

    public MapSystemManager(Inventory playerInventory)
    {
        _playerInventory = playerInventory;
        MapStorage = new MapItemStorage();
    }

    /// <summary>
    /// Initialize UI components (call after graphics device is ready).
    /// </summary>
    public void InitializeUI()
    {
        DeviceUI = new MapDeviceUI(MapStorage, _playerInventory);
        DeviceUI.OnZoneActivated += HandleZoneActivated;
        DeviceUI.OnClosed += HandleUIClose;
    }

    /// <summary>
    /// Update the map system.
    /// </summary>
    public void Update(GameTime gameTime, MouseState mouse, MouseState prevMouse, KeyboardState keyboard)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Update devices
        foreach (var device in _devices)
        {
            device.Update(deltaTime);
        }

        // Update active zones
        foreach (var zone in _activeZones)
        {
            zone.Update(deltaTime);
        }

        // Update UI
        DeviceUI?.Update(gameTime, mouse, prevMouse, keyboard);
    }

    /// <summary>
    /// Draw map system UI.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel, Rectangle viewport)
    {
        DeviceUI?.Draw(spriteBatch, font, pixel, viewport);
    }

    #region Device Management

    /// <summary>
    /// Create and place a map device at a position.
    /// </summary>
    public MapDevice CreateDevice(Vector2 position)
    {
        var device = new MapDevice(position);
        device.OnZoneCreated += HandleZoneCreated;
        device.OnZoneClosed += HandleZoneClosed;
        _devices.Add(device);

        System.Diagnostics.Debug.WriteLine($"MapSystemManager: Created device at {position}");
        return device;
    }

    /// <summary>
    /// Create and place a fusion device at a position.
    /// </summary>
    public MapFusionDevice CreateFusionDevice(Vector2 position)
    {
        var device = new MapFusionDevice { Position = position };
        _fusionDevices.Add(device);

        System.Diagnostics.Debug.WriteLine($"MapSystemManager: Created fusion device at {position}");
        return device;
    }

    /// <summary>
    /// Get the nearest device within interaction range.
    /// </summary>
    public MapDevice? GetNearestDevice(Vector2 position, float maxRange = 100f)
    {
        MapDevice? nearest = null;
        float nearestDist = maxRange;

        foreach (var device in _devices)
        {
            float dist = Vector2.Distance(position, device.Position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = device;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Get the nearest fusion device within interaction range.
    /// </summary>
    public MapFusionDevice? GetNearestFusionDevice(Vector2 position, float maxRange = 100f)
    {
        return _fusionDevices
            .Where(d => d.IsInRange(position))
            .OrderBy(d => Vector2.Distance(position, d.Position))
            .FirstOrDefault();
    }

    /// <summary>
    /// Try to interact with a map device.
    /// </summary>
    public bool TryInteractWithDevice(Vector2 playerPosition)
    {
        var device = GetNearestDevice(playerPosition);
        if (device != null && device.IsInRange(playerPosition))
        {
            DeviceUI?.Open(device);
            return true;
        }
        return false;
    }

    #endregion

    #region Zone Management

    /// <summary>
    /// Enter a map zone through a portal.
    /// </summary>
    public bool EnterZone(MapPortal portal, Vector2 currentPosition)
    {
        if (!portal.TargetZoneId.HasValue) return false;

        var zone = _activeZones.FirstOrDefault(z => z.Id == portal.TargetZoneId.Value);
        if (zone == null) return false;

        if (!zone.UsePortal())
        {
            System.Diagnostics.Debug.WriteLine("MapSystemManager: No portals remaining!");
            return false;
        }

        _overworldReturnPosition = currentPosition;
        CurrentZone = zone;

        OnZoneEntered?.Invoke(zone);
        System.Diagnostics.Debug.WriteLine($"MapSystemManager: Entered zone {zone.GetDisplayName()}");
        return true;
    }

    /// <summary>
    /// Exit the current zone through an exit portal.
    /// </summary>
    public Vector2? ExitZone()
    {
        if (CurrentZone == null) return null;

        var zone = CurrentZone;
        CurrentZone = null;

        OnZoneExited?.Invoke(zone);
        System.Diagnostics.Debug.WriteLine($"MapSystemManager: Exited zone {zone.GetDisplayName()}");

        return _overworldReturnPosition;
    }

    /// <summary>
    /// Check if player is currently in a map zone.
    /// </summary>
    public bool IsInZone => CurrentZone != null;

    /// <summary>
    /// Get zone modifiers for enemy scaling.
    /// </summary>
    public ZoneModifiers? GetCurrentZoneModifiers()
    {
        if (CurrentZone == null) return null;

        return new ZoneModifiers
        {
            MonsterLevel = CurrentZone.MonsterLevel,
            PhysicalDamageMultiplier = CurrentZone.MonsterPhysicalDamage,
            ElementalDamageMultiplier = CurrentZone.MonsterElementalDamage,
            AttackSpeedMultiplier = CurrentZone.MonsterAttackSpeed,
            MovementSpeedMultiplier = CurrentZone.MonsterMovementSpeed,
            LifeMultiplier = CurrentZone.MonsterLife,
            CritChanceMultiplier = CurrentZone.MonsterCritChance,
            PhysicalReflect = CurrentZone.PhysicalReflect,
            ElementalReflect = CurrentZone.ElementalReflect,
            ExtraPacks = CurrentZone.ExtraPacks,
            HasAdditionalBoss = CurrentZone.HasAdditionalBoss,
        };
    }

    /// <summary>
    /// Get loot modifiers for the current zone.
    /// </summary>
    public LootModifiers? GetCurrentLootModifiers()
    {
        if (CurrentZone == null) return null;

        return new LootModifiers
        {
            ItemQuantityBonus = CurrentZone.ItemQuantity,
            ItemRarityBonus = CurrentZone.ItemRarity,
            ExperienceMultiplier = CurrentZone.ExperienceMultiplier,
            BossDropMultiplier = CurrentZone.BossDropMultiplier,
        };
    }

    /// <summary>
    /// Record a kill in the current zone.
    /// </summary>
    public void RecordKill(bool isBoss = false)
    {
        CurrentZone?.RecordKill(isBoss);

        if (isBoss && CurrentZone != null)
        {
            System.Diagnostics.Debug.WriteLine($"MapSystemManager: Boss killed in {CurrentZone.GetDisplayName()}!");

            // Generate completion drops
            var lootMods = GetCurrentLootModifiers();
            var drops = MapDropTable.GenerateZoneCompletionDrops(
                CurrentZone.Tier,
                lootMods?.ItemQuantityBonus ?? 0,
                lootMods?.ItemRarityBonus ?? 0,
                true
            );

            foreach (var map in drops)
            {
                OnMapDropped?.Invoke(map);
            }
        }
    }

    #endregion

    #region Map/Currency Drops

    /// <summary>
    /// Try to generate a map drop from a source.
    /// </summary>
    public MapItem? TryGenerateMapDrop(MapDropConfig config, int baseTier = 1)
    {
        var lootMods = GetCurrentLootModifiers();
        float iiq = lootMods?.ItemQuantityBonus ?? 0;
        float iir = lootMods?.ItemRarityBonus ?? 0;

        var map = MapDropTable.TryDrop(config, baseTier, iiq, iir);

        if (map != null)
        {
            OnMapDropped?.Invoke(map);
        }

        return map;
    }

    /// <summary>
    /// Try to generate a currency drop.
    /// </summary>
    public MapCurrency? TryGenerateCurrencyDrop(float dropChance = 2f)
    {
        var lootMods = GetCurrentLootModifiers();
        float iiq = lootMods?.ItemQuantityBonus ?? 0;

        var currency = MapDropTable.TryDropCurrency(dropChance, iiq);

        if (currency.HasValue)
        {
            OnCurrencyDropped?.Invoke(currency.Value);
        }

        return currency;
    }

    /// <summary>
    /// Add a dropped map to player inventory.
    /// </summary>
    public bool AddMapToInventory(MapItem map)
    {
        var itemType = MapItemTypeExtensions.GetMapItemType(map.Tier);
        int slot = _playerInventory.FindEmptySlot();

        if (slot < 0)
        {
            System.Diagnostics.Debug.WriteLine("MapSystemManager: Inventory full, cannot add map");
            return false;
        }

        _playerInventory.AddItem(itemType, 1);
        MapStorage.StoreMap(slot, map);

        System.Diagnostics.Debug.WriteLine($"MapSystemManager: Added {map.GetDisplayName()} to inventory slot {slot}");
        return true;
    }

    /// <summary>
    /// Add currency to player inventory.
    /// </summary>
    public bool AddCurrencyToInventory(MapCurrency currency, int count = 1)
    {
        var itemType = currency.ToItemType();
        int added = _playerInventory.AddItem(itemType, count);
        return added > 0;
    }

    #endregion

    #region Event Handlers

    private void HandleZoneCreated(MapZone zone)
    {
        _activeZones.Add(zone);
        System.Diagnostics.Debug.WriteLine($"MapSystemManager: Zone created - {zone.GetDisplayName()}");
    }

    private void HandleZoneClosed(MapZone zone)
    {
        _activeZones.Remove(zone);

        if (CurrentZone == zone)
        {
            CurrentZone = null;
        }

        System.Diagnostics.Debug.WriteLine($"MapSystemManager: Zone closed - {zone.GetDisplayName()}");
    }

    private void HandleZoneActivated(MapZone zone)
    {
        System.Diagnostics.Debug.WriteLine($"MapSystemManager: Zone activated - {zone.GetDisplayName()}");
    }

    private void HandleUIClose()
    {
        System.Diagnostics.Debug.WriteLine("MapSystemManager: Device UI closed");
    }

    #endregion

    #region Serialization

    public void SaveTo(BinaryWriter writer)
    {
        // Save map storage
        MapStorage.SaveTo(writer);

        // Save devices
        writer.Write(_devices.Count);
        foreach (var device in _devices)
        {
            device.SaveTo(writer);
        }

        // Save fusion devices
        writer.Write(_fusionDevices.Count);
        foreach (var device in _fusionDevices)
        {
            writer.Write(device.Position.X);
            writer.Write(device.Position.Y);
        }

        // Save current zone state
        writer.Write(CurrentZone != null);
        if (CurrentZone != null)
        {
            CurrentZone.SaveTo(writer);
            writer.Write(_overworldReturnPosition.X);
            writer.Write(_overworldReturnPosition.Y);
        }
    }

    public void LoadFrom(BinaryReader reader)
    {
        // Load map storage
        var storage = MapItemStorage.LoadFrom(reader);
        // Copy data to our storage
        foreach (var (slot, map) in storage.GetAllMaps())
        {
            MapStorage.StoreMap(slot, map);
        }

        // Load devices
        _devices.Clear();
        int deviceCount = reader.ReadInt32();
        for (int i = 0; i < deviceCount; i++)
        {
            var device = MapDevice.LoadFrom(reader);
            device.OnZoneCreated += HandleZoneCreated;
            device.OnZoneClosed += HandleZoneClosed;
            _devices.Add(device);

            if (device.ActiveZone != null)
            {
                _activeZones.Add(device.ActiveZone);
            }
        }

        // Load fusion devices
        _fusionDevices.Clear();
        int fusionCount = reader.ReadInt32();
        for (int i = 0; i < fusionCount; i++)
        {
            var position = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _fusionDevices.Add(new MapFusionDevice { Position = position });
        }

        // Load current zone state
        if (reader.ReadBoolean())
        {
            CurrentZone = MapZone.LoadFrom(reader);
            _overworldReturnPosition = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _activeZones.Add(CurrentZone);
        }
    }

    #endregion

    /// <summary>
    /// Get all active devices.
    /// </summary>
    public IEnumerable<MapDevice> GetDevices() => _devices;

    /// <summary>
    /// Get all fusion devices.
    /// </summary>
    public IEnumerable<MapFusionDevice> GetFusionDevices() => _fusionDevices;

    /// <summary>
    /// Get all active zones.
    /// </summary>
    public IEnumerable<MapZone> GetActiveZones() => _activeZones;
}

/// <summary>
/// Modifiers applied to monsters in a map zone.
/// </summary>
public class ZoneModifiers
{
    public int MonsterLevel { get; init; }
    public float PhysicalDamageMultiplier { get; init; } = 1f;
    public float ElementalDamageMultiplier { get; init; } = 1f;
    public float AttackSpeedMultiplier { get; init; } = 1f;
    public float MovementSpeedMultiplier { get; init; } = 1f;
    public float LifeMultiplier { get; init; } = 1f;
    public float CritChanceMultiplier { get; init; } = 1f;
    public float PhysicalReflect { get; init; }
    public float ElementalReflect { get; init; }
    public int ExtraPacks { get; init; }
    public bool HasAdditionalBoss { get; init; }
}

/// <summary>
/// Modifiers applied to loot in a map zone.
/// </summary>
public class LootModifiers
{
    public float ItemQuantityBonus { get; init; }
    public float ItemRarityBonus { get; init; }
    public float ExperienceMultiplier { get; init; } = 1f;
    public float BossDropMultiplier { get; init; } = 1f;
}