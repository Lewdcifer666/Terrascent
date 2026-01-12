using Microsoft.Xna.Framework;
using Terrascent.World.Biomes;

namespace Terrascent.Maps;

/// <summary>
/// State of a map zone.
/// </summary>
public enum MapZoneState
{
    /// <summary>Zone is generating.</summary>
    Generating,

    /// <summary>Zone is active and playable.</summary>
    Active,

    /// <summary>Boss has been killed, zone completing.</summary>
    Completing,

    /// <summary>Zone is completed (can still exit).</summary>
    Completed,

    /// <summary>Zone has closed (all portals used or expired).</summary>
    Closed
}

/// <summary>
/// An active map zone instance with applied modifiers.
/// </summary>
public class MapZone
{
    /// <summary>Unique identifier for this zone instance.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>The map item that created this zone.</summary>
    public MapItem SourceMap { get; }

    /// <summary>Current state of the zone.</summary>
    public MapZoneState State { get; private set; } = MapZoneState.Generating;

    /// <summary>Zone generation seed.</summary>
    public int ZoneSeed => SourceMap.ZoneSeed;

    /// <summary>Primary biome of the zone.</summary>
    public BiomeType PrimaryBiome => SourceMap.BiomeType;

    /// <summary>Secondary biome (for fusion maps).</summary>
    public BiomeType? SecondaryBiome => SourceMap.SecondaryBiome;

    /// <summary>Whether this is a fusion zone.</summary>
    public bool IsFusion => SourceMap.IsFusion;

    /// <summary>Monster level in this zone.</summary>
    public int MonsterLevel => SourceMap.MonsterLevel;

    /// <summary>Map tier.</summary>
    public int Tier => SourceMap.Tier;

    // === Portal Management ===

    /// <summary>Maximum number of portals for this zone.</summary>
    public int MaxPortals { get; set; } = 6;

    /// <summary>Number of portal uses remaining.</summary>
    public int PortalsRemaining { get; private set; }

    /// <summary>List of portals for this zone.</summary>
    public List<MapPortal> Portals { get; } = new();

    /// <summary>Exit portal position within the zone.</summary>
    public Vector2 ExitPortalPosition { get; set; }

    // === Stat Modifiers ===

    /// <summary>Monster physical damage multiplier.</summary>
    public float MonsterPhysicalDamage { get; private set; } = 1f;

    /// <summary>Monster elemental damage multiplier.</summary>
    public float MonsterElementalDamage { get; private set; } = 1f;

    /// <summary>Monster attack speed multiplier.</summary>
    public float MonsterAttackSpeed { get; private set; } = 1f;

    /// <summary>Monster movement speed multiplier.</summary>
    public float MonsterMovementSpeed { get; private set; } = 1f;

    /// <summary>Monster life multiplier.</summary>
    public float MonsterLife { get; private set; } = 1f;

    /// <summary>Monster crit chance multiplier.</summary>
    public float MonsterCritChance { get; private set; } = 1f;

    /// <summary>Physical reflect percentage.</summary>
    public float PhysicalReflect { get; private set; } = 0f;

    /// <summary>Elemental reflect percentage.</summary>
    public float ElementalReflect { get; private set; } = 0f;

    /// <summary>Extra monster packs.</summary>
    public int ExtraPacks { get; private set; } = 0;

    /// <summary>Monster ailment avoidance chance.</summary>
    public float AilmentAvoidance { get; private set; } = 0f;

    /// <summary>Extra projectile chains for monsters.</summary>
    public int ChainedAttacks { get; private set; } = 0;

    /// <summary>Monster accuracy multiplier.</summary>
    public float MonsterAccuracy { get; private set; } = 1f;

    // === Player Debuffs ===

    /// <summary>Player armour reduction percentage.</summary>
    public float ReducedArmour { get; private set; } = 0f;

    /// <summary>Player max resistance reduction.</summary>
    public float ReducedResistances { get; private set; } = 0f;

    /// <summary>Whether player can regenerate life.</summary>
    public bool CanRegenerate { get; private set; } = true;

    /// <summary>Player recovery rate reduction percentage.</summary>
    public float ReducedRecovery { get; private set; } = 0f;

    /// <summary>Player movement speed reduction percentage.</summary>
    public float PlayerSlowed { get; private set; } = 0f;

    /// <summary>Player cooldown recovery reduction percentage.</summary>
    public float ReducedCooldownRecovery { get; private set; } = 0f;

    // === Reward Modifiers ===

    /// <summary>Item quantity bonus percentage.</summary>
    public float ItemQuantity => SourceMap.ItemQuantity;

    /// <summary>Item rarity bonus percentage.</summary>
    public float ItemRarity => SourceMap.ItemRarity;

    /// <summary>Pack size bonus percentage.</summary>
    public float PackSize => SourceMap.PackSize;

    /// <summary>Experience multiplier.</summary>
    public float ExperienceMultiplier => SourceMap.ExperienceMultiplier;

    /// <summary>Whether zone has additional boss.</summary>
    public bool HasAdditionalBoss { get; private set; } = false;

    /// <summary>Boss item drop multiplier.</summary>
    public float BossDropMultiplier { get; private set; } = 1f;

    // === Zone State ===

    /// <summary>Whether the main boss has been killed.</summary>
    public bool BossKilled { get; private set; }

    /// <summary>Kill count in this zone.</summary>
    public int KillCount { get; private set; }

    /// <summary>Time spent in zone.</summary>
    public float TimeInZone { get; private set; }

    /// <summary>Zone creation time.</summary>
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Create a new map zone from a map item.
    /// </summary>
    public MapZone(MapItem map)
    {
        SourceMap = map;
        PortalsRemaining = MaxPortals;
        ApplyAffixes();
    }

    /// <summary>
    /// Apply all affixes from the source map to zone modifiers.
    /// </summary>
    private void ApplyAffixes()
    {
        foreach (var affix in SourceMap.AllAffixes)
        {
            ApplyAffix(affix);
        }
    }

    /// <summary>
    /// Apply a single affix to zone modifiers.
    /// </summary>
    private void ApplyAffix(MapAffixInstance affix)
    {
        switch (affix.Type)
        {
            // Monster modifiers
            case MapAffixType.MonsterPhysicalDamage:
                MonsterPhysicalDamage += affix.Value / 100f;
                break;
            case MapAffixType.MonsterElementalDamage:
                MonsterElementalDamage += affix.Value / 100f;
                break;
            case MapAffixType.MonsterAttackSpeed:
                MonsterAttackSpeed += affix.Value / 100f;
                break;
            case MapAffixType.MonsterMovementSpeed:
                MonsterMovementSpeed += affix.Value / 100f;
                break;
            case MapAffixType.MonsterLife:
                MonsterLife += affix.Value / 100f;
                break;
            case MapAffixType.MonsterCritChance:
                MonsterCritChance += affix.Value / 100f;
                break;
            case MapAffixType.MonsterAccuracy:
                MonsterAccuracy += affix.Value / 100f;
                break;
            case MapAffixType.PhysicalReflect:
                PhysicalReflect += affix.Value;
                break;
            case MapAffixType.ElementalReflect:
                ElementalReflect += affix.Value;
                break;
            case MapAffixType.ExtraPacks:
                ExtraPacks += (int)affix.Value;
                break;
            case MapAffixType.AilmentAvoidance:
                AilmentAvoidance += affix.Value;
                break;
            case MapAffixType.ChainedAttacks:
                ChainedAttacks += (int)affix.Value;
                break;

            // Player debuffs
            case MapAffixType.ReducedArmour:
                ReducedArmour += affix.Value;
                break;
            case MapAffixType.ReducedResistances:
                ReducedResistances += affix.Value;
                break;
            case MapAffixType.NoRegeneration:
                CanRegenerate = false;
                break;
            case MapAffixType.ReducedRecovery:
                ReducedRecovery += affix.Value;
                break;
            case MapAffixType.PlayerSlowed:
                PlayerSlowed += affix.Value;
                break;
            case MapAffixType.ReducedCooldownRecovery:
                ReducedCooldownRecovery += affix.Value;
                break;

            // Boss/reward modifiers
            case MapAffixType.AdditionalBoss:
                HasAdditionalBoss = true;
                break;
            case MapAffixType.BossDropsMore:
                BossDropMultiplier += affix.Value / 100f;
                break;
        }
    }

    /// <summary>
    /// Update zone state.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (State == MapZoneState.Active)
        {
            TimeInZone += deltaTime;

            // Update portals
            foreach (var portal in Portals)
            {
                portal.Update(deltaTime);
            }
        }
    }

    /// <summary>
    /// Activate the zone after generation.
    /// </summary>
    public void Activate()
    {
        State = MapZoneState.Active;
    }

    /// <summary>
    /// Record a kill in the zone.
    /// </summary>
    public void RecordKill(bool isBoss = false)
    {
        KillCount++;

        if (isBoss)
        {
            BossKilled = true;
            State = MapZoneState.Completing;
        }
    }

    /// <summary>
    /// Use a portal (decrements remaining count).
    /// </summary>
    public bool UsePortal()
    {
        if (PortalsRemaining <= 0) return false;

        PortalsRemaining--;

        if (PortalsRemaining <= 0)
        {
            // Close all portals when none remain
            foreach (var portal in Portals)
            {
                portal.Close();
            }
        }

        return true;
    }

    /// <summary>
    /// Complete the zone.
    /// </summary>
    public void Complete()
    {
        State = MapZoneState.Completed;
    }

    /// <summary>
    /// Close the zone (all portals used).
    /// </summary>
    public void Close()
    {
        State = MapZoneState.Closed;

        foreach (var portal in Portals)
        {
            portal.Close();
        }
    }

    /// <summary>
    /// Get modifier summary for UI display.
    /// </summary>
    public IEnumerable<string> GetModifierSummary()
    {
        yield return $"Monster Level: {MonsterLevel}";
        yield return $"Tier: {MapTier.GetDisplayName(Tier)}";

        if (MonsterPhysicalDamage > 1f)
            yield return $"Monsters deal {(MonsterPhysicalDamage - 1f) * 100:F0}% increased Physical Damage";
        if (MonsterElementalDamage > 1f)
            yield return $"Monsters deal {(MonsterElementalDamage - 1f) * 100:F0}% increased Elemental Damage";
        if (MonsterLife > 1f)
            yield return $"Monsters have {(MonsterLife - 1f) * 100:F0}% increased Life";
        if (PhysicalReflect > 0)
            yield return $"Monsters reflect {PhysicalReflect:F0}% Physical Damage";
        if (ElementalReflect > 0)
            yield return $"Monsters reflect {ElementalReflect:F0}% Elemental Damage";
        if (!CanRegenerate)
            yield return "Players cannot Regenerate Life";
        if (ReducedResistances > 0)
            yield return $"Players have -{ReducedResistances:F0}% to Maximum Resistances";
        if (HasAdditionalBoss)
            yield return "Area contains an additional Unique Boss";

        yield return $"Item Quantity: +{ItemQuantity:F0}%";
        yield return $"Item Rarity: +{ItemRarity:F0}%";
    }

    /// <summary>
    /// Get zone display name.
    /// </summary>
    public string GetDisplayName()
    {
        return SourceMap.GetDisplayName();
    }

    /// <summary>
    /// Get zone display color.
    /// </summary>
    public Color GetDisplayColor()
    {
        return SourceMap.GetDisplayColor();
    }

    #region Serialization

    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(Id.ToByteArray());
        SourceMap.SaveTo(writer);
        writer.Write((int)State);
        writer.Write(PortalsRemaining);
        writer.Write(BossKilled);
        writer.Write(KillCount);
        writer.Write(TimeInZone);
        writer.Write(ExitPortalPosition.X);
        writer.Write(ExitPortalPosition.Y);

        // Save portals
        writer.Write(Portals.Count);
        foreach (var portal in Portals)
        {
            portal.SaveTo(writer);
        }
    }

    public static MapZone LoadFrom(BinaryReader reader)
    {
        var id = new Guid(reader.ReadBytes(16));
        var map = MapItem.LoadFrom(reader);
        var zone = new MapZone(map);

        // Use reflection or property setter to set Id if needed
        // For now, we'll just load the rest

        zone.State = (MapZoneState)reader.ReadInt32();
        zone.PortalsRemaining = reader.ReadInt32();
        zone.BossKilled = reader.ReadBoolean();
        zone.KillCount = reader.ReadInt32();
        zone.TimeInZone = reader.ReadSingle();
        zone.ExitPortalPosition = new Vector2(reader.ReadSingle(), reader.ReadSingle());

        int portalCount = reader.ReadInt32();
        for (int i = 0; i < portalCount; i++)
        {
            zone.Portals.Add(MapPortal.LoadFrom(reader));
        }

        return zone;
    }

    #endregion

    // Property setters for serialization (private set doesn't work with LoadFrom)
    private MapZoneState _state;
    private int _portalsRemaining;
    private bool _bossKilled;
    private int _killCount;
    private float _timeInZone;
}