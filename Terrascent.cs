using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terrascent.Core;
using Terrascent.Entities;
using Terrascent.Items;
using Terrascent.Saves;
using Terrascent.Systems;
using Terrascent.World;
using Terrascent.World.Generation;
using Terrascent.Combat;
using Terrascent.UI;

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

    // Entities
    private Player _player = null!;

    // Systems
    private MiningSystem _mining = null!;
    private BuildingSystem _building = null!;

    private CombatSystem _combat = null!;

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
                // No player save, spawn at surface
                int surfaceY = _worldGenerator.GetSurfaceHeight(0);
                _player.SpawnAt(0, surfaceY);
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

            // Spawn player above the surface at world center
            int spawnX = 0;
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
    }

    protected override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _input.Update();

        // Update UI first (may consume input)
        _uiManager.Update(deltaTime);

        // Only exit with Escape if no UI is open
        if (_input.IsKeyPressed(Keys.Escape) && !_uiManager.IsAnyPanelOpen)
            Exit();

        // Save game (F6)
        if (_input.IsKeyPressed(Keys.F6))
        {
            _saveManager.SaveAll(_worldSeed, _player, _chunkManager);
        }

        // Regenerate world with new seed (F5) - also deletes save
        if (_input.IsKeyPressed(Keys.F5))
        {
            RegenerateWorld();
        }

        // Only run gameplay updates if no UI panel is blocking
        if (!_uiManager.IsAnyPanelOpen)
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
        _saveManager.SaveAll(_worldSeed, _player, _chunkManager);
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

        _worldGenerator = new WorldGenerator(_worldSeed);
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

        // Respawn player
        int surfaceY = _worldGenerator.GetSurfaceHeight(0);
        _player.SpawnAt(0, surfaceY);
        _camera.CenterOn(_player.Center);

        System.Diagnostics.Debug.WriteLine($"Regenerated world with seed: {_worldSeed}");
    }

    private void FixedUpdate()
    {
        float dt = GameLoop.TICK_DURATION;

        _player.HandleInput(_input, dt);
        _player.Update(dt);
        _player.ApplyMovement(dt, _chunkManager);

        // Update equipped weapon based on hotbar selection
        _player.UpdateEquippedWeapon();

        // Debug: Show what's selected
        var selected = _player.Inventory.SelectedItem;
        if (_input.IsKeyPressed(Keys.Tab))
        {
            System.Diagnostics.Debug.WriteLine($"Selected slot {_player.Inventory.SelectedSlot}: {selected.Type} x{selected.Count}");
            System.Diagnostics.Debug.WriteLine($"Is weapon? {WeaponRegistry.IsWeapon(selected.Type)}");
            System.Diagnostics.Debug.WriteLine($"Equipped weapon: {_player.Weapons.EquippedWeapon}");
        }

        if (_input.IsKeyPressed(Keys.Tab))
        {
            System.Diagnostics.Debug.WriteLine($"Stats: {_player.Stats.GetStatSummary()}");
            System.Diagnostics.Debug.WriteLine($"Attack Speed: {_player.Stats.AttackSpeed:P0}");
            System.Diagnostics.Debug.WriteLine($"Move Speed: {_player.Stats.MoveSpeed:F0}");
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

        _spriteBatch.End();

        // Draw UI (no camera transform)
        _spriteBatch.Begin();

        // Always draw hotbar (unless inventory is open, then inventory shows hotbar)
        if (!_uiManager.IsInventoryOpen)
        {
            DrawHotbar();
        }

        DrawChargeBar();
        DrawDebugInfo();

        // Draw UI panels
        _uiManager.Draw(_spriteBatch, _pixelTexture, _input.MousePositionV);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawPlayer()
    {
        // Draw player body
        Color playerColor = _player.OnGround ? Color.CornflowerBlue : Color.DodgerBlue;
        DrawRectangle(_player.Position, _player.Width, _player.Height, playerColor);

        // Draw a simple "eye" to show facing direction
        int eyeX = _player.FacingDirection > 0 ? _player.Width - 8 : 4;
        Vector2 eyePos = new(_player.Position.X + eyeX, _player.Position.Y + 8);
        DrawRectangle(eyePos, 4, 4, Color.White);
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

    private static Color GetTileColor(TileType type)
    {
        return type switch
        {
            TileType.Dirt => new Color(139, 90, 43),
            TileType.Stone => new Color(128, 128, 128),
            TileType.Grass => new Color(34, 139, 34),
            TileType.Leaves => new Color(34, 120, 34),
            TileType.Sand => new Color(238, 214, 175),
            TileType.CopperOre => new Color(184, 115, 51),
            TileType.IronOre => new Color(165, 142, 142),
            TileType.SilverOre => new Color(192, 192, 210),
            TileType.GoldOre => new Color(255, 215, 0),
            TileType.Wood => new Color(160, 82, 45),
            TileType.Torch => Color.Yellow,
            _ => Color.Magenta
        };
    }

    private void DrawDebugInfo()
    {
        // TODO: Add font and draw debug text
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