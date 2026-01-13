using Microsoft.Xna.Framework;
using Terrascent.Entities;
using Terrascent.Items;
using Terrascent.World;

namespace Terrascent.Saves;

/// <summary>
/// Handles saving and loading world and player data.
/// </summary>
public class SaveManager
{
    private const int SAVE_VERSION = 3;  // Updated for Hardmode system
    private const string WORLD_FILE = "world.dat";
    private const string PLAYER_FILE = "player.dat";
    private const string CHUNKS_FOLDER = "chunks";

    /// <summary>
    /// Base directory for all saves.
    /// </summary>
    public string SaveDirectory { get; }

    /// <summary>
    /// Current world name/folder.
    /// </summary>
    public string WorldName { get; private set; }

    /// <summary>
    /// Full path to current world folder.
    /// </summary>
    public string WorldPath => Path.Combine(SaveDirectory, WorldName);

    // Loaded hardmode state (available after LoadWorldData)
    public bool LoadedIsHardmode { get; private set; }
    public bool LoadedHasTransformed { get; private set; }

    public SaveManager(string saveDirectory = "Saves")
    {
        // Use AppData for saves
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        SaveDirectory = Path.Combine(appData, "Terrascent", saveDirectory);
        WorldName = "World1";

        System.Diagnostics.Debug.WriteLine($"Save directory: {SaveDirectory}");
    }

    #region World Data

    /// <summary>
    /// Save world metadata (seed, hardmode state, etc).
    /// </summary>
    public void SaveWorldData(int seed, bool isHardmode = false, bool hasTransformed = false)
    {
        EnsureDirectoryExists(WorldPath);

        string filePath = Path.Combine(WorldPath, WORLD_FILE);

        using var stream = File.Create(filePath);
        using var writer = new BinaryWriter(stream);

        writer.Write(SAVE_VERSION);
        writer.Write(seed);
        writer.Write(DateTime.UtcNow.ToBinary());

        // Version 3: Hardmode state
        writer.Write(isHardmode);
        writer.Write(hasTransformed);

        System.Diagnostics.Debug.WriteLine($"Saved world data: seed={seed}, isHardmode={isHardmode}, hasTransformed={hasTransformed}");
    }

    /// <summary>
    /// Load world metadata. Returns seed, or null if no save exists.
    /// Also sets LoadedIsHardmode and LoadedHasTransformed properties.
    /// </summary>
    public int? LoadWorldData()
    {
        string filePath = Path.Combine(WorldPath, WORLD_FILE);

        // Reset loaded state
        LoadedIsHardmode = false;
        LoadedHasTransformed = false;

        if (!File.Exists(filePath))
            return null;

        try
        {
            using var stream = File.OpenRead(filePath);
            using var reader = new BinaryReader(stream);

            int version = reader.ReadInt32();
            if (version < 2 || version > SAVE_VERSION)
            {
                System.Diagnostics.Debug.WriteLine($"Save version mismatch: {version} != {SAVE_VERSION}");
                return null;
            }

            int seed = reader.ReadInt32();
            long timeBinary = reader.ReadInt64();
            DateTime saveTime = DateTime.FromBinary(timeBinary);

            // Version 3+: Load hardmode state
            if (version >= 3)
            {
                LoadedIsHardmode = reader.ReadBoolean();
                LoadedHasTransformed = reader.ReadBoolean();
            }

            System.Diagnostics.Debug.WriteLine($"Loaded world data: seed={seed}, saved={saveTime}, hardmode={LoadedIsHardmode}, transformed={LoadedHasTransformed}");
            return seed;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load world data: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Chunk Data

    /// <summary>
    /// Save a single chunk to disk.
    /// </summary>
    public void SaveChunk(Chunk chunk)
    {
        string chunksPath = Path.Combine(WorldPath, CHUNKS_FOLDER);
        EnsureDirectoryExists(chunksPath);

        string fileName = $"chunk_{chunk.Position.X}_{chunk.Position.Y}.dat";
        string filePath = Path.Combine(chunksPath, fileName);

        using var stream = File.Create(filePath);
        using var writer = new BinaryWriter(stream);

        // Write chunk position
        writer.Write(chunk.Position.X);
        writer.Write(chunk.Position.Y);

        // Write all tiles
        for (int y = 0; y < Chunk.SIZE; y++)
        {
            for (int x = 0; x < Chunk.SIZE; x++)
            {
                ref var tile = ref chunk.GetTile(x, y);
                writer.Write((ushort)tile.Type);
                writer.Write((ushort)tile.Wall);
                writer.Write(tile.FrameX);
                writer.Write(tile.FrameY);
                writer.Write(tile.Light);
                writer.Write((byte)tile.Flags);
            }
        }

        chunk.MarkSaved();
    }

    /// <summary>
    /// Load a chunk from disk. Returns null if not found.
    /// </summary>
    public Chunk? LoadChunk(int chunkX, int chunkY)
    {
        string chunksPath = Path.Combine(WorldPath, CHUNKS_FOLDER);
        string fileName = $"chunk_{chunkX}_{chunkY}.dat";
        string filePath = Path.Combine(chunksPath, fileName);

        if (!File.Exists(filePath))
            return null;

        try
        {
            using var stream = File.OpenRead(filePath);
            using var reader = new BinaryReader(stream);

            int posX = reader.ReadInt32();
            int posY = reader.ReadInt32();

            var chunk = new Chunk(posX, posY);

            for (int y = 0; y < Chunk.SIZE; y++)
            {
                for (int x = 0; x < Chunk.SIZE; x++)
                {
                    ref var tile = ref chunk.GetTile(x, y);
                    tile.Type = (TileType)reader.ReadUInt16();
                    tile.Wall = (WallType)reader.ReadUInt16();
                    tile.FrameX = reader.ReadByte();
                    tile.FrameY = reader.ReadByte();
                    tile.Light = reader.ReadByte();
                    tile.Flags = (TileFlags)reader.ReadByte();
                }
            }

            chunk.MarkLoaded();
            chunk.MarkSaved(); // Just loaded, so not dirty

            return chunk;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load chunk ({chunkX}, {chunkY}): {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Save all dirty chunks.
    /// </summary>
    public int SaveAllChunks(ChunkManager chunkManager)
    {
        int saved = 0;

        foreach (var chunk in chunkManager.GetLoadedChunks())
        {
            if (chunk.IsDirty)
            {
                SaveChunk(chunk);
                saved++;
            }
        }

        System.Diagnostics.Debug.WriteLine($"Saved {saved} chunks");
        return saved;
    }

    #endregion

    #region Player Data

    /// <summary>
    /// Save player data (position, inventory, XP, currency, health).
    /// </summary>
    public void SavePlayer(Player player)
    {
        EnsureDirectoryExists(WorldPath);

        string filePath = Path.Combine(WorldPath, PLAYER_FILE);

        using var stream = File.Create(filePath);
        using var writer = new BinaryWriter(stream);

        writer.Write(SAVE_VERSION);

        // Position
        writer.Write(player.Position.X);
        writer.Write(player.Position.Y);

        // Velocity
        writer.Write(player.Velocity.X);
        writer.Write(player.Velocity.Y);

        // Health
        writer.Write(player.CurrentHealth);
        writer.Write(player.MaxHealth);

        // XP System
        player.XP.SaveTo(writer);

        // Currency
        player.Currency.SaveTo(writer);

        // Inventory
        writer.Write(player.Inventory.Size);
        writer.Write(player.Inventory.SelectedSlot);

        for (int i = 0; i < player.Inventory.Size; i++)
        {
            var stack = player.Inventory.GetSlot(i);
            writer.Write((ushort)stack.Type);
            writer.Write(stack.Count);
        }

        System.Diagnostics.Debug.WriteLine($"Saved player at {player.Position} (Level {player.XP.Level})");
    }

    /// <summary>
    /// Load player data. Returns false if no save exists.
    /// </summary>
    public bool LoadPlayer(Player player)
    {
        string filePath = Path.Combine(WorldPath, PLAYER_FILE);

        if (!File.Exists(filePath))
            return false;

        try
        {
            using var stream = File.OpenRead(filePath);
            using var reader = new BinaryReader(stream);

            int version = reader.ReadInt32();
            if (version < 1 || version > SAVE_VERSION)
            {
                System.Diagnostics.Debug.WriteLine($"Player save version mismatch: {version}");
                return false;
            }

            // Position
            float posX = reader.ReadSingle();
            float posY = reader.ReadSingle();
            player.Position = new Vector2(posX, posY);

            // Velocity
            float velX = reader.ReadSingle();
            float velY = reader.ReadSingle();
            player.Velocity = new Vector2(velX, velY);

            // Version 2+ includes health, XP, and currency
            if (version >= 2)
            {
                // Health
                int currentHealth = reader.ReadInt32();
                int maxHealth = reader.ReadInt32();
                // We'll set health after loading stats

                // XP System
                player.XP.LoadFrom(reader);

                // Currency
                player.Currency.LoadFrom(reader);
            }

            // Inventory
            int invSize = reader.ReadInt32();
            int selectedSlot = reader.ReadInt32();
            player.Inventory.SelectSlot(selectedSlot);

            player.Inventory.Clear();
            int slotsToRead = Math.Min(invSize, player.Inventory.Size);

            for (int i = 0; i < slotsToRead; i++)
            {
                var type = (ItemType)reader.ReadUInt16();
                int count = reader.ReadInt32();

                if (type != ItemType.None && count > 0)
                {
                    player.Inventory.SetSlot(i, new ItemStack(type, count));
                }
            }

            // Skip any extra slots if save has more than current inventory
            for (int i = slotsToRead; i < invSize; i++)
            {
                reader.ReadUInt16();
                reader.ReadInt32();
            }

            System.Diagnostics.Debug.WriteLine($"Loaded player at {player.Position} (Level {player.XP.Level})");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load player: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Exploration Data

    private const string EXPLORATION_FILE = "exploration.dat";
    private const string MAP_CACHE_FILE = "mapcache.dat";

    /// <summary>
    /// Save explored chunks data and tile color cache.
    /// </summary>
    public void SaveExploration(IEnumerable<Point> exploredChunks, Dictionary<Point, int[,]>? tileColorCache = null)
    {
        EnsureDirectoryExists(WorldPath);
        string filePath = Path.Combine(WorldPath, EXPLORATION_FILE);

        var chunks = exploredChunks.ToList();

        using var stream = File.Create(filePath);
        using var writer = new BinaryWriter(stream);

        writer.Write(SAVE_VERSION);
        writer.Write(chunks.Count);

        foreach (var chunk in chunks)
        {
            writer.Write(chunk.X);
            writer.Write(chunk.Y);
        }

        System.Diagnostics.Debug.WriteLine($"Saved exploration data: {chunks.Count} chunks");

        // Save tile color cache separately
        if (tileColorCache != null)
        {
            SaveMapCache(tileColorCache);
        }
    }

    /// <summary>
    /// Save tile color cache to file.
    /// </summary>
    private void SaveMapCache(Dictionary<Point, int[,]> cache)
    {
        string filePath = Path.Combine(WorldPath, MAP_CACHE_FILE);

        using var stream = File.Create(filePath);
        using var writer = new BinaryWriter(stream);

        writer.Write(SAVE_VERSION);
        writer.Write(cache.Count);

        foreach (var kvp in cache)
        {
            // Write chunk position
            writer.Write(kvp.Key.X);
            writer.Write(kvp.Key.Y);

            // Write 32x32 color array
            int[,] colors = kvp.Value;
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    writer.Write(colors[x, y]);
                }
            }
        }

        System.Diagnostics.Debug.WriteLine($"Saved map cache: {cache.Count} chunks");
    }

    /// <summary>
    /// Load explored chunks data.
    /// </summary>
    public List<Point>? LoadExploration()
    {
        string filePath = Path.Combine(WorldPath, EXPLORATION_FILE);

        if (!File.Exists(filePath))
            return null;

        try
        {
            using var stream = File.OpenRead(filePath);
            using var reader = new BinaryReader(stream);

            int version = reader.ReadInt32();
            if (version < 2 || version > SAVE_VERSION)
                return null;

            int count = reader.ReadInt32();
            var chunks = new List<Point>(count);

            for (int i = 0; i < count; i++)
            {
                int x = reader.ReadInt32();
                int y = reader.ReadInt32();
                chunks.Add(new Point(x, y));
            }

            System.Diagnostics.Debug.WriteLine($"Loaded exploration data: {chunks.Count} chunks");
            return chunks;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading exploration: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Load tile color cache from file.
    /// </summary>
    public Dictionary<Point, int[,]>? LoadMapCache()
    {
        string filePath = Path.Combine(WorldPath, MAP_CACHE_FILE);

        if (!File.Exists(filePath))
            return null;

        try
        {
            using var stream = File.OpenRead(filePath);
            using var reader = new BinaryReader(stream);

            int version = reader.ReadInt32();
            if (version < 2 || version > SAVE_VERSION)
                return null;

            int count = reader.ReadInt32();
            var cache = new Dictionary<Point, int[,]>(count);

            for (int i = 0; i < count; i++)
            {
                // Read chunk position
                int chunkX = reader.ReadInt32();
                int chunkY = reader.ReadInt32();
                Point chunkPos = new Point(chunkX, chunkY);

                // Read 32x32 color array
                int[,] colors = new int[32, 32];
                for (int x = 0; x < 32; x++)
                {
                    for (int y = 0; y < 32; y++)
                    {
                        colors[x, y] = reader.ReadInt32();
                    }
                }

                cache[chunkPos] = colors;
            }

            System.Diagnostics.Debug.WriteLine($"Loaded map cache: {cache.Count} chunks");
            return cache;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading map cache: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Full Save/Load

    /// <summary>
    /// Save everything (world + player + chunks + exploration + map cache).
    /// </summary>
    public void SaveAll(int seed, Player player, ChunkManager chunkManager,
                        bool isHardmode = false, bool hasTransformed = false,
                        IEnumerable<Point>? exploredChunks = null,
                        Dictionary<Point, int[,]>? tileColorCache = null)
    {
        SaveWorldData(seed, isHardmode, hasTransformed);
        SavePlayer(player);
        SaveAllChunks(chunkManager);

        if (exploredChunks != null)
        {
            SaveExploration(exploredChunks, tileColorCache);
        }

        System.Diagnostics.Debug.WriteLine($"=== GAME SAVED (Hardmode: {isHardmode}) ===");
    }

    /// <summary>
    /// Check if a save exists for the current world.
    /// </summary>
    public bool SaveExists()
    {
        string filePath = Path.Combine(WorldPath, WORLD_FILE);
        return File.Exists(filePath);
    }

    /// <summary>
    /// Delete the current world save.
    /// </summary>
    public void DeleteSave()
    {
        if (Directory.Exists(WorldPath))
        {
            Directory.Delete(WorldPath, true);
            System.Diagnostics.Debug.WriteLine($"Deleted save: {WorldPath}");
        }
    }

    /// <summary>
    /// Get list of available world saves.
    /// </summary>
    public string[] GetAvailableWorlds()
    {
        if (!Directory.Exists(SaveDirectory))
            return Array.Empty<string>();

        return Directory.GetDirectories(SaveDirectory)
            .Select(Path.GetFileName)
            .Where(name => name != null)
            .ToArray()!;
    }

    /// <summary>
    /// Set the current world name.
    /// </summary>
    public void SetWorld(string worldName)
    {
        WorldName = worldName;
    }

    #endregion

    #region Utility

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    #endregion
}