namespace Terrascent.Combat;

/// <summary>
/// Static properties for a weapon definition.
/// </summary>
public readonly struct WeaponData
{
    /// <summary>Display name.</summary>
    public string Name { get; init; }

    /// <summary>Weapon category.</summary>
    public WeaponType WeaponType { get; init; }

    /// <summary>Base damage before modifiers.</summary>
    public int BaseDamage { get; init; }

    /// <summary>Base attack range in pixels.</summary>
    public float BaseRange { get; init; }

    /// <summary>Base attack speed (attacks per second).</summary>
    public float AttackSpeed { get; init; }

    /// <summary>Base knockback force.</summary>
    public float Knockback { get; init; }

    /// <summary>Rarity tier (affects glow, value, etc).</summary>
    public Items.ItemRarity Rarity { get; init; }

    /// <summary>Sell value in copper coins.</summary>
    public int SellValue { get; init; }

    /// <summary>Flavor text/description.</summary>
    public string Description { get; init; }

    public static WeaponData Default => new()
    {
        Name = "Unknown Weapon",
        WeaponType = WeaponType.Sword,
        BaseDamage = 10,
        BaseRange = 40f,
        AttackSpeed = 1.5f,
        Knockback = 100f,
        Rarity = Items.ItemRarity.Common,
        SellValue = 0,
        Description = "",
    };
}

/// <summary>
/// Registry of all weapon definitions, indexed by ItemType.
/// </summary>
public static class WeaponRegistry
{
    private static readonly Dictionary<Items.ItemType, WeaponData> _weapons = new()
    {
        // ===== SWORDS =====
        [Items.ItemType.WoodSword] = new WeaponData
        {
            Name = "Wooden Sword",
            WeaponType = WeaponType.Sword,
            BaseDamage = 8,
            BaseRange = 40f,
            AttackSpeed = 1.5f,
            Knockback = 80f,
            Rarity = Items.ItemRarity.Common,
            SellValue = 20,
            Description = "A simple wooden training sword.",
        },

        [Items.ItemType.CopperSword] = new WeaponData
        {
            Name = "Copper Sword",
            WeaponType = WeaponType.Sword,
            BaseDamage = 12,
            BaseRange = 44f,
            AttackSpeed = 1.6f,
            Knockback = 100f,
            Rarity = Items.ItemRarity.Common,
            SellValue = 100,
            Description = "A sturdy copper blade.",
        },

        [Items.ItemType.IronSword] = new WeaponData
        {
            Name = "Iron Sword",
            WeaponType = WeaponType.Sword,
            BaseDamage = 18,
            BaseRange = 48f,
            AttackSpeed = 1.5f,
            Knockback = 120f,
            Rarity = Items.ItemRarity.Common,
            SellValue = 250,
            Description = "A reliable iron sword.",
        },

        // ===== ADDITIONAL SWORDS =====
        [Items.ItemType.SilverSword] = new WeaponData
        {
            Name = "Silver Sword",
            WeaponType = WeaponType.Sword,
            BaseDamage = 24,
            BaseRange = 50f,
            AttackSpeed = 1.5f,
            Knockback = 130f,
            Rarity = Items.ItemRarity.Uncommon,
            SellValue = 500,
            Description = "A gleaming silver blade.",
        },

        [Items.ItemType.GoldSword] = new WeaponData
        {
            Name = "Gold Sword",
            WeaponType = WeaponType.Sword,
            BaseDamage = 28,
            BaseRange = 52f,
            AttackSpeed = 1.4f,
            Knockback = 140f,
            Rarity = Items.ItemRarity.Uncommon,
            SellValue = 800,
            Description = "An ornate golden sword.",
        },

        // ===== SPEARS =====
        [Items.ItemType.WoodSpear] = new WeaponData
        {
            Name = "Wooden Spear",
            WeaponType = WeaponType.Spear,
            BaseDamage = 7,
            BaseRange = 56f,
            AttackSpeed = 1.3f,
            Knockback = 100f,
            Rarity = Items.ItemRarity.Common,
            SellValue = 15,
            Description = "A simple wooden spear with good reach.",
        },

        [Items.ItemType.CopperSpear] = new WeaponData
        {
            Name = "Copper Spear",
            WeaponType = WeaponType.Spear,
            BaseDamage = 11,
            BaseRange = 60f,
            AttackSpeed = 1.4f,
            Knockback = 120f,
            Rarity = Items.ItemRarity.Common,
            SellValue = 80,
            Description = "A copper-tipped spear.",
        },

        [Items.ItemType.IronSpear] = new WeaponData
        {
            Name = "Iron Spear",
            WeaponType = WeaponType.Spear,
            BaseDamage = 16,
            BaseRange = 64f,
            AttackSpeed = 1.4f,
            Knockback = 140f,
            Rarity = Items.ItemRarity.Common,
            SellValue = 200,
            Description = "A sturdy iron spear.",
        },

        // ===== AXES =====
        [Items.ItemType.BattleAxe] = new WeaponData
        {
            Name = "Battle Axe",
            WeaponType = WeaponType.Axe,
            BaseDamage = 12,
            BaseRange = 36f,
            AttackSpeed = 0.9f,
            Knockback = 180f,
            Rarity = Items.ItemRarity.Common,
            SellValue = 25,
            Description = "A heavy axe meant for combat.",
        },

        // ===== BOWS =====
        [Items.ItemType.WoodBow] = new WeaponData
        {
            Name = "Wooden Bow",
            WeaponType = WeaponType.Bow,
            BaseDamage = 6,
            BaseRange = 200f,
            AttackSpeed = 1.2f,
            Knockback = 40f,
            Rarity = Items.ItemRarity.Common,
            SellValue = 20,
            Description = "A simple wooden bow.",
        },

        [Items.ItemType.CopperBow] = new WeaponData
        {
            Name = "Copper Bow",
            WeaponType = WeaponType.Bow,
            BaseDamage = 10,
            BaseRange = 240f,
            AttackSpeed = 1.3f,
            Knockback = 60f,
            Rarity = Items.ItemRarity.Common,
            SellValue = 90,
            Description = "A bow with copper reinforcement.",
        },

        [Items.ItemType.IronBow] = new WeaponData
        {
            Name = "Iron Bow",
            WeaponType = WeaponType.Bow,
            BaseDamage = 14,
            BaseRange = 280f,
            AttackSpeed = 1.4f,
            Knockback = 80f,
            Rarity = Items.ItemRarity.Common,
            SellValue = 180,
            Description = "A powerful iron-reinforced bow.",
        },

        // ===== WHIPS =====
        [Items.ItemType.LeatherWhip] = new WeaponData
        {
            Name = "Leather Whip",
            WeaponType = WeaponType.Whip,
            BaseDamage = 8,
            BaseRange = 72f,
            AttackSpeed = 1.8f,
            Knockback = 60f,
            Rarity = Items.ItemRarity.Common,
            SellValue = 30,
            Description = "A flexible leather whip.",
        },

        [Items.ItemType.ChainWhip] = new WeaponData
        {
            Name = "Chain Whip",
            WeaponType = WeaponType.Whip,
            BaseDamage = 14,
            BaseRange = 80f,
            AttackSpeed = 1.6f,
            Knockback = 100f,
            Rarity = Items.ItemRarity.Uncommon,
            SellValue = 150,
            Description = "A dangerous chain whip.",
        },

        // ===== STAVES =====
        [Items.ItemType.WoodStaff] = new WeaponData
        {
            Name = "Wooden Staff",
            WeaponType = WeaponType.Staff,
            BaseDamage = 5,
            BaseRange = 120f,
            AttackSpeed = 1.0f,
            Knockback = 30f,
            Rarity = Items.ItemRarity.Common,
            SellValue = 25,
            Description = "A basic magic staff.",
        },

        [Items.ItemType.ApprenticeStaff] = new WeaponData
        {
            Name = "Apprentice Staff",
            WeaponType = WeaponType.Staff,
            BaseDamage = 10,
            BaseRange = 150f,
            AttackSpeed = 1.2f,
            Knockback = 50f,
            Rarity = Items.ItemRarity.Uncommon,
            SellValue = 120,
            Description = "A staff for aspiring mages.",
        },

        [Items.ItemType.MageStaff] = new WeaponData
        {
            Name = "Mage Staff",
            WeaponType = WeaponType.Staff,
            BaseDamage = 18,
            BaseRange = 180f,
            AttackSpeed = 1.4f,
            Knockback = 80f,
            Rarity = Items.ItemRarity.Rare,
            SellValue = 350,
            Description = "A powerful mage's staff.",
        },

        // ===== GLOVES =====
        [Items.ItemType.LeatherGloves] = new WeaponData
        {
            Name = "Leather Gloves",
            WeaponType = WeaponType.Glove,
            BaseDamage = 5,
            BaseRange = 24f,
            AttackSpeed = 3.0f,
            Knockback = 20f,
            Rarity = Items.ItemRarity.Common,
            SellValue = 20,
            Description = "Simple fighting gloves.",
        },

        [Items.ItemType.IronKnuckles] = new WeaponData
        {
            Name = "Iron Knuckles",
            WeaponType = WeaponType.Glove,
            BaseDamage = 10,
            BaseRange = 28f,
            AttackSpeed = 2.8f,
            Knockback = 60f,
            Rarity = Items.ItemRarity.Common,
            SellValue = 100,
            Description = "Iron-plated fighting gloves.",
        },

        // ===== BOOMERANGS =====
        [Items.ItemType.WoodBoomerang] = new WeaponData
        {
            Name = "Wooden Boomerang",
            WeaponType = WeaponType.Boomerang,
            BaseDamage = 7,
            BaseRange = 160f,
            AttackSpeed = 0.8f,
            Knockback = 50f,
            Rarity = Items.ItemRarity.Common,
            SellValue = 30,
            Description = "A returning wooden projectile.",
        },

        [Items.ItemType.IronBoomerang] = new WeaponData
        {
            Name = "Iron Boomerang",
            WeaponType = WeaponType.Boomerang,
            BaseDamage = 13,
            BaseRange = 200f,
            AttackSpeed = 0.9f,
            Knockback = 80f,
            Rarity = Items.ItemRarity.Common,
            SellValue = 150,
            Description = "A deadly iron boomerang.",
        },
    };

    /// <summary>
    /// Get weapon data for an item type.
    /// </summary>
    public static WeaponData Get(Items.ItemType type)
    {
        return _weapons.TryGetValue(type, out var data) ? data : WeaponData.Default;
    }

    /// <summary>
    /// Check if an item type is a weapon.
    /// </summary>
    public static bool IsWeapon(Items.ItemType type)
    {
        return _weapons.ContainsKey(type);
    }

    /// <summary>
    /// Get all registered weapon types.
    /// </summary>
    public static IEnumerable<Items.ItemType> GetAllWeaponTypes()
    {
        return _weapons.Keys;
    }
}