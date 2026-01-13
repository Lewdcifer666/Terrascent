using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terrascent.Core;
using Terrascent.Items;
using Terrascent.UI;
using Terrascent.World.Biomes;

namespace Terrascent.Maps;

/// <summary>
/// Test harness for the Map System.
/// Press NumPad1 to spawn a Map Device at the player's position.
/// Press NumPad2 to give test maps and currency.
/// Press NumPad3 to open/close the Map Device UI (when near a device).
/// Press NumPad4 to simulate a map drop.
/// </summary>
public class MapSystemTest
{
    private readonly MapSystemManager _mapSystem;
    private readonly Inventory _playerInventory;
    private Vector2 _playerPosition;

    // Debug messages
    private readonly List<(string message, float timer)> _messages = new();
    private const float MESSAGE_DURATION = 3f;

    public MapSystemTest(Inventory playerInventory)
    {
        _playerInventory = playerInventory;
        _mapSystem = new MapSystemManager(playerInventory);
        _mapSystem.InitializeUI();

        // Wire up events (matching correct signatures)
        _mapSystem.OnMapDropped += map => AddMessage($"Map dropped: {map.GetDisplayName()}", Color.Yellow);
        _mapSystem.OnCurrencyDropped += currency => AddMessage($"Currency dropped: {currency.GetDisplayName()}", Color.Cyan);
        _mapSystem.OnZoneEntered += zone => AddMessage($"Entered zone: {zone.SourceMap.GetDisplayName()}", Color.LimeGreen);
        _mapSystem.OnZoneExited += zone => AddMessage("Exited zone, returned to overworld", Color.Orange);
    }

    /// <summary>
    /// Update the test system. Call from main game Update.
    /// </summary>
    public void Update(GameTime gameTime, Vector2 playerPosition, InputManager input)
    {
        _playerPosition = playerPosition;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Update messages
        for (int i = _messages.Count - 1; i >= 0; i--)
        {
            var (msg, timer) = _messages[i];
            timer -= dt;
            if (timer <= 0)
                _messages.RemoveAt(i);
            else
                _messages[i] = (msg, timer);
        }

        // Handle test keys
        HandleTestInput(input);

        // Update map system (pass mouse and keyboard state)
        var keyboard = Keyboard.GetState();
        _mapSystem.Update(gameTime, input._currentMouse, input._previousMouse, keyboard);
    }

    private void HandleTestInput(InputManager input)
    {
        // NumPad1 - Spawn Map Device at player position
        if (input.IsKeyPressed(Keys.NumPad1))
        {
            input.ConsumeKeyPress(Keys.NumPad1);
            SpawnMapDevice();
        }

        // NumPad2 - Give test maps and currency
        if (input.IsKeyPressed(Keys.NumPad2))
        {
            input.ConsumeKeyPress(Keys.NumPad2);
            GiveTestItems();
        }

        // NumPad3 - Open/close Map Device UI
        if (input.IsKeyPressed(Keys.NumPad3))
        {
            input.ConsumeKeyPress(Keys.NumPad3);
            ToggleMapDeviceUI();
        }

        // NumPad4 - Generate a random map drop (simulates enemy kill)
        if (input.IsKeyPressed(Keys.NumPad4))
        {
            input.ConsumeKeyPress(Keys.NumPad4);
            SimulateMapDrop();
        }
    }

    private void SpawnMapDevice()
    {
        var device = _mapSystem.CreateDevice(_playerPosition);
        AddMessage($"Spawned Map Device at ({_playerPosition.X:F0}, {_playerPosition.Y:F0})", Color.LimeGreen);
        AddMessage("Press NumPad3 when near to open it!", Color.White);
    }

    private void GiveTestItems()
    {
        // Give some maps of various tiers using constructor
        var testMaps = new[]
        {
            new MapItem(BiomeType.Forest, 1),      // T1 Normal
            new MapItem(BiomeType.Desert, 3),      // T3 Normal
            new MapItem(BiomeType.Jungle, 5),      // T5 Normal
            new MapItem(BiomeType.Corruption, 8),  // T8 Normal
        };

        foreach (var map in testMaps)
        {
            _mapSystem.AddMapToInventory(map);
        }
        AddMessage("Added 4 test maps (T1, T3, T5, T8) to inventory!", Color.Yellow);

        // Give currency
        _playerInventory.AddItem(ItemType.OrbOfTransmutation, 10);
        _playerInventory.AddItem(ItemType.OrbOfAlteration, 10);
        _playerInventory.AddItem(ItemType.OrbOfAugmentation, 5);
        _playerInventory.AddItem(ItemType.OrbOfAlchemy, 5);
        _playerInventory.AddItem(ItemType.ChaosOrb, 3);
        _playerInventory.AddItem(ItemType.OrbOfScouring, 3);
        AddMessage("Added map currency to inventory!", Color.Cyan);
    }

    private void ToggleMapDeviceUI()
    {
        if (_mapSystem.DeviceUI != null && _mapSystem.DeviceUI.IsOpen)
        {
            _mapSystem.DeviceUI.Close();
            AddMessage("Closed Map Device UI", Color.Gray);
        }
        else
        {
            // Try to interact with nearest device
            if (_mapSystem.TryInteractWithDevice(_playerPosition))
            {
                AddMessage("Opened Map Device UI", Color.LimeGreen);

                // Auto-insert a test map if device is empty and we have maps in storage
                var device = _mapSystem.DeviceUI?.ActiveDevice;
                if (device != null && device.InsertedMap == null)
                {
                    // Try to get a map from storage to insert
                    var firstMap = _mapSystem.MapStorage.GetAllMaps().FirstOrDefault();
                    if (firstMap.map != null)
                    {
                        device.InsertMap(firstMap.map);
                        _mapSystem.MapStorage.RemoveMap(firstMap.slot);
                        AddMessage($"Auto-inserted: {firstMap.map.GetDisplayName()}", Color.Yellow);
                    }
                    else
                    {
                        AddMessage("No maps in storage! Press NumPad2 first.", Color.Orange);
                    }
                }
            }
            else
            {
                AddMessage("No Map Device nearby! Press NumPad1 to spawn one.", Color.Red);
            }
        }
    }

    private void SimulateMapDrop()
    {
        // Simulate a boss drop with high chance
        var map = _mapSystem.TryGenerateMapDrop(MapDropTable.BossConfig, baseTier: 3);
        if (map != null)
        {
            _mapSystem.AddMapToInventory(map);
            AddMessage($"Simulated drop: {map.GetDisplayName()}", Color.Gold);
        }
        else
        {
            // Try currency drop instead
            var currency = _mapSystem.TryGenerateCurrencyDrop(50f);
            if (currency.HasValue)
            {
                _mapSystem.AddCurrencyToInventory(currency.Value, 1);
                AddMessage($"Simulated drop: {currency.Value.GetDisplayName()}", Color.Cyan);
            }
            else
            {
                AddMessage("No drop this time (RNG)", Color.Gray);
            }
        }
    }

    private void AddMessage(string text, Color color)
    {
        _messages.Add((text, MESSAGE_DURATION));
        System.Diagnostics.Debug.WriteLine($"[MapSystem] {text}");
    }

    /// <summary>
    /// Draw the map system UI and debug info. Call from main game Draw.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, Rectangle viewport)
    {
        // Draw test controls info (top-left)
        DrawTestInfo(spriteBatch, pixel);

        // Draw debug messages
        DrawMessages(spriteBatch, pixel, viewport);

        // Draw device proximity indicator
        DrawDeviceProximity(spriteBatch, pixel, viewport);

        // Draw the Map Device UI if open
        if (_mapSystem.DeviceUI != null && _mapSystem.DeviceUI.IsOpen)
        {
            DrawSimpleDeviceUI(spriteBatch, pixel, viewport);
        }
    }

    /// <summary>
    /// Draw a simple test UI for the map device (placeholder until proper UI integration).
    /// </summary>
    private void DrawSimpleDeviceUI(SpriteBatch spriteBatch, Texture2D pixel, Rectangle viewport)
    {
        // Panel dimensions
        int panelWidth = 350;
        int panelHeight = 400;
        int panelX = (viewport.Width - panelWidth) / 2;
        int panelY = (viewport.Height - panelHeight) / 2;

        // Draw panel background
        spriteBatch.Draw(pixel, new Rectangle(panelX - 2, panelY - 2, panelWidth + 4, panelHeight + 4), new Color(80, 80, 100));
        spriteBatch.Draw(pixel, new Rectangle(panelX, panelY, panelWidth, panelHeight), new Color(30, 30, 40, 240));

        int y = panelY + 10;
        int x = panelX + 10;

        // Title
        InventoryUI.DrawText(spriteBatch, pixel, "=== MAP DEVICE ===", panelX + panelWidth / 2 - 70, y, Color.Gold);
        y += 24;

        // Get the inserted map from device UI
        var insertedMap = GetInsertedMap();

        if (insertedMap != null)
        {
            // Show inserted map info
            InventoryUI.DrawText(spriteBatch, pixel, "INSERTED MAP:", x, y, Color.Cyan);
            y += 16;

            InventoryUI.DrawText(spriteBatch, pixel, insertedMap.GetDisplayName(), x + 10, y, insertedMap.Rarity.GetColor());
            y += 14;

            InventoryUI.DrawText(spriteBatch, pixel, $"Tier: {insertedMap.Tier}", x + 10, y, Color.White);
            y += 14;

            InventoryUI.DrawText(spriteBatch, pixel, $"Biome: {insertedMap.BiomeType.GetDisplayName()}", x + 10, y, Color.White);
            y += 14;

            InventoryUI.DrawText(spriteBatch, pixel, $"Monster Level: {insertedMap.MonsterLevel}", x + 10, y, Color.Orange);
            y += 14;

            InventoryUI.DrawText(spriteBatch, pixel, $"Item Quantity: +{insertedMap.ItemQuantity:F0}%", x + 10, y, Color.LimeGreen);
            y += 14;

            InventoryUI.DrawText(spriteBatch, pixel, $"Item Rarity: +{insertedMap.ItemRarity:F0}%", x + 10, y, Color.Yellow);
            y += 20;

            // Show affixes if any
            if (insertedMap.Prefixes.Count > 0 || insertedMap.Suffixes.Count > 0)
            {
                InventoryUI.DrawText(spriteBatch, pixel, "AFFIXES:", x, y, Color.Magenta);
                y += 14;

                foreach (var prefix in insertedMap.Prefixes)
                {
                    InventoryUI.DrawText(spriteBatch, pixel, $"  {prefix.GetDescription()}", x, y, Color.Red);
                    y += 12;
                }
                foreach (var suffix in insertedMap.Suffixes)
                {
                    InventoryUI.DrawText(spriteBatch, pixel, $"  {suffix.GetDescription()}", x, y, Color.Blue);
                    y += 12;
                }
                y += 8;
            }
        }
        else
        {
            // No map inserted
            InventoryUI.DrawText(spriteBatch, pixel, "No map inserted", x, y, Color.Gray);
            y += 20;
            InventoryUI.DrawText(spriteBatch, pixel, "Press NumPad2 to get test maps,", x, y, Color.White);
            y += 14;
            InventoryUI.DrawText(spriteBatch, pixel, "then open inventory (I) to see them.", x, y, Color.White);
            y += 30;
        }

        // Currency section
        y = panelY + 220;
        InventoryUI.DrawText(spriteBatch, pixel, "CURRENCY (in inventory):", x, y, Color.Cyan);
        y += 16;

        var currencyTypes = new[] {
            (ItemType.OrbOfTransmutation, "Transmute (Normal->Magic)"),
            (ItemType.OrbOfAlteration, "Alteration (Reroll Magic)"),
            (ItemType.OrbOfAugmentation, "Augment (Add Magic mod)"),
            (ItemType.OrbOfAlchemy, "Alchemy (Normal->Rare)"),
            (ItemType.ChaosOrb, "Chaos (Reroll Rare)"),
            (ItemType.OrbOfScouring, "Scouring (Remove mods)"),
        };

        foreach (var (itemType, desc) in currencyTypes)
        {
            int count = _playerInventory.CountItem(itemType);
            Color color = count > 0 ? Color.White : Color.DarkGray;
            InventoryUI.DrawText(spriteBatch, pixel, $"{count}x {desc}", x + 10, y, color);
            y += 12;
        }

        // Instructions
        y = panelY + panelHeight - 50;
        InventoryUI.DrawText(spriteBatch, pixel, "Press NumPad3 to close", panelX + panelWidth / 2 - 70, y, Color.Gray);
        y += 14;
        InventoryUI.DrawText(spriteBatch, pixel, "(Full UI coming soon!)", panelX + panelWidth / 2 - 65, y, Color.DarkGray);
    }

    /// <summary>
    /// Get the currently inserted map from the active device.
    /// </summary>
    private MapItem? GetInsertedMap()
    {
        // Check if device has a map inserted
        var device = _mapSystem.DeviceUI?.ActiveDevice;
        if (device != null && device.InsertedMap != null)
        {
            return device.InsertedMap;
        }
        return null;
    }

    private void DrawTestInfo(SpriteBatch spriteBatch, Texture2D pixel)
    {
        int y = 380;  // Below debug overlay (F3) which ends around y=370
        int x = 10;

        // Background
        spriteBatch.Draw(pixel, new Rectangle(x - 5, y - 5, 220, 130), new Color(0, 0, 0, 180));

        // Use InventoryUI.DrawText for pixel-based rendering
        InventoryUI.DrawText(spriteBatch, pixel, "=== MAP SYSTEM TEST ===", x, y, Color.Yellow);
        y += 14;
        InventoryUI.DrawText(spriteBatch, pixel, "NumPad1 - Spawn Map Device", x, y, Color.White);
        y += 14;
        InventoryUI.DrawText(spriteBatch, pixel, "NumPad2 - Give Maps/Currency", x, y, Color.White);
        y += 14;
        InventoryUI.DrawText(spriteBatch, pixel, "NumPad3 - Open/Close Device UI", x, y, Color.White);
        y += 14;
        InventoryUI.DrawText(spriteBatch, pixel, "NumPad4 - Simulate Map Drop", x, y, Color.White);
        y += 18;

        // Get device count via the GetDevices() method
        int deviceCount = _mapSystem.GetDevices().Count();
        InventoryUI.DrawText(spriteBatch, pixel, $"Devices: {deviceCount}", x, y, Color.Cyan);
        y += 14;
        string zoneStatus = _mapSystem.CurrentZone != null ? "IN ZONE" : "Overworld";
        Color zoneColor = _mapSystem.CurrentZone != null ? Color.LimeGreen : Color.Gray;
        InventoryUI.DrawText(spriteBatch, pixel, $"Status: {zoneStatus}", x, y, zoneColor);
    }

    private void DrawMessages(SpriteBatch spriteBatch, Texture2D pixel, Rectangle viewport)
    {
        if (_messages.Count == 0) return;

        int y = viewport.Height - 100;

        foreach (var (msg, timer) in _messages)
        {
            float alpha = Math.Min(1f, timer);
            var color = Color.White * alpha;

            // Center the message
            int textWidth = msg.Length * 6; // Approximate width
            int x = (viewport.Width - textWidth) / 2;

            // Background
            spriteBatch.Draw(pixel, new Rectangle(x - 5, y - 2, textWidth + 10, 14), new Color(0, 0, 0, (int)(150 * alpha)));

            InventoryUI.DrawText(spriteBatch, pixel, msg, x, y, color);
            y -= 18;
        }
    }

    private void DrawDeviceProximity(SpriteBatch spriteBatch, Texture2D pixel, Rectangle viewport)
    {
        // Find nearest device and show distance
        var nearest = _mapSystem.GetNearestDevice(_playerPosition, 500f);
        if (nearest != null)
        {
            float dist = Vector2.Distance(_playerPosition, nearest.Position);
            bool inRange = dist < nearest.InteractionRadius;

            Color color = inRange ? Color.LimeGreen : Color.Yellow;
            string text = inRange
                ? "MAP DEVICE - Press NumPad3 to interact"
                : $"Map Device nearby ({dist:F0} units)";

            int textWidth = text.Length * 6;
            int x = (viewport.Width - textWidth) / 2;
            int y = 60;

            // Background
            spriteBatch.Draw(pixel, new Rectangle(x - 5, y - 2, textWidth + 10, 14), new Color(0, 0, 0, 180));
            InventoryUI.DrawText(spriteBatch, pixel, text, x, y, color);
        }
    }

    /// <summary>
    /// Check if the map system UI is blocking game input.
    /// </summary>
    public bool IsUIBlocking => _mapSystem.DeviceUI?.IsOpen ?? false;

    /// <summary>
    /// Get the map system manager for direct access.
    /// </summary>
    public MapSystemManager MapSystem => _mapSystem;
}