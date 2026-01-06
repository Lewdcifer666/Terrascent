using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terrascent.Core;
using Terrascent.World;
using Terrascent.World.Generation;

namespace Terrascent;

public class TerrascentGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;

    // Core systems
    private GameLoop _gameLoop = null!;
    private InputManager _input = null!;
    private Camera _camera = null!;

    // World
    private ChunkManager _chunkManager = null!;
    private WorldGenerator _worldGenerator = null!;

    // Temp rendering
    private Texture2D _pixelTexture = null!;

    // Game constants
    public const int TILE_SIZE = 16;
    public const int CHUNK_SIZE = 32;

    // World seed (can be randomized or set)
    private const int WORLD_SEED = 12345;

    // Temp: Player position for testing
    private Vector2 _playerPosition;
    private Vector2 _playerVelocity;
    private const float PLAYER_SPEED = 200f;

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

        // Create world generator
        _worldGenerator = new WorldGenerator(WORLD_SEED);

        // === DEBUG: Test generation directly ===
        System.Diagnostics.Debug.WriteLine("=== GENERATION DEBUG ===");

        // Test surface height
        int testSurfaceY = _worldGenerator.GetSurfaceHeight(0);
        System.Diagnostics.Debug.WriteLine($"Surface at X=0: Y={testSurfaceY}");

        // Create a test chunk and check its contents
        var testChunk = new Chunk(0, 3); // Chunk at Y=3 should contain surface
        _worldGenerator.GenerateChunk(testChunk);

        int solidCount = 0;
        int airCount = 0;
        for (int y = 0; y < Chunk.SIZE; y++)
        {
            for (int x = 0; x < Chunk.SIZE; x++)
            {
                var tile = testChunk.GetTile(x, y);
                if (tile.IsAir)
                    airCount++;
                else
                    solidCount++;
            }
        }

        System.Diagnostics.Debug.WriteLine($"Test Chunk(0,3): Solid={solidCount}, Air={airCount}");
        System.Diagnostics.Debug.WriteLine($"First tile: {testChunk.GetTile(0, 0).Type}");
        System.Diagnostics.Debug.WriteLine($"Tile at 0,20: {testChunk.GetTile(0, 20).Type}");
        System.Diagnostics.Debug.WriteLine("=== END DEBUG ===");

        // Create chunk manager with generator
        _chunkManager = new ChunkManager
        {
            Generator = _worldGenerator,
            LoadRadius = 4  // Load more chunks for smoother exploration
        };

        // Subscribe to chunk events for debugging
        _chunkManager.OnChunkLoaded += chunk =>
            System.Diagnostics.Debug.WriteLine($"Generated: {chunk}");
        _chunkManager.OnChunkUnloaded += chunk =>
            System.Diagnostics.Debug.WriteLine($"Unloaded: {chunk}");

        // Spawn player above the surface at world center
        int spawnX = 0;
        int surfaceY = _worldGenerator.GetSurfaceHeight(spawnX);
        _playerPosition = new Vector2(
            spawnX * TILE_SIZE,
            (surfaceY - 5) * TILE_SIZE  // 5 tiles above surface
        );

        System.Diagnostics.Debug.WriteLine($"World Seed: {WORLD_SEED}");
        System.Diagnostics.Debug.WriteLine($"Spawn surface Y: {surfaceY}");
        System.Diagnostics.Debug.WriteLine($"Player spawn: {_playerPosition}");


        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _camera = new Camera(GraphicsDevice.Viewport);
        _camera.CenterOn(_playerPosition);

        // Create a 1x1 white texture for primitive rendering
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData([Color.White]);

        // Generate initial test terrain
        //GenerateTestTerrain();

    }

    protected override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _input.Update();

        if (_input.IsKeyPressed(Keys.Escape))
            Exit();

        // Regenerate world with new seed
        if (_input.IsKeyPressed(Keys.F5))
        {
            RegenerateWorld();
        }

        _gameLoop.Update(deltaTime, FixedUpdate);
        VariableUpdate(deltaTime);

        base.Update(gameTime);
    }

    private void RegenerateWorld()
    {
        int newSeed = Random.Shared.Next();

        _worldGenerator = new WorldGenerator(newSeed);
        _chunkManager.Clear();
        _chunkManager.Generator = _worldGenerator;

        // Respawn player
        int surfaceY = _worldGenerator.GetSurfaceHeight(0);
        _playerPosition = new Vector2(0, (surfaceY - 5) * TILE_SIZE);
        _camera.CenterOn(_playerPosition);

        System.Diagnostics.Debug.WriteLine($"Regenerated world with seed: {newSeed}");
    }

    private void FixedUpdate()
    {
        Vector2 moveInput = new(
            _input.GetHorizontalAxis(),
            _input.GetVerticalAxis()
        );

        if (moveInput.LengthSquared() > 0)
            moveInput.Normalize();

        _playerVelocity = moveInput * PLAYER_SPEED;
        _playerPosition += _playerVelocity * GameLoop.TICK_DURATION;
    }

    private void VariableUpdate(float deltaTime)
    {
        // Update chunk loading around player
        _chunkManager.UpdateLoadedChunks(_playerPosition);

        // Camera follows player
        _camera.Follow(_playerPosition);
        _camera.Update(deltaTime);

        // Zoom controls
        if (_input.ScrollWheelDelta != 0)
        {
            float zoomDelta = _input.ScrollWheelDelta > 0 ? 0.1f : -0.1f;
            _camera.AdjustZoom(zoomDelta);
        }

        if (_input.IsKeyPressed(Keys.R))
            _camera.SetZoom(1f);

        // Debug: Click to inspect tile
        if (_input.IsLeftMousePressed())
        {
            var worldPos = _camera.ScreenToWorld(_input.MousePositionV);
            var tilePos = WorldCoordinates.WorldToTile(worldPos);
            var tile = _chunkManager.GetTileAt(tilePos);
            int surfaceY = _worldGenerator.GetSurfaceHeight(tilePos.X);
            int depth = tilePos.Y - surfaceY;
            System.Diagnostics.Debug.WriteLine($"Tile {tilePos}: {tile.Type}, Depth: {depth}");
        }

        // Debug: Right-click to mine/place
        if (_input.IsRightMousePressed())
        {
            var worldPos = _camera.ScreenToWorld(_input.MousePositionV);
            var tilePos = WorldCoordinates.WorldToTile(worldPos);
            var tile = _chunkManager.GetTileAt(tilePos);

            if (tile.IsAir)
                _chunkManager.SetTileTypeAt(tilePos.X, tilePos.Y, TileType.Stone);
            else
                _chunkManager.ClearTileAt(tilePos.X, tilePos.Y);
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        // Sky gradient based on depth
        var playerTile = WorldCoordinates.WorldToTile(_playerPosition);
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
        DrawRectangle(_playerPosition - new Vector2(12, 24), 24, 48, Color.CornflowerBlue);

        _spriteBatch.End();

        // Draw UI (no camera transform)
        _spriteBatch.Begin();
        DrawDebugInfo();
        _spriteBatch.End();

        base.Draw(gameTime);
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

                // Draw chunk border (top and left lines)
                DrawRectangle(worldPos, size, 1, borderColor);  // Top
                DrawRectangle(worldPos, 1, size, borderColor);  // Left
            }
        }
    }

    private void DrawTiles()
    {
        // Only draw tiles visible on screen
        var visibleArea = _camera.VisibleArea;

        // Expand slightly to avoid popping at edges
        visibleArea.Inflate(TILE_SIZE * 2, TILE_SIZE * 2);

        foreach (var chunk in _chunkManager.GetChunksInBounds(visibleArea))
        {
            foreach (var (localX, localY, tile) in chunk.EnumerateTiles())
            {
                if (tile.IsAir)
                    continue;

                // Calculate world position
                int worldTileX = chunk.Position.X * CHUNK_SIZE + localX;
                int worldTileY = chunk.Position.Y * CHUNK_SIZE + localY;
                float worldX = worldTileX * TILE_SIZE;
                float worldY = worldTileY * TILE_SIZE;

                // Get tile color based on type
                Color color = GetTileColor(tile.Type);

                // Draw tile
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
            TileType.Sand => new Color(238, 214, 175),
            TileType.CopperOre => new Color(184, 115, 51),
            TileType.IronOre => new Color(165, 142, 142),
            TileType.SilverOre => new Color(192, 192, 210),
            TileType.GoldOre => new Color(255, 215, 0),
            TileType.Wood => new Color(160, 82, 45),
            TileType.Torch => Color.Yellow,
            _ => Color.Magenta  // Unknown type = visible error
        };
    }

    private void DrawDebugInfo()
    {
        // We don't have a font loaded yet, so we'll skip text
        // In a future step we'll add proper debug text

        // Draw a small indicator showing chunk count
        var tilePos = WorldCoordinates.WorldToTile(_playerPosition);
        var chunkPos = WorldCoordinates.TileToChunk(tilePos);

        // Draw chunk grid lines could go here
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