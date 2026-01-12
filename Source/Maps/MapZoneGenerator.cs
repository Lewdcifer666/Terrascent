using Microsoft.Xna.Framework;
using Terrascent.World;
using Terrascent.World.Biomes;

namespace Terrascent.Maps;

/// <summary>
/// Layout type for generated zones.
/// </summary>
public enum ZoneLayout
{
    /// <summary>Open arena with scattered obstacles.</summary>
    Arena,

    /// <summary>Linear corridor with rooms.</summary>
    Corridor,

    /// <summary>Branching paths with dead ends.</summary>
    Labyrinth,

    /// <summary>Circular layout with center boss.</summary>
    Circular,

    /// <summary>Multi-level vertical layout.</summary>
    Tower
}

/// <summary>
/// Spawn point for monsters or items.
/// </summary>
public class ZoneSpawnPoint
{
    /// <summary>World position of the spawn point.</summary>
    public Vector2 Position { get; set; }

    /// <summary>Type of spawn point.</summary>
    public SpawnPointType Type { get; set; }

    /// <summary>Whether this spawn has been used.</summary>
    public bool Used { get; set; }

    /// <summary>Pack size for monster spawns.</summary>
    public int PackSize { get; set; } = 1;
}

/// <summary>
/// Types of spawn points in a zone.
/// </summary>
public enum SpawnPointType
{
    /// <summary>Regular monster pack spawn.</summary>
    MonsterPack,

    /// <summary>Elite/rare monster spawn.</summary>
    EliteMonster,

    /// <summary>Boss spawn location.</summary>
    Boss,

    /// <summary>Chest spawn location.</summary>
    Chest,

    /// <summary>Shrine spawn location.</summary>
    Shrine,

    /// <summary>Player spawn/exit location.</summary>
    PlayerSpawn
}

/// <summary>
/// Generated zone data for a map.
/// </summary>
public class ZoneData
{
    /// <summary>Zone dimensions in tiles.</summary>
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>Layout type used.</summary>
    public ZoneLayout Layout { get; set; }

    /// <summary>Tile data for the zone.</summary>
    public TileType[,]? Tiles { get; set; }

    /// <summary>Spawn points in the zone.</summary>
    public List<ZoneSpawnPoint> SpawnPoints { get; } = new();

    /// <summary>Player spawn position.</summary>
    public Vector2 PlayerSpawn { get; set; }

    /// <summary>Boss spawn position.</summary>
    public Vector2 BossSpawn { get; set; }

    /// <summary>Exit portal position.</summary>
    public Vector2 ExitPosition { get; set; }
}

/// <summary>
/// Generates zone terrain and spawn points from a MapZone.
/// </summary>
public static class MapZoneGenerator
{
    private const int BASE_WIDTH = 150;
    private const int BASE_HEIGHT = 100;
    private const int TIER_SIZE_BONUS = 5; // Extra tiles per tier

    /// <summary>
    /// Generate zone data from a MapZone.
    /// </summary>
    public static ZoneData GenerateZone(MapZone zone)
    {
        var rng = new Random(zone.ZoneSeed);
        var data = new ZoneData();

        // Calculate zone size based on tier
        data.Width = BASE_WIDTH + zone.Tier * TIER_SIZE_BONUS;
        data.Height = BASE_HEIGHT + zone.Tier * TIER_SIZE_BONUS;

        // Pick layout based on biome and rng
        data.Layout = PickLayout(zone.PrimaryBiome, rng);

        // Generate tile data
        data.Tiles = GenerateTiles(data, zone, rng);

        // Generate spawn points
        GenerateSpawnPoints(data, zone, rng);

        return data;
    }

    /// <summary>
    /// Pick a layout type based on biome.
    /// </summary>
    private static ZoneLayout PickLayout(BiomeType biome, Random rng)
    {
        // Weight layouts based on biome
        var weights = biome switch
        {
            BiomeType.Forest => new[] { 40, 20, 20, 15, 5 },      // Arena favored
            BiomeType.Desert => new[] { 50, 30, 10, 10, 0 },      // Arena/Corridor
            BiomeType.Snow => new[] { 30, 25, 25, 15, 5 },        // Balanced
            BiomeType.Jungle => new[] { 20, 15, 45, 15, 5 },      // Labyrinth favored
            BiomeType.Corruption => new[] { 25, 30, 25, 10, 10 }, // Corridor/Labyrinth
            BiomeType.Crimson => new[] { 35, 20, 25, 10, 10 },    // Arena/Labyrinth
            BiomeType.Underground => new[] { 15, 35, 35, 10, 5 }, // Corridor/Labyrinth
            BiomeType.Underworld => new[] { 40, 20, 10, 20, 10 }, // Arena/Circular
            _ => new[] { 30, 25, 25, 15, 5 }
        };

        int total = weights.Sum();
        int roll = rng.Next(total);
        int cumulative = 0;

        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative)
                return (ZoneLayout)i;
        }

        return ZoneLayout.Arena;
    }

    /// <summary>
    /// Generate tile data for the zone.
    /// </summary>
    private static TileType[,] GenerateTiles(ZoneData data, MapZone zone, Random rng)
    {
        var tiles = new TileType[data.Width, data.Height];

        // Fill with biome-appropriate tiles
        var (floorTile, wallTile) = GetBiomeTiles(zone.PrimaryBiome);

        // Initialize all as walls
        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                tiles[x, y] = wallTile;
            }
        }

        // Carve out based on layout
        switch (data.Layout)
        {
            case ZoneLayout.Arena:
                CarveArena(tiles, data, rng);
                break;
            case ZoneLayout.Corridor:
                CarveCorridor(tiles, data, rng);
                break;
            case ZoneLayout.Labyrinth:
                CarveLabyrinth(tiles, data, rng);
                break;
            case ZoneLayout.Circular:
                CarveCircular(tiles, data, rng);
                break;
            case ZoneLayout.Tower:
                CarveTower(tiles, data, rng);
                break;
        }

        return tiles;
    }

    /// <summary>
    /// Get appropriate tiles for a biome.
    /// </summary>
    private static (TileType floor, TileType wall) GetBiomeTiles(BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Forest => (TileType.Grass, TileType.Stone),
            BiomeType.Desert => (TileType.Sand, TileType.Sandstone),
            BiomeType.Snow => (TileType.Snow, TileType.Ice),
            BiomeType.Jungle => (TileType.JungleGrass, TileType.Mud),
            BiomeType.Corruption => (TileType.CorruptGrass, TileType.Ebonstone),
            BiomeType.Crimson => (TileType.CrimsonGrass, TileType.Crimstone),
            BiomeType.Underground => (TileType.Dirt, TileType.Stone),
            BiomeType.Underworld => (TileType.Ash, TileType.Hellstone),
            _ => (TileType.Dirt, TileType.Stone)
        };
    }

    /// <summary>
    /// Carve an arena layout.
    /// </summary>
    private static void CarveArena(TileType[,] tiles, ZoneData data, Random rng)
    {
        // Create large open area with scattered pillars
        int margin = 10;
        var (floor, _) = GetBiomeTiles(BiomeType.Forest);

        // Carve main area
        for (int x = margin; x < data.Width - margin; x++)
        {
            for (int y = margin; y < data.Height - margin; y++)
            {
                tiles[x, y] = TileType.Air;
            }
        }

        // Add scattered pillars
        int pillarCount = 5 + rng.Next(10);
        for (int i = 0; i < pillarCount; i++)
        {
            int px = margin + 5 + rng.Next(data.Width - margin * 2 - 10);
            int py = margin + 5 + rng.Next(data.Height - margin * 2 - 10);
            int size = 2 + rng.Next(3);

            for (int dx = -size; dx <= size; dx++)
            {
                for (int dy = -size; dy <= size; dy++)
                {
                    if (px + dx >= 0 && px + dx < data.Width &&
                        py + dy >= 0 && py + dy < data.Height)
                    {
                        tiles[px + dx, py + dy] = TileType.Stone;
                    }
                }
            }
        }

        // Set spawn positions
        data.PlayerSpawn = new Vector2((margin + 5) * WorldCoordinates.TILE_SIZE, (data.Height / 2) * WorldCoordinates.TILE_SIZE);
        data.BossSpawn = new Vector2((data.Width - margin - 5) * WorldCoordinates.TILE_SIZE, (data.Height / 2) * WorldCoordinates.TILE_SIZE);
        data.ExitPosition = data.PlayerSpawn + new Vector2(32, 0);
    }

    /// <summary>
    /// Carve a corridor layout.
    /// </summary>
    private static void CarveCorridor(TileType[,] tiles, ZoneData data, Random rng)
    {
        int corridorWidth = 8;
        int roomSize = 15;

        // Main corridor
        int y = data.Height / 2;
        for (int x = 5; x < data.Width - 5; x++)
        {
            for (int dy = -corridorWidth / 2; dy <= corridorWidth / 2; dy++)
            {
                if (y + dy >= 0 && y + dy < data.Height)
                    tiles[x, y + dy] = TileType.Air;
            }
        }

        // Add rooms along corridor
        int roomCount = 3 + rng.Next(4);
        int spacing = (data.Width - 20) / (roomCount + 1);

        for (int i = 0; i < roomCount; i++)
        {
            int rx = 10 + (i + 1) * spacing;
            int ry = y + (rng.Next(2) == 0 ? -15 : 15);

            // Carve room
            for (int dx = -roomSize / 2; dx <= roomSize / 2; dx++)
            {
                for (int dy = -roomSize / 2; dy <= roomSize / 2; dy++)
                {
                    int tx = rx + dx;
                    int ty = ry + dy;
                    if (tx >= 0 && tx < data.Width && ty >= 0 && ty < data.Height)
                        tiles[tx, ty] = TileType.Air;
                }
            }

            // Connect to corridor
            int connectY = ry < y ? ry + roomSize / 2 : ry - roomSize / 2;
            for (int cy = Math.Min(connectY, y - corridorWidth / 2);
                 cy <= Math.Max(connectY, y + corridorWidth / 2); cy++)
            {
                if (rx >= 0 && rx < data.Width && cy >= 0 && cy < data.Height)
                    tiles[rx, cy] = TileType.Air;
            }
        }

        data.PlayerSpawn = new Vector2(10 * WorldCoordinates.TILE_SIZE, y * WorldCoordinates.TILE_SIZE);
        data.BossSpawn = new Vector2((data.Width - 10) * WorldCoordinates.TILE_SIZE, y * WorldCoordinates.TILE_SIZE);
        data.ExitPosition = data.PlayerSpawn + new Vector2(32, 0);
    }

    /// <summary>
    /// Carve a labyrinth layout.
    /// </summary>
    private static void CarveLabyrinth(TileType[,] tiles, ZoneData data, Random rng)
    {
        // Simple maze generation using random walk
        int pathWidth = 4;
        var visited = new HashSet<(int, int)>();
        var stack = new Stack<(int x, int y)>();

        int startX = data.Width / 4;
        int startY = data.Height / 2;

        stack.Push((startX, startY));
        visited.Add((startX, startY));

        while (stack.Count > 0)
        {
            var (cx, cy) = stack.Peek();

            // Carve current position
            for (int dx = -pathWidth / 2; dx <= pathWidth / 2; dx++)
            {
                for (int dy = -pathWidth / 2; dy <= pathWidth / 2; dy++)
                {
                    int tx = cx + dx;
                    int ty = cy + dy;
                    if (tx >= 5 && tx < data.Width - 5 && ty >= 5 && ty < data.Height - 5)
                        tiles[tx, ty] = TileType.Air;
                }
            }

            // Find unvisited neighbors
            var neighbors = new List<(int x, int y)>();
            int step = pathWidth + 2;

            if (cx - step >= 5 && !visited.Contains((cx - step, cy)))
                neighbors.Add((cx - step, cy));
            if (cx + step < data.Width - 5 && !visited.Contains((cx + step, cy)))
                neighbors.Add((cx + step, cy));
            if (cy - step >= 5 && !visited.Contains((cx, cy - step)))
                neighbors.Add((cx, cy - step));
            if (cy + step < data.Height - 5 && !visited.Contains((cx, cy + step)))
                neighbors.Add((cx, cy + step));

            if (neighbors.Count > 0)
            {
                var next = neighbors[rng.Next(neighbors.Count)];
                visited.Add(next);

                // Carve path to next
                int midX = (cx + next.x) / 2;
                int midY = (cy + next.y) / 2;

                for (int dx = -pathWidth / 2; dx <= pathWidth / 2; dx++)
                {
                    for (int dy = -pathWidth / 2; dy <= pathWidth / 2; dy++)
                    {
                        int tx = midX + dx;
                        int ty = midY + dy;
                        if (tx >= 5 && tx < data.Width - 5 && ty >= 5 && ty < data.Height - 5)
                            tiles[tx, ty] = TileType.Air;
                    }
                }

                stack.Push(next);
            }
            else
            {
                stack.Pop();
            }
        }

        data.PlayerSpawn = new Vector2(startX * WorldCoordinates.TILE_SIZE, startY * WorldCoordinates.TILE_SIZE);
        data.BossSpawn = new Vector2((data.Width * 3 / 4) * WorldCoordinates.TILE_SIZE, (data.Height / 2) * WorldCoordinates.TILE_SIZE);
        data.ExitPosition = data.PlayerSpawn + new Vector2(32, 0);
    }

    /// <summary>
    /// Carve a circular layout.
    /// </summary>
    private static void CarveCircular(TileType[,] tiles, ZoneData data, Random rng)
    {
        int centerX = data.Width / 2;
        int centerY = data.Height / 2;
        int outerRadius = Math.Min(data.Width, data.Height) / 2 - 10;
        int innerRadius = outerRadius / 3;

        // Carve rings
        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                float dist = MathF.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));

                // Outer ring
                if (dist < outerRadius && dist > outerRadius - 8)
                    tiles[x, y] = TileType.Air;

                // Inner ring
                if (dist < outerRadius - 15 && dist > innerRadius + 5)
                    tiles[x, y] = TileType.Air;

                // Center arena
                if (dist < innerRadius)
                    tiles[x, y] = TileType.Air;
            }
        }

        // Carve spokes connecting rings
        int spokeCount = 4 + rng.Next(3);
        for (int i = 0; i < spokeCount; i++)
        {
            float angle = (float)(i * 2 * Math.PI / spokeCount);

            for (float r = innerRadius; r < outerRadius; r += 0.5f)
            {
                int x = (int)(centerX + MathF.Cos(angle) * r);
                int y = (int)(centerY + MathF.Sin(angle) * r);

                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        if (x + dx >= 0 && x + dx < data.Width &&
                            y + dy >= 0 && y + dy < data.Height)
                        {
                            tiles[x + dx, y + dy] = TileType.Air;
                        }
                    }
                }
            }
        }

        data.PlayerSpawn = new Vector2(
            (centerX - outerRadius + 5) * WorldCoordinates.TILE_SIZE,
            centerY * WorldCoordinates.TILE_SIZE);
        data.BossSpawn = new Vector2(
            centerX * WorldCoordinates.TILE_SIZE,
            centerY * WorldCoordinates.TILE_SIZE);
        data.ExitPosition = data.PlayerSpawn + new Vector2(32, 0);
    }

    /// <summary>
    /// Carve a tower layout.
    /// </summary>
    private static void CarveTower(TileType[,] tiles, ZoneData data, Random rng)
    {
        int floors = 3 + rng.Next(3);
        int floorHeight = (data.Height - 10) / floors;
        int floorWidth = data.Width - 20;

        for (int floor = 0; floor < floors; floor++)
        {
            int baseY = 5 + floor * floorHeight;

            // Carve floor
            for (int x = 10; x < data.Width - 10; x++)
            {
                for (int y = baseY; y < baseY + floorHeight - 3; y++)
                {
                    tiles[x, y] = TileType.Air;
                }
            }

            // Add stairway to next floor (alternating sides)
            if (floor < floors - 1)
            {
                int stairX = (floor % 2 == 0) ? data.Width - 15 : 15;

                for (int y = baseY + floorHeight - 3; y < baseY + floorHeight + 3; y++)
                {
                    for (int dx = -3; dx <= 3; dx++)
                    {
                        if (stairX + dx >= 0 && stairX + dx < data.Width &&
                            y >= 0 && y < data.Height)
                        {
                            tiles[stairX + dx, y] = TileType.Air;
                        }
                    }
                }
            }
        }

        data.PlayerSpawn = new Vector2(15 * WorldCoordinates.TILE_SIZE, (5 + floorHeight / 2) * WorldCoordinates.TILE_SIZE);
        data.BossSpawn = new Vector2(
            (data.Width / 2) * WorldCoordinates.TILE_SIZE,
            (5 + (floors - 1) * floorHeight + floorHeight / 2) * WorldCoordinates.TILE_SIZE);
        data.ExitPosition = data.PlayerSpawn + new Vector2(32, 0);
    }

    /// <summary>
    /// Generate spawn points for the zone.
    /// </summary>
    private static void GenerateSpawnPoints(ZoneData data, MapZone zone, Random rng)
    {
        if (data.Tiles == null) return;

        // Calculate pack count based on tier and modifiers
        int basePacks = 10 + zone.Tier * 2;
        int extraPacks = zone.ExtraPacks;
        int totalPacks = basePacks + extraPacks;

        // Apply pack size bonus
        float packSizeMultiplier = 1f + (zone.PackSize / 100f);

        // Find valid spawn positions (empty tiles away from spawn/boss)
        var validPositions = new List<(int x, int y)>();

        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                if (data.Tiles[x, y] == TileType.Air)
                {
                    var pos = new Vector2(x * WorldCoordinates.TILE_SIZE, y * WorldCoordinates.TILE_SIZE);
                    float distToPlayer = Vector2.Distance(pos, data.PlayerSpawn);
                    float distToBoss = Vector2.Distance(pos, data.BossSpawn);

                    // Not too close to spawn or boss
                    if (distToPlayer > 150 && distToBoss > 100)
                    {
                        validPositions.Add((x, y));
                    }
                }
            }
        }

        // Shuffle positions
        for (int i = validPositions.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (validPositions[i], validPositions[j]) = (validPositions[j], validPositions[i]);
        }

        // Place monster packs
        int posIndex = 0;
        for (int i = 0; i < totalPacks && posIndex < validPositions.Count; i++)
        {
            var (x, y) = validPositions[posIndex++];

            // 15% chance of elite pack
            var type = rng.Next(100) < 15 ? SpawnPointType.EliteMonster : SpawnPointType.MonsterPack;

            data.SpawnPoints.Add(new ZoneSpawnPoint
            {
                Position = new Vector2(x * WorldCoordinates.TILE_SIZE, y * WorldCoordinates.TILE_SIZE),
                Type = type,
                PackSize = (int)(3 + rng.Next(4) * packSizeMultiplier)
            });
        }

        // Add boss spawn
        data.SpawnPoints.Add(new ZoneSpawnPoint
        {
            Position = data.BossSpawn,
            Type = SpawnPointType.Boss,
            PackSize = 1
        });

        // Add additional boss if modifier present
        if (zone.HasAdditionalBoss && posIndex < validPositions.Count)
        {
            var (x, y) = validPositions[posIndex++];
            data.SpawnPoints.Add(new ZoneSpawnPoint
            {
                Position = new Vector2(x * WorldCoordinates.TILE_SIZE, y * WorldCoordinates.TILE_SIZE),
                Type = SpawnPointType.Boss,
                PackSize = 1
            });
        }

        // Add chests (more in higher tiers)
        int chestCount = 2 + zone.Tier / 4;
        for (int i = 0; i < chestCount && posIndex < validPositions.Count; i++)
        {
            var (x, y) = validPositions[posIndex++];
            data.SpawnPoints.Add(new ZoneSpawnPoint
            {
                Position = new Vector2(x * WorldCoordinates.TILE_SIZE, y * WorldCoordinates.TILE_SIZE),
                Type = SpawnPointType.Chest
            });
        }

        // Add player spawn point
        data.SpawnPoints.Add(new ZoneSpawnPoint
        {
            Position = data.PlayerSpawn,
            Type = SpawnPointType.PlayerSpawn
        });
    }
}