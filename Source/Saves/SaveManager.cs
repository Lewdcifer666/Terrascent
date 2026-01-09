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
    private const int SAVE_VERSION = 2;  // Updated for XP system
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
    /// Save world metadata (seed, etc).
    /// </summary>
    public void SaveWorldData(int seed)
    {
        EnsureDirectoryExists(WorldPath);

        string filePath = Path.Combine(WorldPath, WORLD_FILE);

        using var stream = File.Create(filePath);
        using var writer = new BinaryWriter(stream);

        writer.Write(SAVE_VERSION);
        writer.Write(seed);
        writer.Write(DateTime.UtcNow.ToBinary());

        System.Diagnostics.Debug.WriteLine($"Saved world data: seed={seed}");
    }

    /// <summary>
    /// Load world metadata. Returns seed, or null if no save exists.
    /// </summary>
    public int? LoadWorldData()
    {
        string filePath = Path.Combine(WorldPath, WORLD_FILE);

        if (!File.Exists(filePath))
            return null;

        try
        {
            using var stream = File.OpenRead(filePath);
            using var reader = new BinaryReader(stream);

            int version = reader.ReadInt32();
            if (version != SAVE_VERSION)
            {
                System.Diagnostics.Debug.WriteLine($"Save version mismatch: {version} != {SAVE_VERSION}");
                return null;
            }

            int seed = reader.ReadInt32();
            long timeBinary = reader.ReadInt64();
            DateTime saveTime = DateTime.FromBinary(timeBinary);

            System.Diagnostics.Debug.WriteLine($"Loaded world data: seed={seed}, saved={saveTime}");
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

    #region Full Save/Load

    /// <summary>
    /// Save everything (world + player + chunks).
    /// </summary>
    public void SaveAll(int seed, Player player, ChunkManager chunkManager)
    {
        SaveWorldData(seed);
        SavePlayer(player);
        SaveAllChunks(chunkManager);

        System.Diagnostics.Debug.WriteLine("=== GAME SAVED ===");
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