using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terrascent.Core;
using Terrascent.Entities;
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

    // Entities
    private Player _player = null!;

    // Temp rendering
    private Texture2D _pixelTexture = null!;

    // Game constants
    public const int TILE_SIZE = 16;
    public const int CHUNK_SIZE = 32;

    // World seed
    private int _worldSeed = 12345;

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
        _worldGenerator = new WorldGenerator(_worldSeed);

        // Create chunk manager with generator
        _chunkManager = new ChunkManager
        {
            Generator = _worldGenerator,
            LoadRadius = 4
        };

        // Subscribe to chunk events for debugging
        _chunkManager.OnChunkLoaded += chunk =>
            System.Diagnostics.Debug.WriteLine($"Generated: {chunk}");

        // Create player
        _player = new Player();

        // Spawn player above the surface at world center
        int spawnX = 0;
        int surfaceY = _worldGenerator.GetSurfaceHeight(spawnX);
        _player.SpawnAt(spawnX, surfaceY);

        System.Diagnostics.Debug.WriteLine($"World Seed: {_worldSeed}");
        System.Diagnostics.Debug.WriteLine($"Spawn surface Y: {surfaceY}");
        System.Diagnostics.Debug.WriteLine($"Player spawn: {_player.Position}");

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _camera = new Camera(GraphicsDevice.Viewport);
        _camera.CenterOn(_player.Center);

        // Create a 1x1 white texture for primitive rendering
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData([Color.White]);
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
        _worldSeed = Random.Shared.Next();

        _worldGenerator = new WorldGenerator(_worldSeed);
        _chunkManager.Clear();
        _chunkManager.Generator = _worldGenerator;

        // Respawn player
        int surfaceY = _worldGenerator.GetSurfaceHeight(0);
        _player.SpawnAt(0, surfaceY);
        _camera.CenterOn(_player.Center);

        System.Diagnostics.Debug.WriteLine($"Regenerated world with seed: {_worldSeed}");
    }

    private void FixedUpdate()
    {
        float dt = GameLoop.TICK_DURATION;

        // Handle player input
        _player.HandleInput(_input, dt);

        // Update player physics
        _player.Update(dt);

        // Apply movement with collision
        _player.ApplyMovement(dt, _chunkManager);

        // Extra ground check for corner cases
        _player.CheckGroundState(_chunkManager);
    }

    private void VariableUpdate(float deltaTime)
    {
        // Update chunk loading around player
        _chunkManager.UpdateLoadedChunks(_player.Position);

        // Camera follows player
        _camera.Follow(_player.Center);
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

        _spriteBatch.End();

        // Draw UI (no camera transform)
        _spriteBatch.Begin();
        DrawDebugInfo();
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