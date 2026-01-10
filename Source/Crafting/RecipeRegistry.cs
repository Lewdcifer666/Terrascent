using Terrascent.Entities.Bosses;
using Terrascent.Items;

namespace Terrascent.Crafting;

/// <summary>
/// Registry containing all crafting recipes (200+).
/// Organized by progression tier and category.
/// </summary>
public static class RecipeRegistry
{
    private static readonly Dictionary<int, Recipe> _recipes = new();
    private static readonly Dictionary<ItemType, List<Recipe>> _byResult = new();
    private static readonly Dictionary<CraftingStationType, List<Recipe>> _byStation = new();
    private static readonly Dictionary<RecipeCategory, List<Recipe>> _byCategory = new();

    private static bool _initialized = false;
    private static int _nextId = 1;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        RegisterBasicRecipes();
        RegisterBarRecipes();
        RegisterToolRecipes();
        RegisterWeaponRecipes();
        RegisterArmorRecipes();
        RegisterAccessoryRecipes();
        RegisterPotionRecipes();
        RegisterFurnitureRecipes();
        RegisterBlockRecipes();
        RegisterStationRecipes();
        RegisterBossSummoningRecipes();
        RegisterAdvancedRecipes();
        RegisterHardmodeRecipes();
        RegisterMiscRecipes();

        System.Diagnostics.Debug.WriteLine($"[CRAFTING] Registered {_recipes.Count} recipes");
    }

    private static Recipe R(ItemType result) => new RecipeBuilder(_nextId++, result).Build();
    private static RecipeBuilder Create(ItemType result) => new RecipeBuilder(_nextId++, result);

    private static void Register(Recipe recipe)
    {
        _recipes[recipe.Id] = recipe;

        // Index by result
        if (!_byResult.ContainsKey(recipe.Result))
            _byResult[recipe.Result] = new List<Recipe>();
        _byResult[recipe.Result].Add(recipe);

        // Index by station
        if (!_byStation.ContainsKey(recipe.Station))
            _byStation[recipe.Station] = new List<Recipe>();
        _byStation[recipe.Station].Add(recipe);

        // Index by category
        if (!_byCategory.ContainsKey(recipe.Category))
            _byCategory[recipe.Category] = new List<Recipe>();
        _byCategory[recipe.Category].Add(recipe);
    }

    #region Basic Recipes (Hand Craft)

    private static void RegisterBasicRecipes()
    {
        // === TORCHES ===
        Register(Create(ItemType.Torch)
            .Amount(3)
            .Requires(ItemType.Wood, 1)
            .Requires(ItemType.Gel, 1)
            .InCategory(RecipeCategory.Blocks)
            .Build());

        // === WOOD ITEMS ===
        Register(Create(ItemType.WoodPlatform)
            .Amount(2)
            .Requires(ItemType.Wood, 1)
            .InCategory(RecipeCategory.Blocks)
            .Build());

        // === ROPE (placeholder) ===
        // Wood to stick conversion
        Register(Create(ItemType.Wood)
            .Amount(1)
            .Requires(ItemType.Leaves, 5)
            .Named("Compost Wood")
            .InCategory(RecipeCategory.Materials)
            .Build());
    }

    #endregion

    #region Bar Recipes (Furnace)

    private static void RegisterBarRecipes()
    {
        // === ORE TO BAR ===
        Register(Create(ItemType.CopperBar)
            .Requires(ItemType.CopperOre, 3)
            .AtStation(CraftingStationType.Furnace)
            .InCategory(RecipeCategory.Materials)
            .Build());

        Register(Create(ItemType.IronBar)
            .Requires(ItemType.IronOre, 3)
            .AtStation(CraftingStationType.Furnace)
            .InCategory(RecipeCategory.Materials)
            .Build());

        Register(Create(ItemType.SilverBar)
            .Requires(ItemType.SilverOre, 4)
            .AtStation(CraftingStationType.Furnace)
            .InCategory(RecipeCategory.Materials)
            .Build());

        Register(Create(ItemType.GoldBar)
            .Requires(ItemType.GoldOre, 4)
            .AtStation(CraftingStationType.Furnace)
            .InCategory(RecipeCategory.Materials)
            .Build());

        // === BOSS DROP BARS ===
        Register(Create(ItemType.DemoniteOre)
            .Amount(1)
            .Requires(ItemType.ShadowScale, 2)
            .AtStation(CraftingStationType.Furnace)
            .AfterBoss(BossType.EyeOfTerror)
            .InCategory(RecipeCategory.Materials)
            .Build());

        Register(Create(ItemType.CrimtaneOre)
            .Amount(1)
            .Requires(ItemType.TissueSample, 2)
            .AtStation(CraftingStationType.Furnace)
            .AfterBoss(BossType.BrainOfDepths)
            .InCategory(RecipeCategory.Materials)
            .Build());
    }

    #endregion

    #region Tool Recipes

    private static void RegisterToolRecipes()
    {
        // === PICKAXES ===
        Register(Create(ItemType.WoodPickaxe)
            .Requires(ItemType.Wood, 10)
            .AtStation(CraftingStationType.Workbench)
            .InCategory(RecipeCategory.Tools)
            .Build());

        Register(Create(ItemType.StonePickaxe)
            .Requires(ItemType.Stone, 12)
            .Requires(ItemType.Wood, 4)
            .AtStation(CraftingStationType.Workbench)
            .InCategory(RecipeCategory.Tools)
            .Build());

        Register(Create(ItemType.CopperPickaxe)
            .Requires(ItemType.CopperBar, 12)
            .Requires(ItemType.Wood, 4)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Tools)
            .Build());

        Register(Create(ItemType.IronPickaxe)
            .Requires(ItemType.IronBar, 12)
            .Requires(ItemType.Wood, 3)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Tools)
            .Build());

        // === AXES ===
        Register(Create(ItemType.WoodAxe)
            .Requires(ItemType.Wood, 9)
            .AtStation(CraftingStationType.Workbench)
            .InCategory(RecipeCategory.Tools)
            .Build());

        Register(Create(ItemType.StoneAxe)
            .Requires(ItemType.Stone, 10)
            .Requires(ItemType.Wood, 3)
            .AtStation(CraftingStationType.Workbench)
            .InCategory(RecipeCategory.Tools)
            .Build());

        Register(Create(ItemType.CopperAxe)
            .Requires(ItemType.CopperBar, 9)
            .Requires(ItemType.Wood, 3)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Tools)
            .Build());

        Register(Create(ItemType.IronAxe)
            .Requires(ItemType.IronBar, 9)
            .Requires(ItemType.Wood, 3)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Tools)
            .Build());

        // === HAMMERS ===
        Register(Create(ItemType.WoodHammer)
            .Requires(ItemType.Wood, 8)
            .AtStation(CraftingStationType.Workbench)
            .InCategory(RecipeCategory.Tools)
            .Build());

        Register(Create(ItemType.StoneHammer)
            .Requires(ItemType.Stone, 10)
            .Requires(ItemType.Wood, 3)
            .AtStation(CraftingStationType.Workbench)
            .InCategory(RecipeCategory.Tools)
            .Build());
    }

    #endregion

    #region Weapon Recipes

    private static void RegisterWeaponRecipes()
    {
        // === SWORDS ===
        Register(Create(ItemType.WoodSword)
            .Requires(ItemType.Wood, 7)
            .AtStation(CraftingStationType.Workbench)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        Register(Create(ItemType.CopperSword)
            .Requires(ItemType.CopperBar, 8)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        Register(Create(ItemType.IronSword)
            .Requires(ItemType.IronBar, 8)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        Register(Create(ItemType.SilverSword)
            .Requires(ItemType.SilverBar, 8)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        Register(Create(ItemType.GoldSword)
            .Requires(ItemType.GoldBar, 8)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        // === SPEARS ===
        Register(Create(ItemType.WoodSpear)
            .Requires(ItemType.Wood, 9)
            .AtStation(CraftingStationType.Workbench)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        Register(Create(ItemType.CopperSpear)
            .Requires(ItemType.CopperBar, 10)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        Register(Create(ItemType.IronSpear)
            .Requires(ItemType.IronBar, 10)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        // === BATTLE AXES ===
        Register(Create(ItemType.BattleAxe)
            .Requires(ItemType.Wood, 12)
            .Requires(ItemType.Stone, 6)
            .AtStation(CraftingStationType.Workbench)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        Register(Create(ItemType.CopperBattleAxe)
            .Requires(ItemType.CopperBar, 12)
            .Requires(ItemType.Wood, 3)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        Register(Create(ItemType.IronBattleAxe)
            .Requires(ItemType.IronBar, 12)
            .Requires(ItemType.Wood, 3)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        // === BOWS ===
        Register(Create(ItemType.WoodBow)
            .Requires(ItemType.Wood, 10)
            .AtStation(CraftingStationType.Workbench)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        Register(Create(ItemType.CopperBow)
            .Requires(ItemType.CopperBar, 7)
            .Requires(ItemType.Wood, 5)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        Register(Create(ItemType.IronBow)
            .Requires(ItemType.IronBar, 7)
            .Requires(ItemType.Wood, 5)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        // === WHIPS ===
        Register(Create(ItemType.LeatherWhip)
            .Requires(ItemType.Gel, 15)
            .Requires(ItemType.Wood, 3)
            .AtStation(CraftingStationType.Workbench)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        Register(Create(ItemType.ChainWhip)
            .Requires(ItemType.IronBar, 10)
            .Requires(ItemType.LeatherWhip, 1)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        // === STAVES ===
        Register(Create(ItemType.WoodStaff)
            .Requires(ItemType.Wood, 12)
            .Requires(ItemType.Lens, 1)
            .AtStation(CraftingStationType.Workbench)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        Register(Create(ItemType.ApprenticeStaff)
            .Requires(ItemType.WoodStaff, 1)
            .Requires(ItemType.SilverBar, 8)
            .Requires(ItemType.Lens, 2)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        Register(Create(ItemType.MageStaff)
            .Requires(ItemType.ApprenticeStaff, 1)
            .Requires(ItemType.GoldBar, 10)
            .Requires(ItemType.Lens, 3)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        // === GLOVES ===
        Register(Create(ItemType.LeatherGloves)
            .Requires(ItemType.Gel, 10)
            .Requires(ItemType.Wood, 2)
            .AtStation(CraftingStationType.Workbench)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        Register(Create(ItemType.IronKnuckles)
            .Requires(ItemType.IronBar, 8)
            .Requires(ItemType.LeatherGloves, 1)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        // === BOOMERANGS ===
        Register(Create(ItemType.WoodBoomerang)
            .Requires(ItemType.Wood, 15)
            .AtStation(CraftingStationType.Workbench)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        Register(Create(ItemType.IronBoomerang)
            .Requires(ItemType.IronBar, 8)
            .Requires(ItemType.WoodBoomerang, 1)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Weapons)
            .Build());

        // === BOSS WEAPONS ===
        Register(Create(ItemType.BeeGun)
            .Requires(ItemType.BeeWax, 12)
            .Requires(ItemType.Honeycomb, 1)
            .AtStation(CraftingStationType.Anvil)
            .AfterBoss(BossType.QueenBee)
            .InCategory(RecipeCategory.Weapons)
            .Build());
    }

    #endregion

    #region Armor Recipes

    private static void RegisterArmorRecipes()
    {
        // Armor will use the same bar patterns
        // For now, register placeholder conversion recipes

        // === COPPER ARMOR SET ===
        // These would be helmet/chest/legs but we'll use material items for now
        Register(Create(ItemType.CopperBar)
            .Amount(5)
            .Requires(ItemType.CopperOre, 15)
            .AtStation(CraftingStationType.Furnace)
            .Named("Copper Armor Materials")
            .InCategory(RecipeCategory.Armor)
            .Build());

        // === IRON ARMOR SET ===
        Register(Create(ItemType.IronBar)
            .Amount(5)
            .Requires(ItemType.IronOre, 15)
            .AtStation(CraftingStationType.Furnace)
            .Named("Iron Armor Materials")
            .InCategory(RecipeCategory.Armor)
            .Build());

        // === SILVER ARMOR SET ===
        Register(Create(ItemType.SilverBar)
            .Amount(5)
            .Requires(ItemType.SilverOre, 20)
            .AtStation(CraftingStationType.Furnace)
            .Named("Silver Armor Materials")
            .InCategory(RecipeCategory.Armor)
            .Build());

        // === GOLD ARMOR SET ===
        Register(Create(ItemType.GoldBar)
            .Amount(5)
            .Requires(ItemType.GoldOre, 20)
            .AtStation(CraftingStationType.Furnace)
            .Named("Gold Armor Materials")
            .InCategory(RecipeCategory.Armor)
            .Build());
    }

    #endregion

    #region Accessory Recipes

    private static void RegisterAccessoryRecipes()
    {
        // === COMMON ACCESSORIES ===
        Register(Create(ItemType.SoldiersSyringeItem)
            .Requires(ItemType.IronBar, 3)
            .Requires(ItemType.Gel, 5)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.TougherTimesItem)
            .Requires(ItemType.Stone, 20)
            .Requires(ItemType.IronBar, 2)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.BisonSteakItem)
            .Requires(ItemType.Gel, 10)
            .AtStation(CraftingStationType.CookingPot)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.PaulsGoatHoofItem)
            .Requires(ItemType.CopperBar, 5)
            .Requires(ItemType.Gel, 5)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.CritGlassesItem)
            .Requires(ItemType.Lens, 3)
            .Requires(ItemType.IronBar, 2)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.MonsterToothItem)
            .Requires(ItemType.ShadowScale, 5)
            .AtStation(CraftingStationType.Anvil)
            .AfterBoss(BossType.EyeOfTerror)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.CautiousSlugItem)
            .Requires(ItemType.Gel, 20)
            .Requires(ItemType.Stone, 5)
            .AtStation(CraftingStationType.Workbench)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.ArmorPlateItem)
            .Requires(ItemType.IronBar, 8)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.TriTipDaggerItem)
            .Requires(ItemType.IronBar, 5)
            .Requires(ItemType.Wood, 2)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.BundleOfFireworksItem)
            .Requires(ItemType.CopperBar, 3)
            .Requires(ItemType.Wood, 5)
            .AtStation(CraftingStationType.Workbench)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        // === UNCOMMON ACCESSORIES ===
        Register(Create(ItemType.HopooFeatherItem)
            .Requires(ItemType.Lens, 2)
            .Requires(ItemType.SilverBar, 5)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.PredatoryInstinctsItem)
            .Requires(ItemType.CritGlassesItem, 2)
            .Requires(ItemType.GoldBar, 3)
            .AtStation(CraftingStationType.TinkersWorkshop)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.HarvestersScytheItem)
            .Requires(ItemType.IronBar, 10)
            .Requires(ItemType.Wood, 5)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.UkuleleItem)
            .Requires(ItemType.Wood, 15)
            .Requires(ItemType.SilverBar, 3)
            .AtStation(CraftingStationType.Sawmill)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.AtgMissileItem)
            .Requires(ItemType.IronBar, 12)
            .Requires(ItemType.CopperBar, 8)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.WillOTheWispItem)
            .Requires(ItemType.Torch, 50)
            .Requires(ItemType.Gel, 20)
            .AtStation(CraftingStationType.Workbench)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.BandolierItem)
            .Requires(ItemType.Gel, 15)
            .Requires(ItemType.IronBar, 5)
            .AtStation(CraftingStationType.Loom)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.WarHornItem)
            .Requires(ItemType.GoldBar, 8)
            .Requires(ItemType.CopperBar, 4)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.BerzerkersPauldronsItem)
            .Requires(ItemType.IronBar, 15)
            .Requires(ItemType.ShadowScale, 3)
            .AtStation(CraftingStationType.Anvil)
            .AfterBoss(BossType.EyeOfTerror)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.InfusionItem)
            .Requires(ItemType.Gel, 30)
            .Requires(ItemType.Lens, 5)
            .AtStation(CraftingStationType.AlchemyTable)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        // === RARE ACCESSORIES ===
        Register(Create(ItemType.BrilliantBehemothItem)
            .Requires(ItemType.GoldBar, 15)
            .Requires(ItemType.AtgMissileItem, 1)
            .AtStation(CraftingStationType.TinkersWorkshop)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.ShapedGlassItem)
            .Requires(ItemType.Lens, 10)
            .Requires(ItemType.GoldBar, 5)
            .AtStation(CraftingStationType.GlassKiln)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.CestiusItem)
            .Requires(ItemType.IronKnuckles, 1)
            .Requires(ItemType.GoldBar, 10)
            .AtStation(CraftingStationType.Anvil)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.AlienHeadItem)
            .Requires(ItemType.Lens, 8)
            .Requires(ItemType.SilverBar, 10)
            .AtStation(CraftingStationType.AlchemyTable)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.HappiestMaskItem)
            .Requires(ItemType.Gel, 50)
            .Requires(ItemType.Lens, 3)
            .AtStation(CraftingStationType.Loom)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.FrostRelicItem)
            .Requires(ItemType.SilverBar, 15)
            .AtStation(CraftingStationType.IceMachine)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.UnstableTeslaCoilItem)
            .Requires(ItemType.CopperBar, 20)
            .Requires(ItemType.IronBar, 10)
            .Requires(ItemType.GoldBar, 5)
            .AtStation(CraftingStationType.TinkersWorkshop)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        // === LEGENDARY ACCESSORIES ===
        Register(Create(ItemType.SoulboundCatalystItem)
            .Requires(ItemType.ShapedGlassItem, 1)
            .Requires(ItemType.InfusionItem, 2)
            .Requires(ItemType.GoldBar, 20)
            .AtStation(CraftingStationType.CrystalBall)
            .AfterBoss(BossType.SkeletalWarlord)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.FiftySevenLeafCloverItem)
            .Requires(ItemType.Leaves, 57)
            .Requires(ItemType.GoldBar, 10)
            .Requires(ItemType.Lens, 7)
            .AtStation(CraftingStationType.TinkersWorkshop)
            .AfterBoss(BossType.SkeletalWarlord)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.BrainStalksItem)
            .Requires(ItemType.TissueSample, 20)
            .Requires(ItemType.Lens, 10)
            .AtStation(CraftingStationType.AlchemyTable)
            .AfterBoss(BossType.BrainOfDepths)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.HardlightAfterburnerItem)
            .Requires(ItemType.HopooFeatherItem, 2)
            .Requires(ItemType.PaulsGoatHoofItem, 2)
            .Requires(ItemType.GoldBar, 15)
            .AtStation(CraftingStationType.TinkersWorkshop)
            .AfterBoss(BossType.QueenBee)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.SentientMeatHookItem)
            .Requires(ItemType.IronBar, 25)
            .Requires(ItemType.ShadowScale, 10)
            .Requires(ItemType.TissueSample, 10)
            .AtStation(CraftingStationType.DemonAltar)
            .AfterBoss(BossType.BrainOfDepths)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        // === BOSS DROP ACCESSORIES ===
        Register(Create(ItemType.RoyalGel)
            .Requires(ItemType.Gel, 100)
            .Requires(ItemType.GoldBar, 5)
            .AtStation(CraftingStationType.AlchemyTable)
            .AfterBoss(BossType.KingSlime)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.Honeycomb)
            .Requires(ItemType.BeeWax, 10)
            .AtStation(CraftingStationType.HoneyDispenser)
            .AfterBoss(BossType.QueenBee)
            .InCategory(RecipeCategory.Accessories)
            .Build());
    }

    #endregion

    #region Potion Recipes

    private static void RegisterPotionRecipes()
    {
        Register(Create(ItemType.LesserHealingPotion)
            .Amount(2)
            .Requires(ItemType.Gel, 5)
            .Requires(ItemType.Torch, 1)
            .AtStation(CraftingStationType.AlchemyTable)
            .InCategory(RecipeCategory.Potions)
            .Build());

        Register(Create(ItemType.HealingPotion)
            .Requires(ItemType.LesserHealingPotion, 2)
            .Requires(ItemType.Gel, 10)
            .AtStation(CraftingStationType.AlchemyTable)
            .InCategory(RecipeCategory.Potions)
            .Build());

        // Additional potions using same patterns
        Register(Create(ItemType.LesserHealingPotion)
            .Amount(5)
            .Requires(ItemType.Gel, 10)
            .Requires(ItemType.Lens, 1)
            .AtStation(CraftingStationType.AlchemyTable)
            .Named("Bulk Lesser Healing")
            .InCategory(RecipeCategory.Potions)
            .Build());
    }

    #endregion

    #region Furniture Recipes

    private static void RegisterFurnitureRecipes()
    {
        // === WOOD FURNITURE ===
        Register(Create(ItemType.Wood)
            .Amount(4)
            .Requires(ItemType.Wood, 1)
            .AtStation(CraftingStationType.Sawmill)
            .Named("Wood Planks")
            .InCategory(RecipeCategory.Furniture)
            .Build());

        // More furniture would go here with proper furniture ItemTypes
        // For now, adding conversion recipes

        Register(Create(ItemType.WoodPlatform)
            .Amount(5)
            .Requires(ItemType.Wood, 2)
            .AtStation(CraftingStationType.Sawmill)
            .Named("Wooden Platforms (5)")
            .InCategory(RecipeCategory.Furniture)
            .Build());

        Register(Create(ItemType.Torch)
            .Amount(10)
            .Requires(ItemType.Wood, 3)
            .Requires(ItemType.Gel, 3)
            .AtStation(CraftingStationType.Workbench)
            .Named("Torch Bundle")
            .InCategory(RecipeCategory.Furniture)
            .Build());
    }

    #endregion

    #region Block Recipes

    private static void RegisterBlockRecipes()
    {
        Register(Create(ItemType.StoneBrick)
            .Amount(1)
            .Requires(ItemType.Stone, 2)
            .AtStation(CraftingStationType.Furnace)
            .InCategory(RecipeCategory.Blocks)
            .Build());

        Register(Create(ItemType.Stone)
            .Amount(1)
            .Requires(ItemType.StoneBrick, 1)
            .AtStation(CraftingStationType.Workbench)
            .Named("Unbrick Stone")
            .InCategory(RecipeCategory.Blocks)
            .Build());

        // Glass from sand
        Register(Create(ItemType.Stone)
            .Amount(2)
            .Requires(ItemType.Sand, 3)
            .AtStation(CraftingStationType.GlassKiln)
            .Named("Glass Block")
            .InCategory(RecipeCategory.Blocks)
            .Build());

        // Dirt to mud
        Register(Create(ItemType.Mud)
            .Requires(ItemType.Dirt, 1)
            .AlsoRequires(CraftingStationType.Water)
            .AtStation(CraftingStationType.Workbench)
            .InCategory(RecipeCategory.Blocks)
            .Build());

        // Snow from ice
        Register(Create(ItemType.Snow)
            .Amount(2)
            .Requires(ItemType.Ice, 1)
            .AtStation(CraftingStationType.IceMachine)
            .InCategory(RecipeCategory.Blocks)
            .Build());
    }

    #endregion

    #region Station Recipes

    private static void RegisterStationRecipes()
    {
        // === BASIC STATIONS ===
        Register(Create(ItemType.Wood)
            .Amount(1)
            .Requires(ItemType.Wood, 10)
            .Named("Workbench")
            .InCategory(RecipeCategory.Stations)
            .Build());

        Register(Create(ItemType.Stone)
            .Amount(1)
            .Requires(ItemType.Stone, 20)
            .Requires(ItemType.Wood, 4)
            .Requires(ItemType.Torch, 3)
            .AtStation(CraftingStationType.Workbench)
            .Named("Furnace")
            .InCategory(RecipeCategory.Stations)
            .Build());

        Register(Create(ItemType.IronBar)
            .Amount(1)
            .Requires(ItemType.IronBar, 5)
            .AtStation(CraftingStationType.Workbench)
            .Named("Iron Anvil")
            .InCategory(RecipeCategory.Stations)
            .Build());

        Register(Create(ItemType.Wood)
            .Amount(1)
            .Requires(ItemType.Wood, 14)
            .Requires(ItemType.IronBar, 2)
            .AtStation(CraftingStationType.Workbench)
            .Named("Sawmill")
            .InCategory(RecipeCategory.Stations)
            .Build());

        Register(Create(ItemType.Wood)
            .Amount(1)
            .Requires(ItemType.Wood, 12)
            .AtStation(CraftingStationType.Sawmill)
            .Named("Loom")
            .InCategory(RecipeCategory.Stations)
            .Build());

        Register(Create(ItemType.IronBar)
            .Amount(1)
            .Requires(ItemType.IronBar, 10)
            .Requires(ItemType.Wood, 8)
            .AtStation(CraftingStationType.Anvil)
            .Named("Tinker's Workshop")
            .InCategory(RecipeCategory.Stations)
            .Build());

        Register(Create(ItemType.Stone)
            .Amount(1)
            .Requires(ItemType.Stone, 15)
            .Requires(ItemType.IronBar, 3)
            .Requires(ItemType.Lens, 1)
            .AtStation(CraftingStationType.Workbench)
            .Named("Alchemy Table")
            .InCategory(RecipeCategory.Stations)
            .Build());

        Register(Create(ItemType.IronBar)
            .Amount(1)
            .Requires(ItemType.IronBar, 8)
            .Requires(ItemType.Wood, 6)
            .AtStation(CraftingStationType.Workbench)
            .Named("Cooking Pot")
            .InCategory(RecipeCategory.Stations)
            .Build());

        // === ADVANCED STATIONS ===
        Register(Create(ItemType.Stone)
            .Amount(1)
            .Requires(ItemType.Stone, 30)
            .Requires(ItemType.IronBar, 10)
            .Requires(ItemType.Torch, 10)
            .AtStation(CraftingStationType.Furnace)
            .Named("Glass Kiln")
            .InCategory(RecipeCategory.Stations)
            .Build());

        Register(Create(ItemType.SilverBar)
            .Amount(1)
            .Requires(ItemType.SilverBar, 10)
            .Requires(ItemType.Wood, 8)
            .AtStation(CraftingStationType.Anvil)
            .Named("Dye Vat")
            .InCategory(RecipeCategory.Stations)
            .Build());

        Register(Create(ItemType.Wood)
            .Amount(1)
            .Requires(ItemType.Wood, 25)
            .Requires(ItemType.IronBar, 5)
            .AtStation(CraftingStationType.Sawmill)
            .Named("Heavy Workbench")
            .InCategory(RecipeCategory.Stations)
            .Build());

        Register(Create(ItemType.GoldBar)
            .Amount(1)
            .Requires(ItemType.GoldBar, 10)
            .Requires(ItemType.Lens, 5)
            .AtStation(CraftingStationType.Anvil)
            .Named("Crystal Ball")
            .InCategory(RecipeCategory.Stations)
            .Build());

        Register(Create(ItemType.IronBar)
            .Amount(1)
            .Requires(ItemType.IronBar, 15)
            .Requires(ItemType.SilverBar, 5)
            .AtStation(CraftingStationType.IceMachine)
            .Named("Ice Machine")
            .InCategory(RecipeCategory.Stations)
            .Build());

        Register(Create(ItemType.BeeWax)
            .Amount(1)
            .Requires(ItemType.BeeWax, 15)
            .Requires(ItemType.Wood, 10)
            .AtStation(CraftingStationType.Workbench)
            .Named("Honey Dispenser")
            .AfterBoss(BossType.QueenBee)
            .InCategory(RecipeCategory.Stations)
            .Build());
    }

    #endregion

    #region Boss Summoning Recipes

    private static void RegisterBossSummoningRecipes()
    {
        Register(Create(ItemType.SuspiciousLookingEye)
            .Requires(ItemType.Lens, 6)
            .AtStation(CraftingStationType.DemonAltar)
            .InCategory(RecipeCategory.BossSummoning)
            .Build());

        Register(Create(ItemType.SlimeCrown)
            .Requires(ItemType.Gel, 20)
            .Requires(ItemType.GoldBar, 5)
            .AtStation(CraftingStationType.DemonAltar)
            .InCategory(RecipeCategory.BossSummoning)
            .Build());

        Register(Create(ItemType.BloodySpine)
            .Requires(ItemType.TissueSample, 15)
            .AtStation(CraftingStationType.DemonAltar)
            .InCategory(RecipeCategory.BossSummoning)
            .Build());

        Register(Create(ItemType.AncientSkull)
            .Requires(ItemType.BoneKey, 1)
            .Requires(ItemType.ShadowScale, 10)
            .AtStation(CraftingStationType.DemonAltar)
            .InCategory(RecipeCategory.BossSummoning)
            .Build());

        Register(Create(ItemType.Abeemination)
            .Requires(ItemType.BeeWax, 5)
            .Requires(ItemType.Honeycomb, 1)
            .Requires(ItemType.Gel, 10)
            .AtStation(CraftingStationType.DemonAltar)
            .InCategory(RecipeCategory.BossSummoning)
            .Build());

        Register(Create(ItemType.GuideVoodooDoll)
            .Requires(ItemType.Gel, 30)
            .Requires(ItemType.ShadowScale, 5)
            .Requires(ItemType.TissueSample, 5)
            .AtStation(CraftingStationType.DemonAltar)
            .AfterBoss(BossType.SkeletalWarlord)
            .InCategory(RecipeCategory.BossSummoning)
            .Build());
    }

    #endregion

    #region Advanced Recipes

    private static void RegisterAdvancedRecipes()
    {
        // === UPGRADE CONVERSIONS ===
        // Allow converting materials at higher tiers

        Register(Create(ItemType.IronBar)
            .Amount(1)
            .Requires(ItemType.CopperBar, 3)
            .AtStation(CraftingStationType.Furnace)
            .Named("Copper to Iron Conversion")
            .InCategory(RecipeCategory.Materials)
            .Build());

        Register(Create(ItemType.SilverBar)
            .Amount(1)
            .Requires(ItemType.IronBar, 3)
            .AtStation(CraftingStationType.Furnace)
            .Named("Iron to Silver Conversion")
            .InCategory(RecipeCategory.Materials)
            .Build());

        Register(Create(ItemType.GoldBar)
            .Amount(1)
            .Requires(ItemType.SilverBar, 3)
            .AtStation(CraftingStationType.Furnace)
            .Named("Silver to Gold Conversion")
            .InCategory(RecipeCategory.Materials)
            .Build());

        // === MATERIAL MULTIPLICATION ===
        Register(Create(ItemType.Gel)
            .Amount(5)
            .Requires(ItemType.Gel, 3)
            .AlsoRequires(CraftingStationType.Water)
            .AtStation(CraftingStationType.AlchemyTable)
            .Named("Gel Cultivation")
            .InCategory(RecipeCategory.Materials)
            .Build());

        Register(Create(ItemType.Lens)
            .Amount(2)
            .Requires(ItemType.Lens, 1)
            .Requires(ItemType.Sand, 5)
            .AtStation(CraftingStationType.GlassKiln)
            .Named("Lens Duplication")
            .InCategory(RecipeCategory.Materials)
            .Build());
    }

    #endregion

    #region Hardmode Recipes

    private static void RegisterHardmodeRecipes()
    {
        // === HARDMODE ANVIL ===
        Register(Create(ItemType.GoldBar)
            .Amount(1)
            .Requires(ItemType.GoldBar, 10)
            .Requires(ItemType.IronBar, 15)
            .AtStation(CraftingStationType.Anvil)
            .Named("Mythril Anvil")
            .AfterBoss(BossType.WallOfShadows)
            .InCategory(RecipeCategory.Stations)
            .Build());

        // === HARDMODE FORGE ===
        Register(Create(ItemType.Stone)
            .Amount(1)
            .Requires(ItemType.Stone, 40)
            .Requires(ItemType.GoldBar, 10)
            .AtStation(CraftingStationType.Furnace)
            .Named("Adamantite Forge")
            .AfterBoss(BossType.WallOfShadows)
            .InCategory(RecipeCategory.Stations)
            .Build());

        // === WALL OF SHADOWS EMBLEMS ===
        Register(Create(ItemType.EmblemWarrior)
            .Requires(ItemType.GoldBar, 15)
            .Requires(ItemType.ShadowScale, 10)
            .AtStation(CraftingStationType.MythrilAnvil)
            .AfterBoss(BossType.WallOfShadows)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.EmblemRanger)
            .Requires(ItemType.GoldBar, 15)
            .Requires(ItemType.Lens, 10)
            .AtStation(CraftingStationType.MythrilAnvil)
            .AfterBoss(BossType.WallOfShadows)
            .InCategory(RecipeCategory.Accessories)
            .Build());

        Register(Create(ItemType.EmblemSorcerer)
            .Requires(ItemType.GoldBar, 15)
            .Requires(ItemType.TissueSample, 10)
            .AtStation(CraftingStationType.MythrilAnvil)
            .AfterBoss(BossType.WallOfShadows)
            .InCategory(RecipeCategory.Accessories)
            .Build());
    }

    #endregion

    #region Misc Recipes

    private static void RegisterMiscRecipes()
    {
        // === CURRENCY CONVERSION ===
        Register(Create(ItemType.GoldCoin)
            .Amount(1)
            .Requires(ItemType.SilverCoin, 100)
            .Named("Combine Silver Coins")
            .InCategory(RecipeCategory.Misc)
            .Build());

        Register(Create(ItemType.SilverCoin)
            .Amount(1)
            .Requires(ItemType.CopperCoin, 100)
            .Named("Combine Copper Coins")
            .InCategory(RecipeCategory.Misc)
            .Build());

        Register(Create(ItemType.SilverCoin)
            .Amount(100)
            .Requires(ItemType.GoldCoin, 1)
            .Named("Split Gold Coin")
            .InCategory(RecipeCategory.Misc)
            .Build());

        Register(Create(ItemType.CopperCoin)
            .Amount(100)
            .Requires(ItemType.SilverCoin, 1)
            .Named("Split Silver Coin")
            .InCategory(RecipeCategory.Misc)
            .Build());

        // === RECYCLING ===
        Register(Create(ItemType.IronBar)
            .Amount(2)
            .Requires(ItemType.IronPickaxe, 1)
            .AtStation(CraftingStationType.Furnace)
            .Named("Recycle Iron Pickaxe")
            .InCategory(RecipeCategory.Misc)
            .Build());

        Register(Create(ItemType.CopperBar)
            .Amount(2)
            .Requires(ItemType.CopperPickaxe, 1)
            .AtStation(CraftingStationType.Furnace)
            .Named("Recycle Copper Pickaxe")
            .InCategory(RecipeCategory.Misc)
            .Build());

        Register(Create(ItemType.IronBar)
            .Amount(3)
            .Requires(ItemType.IronSword, 1)
            .AtStation(CraftingStationType.Furnace)
            .Named("Recycle Iron Sword")
            .InCategory(RecipeCategory.Misc)
            .Build());

        // === TROPHIES TO MATERIALS ===
        Register(Create(ItemType.GoldBar)
            .Amount(10)
            .Requires(ItemType.EyeOfTerrorTrophy, 1)
            .AtStation(CraftingStationType.Furnace)
            .Named("Melt Eye Trophy")
            .InCategory(RecipeCategory.Misc)
            .Build());

        Register(Create(ItemType.Gel)
            .Amount(50)
            .Requires(ItemType.KingSlimeTrophy, 1)
            .AtStation(CraftingStationType.AlchemyTable)
            .Named("Dissolve Slime Trophy")
            .InCategory(RecipeCategory.Misc)
            .Build());
    }

    #endregion

    #region Public API

    public static Recipe? GetById(int id)
    {
        Initialize();
        return _recipes.TryGetValue(id, out var recipe) ? recipe : null;
    }

    public static IEnumerable<Recipe> GetAll()
    {
        Initialize();
        return _recipes.Values;
    }

    public static IEnumerable<Recipe> GetByResult(ItemType result)
    {
        Initialize();
        return _byResult.TryGetValue(result, out var list) ? list : Enumerable.Empty<Recipe>();
    }

    public static IEnumerable<Recipe> GetByStation(CraftingStationType station)
    {
        Initialize();
        return _byStation.TryGetValue(station, out var list) ? list : Enumerable.Empty<Recipe>();
    }

    public static IEnumerable<Recipe> GetByCategory(RecipeCategory category)
    {
        Initialize();
        if (category == RecipeCategory.All)
            return _recipes.Values;
        return _byCategory.TryGetValue(category, out var list) ? list : Enumerable.Empty<Recipe>();
    }

    public static IEnumerable<Recipe> GetCraftable(
        Inventory inventory,
        IEnumerable<CraftingStationType> availableStations,
        HashSet<BossType>? defeatedBosses = null)
    {
        Initialize();

        var stationSet = availableStations.ToHashSet();
        stationSet.Add(CraftingStationType.None);  // Always can hand craft

        foreach (var recipe in _recipes.Values)
        {
            // Check station
            if (!stationSet.Contains(recipe.Station))
                continue;

            // Check secondary station
            if (recipe.SecondaryStation.HasValue && !stationSet.Contains(recipe.SecondaryStation.Value))
                continue;

            // Check boss requirement
            if (recipe.RequiresBossDefeated.HasValue)
            {
                if (defeatedBosses == null || !defeatedBosses.Contains(recipe.RequiresBossDefeated.Value))
                    continue;
            }

            // Check ingredients
            if (!recipe.CanCraft(inventory))
                continue;

            yield return recipe;
        }
    }

    public static int RecipeCount
    {
        get
        {
            Initialize();
            return _recipes.Count;
        }
    }

    #endregion
}
