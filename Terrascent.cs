using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Terrascent.Combat;
using Terrascent.Core;
using Terrascent.Crafting;
using Terrascent.Economy;
using Terrascent.Entities;
using Terrascent.Entities.Bosses;
using Terrascent.Entities.Drops;
using Terrascent.Entities.Enemies;
using Terrascent.Items;
using Terrascent.Maps;
using Terrascent.Saves;
using Terrascent.Systems;
using Terrascent.UI;
using Terrascent.World;
using Terrascent.World.Biomes;
using Terrascent.World.Generation;
using Terrascent.World.Hardmode;

namespace Terrascent;

public class TerrascentGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;

    // Core systems
    private GameLoop _gameLoop = null!;
    private InputManager _input = null!;
    private Camera _camera = null!;
    private SaveManager _saveManager = null!;
    private UIManager _uiManager = null!;

    // World
    private ChunkManager _chunkManager = null!;
    private WorldGenerator _worldGenerator = null!;
    private BiomeManager _biomeManager = null!;

    // Entities
    private Player _player = null!;

    // Systems
    private MiningSystem _mining = null!;
    private BuildingSystem _building = null!;
    private CombatSystem _combat = null!;

    // Economy
    private DifficultyManager _difficultyManager = null!;
    private ChestManager _chestManager = null!;
    private List<ChestEntity> _chests = new();

    // Enemy System
    private EnemyManager _enemyManager = null!;
    private DropManager _dropManager = null!;

    // Boss System
    private BossManager _bossManager = null!;

    // Crafting System
    private CraftingManager _craftingManager = null!;

    // Map System
    private MapManager _mapManager = null!;
    private MapSystemTest _mapSystemTest = null!;

    // Hardmode System
    private HardmodeManager _hardmodeManager = null!;
    private HardmodeUI _hardmodeUI = null!;

    // Temp rendering
    private Texture2D _pixelTexture = null!;

    // Game constants
    public const int TILE_SIZE = 16;
    public const int CHUNK_SIZE = 32;

    // World seed
    private int _worldSeed = 12345;

    // Current mouse tile target
    private Point _mouseTilePos;
    private bool _isTargetValid;

    // Debug and Map state
    private bool _showDebugOverlay = false;
    private bool _showMap = false;
    private bool _isDraggingMap = false;
    private Vector2 _lastMapDragPos;

    public TerrascentGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = false;

        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
    }

    protected override void Initialize()
    {
        Window.Title = "Terrascent";

        _gameLoop = new GameLoop();
        _input = new InputManager();
        _saveManager = new SaveManager();

        // Create player first (needed for loading)
        _player = new Player();

        // Try to load existing world
        int? loadedSeed = _saveManager.LoadWorldData();

        if (loadedSeed.HasValue)
        {
            // Load existing world
            _worldSeed = loadedSeed.Value;
            System.Diagnostics.Debug.WriteLine($"Loading existing world with seed: {_worldSeed}");

            _worldGenerator = new WorldGenerator(_worldSeed);
            _chunkManager = new ChunkManager
            {
                Generator = _worldGenerator,
                SaveManager = _saveManager,
                LoadRadius = 4
            };

            // Load player data
            if (!_saveManager.LoadPlayer(_player))
            {
                // No player save, spawn at world center (Forest biome)
                int spawnX = _worldGenerator.Config.Width / 2;
                int surfaceY = _worldGenerator.GetSurfaceHeight(spawnX);
                _player.SpawnAt(spawnX, surfaceY);
            }
        }
        else
        {
            // Create new world
            System.Diagnostics.Debug.WriteLine($"Creating new world with seed: {_worldSeed}");

            _worldGenerator = new WorldGenerator(_worldSeed);
            _chunkManager = new ChunkManager
            {
                Generator = _worldGenerator,
                SaveManager = _saveManager,
                LoadRadius = 4
            };

            // Spawn player above the surface at world center (Forest biome)
            int spawnX = _worldGenerator.Config.Width / 2;
            int surfaceY = _worldGenerator.GetSurfaceHeight(spawnX);
            _player.SpawnAt(spawnX, surfaceY);
        }

        // Subscribe to chunk events for debugging
        _chunkManager.OnChunkLoaded += chunk =>
            System.Diagnostics.Debug.WriteLine($"Loaded: {chunk}");

        // Create systems
        _mining = new MiningSystem();
        _building = new BuildingSystem();
        _combat = new CombatSystem();

        // Create economy systems
        _difficultyManager = new DifficultyManager();
        _chestManager = new ChestManager(_difficultyManager, _worldSeed);

        // Create enemy/drop systems
        _dropManager = new DropManager();
        _enemyManager = new EnemyManager(_difficultyManager, _dropManager, _worldSeed);

        // Create biome manager and wire to world config
        _biomeManager = new BiomeManager(_chunkManager, _worldSeed);
        _biomeManager.SetWorldConfig(_worldGenerator.Config);

        // Create map manager
        _mapManager = new MapManager(_worldGenerator, _chunkManager);

        // Load explored chunks if save exists
        var exploredChunks = _saveManager.LoadExploration();
        if (exploredChunks != null)
        {
            _mapManager.LoadExploredChunks(exploredChunks);
            System.Diagnostics.Debug.WriteLine($"Loaded {exploredChunks.Count} explored chunks");

            // Load tile color cache
            var tileColorCache = _saveManager.LoadMapCache();
            if (tileColorCache != null)
            {
                _mapManager.LoadTileColorCache(tileColorCache);
                System.Diagnostics.Debug.WriteLine($"Loaded {tileColorCache.Count} cached map chunks");
            }
        }

        // Wire biome manager to enemy manager for biome-aware spawning
        _enemyManager.SetBiomeManager(_biomeManager);

        // Subscribe to biome events
        _biomeManager.OnBiomeChanged += (oldBiome, newBiome) =>
        {
            System.Diagnostics.Debug.WriteLine($"Biome changed: {oldBiome.GetDisplayName()} -> {newBiome.GetDisplayName()}");
        };

        // Create boss system
        _bossManager = new BossManager(_difficultyManager, _dropManager, _enemyManager, _worldSeed); ;

        // Create hardmode system (after biome manager and boss manager)
        _hardmodeManager = new HardmodeManager(_chunkManager, _worldGenerator, _biomeManager, _worldSeed);

        // Wire boss defeat to hardmode trigger
        _bossManager.OnHardmodeTriggered += () =>
        {
            System.Diagnostics.Debug.WriteLine("=== HARDMODE TRIGGERED BY BOSS DEFEAT ===");
            _hardmodeManager.ActivateHardmode();
            _biomeManager.EnableHardmode();
        };

        // Subscribe to hardmode events
        _hardmodeManager.OnHardmodeActivated += () =>
        {
            System.Diagnostics.Debug.WriteLine("[HARDMODE] World transformation beginning!");
        };

        _hardmodeManager.OnTransformationProgress += (status) =>
        {
            System.Diagnostics.Debug.WriteLine($"[HARDMODE] {status}");
        };

        _hardmodeManager.OnTransformationComplete += () =>
        {
            System.Diagnostics.Debug.WriteLine("[HARDMODE] World transformation COMPLETE!");
        };

        // Load hardmode state from save if applicable
        if (_saveManager.LoadedIsHardmode)
        {
            System.Diagnostics.Debug.WriteLine("[HARDMODE] Loading saved hardmode state...");
            _hardmodeManager.SetHardmode(true, skipTransformation: _saveManager.LoadedHasTransformed);
            _biomeManager.EnableHardmode();
        }

        // Create crafting system
        _craftingManager = new CraftingManager(_player, _bossManager);

        // Create endgame map system test
        _mapSystemTest = new MapSystemTest(_player.Inventory);
        System.Diagnostics.Debug.WriteLine("Map System Test initialized! Press F5-F8 for test controls.");

        // Connect XP system to drop manager
        _dropManager.SetXPSystem(_player.XP);

        // Connect combat system to enemies and bosses
        _combat.SetEnemyManager(_enemyManager);
        _combat.SetBossManager(_bossManager);
        _combat.SetChunkManager(_chunkManager);

        // Subscribe to enemy events
        _enemyManager.OnEnemyKilled += enemy =>
        {
            System.Diagnostics.Debug.WriteLine($"Killed {enemy.Data.Name}! +{enemy.GoldReward}g +{enemy.XPReward}xp");
        };

        _enemyManager.OnPlayerDamaged += (enemy, damage) =>
        {
            System.Diagnostics.Debug.WriteLine($"OUCH! {enemy.Data.Name} hit for {damage} damage!");
        };

        _dropManager.OnDropCollected += (type, value) =>
        {
            System.Diagnostics.Debug.WriteLine($"Picked up {type}: +{value}");
        };

        // Subscribe to boss events
        _bossManager.OnBossSpawned += boss =>
        {
            System.Diagnostics.Debug.WriteLine($"=== BOSS SPAWNED: {boss.Data.Name} ({boss.CurrentHealth} HP) ===");
        };

        _bossManager.OnBossDefeated += boss =>
        {
            System.Diagnostics.Debug.WriteLine($"=== BOSS DEFEATED: {boss.Data.Name}! ===");
        };

        _bossManager.OnBossPhaseChanged += (boss, phase) =>
        {
            System.Diagnostics.Debug.WriteLine($"Boss {boss.Data.Name} entered {phase}!");
        };

        _bossManager.OnBossEnraged += boss =>
        {
            System.Diagnostics.Debug.WriteLine($"!!! {boss.Data.Name} HAS ENRAGED !!!");
        };

        _bossManager.OnBossItemDropped += (itemType, count, position) =>
        {
            // Add dropped items to player inventory
            int added = _player.Inventory.AddItem(itemType, count);
            if (added > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[LOOT] Added {added}x {itemType} to inventory!");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[LOOT] Inventory full! Could not add {count}x {itemType}");
            }
        };

        // Subscribe to XP events
        _dropManager.OnXPCollected += (xp, position) =>
        {
            System.Diagnostics.Debug.WriteLine($"XP collected: +{xp} (Level {_player.XP.Level}: {_player.XP.CurrentXP}/{_player.XP.XPToNextLevel})");
        };

        // Subscribe to level-up events
        _player.OnLevelUp += level =>
        {
            System.Diagnostics.Debug.WriteLine($"=== LEVEL UP! Now level {level} ===");
            // Level-up UI is triggered automatically by Player.LevelUp.QueueLevelUp()
        };

        // Subscribe to chest events
        _chestManager.OnChestOpened += (type, drop) =>
        {
            System.Diagnostics.Debug.WriteLine($"Chest opened! Type: {type}");
            foreach (var item in drop.Items)
            {
                _player.Inventory.AddItem(item, 1);
                System.Diagnostics.Debug.WriteLine($"  Got: {ItemRegistry.Get(item).Name}");
            }
        };

        // Spawn a test chest near player
        SpawnTestChest();

        // Create UI Manager
        _uiManager = new UIManager(_player, _input);

        // Subscribe to combat events
        _combat.OnAttack += args =>
        {
            System.Diagnostics.Debug.WriteLine($"ATTACK: {args.Attack.Name} dealing {args.Damage} damage");
        };

        System.Diagnostics.Debug.WriteLine($"World Seed: {_worldSeed}");
        System.Diagnostics.Debug.WriteLine($"Player position: {_player.Position}");

        base.Initialize();
    }

    private void SpawnTestChest()
    {
        // Spawn chests near player spawn point
        // Chests are 32px tall (2 tiles), so we place them 2 tiles above surface
        // so their bottom edge rests ON TOP of the grass
        int spawnX = 2;
        int surfaceY1 = _worldGenerator.GetSurfaceHeight(spawnX);
        int surfaceY2 = _worldGenerator.GetSurfaceHeight(spawnX + 3);
        int surfaceY3 = _worldGenerator.GetSurfaceHeight(spawnX + 6);

        // Position = surfaceY - 2 so bottom of chest sits on grass
        var smallChest = new ChestEntity(ChestType.Small, new Point(spawnX, surfaceY1 - 2));
        _chests.Add(smallChest);

        var largeChest = new ChestEntity(ChestType.Large, new Point(spawnX + 3, surfaceY2 - 2));
        _chests.Add(largeChest);

        var equipChest = new ChestEntity(ChestType.Equipment, new Point(spawnX + 6, surfaceY3 - 2));
        _chests.Add(equipChest);

        System.Diagnostics.Debug.WriteLine($"Spawned 3 test chests on surface");
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _camera = new Camera(GraphicsDevice.Viewport);
        _camera.CenterOn(_player.Center);

        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData([Color.White]);

        // Initialize UI (needs graphics device ready)
        _uiManager.Initialize(
            GraphicsDevice,
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight
        );

        // Wire up level-up UI with player's level-up manager
        _uiManager.SetLevelUpManager(
            _player.LevelUp,
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight
        );

        // Wire up boss health bar UI with boss manager
        _uiManager.SetBossManager(
            _bossManager,
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight
        );

        // Wire up crafting UI with crafting manager
        _uiManager.SetCraftingManager(
            _craftingManager,
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight
        );

        // Wire up biome UI with biome manager
        _uiManager.SetBiomeManager(
            _biomeManager,
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight
        );

        // Initialize hardmode UI
        _hardmodeUI = new HardmodeUI(_hardmodeManager);
        _hardmodeUI.Initialize(
            GraphicsDevice,
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight
        );
        // Note: SetFont() can be called later if a font becomes available
    }

    protected override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _input.Update();

        // Update UI first (may consume input)
        _uiManager.Update(deltaTime);

        // Update hardmode UI
        _hardmodeUI.Update(deltaTime);

        // Update hardmode manager (biome spreading, etc)
        _hardmodeManager.Update(deltaTime);

        // Update endgame map system test
        _mapSystemTest.Update(gameTime, _player.Center, _input);

        // Only exit with Escape if no UI is open
        if (_input.IsKeyPressed(Keys.Escape) && !_uiManager.IsAnyPanelOpen)
            Exit();

        // Save game (F6)
        if (_input.IsKeyPressed(Keys.F6))
        {
            _saveManager.SaveAll(_worldSeed, _player, _chunkManager,
                _hardmodeManager.IsHardmode, _hardmodeManager.HasTransformed,
                _mapManager.GetExploredChunks(), _mapManager.GetTileColorCache());
        }

        // Regenerate world with new seed (F5) - also deletes save
        if (_input.IsKeyPressed(Keys.F5))
        {
            RegenerateWorld();
        }

        // === DEBUG KEYS ===
        // Toggle God Mode (F1)
        if (_input.IsKeyPressed(Keys.F1))
        {
            _player.GodMode = !_player.GodMode;
            System.Diagnostics.Debug.WriteLine($"God Mode: {(_player.GodMode ? "ON" : "OFF")}");
        }

        // Toggle Noclip (F2)
        if (_input.IsKeyPressed(Keys.F2))
        {
            _player.Noclip = !_player.Noclip;
            System.Diagnostics.Debug.WriteLine($"Noclip: {(_player.Noclip ? "ON" : "OFF")}");
        }

        // Toggle Debug Overlay (F3)
        if (_input.IsKeyPressed(Keys.F3))
        {
            _showDebugOverlay = !_showDebugOverlay;
            System.Diagnostics.Debug.WriteLine($"Debug Overlay: {(_showDebugOverlay ? "ON" : "OFF")}");
        }

        // Toggle Hardmode (F9) - DEBUG ONLY
        if (_input.IsKeyPressed(Keys.F9))
        {
            if (!_hardmodeManager.IsHardmode)
            {
                // Activate hardmode (will trigger world transformation)
                _hardmodeManager.ActivateHardmode();
                _biomeManager.EnableHardmode();
                System.Diagnostics.Debug.WriteLine("DEBUG: Hardmode ACTIVATED via F9");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("DEBUG: Hardmode already active");
            }
        }

        // Toggle Map (M or F4)
        if (_input.IsKeyPressed(Keys.M) || _input.IsKeyPressed(Keys.F4))
        {
            _showMap = !_showMap;
            _mapManager.IsOpen = _showMap;
            if (_showMap)
            {
                // Center map on player when opening
                Point playerTile = WorldCoordinates.WorldToTile(_player.Center);
                _mapManager.CenterOn(playerTile);
            }
            System.Diagnostics.Debug.WriteLine($"Map: {(_showMap ? "ON" : "OFF")}");
        }

        // Map controls when map is open
        if (_showMap)
        {
            // Zoom with mouse scroll or +/-
            if (_input.ScrollWheelDelta != 0)
            {
                if (_input.ScrollWheelDelta > 0)
                    _mapManager.ZoomIn();
                else
                    _mapManager.ZoomOut();
            }

            if (_input.IsKeyPressed(Keys.OemPlus) || _input.IsKeyPressed(Keys.Add))
                _mapManager.ZoomIn();
            if (_input.IsKeyPressed(Keys.OemMinus) || _input.IsKeyPressed(Keys.Subtract))
                _mapManager.ZoomOut();

            // Pan with arrow keys or WASD
            float panSpeed = 10f / _mapManager.Zoom;
            Vector2 panDelta = Vector2.Zero;
            if (_input.IsKeyDown(Keys.Left) || _input.IsKeyDown(Keys.A)) panDelta.X -= panSpeed;
            if (_input.IsKeyDown(Keys.Right) || _input.IsKeyDown(Keys.D)) panDelta.X += panSpeed;
            if (_input.IsKeyDown(Keys.Up) || _input.IsKeyDown(Keys.W)) panDelta.Y -= panSpeed;
            if (_input.IsKeyDown(Keys.Down) || _input.IsKeyDown(Keys.S)) panDelta.Y += panSpeed;
            if (panDelta != Vector2.Zero)
                _mapManager.Pan(panDelta);

            // Mouse drag to pan
            if (_input.IsLeftMouseDown())
            {
                if (!_isDraggingMap)
                {
                    _isDraggingMap = true;
                    _lastMapDragPos = _input.MousePositionV;
                }
                else
                {
                    Vector2 dragDelta = _lastMapDragPos - _input.MousePositionV;
                    _mapManager.Pan(dragDelta / _mapManager.Zoom);
                    _lastMapDragPos = _input.MousePositionV;
                }
            }
            else
            {
                _isDraggingMap = false;
            }

            // Reset view with R
            if (_input.IsKeyPressed(Keys.R))
            {
                _mapManager.ResetView();
                Point playerTile = WorldCoordinates.WorldToTile(_player.Center);
                _mapManager.CenterOn(playerTile);
            }

            // Reveal all with Shift+R (debug)
            if (_input.IsKeyDown(Keys.LeftShift) && _input.IsKeyPressed(Keys.R))
            {
                _mapManager.RevealAll();
                System.Diagnostics.Debug.WriteLine("Map fully revealed!");
            }

            // Update map hover info
            Rectangle mapBounds = GetMapBounds();
            Point playerTilePos = WorldCoordinates.WorldToTile(_player.Center);
            _mapManager.Update(deltaTime, playerTilePos, _input.MousePositionV, mapBounds);
        }

        // Only run gameplay updates if no UI panel is blocking AND map is closed
        if (!_uiManager.IsAnyPanelOpen && !_showMap)
        {
            int physicsUpdates = _gameLoop.Update(deltaTime, FixedUpdate);
            VariableUpdate(deltaTime);
            _input.ConsumeBufferedPresses(consumeKeyboard: physicsUpdates > 0);
        }
        else
        {
            // Still update camera when UI is open
            _camera.Update(deltaTime);
            _input.ConsumeBufferedPresses(consumeKeyboard: true);
        }

        base.Update(gameTime);
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        // Auto-save on exit
        _saveManager.SaveAll(_worldSeed, _player, _chunkManager,
            _hardmodeManager.IsHardmode, _hardmodeManager.HasTransformed,
            _mapManager.GetExploredChunks(), _mapManager.GetTileColorCache());
        base.OnExiting(sender, args);
    }

    private void RegenerateWorld()
    {
        // Close inventory if open
        if (_uiManager.IsInventoryOpen)
            _uiManager.CloseInventory();

        // Delete existing save
        _saveManager.DeleteSave();

        _worldSeed = Random.Shared.Next();

        _worldGenerator = new WorldGenerator(_worldSeed, WorldSize.Medium);
        _chunkManager.Clear();
        _chunkManager.Generator = _worldGenerator;

        // Reset player inventory
        _player.Inventory.Clear();
        _player.Inventory.AddItem(ItemType.Dirt, 50);
        _player.Inventory.AddItem(ItemType.Stone, 50);
        _player.Inventory.AddItem(ItemType.Wood, 30);
        _player.Inventory.AddItem(ItemType.Torch, 20);
        _player.Inventory.AddItem(ItemType.WoodPickaxe, 1);
        _player.Inventory.AddItem(ItemType.WoodSword, 1);
        _player.Inventory.AddItem(ItemType.WoodSpear, 1);
        _player.Inventory.AddItem(ItemType.WoodBow, 1);

        // Stackable items for testing
        _player.Inventory.AddItem(ItemType.SoldiersSyringeItem, 3);
        _player.Inventory.AddItem(ItemType.PaulsGoatHoofItem, 2);
        _player.Inventory.AddItem(ItemType.CritGlassesItem, 5);

        // Reset economy
        _player.Currency.SetGold(100);
        _difficultyManager.Reset();
        _chestManager.Reset();
        _chests.Clear();
        SpawnTestChest();

        _enemyManager.Clear();
        _dropManager.Clear();
        _bossManager.Clear();

        // Recreate biome manager with new seed and world config
        _biomeManager = new BiomeManager(_chunkManager, _worldSeed);
        _biomeManager.SetWorldConfig(_worldGenerator.Config);
        _enemyManager.SetBiomeManager(_biomeManager);

        // Recreate map manager with new world
        _mapManager = new MapManager(_worldGenerator, _chunkManager);

        // Recreate biome UI with new manager
        _uiManager.SetBiomeManager(
            _biomeManager,
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight
        );

        // Respawn player at world center (Forest biome)
        int spawnX = _worldGenerator.Config.Width / 2;
        int surfaceY = _worldGenerator.GetSurfaceHeight(spawnX);
        _player.SpawnAt(spawnX, surfaceY);
        _camera.CenterOn(_player.Center);

        System.Diagnostics.Debug.WriteLine($"Regenerated world with seed: {_worldSeed}");
        System.Diagnostics.Debug.WriteLine($"  Player spawned at X={spawnX} (world center)");
    }

    private void FixedUpdate()
    {
        float dt = GameLoop.TICK_DURATION;

        // Update difficulty (time-based scaling)
        _difficultyManager.Update(dt);

        _player.HandleInput(_input, dt);
        _player.Update(dt);
        _player.ApplyMovement(dt, _chunkManager);

        // Update equipped weapon based on hotbar selection
        _player.UpdateEquippedWeapon();

        // Update enemies
        _enemyManager.Update(dt, _player, _chunkManager);

        // Update drops
        _dropManager.Update(dt, _player, _chunkManager);

        // Update bosses
        _bossManager.Update(dt, _player, _chunkManager);

        // Update crafting (station detection)
        _craftingManager.Update(dt, _chunkManager);

        // Update biome manager (spreading and detection)
        Point playerTilePos = WorldCoordinates.WorldToTile(_player.Center);
        _biomeManager.Update(dt, playerTilePos);

        // Detect current biome at player position (updates UI via event)
        _biomeManager.DetectBiome(playerTilePos);

        // Update map exploration (reveal tiles around player)
        _mapManager.UpdateExploredArea(playerTilePos);

        // Debug: Spawn test enemy (F7) - now biome-aware
        if (_input.IsKeyPressed(Keys.F7))
        {
            Vector2 spawnPos = _player.Position + new Vector2(_player.FacingDirection * 100, -50);
            BiomeType currentBiome = _biomeManager.CurrentBiome;
            var enemy = _enemyManager.SpawnBiomeEnemy(currentBiome, spawnPos);
            if (enemy != null)
            {
                System.Diagnostics.Debug.WriteLine($"Spawned biome enemy in {currentBiome}!");
            }
            else
            {
                // Fallback to slime if no biome enemy available
                _enemyManager.SpawnEnemy(EnemyType.Slime, spawnPos);
                System.Diagnostics.Debug.WriteLine("Spawned fallback Slime!");
            }
        }

        // Debug: Spawn harder enemy (F8) - specific type
        if (_input.IsKeyPressed(Keys.F8))
        {
            Vector2 spawnPos = _player.Position + new Vector2(_player.FacingDirection * 100, -50);
            _enemyManager.SpawnEnemy(EnemyType.Skeleton, spawnPos);
            System.Diagnostics.Debug.WriteLine("Spawned test Skeleton!");
        }

        // Debug: Trigger test level-up (F9)
        if (_input.IsKeyPressed(Keys.F9))
        {
            _player.XP.AddXP(_player.XP.XPToNextLevel);  // Give enough XP to level up
            System.Diagnostics.Debug.WriteLine("Test level-up triggered!");
        }

        // Debug: Spawn King Slime (F10)
        if (_input.IsKeyPressed(Keys.F10))
        {
            Vector2 spawnPos = _player.Position + new Vector2(_player.FacingDirection * 150, -100);
            _bossManager.SummonBoss(BossType.KingSlime, spawnPos);
            System.Diagnostics.Debug.WriteLine("Spawned King Slime boss!");
        }

        // Debug: Spawn Eye of Terror (F11)
        if (_input.IsKeyPressed(Keys.F11))
        {
            Vector2 spawnPos = _player.Position + new Vector2(_player.FacingDirection * 200, -150);
            _bossManager.SummonBoss(BossType.EyeOfTerror, spawnPos);
            System.Diagnostics.Debug.WriteLine("Spawned Eye of Terror boss!");
        }

        // Debug: Spawn Skeletal Warlord (F12)
        if (_input.IsKeyPressed(Keys.F12))
        {
            Vector2 spawnPos = _player.Position + new Vector2(_player.FacingDirection * 150, -50);
            _bossManager.SummonBoss(BossType.SkeletalWarlord, spawnPos);
            System.Diagnostics.Debug.WriteLine("Spawned Skeletal Warlord boss!");
        }

        // Determine what action to take with left mouse
        bool hasWeapon = _player.Weapons.HasWeaponEquipped;
        bool leftMouseDown = _input.IsLeftMouseDown();
        bool leftMousePressed = _input.IsLeftMousePressed();

        if (hasWeapon)
        {
            // Combat mode - weapon is equipped
            _mining.CancelMining();
            _combat.Update(dt, _player, _player.Weapons.EquippedWeapon, leftMousePressed, leftMouseDown);
        }
        else
        {
            // Mining/building mode - no weapon equipped
            if (leftMouseDown && _isTargetValid)
            {
                var tile = _chunkManager.GetTileAt(_mouseTilePos);
                if (!tile.IsAir)
                {
                    bool mined = _mining.UpdateMining(_mouseTilePos, _chunkManager, _player, dt);
                    if (mined)
                    {
                        System.Diagnostics.Debug.WriteLine($"Mined tile at {_mouseTilePos}");
                    }
                }
                else
                {
                    _mining.CancelMining();
                }
            }
            else
            {
                _mining.CancelMining();
            }
        }
        // Chest interaction (E key)
        if (_input.IsKeyPressed(Keys.E))
        {
            TryInteractWithChest();
        }
    }

    private void TryInteractWithChest()
    {
        // Find the closest chest in range
        ChestEntity? closestChest = null;
        float closestDistance = float.MaxValue;

        foreach (var chest in _chests)
        {
            if (chest.IsOpened || !chest.IsInRange(_player))
                continue;

            float distance = Vector2.Distance(_player.Center, chest.Center);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestChest = chest;
            }
        }

        // Interact with the closest chest
        if (closestChest != null)
        {
            int cost = closestChest.GetCost(_chestManager);
            if (_player.Currency.CanAfford(cost))
            {
                var drop = closestChest.TryOpen(_chestManager, _player.Currency, _player.Stats.LuckBonus);
                if (drop.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine($"Opened {closestChest.ChestType} chest for {cost} gold!");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Can't afford chest! Cost: {cost}, Gold: {_player.Currency.Gold}");
            }
        }
    }

    private void VariableUpdate(float deltaTime)
    {
        _chunkManager.UpdateLoadedChunks(_player.Position);

        _camera.Follow(_player.Center);
        _camera.Update(deltaTime);

        if (_input.IsKeyDown(Keys.LeftControl) || _input.IsKeyDown(Keys.RightControl))
        {
            if (_input.ScrollWheelDelta != 0)
            {
                float zoomDelta = _input.ScrollWheelDelta > 0 ? 0.1f : -0.1f;
                _camera.AdjustZoom(zoomDelta);
            }
        }
        else
        {
            if (_input.ScrollWheelDelta != 0)
            {
                int scrollDir = _input.ScrollWheelDelta > 0 ? -1 : 1;
                _player.Inventory.ScrollSelection(scrollDir);
            }
        }

        if (_input.IsKeyPressed(Keys.R))
            _camera.SetZoom(1f);

        var worldPos = _camera.ScreenToWorld(_input.MousePositionV);
        _mouseTilePos = WorldCoordinates.WorldToTile(worldPos);
        _isTargetValid = _mining.IsInRange(_player, _mouseTilePos);

        if (_input.IsRightMousePressed())
        {
            if (_building.TryPlace(_mouseTilePos, _chunkManager, _player))
            {
                System.Diagnostics.Debug.WriteLine($"Placed block at {_mouseTilePos}");
            }
        }
    }

    private void DrawHotbar()
    {
        int slotSize = 40;
        int padding = 4;
        int hotbarWidth = _player.Inventory.HotbarSize * (slotSize + padding) + padding;
        int hotbarX = (_graphics.PreferredBackBufferWidth - hotbarWidth) / 2;
        int hotbarY = _graphics.PreferredBackBufferHeight - slotSize - padding * 2 - 10;

        // Background
        DrawRectangle(new Vector2(hotbarX, hotbarY), hotbarWidth, slotSize + padding * 2, new Color(0, 0, 0, 180));

        for (int i = 0; i < _player.Inventory.HotbarSize; i++)
        {
            int x = hotbarX + padding + i * (slotSize + padding);
            int y = hotbarY + padding;

            // Slot background
            Color slotColor = i == _player.Inventory.SelectedSlot
                ? new Color(100, 100, 150, 200)
                : new Color(60, 60, 60, 200);
            DrawRectangle(new Vector2(x, y), slotSize, slotSize, slotColor);

            // Selection highlight
            if (i == _player.Inventory.SelectedSlot)
            {
                DrawRectangle(new Vector2(x - 2, y - 2), slotSize + 4, 2, Color.Yellow);
                DrawRectangle(new Vector2(x - 2, y + slotSize), slotSize + 4, 2, Color.Yellow);
                DrawRectangle(new Vector2(x - 2, y), 2, slotSize, Color.Yellow);
                DrawRectangle(new Vector2(x + slotSize, y), 2, slotSize, Color.Yellow);
            }

            // Item in slot
            var stack = _player.Inventory.GetSlot(i);
            if (!stack.IsEmpty)
            {
                // Draw item color (placeholder for sprite)
                Color itemColor = GetItemColor(stack.Type);
                int itemSize = slotSize - 8;
                DrawRectangle(new Vector2(x + 4, y + 4), itemSize, itemSize, itemColor);

                // Draw stack count (bottom right) with actual numbers
                if (stack.Count > 1)
                {
                    Rectangle slotBounds = new(x, y, slotSize, slotSize);
                    InventoryUI.DrawStackNumber(_spriteBatch, _pixelTexture, slotBounds, stack.Count);
                }
            }

            // Draw hotkey number (top left) - 1-9 then 0
            Rectangle bounds = new(x, y, slotSize, slotSize);
            InventoryUI.DrawSlotNumber(_spriteBatch, _pixelTexture, bounds, i);
        }
    }

    private void DrawChargeBar()
    {
        var weapon = _player.Weapons.EquippedWeapon;
        if (weapon == null || !weapon.IsCharging)
            return;

        int barWidth = 100;
        int barHeight = 8;
        int x = (_graphics.PreferredBackBufferWidth - barWidth) / 2;
        int y = _graphics.PreferredBackBufferHeight - 80;

        // Background
        DrawRectangle(new Vector2(x - 2, y - 2), barWidth + 4, barHeight + 4, new Color(0, 0, 0, 200));

        // Charge segments (one per max charge level)
        int maxLevel = Math.Max(1, weapon.MaxChargeLevel);
        int segmentWidth = barWidth / maxLevel;

        for (int i = 0; i < maxLevel; i++)
        {
            int segX = x + i * segmentWidth;
            Color segColor;

            if (i < weapon.CurrentChargeLevel)
            {
                // Fully charged segment
                segColor = GetChargeLevelColor(i + 1);
            }
            else if (i == weapon.CurrentChargeLevel)
            {
                // Currently charging segment
                float progress = weapon.ChargeProgress;
                segColor = Color.Lerp(new Color(40, 40, 40), GetChargeLevelColor(i + 1), progress);
            }
            else
            {
                // Not yet reached
                segColor = new Color(40, 40, 40);
            }

            DrawRectangle(new Vector2(segX + 1, y), segmentWidth - 2, barHeight, segColor);
        }

        // Draw current charge level number
        if (weapon.CurrentChargeLevel > 0)
        {
            // Visual indicator of charge level
            int indicatorSize = 16 + weapon.CurrentChargeLevel * 2;
            Color indicatorColor = GetChargeLevelColor(weapon.CurrentChargeLevel);
            DrawRectangle(
                new Vector2(x + barWidth / 2 - indicatorSize / 2, y - indicatorSize - 4),
                indicatorSize, indicatorSize,
                indicatorColor * 0.7f
            );
        }
    }

    private static Color GetChargeLevelColor(int level)
    {
        return level switch
        {
            1 => Color.LightGreen,
            2 => Color.Green,
            3 => Color.Cyan,
            4 => Color.Blue,
            5 => Color.Purple,
            6 => Color.Magenta,
            7 => Color.Orange,
            8 => Color.Gold,
            _ => Color.White,
        };
    }

    private static Color GetItemColor(ItemType type)
    {
        return type switch
        {
            // Blocks
            ItemType.Dirt => new Color(139, 90, 43),
            ItemType.Stone => new Color(128, 128, 128),
            ItemType.Sand => new Color(238, 214, 175),
            ItemType.Wood => new Color(160, 82, 45),
            ItemType.Torch => Color.Yellow,
            ItemType.CopperOre => new Color(184, 115, 51),
            ItemType.IronOre => new Color(165, 142, 142),
            ItemType.SilverOre => new Color(192, 192, 210),
            ItemType.GoldOre => new Color(255, 215, 0),

            // Tools
            ItemType.WoodPickaxe => new Color(139, 90, 43),
            ItemType.CopperPickaxe => new Color(184, 115, 51),
            ItemType.IronPickaxe => new Color(150, 150, 160),

            // Swords (silver-blue tint)
            ItemType.WoodSword => new Color(180, 140, 100),
            ItemType.CopperSword => new Color(200, 130, 80),
            ItemType.IronSword => new Color(180, 180, 195),
            ItemType.SilverSword => new Color(210, 210, 230),
            ItemType.GoldSword => new Color(255, 215, 0),

            // Spears (brown shaft tint)
            ItemType.WoodSpear => new Color(160, 120, 80),
            ItemType.CopperSpear => new Color(190, 120, 70),
            ItemType.IronSpear => new Color(170, 170, 185),

            // Axes
            ItemType.BattleAxe => new Color(140, 100, 70),

            // Bows (wood brown)
            ItemType.WoodBow => new Color(150, 100, 60),
            ItemType.CopperBow => new Color(180, 110, 60),
            ItemType.IronBow => new Color(160, 160, 175),

            // Whips (leather brown)
            ItemType.LeatherWhip => new Color(139, 90, 60),
            ItemType.ChainWhip => new Color(170, 170, 180),

            // Staves (magical purple)
            ItemType.WoodStaff => new Color(120, 90, 140),
            ItemType.ApprenticeStaff => new Color(140, 100, 180),
            ItemType.MageStaff => new Color(160, 80, 200),

            // Gloves (leather tan)
            ItemType.LeatherGloves => new Color(180, 140, 100),
            ItemType.IronKnuckles => new Color(160, 160, 170),

            // Boomerangs
            ItemType.WoodBoomerang => new Color(170, 130, 80),
            ItemType.IronBoomerang => new Color(165, 165, 180),

            // Stackable items - Common (white/gray)
            ItemType.SoldiersSyringeItem => new Color(200, 50, 50),   // Red syringe
            ItemType.TougherTimesItem => new Color(150, 150, 200),    // Blue bear
            ItemType.BisonSteakItem => new Color(180, 80, 80),        // Meat red
            ItemType.PaulsGoatHoofItem => new Color(139, 90, 60),     // Brown hoof
            ItemType.CritGlassesItem => new Color(200, 200, 220),     // Glass/silver
            ItemType.MonsterToothItem => new Color(220, 220, 200),    // Bone white

            // Stackable items - Uncommon (green tint)
            ItemType.HopooFeatherItem => new Color(100, 200, 100),    // Green feather
            ItemType.PredatoryInstinctsItem => new Color(180, 100, 100), // Red instincts
            ItemType.HarvestersScytheItem => new Color(150, 150, 180),// Steel scythe
            ItemType.UkuleleItem => new Color(200, 150, 100),         // Wood ukulele
            ItemType.AtgMissileItem => new Color(80, 120, 80),        // Military green

            // Stackable items - Rare (blue/red tint)
            ItemType.BrilliantBehemothItem => new Color(255, 100, 50),// Orange explosive
            ItemType.ShapedGlassItem => new Color(200, 150, 255),     // Purple glass
            ItemType.CestiusItem => new Color(200, 180, 100),         // Brass knuckles

            // Stackable items - Legendary (orange/gold)
            ItemType.SoulboundCatalystItem => new Color(255, 180, 50),// Gold catalyst
            ItemType.FiftySevenLeafCloverItem => new Color(50, 255, 50), // Bright green

            _ => Color.Magenta
        };
    }

    protected override void Draw(GameTime gameTime)
    {
        // Sky gradient based on depth
        var playerTile = WorldCoordinates.WorldToTile(_player.Position);
        int surfaceY = _worldGenerator.GetSurfaceHeight(playerTile.X);
        float depthRatio = Math.Clamp((playerTile.Y - surfaceY) / 100f, 0f, 1f);

        Color skyColor = Color.Lerp(
            new Color(135, 206, 235),  // Sky blue
            new Color(20, 20, 40),      // Dark underground
            depthRatio
        );

        GraphicsDevice.Clear(skyColor);

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: _camera.GetTransformMatrix()
        );

        DrawTiles();
        DrawChunkBorders();
        DrawPlayer();
        DrawTargetTile();
        DrawMiningProgress();
        DrawChests();
        DrawEnemies();
        DrawBosses();
        DrawDrops();
        DrawAttackHitbox();

        _spriteBatch.End();

        // Draw UI (no camera transform)
        _spriteBatch.Begin();

        // Always draw hotbar (unless inventory is open, then inventory shows hotbar)
        if (!_uiManager.IsInventoryOpen)
        {
            DrawHotbar();
        }

        DrawChargeBar();
        DrawGoldDisplay();
        DrawPlayerHealthBar();
        DrawDifficultyDisplay();
        DrawChestUI();  // Chest interaction prompts (in screen space)
        DrawDebugInfo();

        // Draw UI panels
        _uiManager.Draw(_spriteBatch, _pixelTexture, _input.MousePositionV);

        // Draw hardmode UI (progress bar, notifications, indicator)
        _hardmodeUI.Draw(_spriteBatch);

        // Draw endgame map system test UI
        _mapSystemTest.Draw(_spriteBatch, _pixelTexture, GraphicsDevice.Viewport.Bounds);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawPlayer()
    {
        // Skip drawing if player is fully dead (after fade out)
        if (_player.IsDead && _player.HealthPercent <= 0) return;

        // Get base color with invincibility flicker
        Color playerColor = Color.CornflowerBlue;

        if (_player.IsDamageFlashing)
        {
            playerColor = Color.Red;
        }
        else if (_player.IsInvincible && !_player.IsDead)
        {
            // Flicker effect - alternate between normal and semi-transparent
            double time = DateTime.Now.Ticks / (double)TimeSpan.TicksPerMillisecond;
            bool visible = ((int)(time / 50) % 2) == 0;  // Toggle every 50ms
            playerColor = visible ? Color.CornflowerBlue : Color.CornflowerBlue * 0.3f;
        }

        if (_player.IsDead)
        {
            playerColor = Color.Gray * 0.5f;
        }

        DrawRectangle(_player.Position, _player.Width, _player.Height, playerColor);

        // Draw eyes (existing code)
        int eyeSize = 4;
        int eyeOffsetY = 8;
        int eyeSpacing = 6;
        Color eyeColor = Color.White;

        if (_player.FacingDirection > 0)
        {
            DrawRectangle(
                new Vector2(_player.Position.X + _player.Width - eyeSize - 4, _player.Position.Y + eyeOffsetY),
                eyeSize, eyeSize, eyeColor
            );
            DrawRectangle(
                new Vector2(_player.Position.X + _player.Width - eyeSize - 4 - eyeSpacing, _player.Position.Y + eyeOffsetY),
                eyeSize, eyeSize, eyeColor
            );
        }
        else
        {
            DrawRectangle(
                new Vector2(_player.Position.X + 4, _player.Position.Y + eyeOffsetY),
                eyeSize, eyeSize, eyeColor
            );
            DrawRectangle(
                new Vector2(_player.Position.X + 4 + eyeSpacing, _player.Position.Y + eyeOffsetY),
                eyeSize, eyeSize, eyeColor
            );
        }
    }

    private void DrawTargetTile()
    {
        // Don't draw target tile when inventory is open
        if (_uiManager.IsAnyPanelOpen)
            return;

        // Draw outline around targeted tile
        Vector2 tileWorldPos = WorldCoordinates.TileToWorld(_mouseTilePos);

        Color outlineColor = _isTargetValid ? new Color(255, 255, 255, 150) : new Color(255, 0, 0, 100);

        // Draw outline (4 edges)
        int size = TILE_SIZE;
        int thickness = 2;

        // Top
        DrawRectangle(tileWorldPos, size, thickness, outlineColor);
        // Bottom
        DrawRectangle(new Vector2(tileWorldPos.X, tileWorldPos.Y + size - thickness), size, thickness, outlineColor);
        // Left
        DrawRectangle(tileWorldPos, thickness, size, outlineColor);
        // Right
        DrawRectangle(new Vector2(tileWorldPos.X + size - thickness, tileWorldPos.Y), thickness, size, outlineColor);
    }

    private void DrawMiningProgress()
    {
        if (_mining.CurrentTarget == null || _mining.Progress <= 0)
            return;

        var target = _mining.CurrentTarget.Value;
        Vector2 tileWorldPos = WorldCoordinates.TileToWorld(target);

        // Draw darkening overlay based on progress
        int crackedSize = (int)(TILE_SIZE * _mining.Progress);
        Color crackColor = new Color(0, 0, 0, (int)(150 * _mining.Progress));

        // Center the crack overlay
        Vector2 crackPos = tileWorldPos + new Vector2((TILE_SIZE - crackedSize) / 2f);
        DrawRectangle(crackPos, crackedSize, crackedSize, crackColor);
    }

    private void DrawChunkBorders()
    {
        var visibleArea = _camera.VisibleArea;
        var minChunk = WorldCoordinates.WorldToChunk(new Vector2(visibleArea.Left, visibleArea.Top));
        var maxChunk = WorldCoordinates.WorldToChunk(new Vector2(visibleArea.Right, visibleArea.Bottom));

        Color borderColor = new Color(255, 255, 255, 50);

        for (int cy = minChunk.Y; cy <= maxChunk.Y; cy++)
        {
            for (int cx = minChunk.X; cx <= maxChunk.X; cx++)
            {
                var worldPos = WorldCoordinates.ChunkToWorld(new Point(cx, cy));
                int size = CHUNK_SIZE * TILE_SIZE;

                DrawRectangle(worldPos, size, 1, borderColor);
                DrawRectangle(worldPos, 1, size, borderColor);
            }
        }
    }

    private void DrawTiles()
    {
        var visibleArea = _camera.VisibleArea;
        visibleArea.Inflate(TILE_SIZE * 2, TILE_SIZE * 2);

        foreach (var chunk in _chunkManager.GetChunksInBounds(visibleArea))
        {
            foreach (var (localX, localY, tile) in chunk.EnumerateTiles())
            {
                if (tile.IsAir)
                    continue;

                int worldTileX = chunk.Position.X * CHUNK_SIZE + localX;
                int worldTileY = chunk.Position.Y * CHUNK_SIZE + localY;
                float worldX = worldTileX * TILE_SIZE;
                float worldY = worldTileY * TILE_SIZE;

                Color color = GetTileColor(tile.Type);
                DrawRectangle(new Vector2(worldX, worldY), TILE_SIZE, TILE_SIZE, color);
            }
        }
    }

    private void DrawChests()
    {
        foreach (var chest in _chests)
        {
            Color chestColor = chest.GetColor();

            // Draw chest body
            DrawRectangle(chest.Position, chest.Width, chest.Height, chestColor);

            // Draw lid (lighter top part)
            Color lidColor = new Color(
                Math.Min(255, chestColor.R + 40),
                Math.Min(255, chestColor.G + 40),
                Math.Min(255, chestColor.B + 40)
            );
            DrawRectangle(chest.Position, chest.Width, 8, lidColor);

            // Draw lock/keyhole detail
            DrawRectangle(new Vector2(chest.Position.X + 12, chest.Position.Y + 12), 8, 10, new Color(60, 60, 60));
            DrawRectangle(new Vector2(chest.Position.X + 14, chest.Position.Y + 10), 4, 4, Color.Gold);
        }
    }

    /// <summary>
    /// Draw chest interaction UI (called in screen space, not world space).
    /// </summary>
    private void DrawChestUI()
    {
        // First, find the closest chest (same logic as TryInteractWithChest)
        ChestEntity? closestChest = null;
        float closestDistance = float.MaxValue;

        foreach (var chest in _chests)
        {
            if (chest.IsOpened || !chest.IsInRange(_player))
                continue;

            float distance = Vector2.Distance(_player.Center, chest.Center);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestChest = chest;
            }
        }

        // Draw UI for all chests in range, but highlight the closest
        foreach (var chest in _chests)
        {
            if (chest.IsOpened || !chest.IsInRange(_player))
                continue;

            bool isClosest = chest == closestChest;

            // Convert chest world position to screen position
            Vector2 screenPos = _camera.WorldToScreen(chest.Position);

            int cost = chest.GetCost(_chestManager);
            bool canAfford = _player.Currency.CanAfford(cost);

            // Draw cost popup above chest
            int popupWidth = isClosest ? 70 : 50;
            int popupHeight = 20;
            int popupX = (int)screenPos.X - popupWidth / 2 + chest.Width / 2;
            int popupY = (int)screenPos.Y - popupHeight - 8;

            // Background - brighter for closest chest
            Color bgColor;
            if (isClosest)
            {
                bgColor = canAfford ? new Color(0, 100, 0, 220) : new Color(100, 0, 0, 220);
            }
            else
            {
                bgColor = new Color(40, 40, 40, 160); // Dimmed for non-closest
            }
            DrawRectangle(new Vector2(popupX, popupY), popupWidth, popupHeight, bgColor);

            // Gold icon
            DrawRectangle(new Vector2(popupX + 4, popupY + 6), 8, 8, isClosest ? Color.Gold : Color.Gray);

            // Cost text
            Color textColor;
            if (isClosest)
            {
                textColor = canAfford ? Color.LightGreen : Color.Red;
            }
            else
            {
                textColor = new Color(150, 150, 150); // Dimmed
            }
            InventoryUI.DrawText(_spriteBatch, _pixelTexture, cost.ToString(), popupX + 16, popupY + 6, textColor);

            // "Press E" hint - only show for closest chest
            if (isClosest)
            {
                InventoryUI.DrawText(_spriteBatch, _pixelTexture, "[E]", popupX + popupWidth - 22, popupY + 6, Color.Yellow);
            }
        }
    }

    private void DrawGoldDisplay()
    {
        int x = 10;
        int y = 10;

        // Background
        DrawRectangle(new Vector2(x, y), 100, 24, new Color(0, 0, 0, 180));

        // Gold icon
        DrawRectangle(new Vector2(x + 4, y + 4), 16, 16, Color.Gold);

        // Gold amount text
        string goldText = _player.Currency.Gold.ToString();
        InventoryUI.DrawText(_spriteBatch, _pixelTexture, goldText, x + 24, y + 8, Color.White);
    }

    private void DrawDifficultyDisplay()
    {
        int x = _graphics.PreferredBackBufferWidth - 110;
        int y = 10;

        var color = _difficultyManager.DifficultyColor;
        Color diffColor = new Color(color.R, color.G, color.B);

        // Background (extended to include time)
        DrawRectangle(new Vector2(x, y), 100, 40, new Color(0, 0, 0, 180));

        // Difficulty indicator bar
        float fillPercent = Math.Min(_difficultyManager.Coefficient / 5f, 1f);
        DrawRectangle(new Vector2(x + 2, y + 2), (int)(96 * fillPercent), 20, diffColor);

        // Difficulty tier text
        InventoryUI.DrawText(_spriteBatch, _pixelTexture, _difficultyManager.DifficultyTier.ToUpper(), x + 4, y + 8, Color.White);

        // Time display below difficulty bar - brighter color
        int minutes = (int)(_difficultyManager.ElapsedTime / 60f);
        int seconds = (int)(_difficultyManager.ElapsedTime % 60f);
        string timeText = $"{minutes}:{seconds:D2}";
        InventoryUI.DrawText(_spriteBatch, _pixelTexture, timeText, x + 4, y + 28, Color.White);
    }

    private static Color GetTileColor(TileType type)
    {
        return type switch
        {
            // === Natural Terrain ===
            TileType.Dirt => new Color(139, 90, 43),
            TileType.Stone => new Color(128, 128, 128),
            TileType.Grass => new Color(34, 139, 34),
            TileType.Sand => new Color(238, 214, 175),
            TileType.Clay => new Color(146, 81, 68),
            TileType.Mud => new Color(92, 68, 52),
            TileType.Snow => new Color(235, 245, 255),
            TileType.Ice => new Color(185, 220, 245),
            TileType.Ash => new Color(68, 68, 68),

            // === Desert Tiles ===
            TileType.Sandstone => new Color(210, 180, 140),
            TileType.HardenedSand => new Color(200, 170, 120),
            TileType.DesertFossil => new Color(180, 160, 110),

            // === Snow/Ice Tiles ===
            TileType.SnowBrick => new Color(220, 230, 240),
            TileType.ThinIce => new Color(200, 230, 250),

            // === Jungle Tiles ===
            TileType.JungleGrass => new Color(100, 170, 70),
            TileType.LivingMahogany => new Color(120, 60, 30),
            TileType.Hive => new Color(200, 150, 50),
            TileType.HoneyBlock => new Color(255, 200, 80),

            // === Mushroom Tiles ===
            TileType.MushroomGrass => new Color(93, 127, 255),

            // === Corruption Tiles ===
            TileType.CorruptGrass => new Color(120, 100, 170),
            TileType.Ebonstone => new Color(75, 70, 100),
            TileType.CorruptSand => new Color(150, 130, 180),
            TileType.CorruptSandstone => new Color(130, 110, 160),
            TileType.CorruptIce => new Color(140, 130, 180),

            // === Crimson Tiles ===
            TileType.CrimsonGrass => new Color(180, 80, 80),
            TileType.Crimstone => new Color(140, 60, 60),
            TileType.CrimsonSand => new Color(190, 110, 110),
            TileType.CrimsonSandstone => new Color(160, 80, 80),
            TileType.CrimsonIce => new Color(180, 100, 120),
            TileType.Flesh => new Color(150, 50, 50),

            // === Hallow Tiles ===
            TileType.HallowedGrass => new Color(140, 200, 255),
            TileType.Pearlstone => new Color(200, 180, 220),
            TileType.HallowedSand => new Color(255, 220, 255),
            TileType.HallowedSandstone => new Color(230, 200, 240),
            TileType.HallowedIce => new Color(200, 180, 255),

            // === Ores ===
            TileType.CopperOre => new Color(184, 115, 51),
            TileType.IronOre => new Color(165, 142, 142),
            TileType.SilverOre => new Color(192, 192, 210),
            TileType.GoldOre => new Color(255, 215, 0),
            TileType.CobaltOre => new Color(60, 120, 200),
            TileType.MythrilOre => new Color(100, 200, 150),
            TileType.AdamantiteOre => new Color(200, 80, 80),
            TileType.Hellstone => new Color(200, 80, 40),
            TileType.DemoniteOre => new Color(100, 80, 140),
            TileType.CrimtaneOre => new Color(180, 70, 70),

            // === Wood & Plants ===
            TileType.Wood => new Color(160, 82, 45),
            TileType.LivingWood => new Color(140, 90, 50),
            TileType.Leaves => new Color(34, 120, 34),
            TileType.Cactus => new Color(85, 140, 65),
            TileType.Mushroom => new Color(200, 180, 160),
            TileType.GlowingMushroom => new Color(80, 120, 220),
            TileType.BorealWood => new Color(130, 110, 90),
            TileType.PalmWood => new Color(180, 140, 90),
            TileType.RichMahogany => new Color(140, 70, 40),
            TileType.Ebonwood => new Color(80, 70, 100),
            TileType.Shadewood => new Color(120, 60, 70),
            TileType.Pearlwood => new Color(230, 220, 240),

            // === Vines ===
            TileType.Vines => new Color(30, 100, 30),
            TileType.JungleVines => new Color(80, 150, 60),
            TileType.CorruptVines => new Color(100, 80, 150),
            TileType.CrimsonVines => new Color(160, 60, 60),
            TileType.HallowedVines => new Color(120, 180, 230),

            // === Bricks & Crafted ===
            TileType.StoneBrick => new Color(150, 150, 150),
            TileType.WoodPlatform => new Color(140, 90, 50),
            TileType.Torch => Color.Yellow,
            TileType.GrayBrick => new Color(130, 130, 130),
            TileType.RedBrick => new Color(160, 80, 70),
            TileType.DungeonBrick => new Color(60, 60, 100),
            TileType.CrackedDungeonBrick => new Color(50, 50, 85),
            TileType.LihzahrdBrick => new Color(150, 100, 50),
            TileType.Obsidian => new Color(40, 30, 50),
            TileType.CrystalBlock => new Color(200, 150, 220),
            TileType.GraniteBlock => new Color(50, 50, 70),
            TileType.MarbleBlock => new Color(230, 230, 235),

            // === Special ===
            TileType.Bedrock => new Color(30, 30, 30),

            _ => Color.Magenta  // Fallback for truly unknown types
        };
    }

    private void DrawEnemies()
    {
        foreach (var enemy in _enemyManager.GetEnemies())
        {
            Color color = enemy.GetRenderColor();

            // Draw enemy body
            DrawRectangle(enemy.Position, enemy.Width, enemy.Height, color);

            // Draw health bar above enemy (only if damaged)
            if (enemy.CurrentHealth < enemy.MaxHealth && !enemy.IsDead)
            {
                int barWidth = enemy.Width;
                int barHeight = 4;
                float healthPercent = (float)enemy.CurrentHealth / enemy.MaxHealth;

                Vector2 barPos = new Vector2(enemy.Position.X, enemy.Position.Y - 8);

                // Background
                DrawRectangle(barPos, barWidth, barHeight, Color.DarkRed);

                // Health fill
                DrawRectangle(barPos, (int)(barWidth * healthPercent), barHeight, Color.LimeGreen);
            }

            // Debug: Draw attack hitbox during attack
            if (enemy.AIState == EnemyAIState.Attack)
            {
                Rectangle atkBox = enemy.GetAttackHitbox();
                DrawRectangle(new Vector2(atkBox.X, atkBox.Y), atkBox.Width, atkBox.Height,
                    new Color(255, 0, 0, 100));
            }
        }
    }

    private void DrawBosses()
    {
        foreach (var boss in _bossManager.GetBosses())
        {
            // Convert boss color tuple to XNA Color
            var colorTuple = boss.Data.Color;
            Color baseColor = new Color(colorTuple.R, colorTuple.G, colorTuple.B);
            Color bossColor = baseColor;

            // Damage flash
            if (boss.IsDamageFlashing)
            {
                bossColor = Color.White;
            }
            // Phase transition flash
            else if (boss.IsPhaseTransitioning)
            {
                double time = DateTime.Now.Ticks / (double)TimeSpan.TicksPerMillisecond;
                bool flash = ((int)(time / 100) % 2) == 0;
                bossColor = flash ? Color.White : baseColor;
            }
            // Enrage pulsing effect
            else if (boss.IsEnraged)
            {
                double time = DateTime.Now.Ticks / (double)TimeSpan.TicksPerMillisecond;
                float pulse = (float)(Math.Sin(time / 100) * 0.3 + 0.7);
                bossColor = Color.Lerp(baseColor, Color.Red, 1f - pulse);
            }

            // Draw boss body with outline for visibility
            // Outer dark outline
            DrawRectangle(boss.Position - new Vector2(2), boss.Width + 4, boss.Height + 4, new Color(0, 0, 0, 200));
            // Boss body
            DrawRectangle(boss.Position, boss.Width, boss.Height, bossColor);

            // Draw eyes (for visual interest)
            int eyeSize = Math.Max(4, boss.Width / 8);
            int eyeY = boss.Height / 4;

            if (boss.FacingDirection > 0)
            {
                // Facing right
                DrawRectangle(new Vector2(boss.Position.X + boss.Width - eyeSize * 2 - 4, boss.Position.Y + eyeY),
                    eyeSize, eyeSize, Color.White);
                DrawRectangle(new Vector2(boss.Position.X + boss.Width - eyeSize * 4 - 8, boss.Position.Y + eyeY),
                    eyeSize, eyeSize, Color.White);
                // Pupils
                DrawRectangle(new Vector2(boss.Position.X + boss.Width - eyeSize - 4, boss.Position.Y + eyeY + 1),
                    eyeSize / 2, eyeSize / 2, Color.Black);
                DrawRectangle(new Vector2(boss.Position.X + boss.Width - eyeSize * 3 - 8, boss.Position.Y + eyeY + 1),
                    eyeSize / 2, eyeSize / 2, Color.Black);
            }
            else
            {
                // Facing left
                DrawRectangle(new Vector2(boss.Position.X + 4, boss.Position.Y + eyeY),
                    eyeSize, eyeSize, Color.White);
                DrawRectangle(new Vector2(boss.Position.X + eyeSize * 2 + 8, boss.Position.Y + eyeY),
                    eyeSize, eyeSize, Color.White);
                // Pupils
                DrawRectangle(new Vector2(boss.Position.X + 4, boss.Position.Y + eyeY + 1),
                    eyeSize / 2, eyeSize / 2, Color.Black);
                DrawRectangle(new Vector2(boss.Position.X + eyeSize * 2 + 8, boss.Position.Y + eyeY + 1),
                    eyeSize / 2, eyeSize / 2, Color.Black);
            }

            // Debug: Draw attack hitbox during attack
            if (boss.IsAttacking)
            {
                Rectangle? atkBoxNullable = boss.GetAttackHitbox();
                if (atkBoxNullable.HasValue)
                {
                    Rectangle atkBox = atkBoxNullable.Value;
                    DrawRectangle(new Vector2(atkBox.X, atkBox.Y), atkBox.Width, atkBox.Height,
                        new Color(255, 100, 0, 100));
                }
            }
        }
    }

    private void DrawDrops()
    {
        foreach (var drop in _dropManager.GetDrops())
        {
            Vector2 renderPos = drop.GetRenderPosition();
            Color color = drop.GetColor();

            // Draw drop as a small gem/coin shape
            DrawRectangle(renderPos, drop.Width, drop.Height, color);

            // Add shine effect
            DrawRectangle(
                new Vector2(renderPos.X + 2, renderPos.Y + 2),
                drop.Width / 3,
                drop.Height / 3,
                Color.White * 0.5f
            );
        }
    }

    private void DrawAttackHitbox()
    {
        if (_combat.IsAttacking)
        {
            var box = _combat.CurrentAttackBox;
            DrawRectangle(new Vector2(box.X, box.Y), box.Width, box.Height,
                new Color(255, 255, 0, 80));
        }
    }

    private void DrawPlayerHealthBar()
    {
        int barWidth = 200;
        int barHeight = 20;
        int x = (_graphics.PreferredBackBufferWidth - barWidth) / 2;  // Centered
        int y = 10;  // At the TOP

        // Background
        DrawRectangle(new Vector2(x - 2, y - 2), barWidth + 4, barHeight + 4, new Color(0, 0, 0, 200));

        // Health bar background (dark red)
        DrawRectangle(new Vector2(x, y), barWidth, barHeight, new Color(60, 0, 0));

        // Health bar fill
        float healthPercent = _player.HealthPercent;
        int fillWidth = (int)(barWidth * healthPercent);

        // Color changes based on health
        Color healthColor;
        if (healthPercent > 0.6f)
            healthColor = Color.LimeGreen;
        else if (healthPercent > 0.3f)
            healthColor = Color.Yellow;
        else
            healthColor = Color.Red;

        // Flash when recently damaged
        if (_player.IsDamageFlashing)
        {
            healthColor = Color.White;
        }

        DrawRectangle(new Vector2(x, y), fillWidth, barHeight, healthColor);

        // Health text
        string healthText = $"{_player.CurrentHealth}/{_player.MaxHealth}";
        int textX = x + barWidth / 2 - (healthText.Length * 4);  // Rough centering
        InventoryUI.DrawText(_spriteBatch, _pixelTexture, healthText, textX, y + 6, Color.White);

        // Invincibility indicator bar below health
        if (_player.IsInvincible && !_player.IsDead)
        {
            DrawRectangle(new Vector2(x, y + barHeight + 2), barWidth, 3, new Color(0, 200, 255, 150));
        }

        // "DEAD" text if dead - show in center of screen
        if (_player.IsDead)
        {
            string deadText = "DEAD - Respawning...";
            int deadX = (_graphics.PreferredBackBufferWidth - deadText.Length * 8) / 2;
            int deadY = _graphics.PreferredBackBufferHeight / 2;

            // Background
            DrawRectangle(new Vector2(deadX - 10, deadY - 10), deadText.Length * 8 + 20, 30, new Color(0, 0, 0, 220));
            InventoryUI.DrawText(_spriteBatch, _pixelTexture, deadText, deadX, deadY, Color.Red);
        }
    }

    private void DrawDebugInfo()
    {
        // Debug info - press Tab to see output in console
        if (_input.IsKeyPressed(Keys.Tab))
        {
            System.Diagnostics.Debug.WriteLine("=== DEBUG INFO ===");
            System.Diagnostics.Debug.WriteLine($"Player Position: {_player.Position}");
            System.Diagnostics.Debug.WriteLine($"Player HP: {_player.CurrentHealth}/{_player.MaxHealth}");
            System.Diagnostics.Debug.WriteLine($"Player Level: {_player.XP.Level} ({_player.XP.CurrentXP}/{_player.XP.XPToNextLevel} XP)");
            System.Diagnostics.Debug.WriteLine($"Gold: {_player.Currency.Gold}");
            System.Diagnostics.Debug.WriteLine($"Difficulty: {_difficultyManager.DifficultyTier} ({_difficultyManager.Coefficient:F2})");
            System.Diagnostics.Debug.WriteLine($"Time: {_difficultyManager.ElapsedTime:F0}s");
            System.Diagnostics.Debug.WriteLine($"Chests Opened: {_chestManager.TotalChestsOpened}");
            System.Diagnostics.Debug.WriteLine($"Active Enemies: {_enemyManager.ActiveEnemyCount}");

            // Biome info
            BiomeType currentBiome = _biomeManager.CurrentBiome;
            var biomeData = BiomeRegistry.Get(currentBiome);
            System.Diagnostics.Debug.WriteLine($"Current Biome: {currentBiome.GetDisplayName()} (Danger: {biomeData.DangerLevel})");
            System.Diagnostics.Debug.WriteLine($"  Gold x{biomeData.GoldMultiplier:F1} | XP x{biomeData.XPMultiplier:F1} | Spawns x{biomeData.SpawnRateMultiplier:F1}");
            System.Diagnostics.Debug.WriteLine($"  Hardmode: {_biomeManager.IsHardmode}");

            System.Diagnostics.Debug.WriteLine($"Stats: {_player.Stats.GetStatSummary()}");
            System.Diagnostics.Debug.WriteLine($"Upgrades: {_player.UpgradeStats.GetSummary()}");
            System.Diagnostics.Debug.WriteLine($"Rerolls: {_player.LevelUp.RerollsRemaining}/{_player.LevelUp.MaxRerolls} | Banishes: {_player.LevelUp.BanishesRemaining}/{_player.LevelUp.MaxBanishes}");
            System.Diagnostics.Debug.WriteLine($"Attack Speed: {_player.Stats.AttackSpeed:P0}");
            System.Diagnostics.Debug.WriteLine($"Move Speed: {_player.Stats.MoveSpeed:F0}");

            var selected = _player.Inventory.SelectedItem;
            System.Diagnostics.Debug.WriteLine($"Selected: {selected.Type} x{selected.Count}");
            System.Diagnostics.Debug.WriteLine($"Equipped Weapon: {(_player.Weapons.HasWeaponEquipped ? $"{_player.Weapons.EquippedType} (Lv.{_player.Weapons.EquippedWeapon?.Level})" : "None")}");

            foreach (var chest in _chests)
            {
                if (!chest.IsOpened && chest.IsInRange(_player))
                {
                    System.Diagnostics.Debug.WriteLine($"Nearby chest: {chest.ChestType} - Cost: {chest.GetCost(_chestManager)}");
                }
            }
            System.Diagnostics.Debug.WriteLine("==================");
        }

        // Draw debug overlay (F3)
        if (_showDebugOverlay)
        {
            DrawDebugOverlay();
        }

        // Draw map (M or F4)
        if (_showMap)
        {
            DrawMap();
        }
    }

    /// <summary>
    /// Draw on-screen debug overlay with current game state.
    /// </summary>
    private void DrawDebugOverlay()
    {
        int x = 10;
        int y = 150;
        int lineHeight = 14;
        Color textColor = Color.White;
        Color bgColor = new Color(0, 0, 0, 180);

        // Background panel
        int panelWidth = 280;
        int panelHeight = 220;
        _spriteBatch.Draw(_pixelTexture, new Rectangle(x - 5, y - 5, panelWidth, panelHeight), bgColor);

        // Title
        InventoryUI.DrawText(_spriteBatch, _pixelTexture, "=== DEBUG (F3) ===", x, y, Color.Yellow);
        y += lineHeight + 4;

        // Debug mode status
        string godStatus = _player.GodMode ? "ON" : "OFF";
        string noclipStatus = _player.Noclip ? "ON" : "OFF";
        InventoryUI.DrawText(_spriteBatch, _pixelTexture, $"God Mode (F1): {godStatus}", x, y, _player.GodMode ? Color.LimeGreen : textColor);
        y += lineHeight;
        InventoryUI.DrawText(_spriteBatch, _pixelTexture, $"Noclip (F2): {noclipStatus}", x, y, _player.Noclip ? Color.LimeGreen : textColor);
        y += lineHeight + 4;

        // Position info
        Point tilePos = WorldCoordinates.WorldToTile(_player.Center);
        int surfaceY = _worldGenerator.GetSurfaceHeight(tilePos.X);
        int depth = tilePos.Y - surfaceY;
        InventoryUI.DrawText(_spriteBatch, _pixelTexture, $"Position: {tilePos.X}, {tilePos.Y}", x, y, textColor);
        y += lineHeight;
        InventoryUI.DrawText(_spriteBatch, _pixelTexture, $"Depth: {depth} tiles", x, y, textColor);
        y += lineHeight;

        // Layer info
        WorldLayer layer = _worldGenerator.Config.GetLayerAt(tilePos.Y);
        InventoryUI.DrawText(_spriteBatch, _pixelTexture, $"Layer: {layer}", x, y, GetLayerColor(layer));
        y += lineHeight;

        // Biome info
        BiomeType biome = _biomeManager.CurrentBiome;
        InventoryUI.DrawText(_spriteBatch, _pixelTexture, $"Biome: {biome.GetDisplayName()}", x, y, textColor);
        y += lineHeight + 4;

        // Player stats
        InventoryUI.DrawText(_spriteBatch, _pixelTexture, $"HP: {_player.CurrentHealth}/{_player.MaxHealth}", x, y, textColor);
        y += lineHeight;
        InventoryUI.DrawText(_spriteBatch, _pixelTexture, $"Level: {_player.XP.Level} | XP: {_player.XP.CurrentXP}/{_player.XP.XPToNextLevel}", x, y, textColor);
        y += lineHeight;
        InventoryUI.DrawText(_spriteBatch, _pixelTexture, $"Gold: {_player.Currency.Gold}", x, y, Color.Gold);
        y += lineHeight;

        // Difficulty
        InventoryUI.DrawText(_spriteBatch, _pixelTexture, $"Difficulty: {_difficultyManager.DifficultyTier} ({_difficultyManager.Coefficient:F2}x)", x, y, textColor);
        y += lineHeight;

        // Enemy count
        InventoryUI.DrawText(_spriteBatch, _pixelTexture, $"Enemies: {_enemyManager.ActiveEnemyCount}", x, y, textColor);
    }

    private Color GetLayerColor(WorldLayer layer)
    {
        return layer switch
        {
            WorldLayer.Space => Color.DarkBlue,
            WorldLayer.Surface => Color.LimeGreen,
            WorldLayer.Underground => Color.SandyBrown,
            WorldLayer.Cavern => Color.Gray,
            WorldLayer.Underworld => Color.OrangeRed,
            _ => Color.White
        };
    }

    /// <summary>
    /// Get the map bounds rectangle.
    /// </summary>
    private Rectangle GetMapBounds()
    {
        int screenWidth = _graphics.PreferredBackBufferWidth;
        int screenHeight = _graphics.PreferredBackBufferHeight;
        int margin = 50;
        return new Rectangle(margin, margin, screenWidth - margin * 2, screenHeight - margin * 2 - 60);
    }

    /// <summary>
    /// Draw world map overlay with zoom, pan, and fog of war.
    /// Shows exact 1:1 tile representation.
    /// </summary>
    private void DrawMap()
    {
        int screenWidth = _graphics.PreferredBackBufferWidth;
        int screenHeight = _graphics.PreferredBackBufferHeight;

        Rectangle mapBounds = GetMapBounds();

        // Full screen semi-transparent background
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, screenWidth, screenHeight), new Color(0, 0, 0, 200));

        // Map border
        _spriteBatch.Draw(_pixelTexture, new Rectangle(mapBounds.X - 3, mapBounds.Y - 3, mapBounds.Width + 6, mapBounds.Height + 6), Color.Gray);
        _spriteBatch.Draw(_pixelTexture, mapBounds, new Color(20, 30, 40));

        // Title and controls
        InventoryUI.DrawText(_spriteBatch, _pixelTexture, "WORLD MAP", mapBounds.X, mapBounds.Y - 25, Color.White);
        string controls = "[M] Close  [Scroll/+/-] Zoom  [WASD/Arrows/Drag] Pan  [R] Reset";
        InventoryUI.DrawText(_spriteBatch, _pixelTexture, controls, mapBounds.X + 120, mapBounds.Y - 25, Color.Gray);

        // Get visible world area based on zoom/pan
        Rectangle visibleWorld = _mapManager.GetVisibleWorldBounds();
        int worldWidth = _worldGenerator.Config.Width;
        int worldHeight = _worldGenerator.Config.Height;

        // Calculate scale for rendering (pixels per tile)
        float scaleX = (float)mapBounds.Width / visibleWorld.Width;
        float scaleY = (float)mapBounds.Height / visibleWorld.Height;
        float scale = Math.Min(scaleX, scaleY);

        // Calculate actual rendered size
        int renderedWidth = (int)(visibleWorld.Width * scale);
        int renderedHeight = (int)(visibleWorld.Height * scale);
        int offsetX = mapBounds.X + (mapBounds.Width - renderedWidth) / 2;
        int offsetY = mapBounds.Y + (mapBounds.Height - renderedHeight) / 2;

        // Pixel size for each tile (at least 1, more when zoomed in)
        int pixelSize = Math.Max(1, (int)Math.Ceiling(scale));

        // Sample rate - at high zoom, render every tile (sampleRate=1)
        // At low zoom, skip tiles to improve performance
        int sampleRate = scale >= 1.0f ? 1 : Math.Max(1, (int)(1.0f / scale));

        // Clamp visible bounds to world
        int startX = Math.Max(0, visibleWorld.X);
        int endX = Math.Min(worldWidth, visibleWorld.X + visibleWorld.Width);
        int startY = Math.Max(0, visibleWorld.Y);
        int endY = Math.Min(worldHeight, visibleWorld.Y + visibleWorld.Height);

        // Draw terrain with exact tile colors
        for (int wx = startX; wx < endX; wx += sampleRate)
        {
            int screenX = offsetX + (int)((wx - visibleWorld.X) * scale);

            // Skip if outside map bounds
            if (screenX + pixelSize < mapBounds.X || screenX >= mapBounds.X + mapBounds.Width) continue;

            for (int wy = startY; wy < endY; wy += sampleRate)
            {
                int screenY = offsetY + (int)((wy - visibleWorld.Y) * scale);

                // Skip if outside map bounds
                if (screenY + pixelSize < mapBounds.Y || screenY >= mapBounds.Y + mapBounds.Height) continue;

                // Check fog of war - is this tile explored?
                bool explored = _mapManager.IsTileExplored(wx, wy);

                Color tileColor;
                if (!explored)
                {
                    // Unexplored - dark fog
                    tileColor = new Color(15, 15, 25);
                }
                else
                {
                    // Try to get from cache first (for unloaded chunks)
                    Color? cachedColor = _mapManager.GetCachedTileColor(wx, wy);
                    if (cachedColor.HasValue)
                    {
                        tileColor = cachedColor.Value;
                    }
                    else
                    {
                        // Get live data from chunk manager
                        var tile = _chunkManager.GetTileAt(wx, wy);
                        tileColor = GetTileMapColor(tile.Type);
                    }
                }

                // Draw the tile
                int drawX = Math.Max(screenX, mapBounds.X);
                int drawY = Math.Max(screenY, mapBounds.Y);
                int drawWidth = Math.Min(pixelSize, mapBounds.X + mapBounds.Width - drawX);
                int drawHeight = Math.Min(pixelSize, mapBounds.Y + mapBounds.Height - drawY);

                if (drawWidth > 0 && drawHeight > 0)
                {
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(drawX, drawY, drawWidth, drawHeight), tileColor);
                }
            }
        }

        // Draw player position marker
        Point playerTile = WorldCoordinates.WorldToTile(_player.Center);
        if (playerTile.X >= visibleWorld.X && playerTile.X < visibleWorld.X + visibleWorld.Width &&
            playerTile.Y >= visibleWorld.Y && playerTile.Y < visibleWorld.Y + visibleWorld.Height)
        {
            int playerScreenX = offsetX + (int)((playerTile.X - visibleWorld.X) * scale);
            int playerScreenY = offsetY + (int)((playerTile.Y - visibleWorld.Y) * scale);

            // Blinking marker - size scales with zoom but has minimum visibility
            bool blink = ((int)(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond / 250) % 2) == 0;
            int markerSize = Math.Max(4, (int)(scale * 2));

            if (blink)
            {
                _spriteBatch.Draw(_pixelTexture, new Rectangle(playerScreenX - markerSize / 2 - 1, playerScreenY - markerSize / 2 - 1, markerSize + 2, markerSize + 2), Color.White);
            }
            _spriteBatch.Draw(_pixelTexture, new Rectangle(playerScreenX - markerSize / 2, playerScreenY - markerSize / 2, markerSize, markerSize), Color.Blue);
        }

        // Draw spawn point marker (if visible and explored)
        int spawnX = _worldGenerator.Config.Width / 2;
        int spawnY = _worldGenerator.GetSurfaceHeight(spawnX);
        if (spawnX >= visibleWorld.X && spawnX < visibleWorld.X + visibleWorld.Width &&
            spawnY >= visibleWorld.Y && spawnY < visibleWorld.Y + visibleWorld.Height &&
            _mapManager.IsTileExplored(spawnX, spawnY))
        {
            int spawnScreenX = offsetX + (int)((spawnX - visibleWorld.X) * scale);
            int spawnScreenY = offsetY + (int)((spawnY - visibleWorld.Y) * scale);
            int markerSize = Math.Max(3, (int)(scale * 1.5f));
            _spriteBatch.Draw(_pixelTexture, new Rectangle(spawnScreenX - markerSize / 2, spawnScreenY - markerSize / 2, markerSize, markerSize), Color.Yellow);
        }

        // Draw status bar at top of map
        DrawMapStatusBar(mapBounds);

        // Draw hover info at center bottom
        DrawMapHoverInfo(mapBounds);
    }

    /// <summary>
    /// Get the map color for a specific tile type.
    /// Returns the exact visual color for 1:1 tile representation.
    /// </summary>
    private Color GetTileMapColor(TileType tile)
    {
        return tile switch
        {
            // Air/Sky
            TileType.Air => new Color(135, 206, 235),  // Sky blue

            // Basic terrain
            TileType.Dirt => new Color(151, 107, 75),
            TileType.Stone => new Color(128, 128, 128),
            TileType.Grass => new Color(28, 216, 94),
            TileType.Sand => new Color(219, 190, 127),
            TileType.Clay => new Color(146, 81, 68),
            TileType.Mud => new Color(92, 68, 73),
            TileType.Snow => new Color(235, 240, 255),
            TileType.Ice => new Color(160, 200, 255),
            TileType.Ash => new Color(68, 68, 76),

            // Desert tiles
            TileType.Sandstone => new Color(215, 182, 109),
            TileType.HardenedSand => new Color(190, 160, 100),
            TileType.DesertFossil => new Color(180, 165, 130),

            // Snow/Ice tiles
            TileType.SnowBrick => new Color(200, 210, 230),
            TileType.ThinIce => new Color(180, 220, 255),

            // Jungle tiles
            TileType.JungleGrass => new Color(143, 215, 29),
            TileType.LivingMahogany => new Color(130, 80, 55),
            TileType.Hive => new Color(218, 164, 32),
            TileType.HoneyBlock => new Color(255, 200, 50),

            // Mushroom tiles
            TileType.MushroomGrass => new Color(93, 127, 255),

            // Corruption tiles
            TileType.CorruptGrass => new Color(109, 90, 178),
            TileType.Ebonstone => new Color(75, 70, 100),
            TileType.CorruptSand => new Color(120, 100, 140),
            TileType.CorruptSandstone => new Color(100, 85, 120),
            TileType.CorruptIce => new Color(140, 130, 180),

            // Crimson tiles
            TileType.CrimsonGrass => new Color(185, 50, 50),
            TileType.Crimstone => new Color(140, 50, 55),
            TileType.CrimsonSand => new Color(170, 90, 80),
            TileType.CrimsonSandstone => new Color(150, 70, 65),
            TileType.CrimsonIce => new Color(200, 100, 110),
            TileType.Flesh => new Color(160, 60, 65),

            // Hallow tiles
            TileType.HallowedGrass => new Color(80, 230, 200),
            TileType.Pearlstone => new Color(200, 180, 255),
            TileType.HallowedSand => new Color(230, 210, 255),
            TileType.HallowedSandstone => new Color(210, 190, 240),
            TileType.HallowedIce => new Color(220, 200, 255),

            // Ores - distinctive bright colors
            TileType.CopperOre => new Color(205, 130, 80),
            TileType.IronOre => new Color(150, 120, 100),
            TileType.SilverOre => new Color(185, 195, 205),
            TileType.GoldOre => new Color(255, 215, 0),
            TileType.CobaltOre => new Color(60, 100, 200),
            TileType.MythrilOre => new Color(100, 200, 130),
            TileType.AdamantiteOre => new Color(200, 60, 100),
            TileType.Hellstone => new Color(255, 90, 30),
            TileType.DemoniteOre => new Color(120, 80, 180),
            TileType.CrimtaneOre => new Color(200, 50, 60),

            // Wood & Plants
            TileType.Wood => new Color(168, 125, 72),
            TileType.LivingWood => new Color(130, 100, 60),
            TileType.Leaves => new Color(50, 180, 60),
            TileType.Cactus => new Color(90, 150, 50),
            TileType.Mushroom => new Color(200, 170, 140),
            TileType.GlowingMushroom => new Color(90, 130, 220),
            TileType.BorealWood => new Color(140, 130, 120),
            TileType.PalmWood => new Color(180, 140, 90),
            TileType.RichMahogany => new Color(140, 70, 50),
            TileType.Ebonwood => new Color(80, 75, 95),
            TileType.Shadewood => new Color(120, 60, 65),
            TileType.Pearlwood => new Color(200, 200, 220),

            // Vines
            TileType.Vines => new Color(40, 140, 50),
            TileType.JungleVines => new Color(100, 180, 40),
            TileType.CorruptVines => new Color(90, 80, 140),
            TileType.CrimsonVines => new Color(150, 50, 55),
            TileType.HallowedVines => new Color(120, 200, 180),

            // Bricks & Crafted
            TileType.StoneBrick => new Color(140, 140, 140),
            TileType.WoodPlatform => new Color(150, 110, 65),
            TileType.Torch => new Color(255, 200, 80),
            TileType.GrayBrick => new Color(110, 110, 110),
            TileType.RedBrick => new Color(170, 80, 70),
            TileType.DungeonBrick => new Color(65, 75, 105),
            TileType.CrackedDungeonBrick => new Color(55, 65, 90),
            TileType.LihzahrdBrick => new Color(180, 130, 50),
            TileType.Obsidian => new Color(40, 30, 50),
            TileType.CrystalBlock => new Color(180, 100, 200),
            TileType.GraniteBlock => new Color(50, 50, 70),
            TileType.MarbleBlock => new Color(220, 220, 230),

            // Special
            TileType.Bedrock => new Color(20, 20, 20),

            // Default fallback - magenta for unmapped tiles (easy to spot)
            _ => Color.Magenta
        };
    }

    /// <summary>
    /// Draw map status bar with zoom, exploration, and coordinates.
    /// </summary>
    private void DrawMapStatusBar(Rectangle mapBounds)
    {
        Point playerTile = WorldCoordinates.WorldToTile(_player.Center);
        int surfaceY = _worldGenerator.GetSurfaceHeight(playerTile.X);
        int depth = playerTile.Y - surfaceY;

        string zoomText = $"Zoom: {_mapManager.Zoom:F2}x";
        string exploreText = $"Explored: {_mapManager.ExplorationPercent:F1}%";
        string posText = $"Position: {playerTile.X}, {playerTile.Y} (Depth: {depth})";

        int y = mapBounds.Y + mapBounds.Height + 5;

        InventoryUI.DrawText(_spriteBatch, _pixelTexture, zoomText, mapBounds.X, y, Color.Cyan);
        InventoryUI.DrawText(_spriteBatch, _pixelTexture, exploreText, mapBounds.X + 120, y, Color.LimeGreen);
        InventoryUI.DrawText(_spriteBatch, _pixelTexture, posText, mapBounds.X + 280, y, Color.White);
    }

    /// <summary>
    /// Draw block hover info at center bottom of map.
    /// </summary>
    private void DrawMapHoverInfo(Rectangle mapBounds)
    {
        if (_mapManager.HoveredTile == null) return;

        Point tile = _mapManager.HoveredTile.Value;
        int infoWidth = 350;
        int infoHeight = 70;
        int infoX = mapBounds.X + (mapBounds.Width - infoWidth) / 2;
        int infoY = mapBounds.Y + mapBounds.Height - infoHeight - 10;

        // Background panel
        _spriteBatch.Draw(_pixelTexture, new Rectangle(infoX - 2, infoY - 2, infoWidth + 4, infoHeight + 4), Color.Gray);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(infoX, infoY, infoWidth, infoHeight), new Color(30, 30, 50));

        // Title
        InventoryUI.DrawText(_spriteBatch, _pixelTexture, "TILE INFO", infoX + infoWidth / 2 - 35, infoY + 5, Color.Yellow);

        if (!_mapManager.IsTileExplored(tile.X, tile.Y))
        {
            InventoryUI.DrawText(_spriteBatch, _pixelTexture, "Unexplored", infoX + infoWidth / 2 - 40, infoY + 25, Color.Gray);
            InventoryUI.DrawText(_spriteBatch, _pixelTexture, $"Position: {tile.X}, {tile.Y}", infoX + infoWidth / 2 - 60, infoY + 45, Color.DarkGray);
        }
        else
        {
            // Position
            InventoryUI.DrawText(_spriteBatch, _pixelTexture, $"Position: {tile.X}, {tile.Y}", infoX + 10, infoY + 22, Color.White);

            // Tile type
            string tileName = _mapManager.HoveredTileType.ToString();
            InventoryUI.DrawText(_spriteBatch, _pixelTexture, $"Tile: {tileName}", infoX + 10, infoY + 38, Color.LightGray);

            // Biome and layer
            string biome = _mapManager.HoveredBiome.GetDisplayName();
            string layer = _mapManager.HoveredLayer.ToString();
            InventoryUI.DrawText(_spriteBatch, _pixelTexture, $"Biome: {biome}", infoX + 180, infoY + 22, GetBiomeMapColor(_mapManager.HoveredBiome));
            InventoryUI.DrawText(_spriteBatch, _pixelTexture, $"Layer: {layer}", infoX + 180, infoY + 38, GetLayerColor(_mapManager.HoveredLayer));

            // Depth
            if (_mapManager.HoveredDepth >= 0)
            {
                InventoryUI.DrawText(_spriteBatch, _pixelTexture, $"Depth: {_mapManager.HoveredDepth}", infoX + 180, infoY + 54, Color.Gray);
            }
        }
    }

    private void DrawBiomeLabels(int offsetX, int offsetY, float scale, int mapWidth)
    {
        // Sample biome zones and draw labels
        int lastBiomeX = 0;
        BiomeType lastBiome = BiomeType.Ocean;

        for (int x = 0; x < _worldGenerator.Config.Width; x += 100)
        {
            BiomeType biome = _worldGenerator.GetSurfaceBiome(x);
            if (biome != lastBiome)
            {
                // Draw label at biome boundary
                int labelX = offsetX + (int)(x * scale);
                int labelY = offsetY - 15;
                InventoryUI.DrawText(_spriteBatch, _pixelTexture, biome.GetDisplayName(), labelX, labelY, GetBiomeMapColor(biome));
                lastBiome = biome;
                lastBiomeX = x;
            }
        }
    }

    private Color GetBiomeMapColor(BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Forest => new Color(34, 139, 34),
            BiomeType.Desert => new Color(238, 214, 175),
            BiomeType.Snow => new Color(200, 220, 255),
            BiomeType.Jungle => new Color(80, 150, 60),
            BiomeType.Ocean => new Color(65, 105, 225),
            BiomeType.Corruption => new Color(100, 80, 150),
            BiomeType.Crimson => new Color(180, 80, 80),
            BiomeType.Hallow => new Color(200, 180, 255),
            BiomeType.Mushroom => new Color(93, 127, 255),
            _ => Color.Green
        };
    }

    private void DrawRectangle(Vector2 position, int width, int height, Color color)
    {
        _spriteBatch.Draw(
            _pixelTexture,
            new Rectangle((int)position.X, (int)position.Y, width, height),
            color
        );
    }
}