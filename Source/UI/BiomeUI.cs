using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terrascent.World.Biomes;

namespace Terrascent.UI;

/// <summary>
/// UI component for displaying the current biome name and info on the HUD.
/// </summary>
public class BiomeUI
{
    private readonly BiomeManager _biomeManager;

    // Position and sizing
    private int _screenWidth;
    private int _screenHeight;
    private const int PADDING = 10;
    private const int BAR_HEIGHT = 24;

    // Animation for biome transitions
    private BiomeType _displayedBiome;
    private float _transitionAlpha = 1f;
    private float _transitionTimer;
    private const float TRANSITION_DURATION = 1.5f;
    private const float FADE_DURATION = 0.5f;

    // Display state
    private string _biomeName = "Forest";
    private Color _biomeColor = new Color(34, 139, 34);  // Forest green
    private int _dangerLevel = 1;

    // Show extended info briefly after biome change
    private float _extendedInfoTimer;
    private const float EXTENDED_INFO_DURATION = 3f;
    private bool _showExtendedInfo;

    public BiomeUI(BiomeManager biomeManager, int screenWidth, int screenHeight)
    {
        _biomeManager = biomeManager;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        // Subscribe to biome changes
        _biomeManager.OnBiomeChanged += OnBiomeChanged;

        // Initialize with current biome
        UpdateBiomeDisplay(_biomeManager.CurrentBiome);
    }

    private void OnBiomeChanged(BiomeType oldBiome, BiomeType newBiome)
    {
        // Start transition animation
        _transitionTimer = TRANSITION_DURATION;
        _transitionAlpha = 0f;
        _showExtendedInfo = true;
        _extendedInfoTimer = EXTENDED_INFO_DURATION;

        UpdateBiomeDisplay(newBiome);

        System.Diagnostics.Debug.WriteLine($"Biome changed: {oldBiome.GetDisplayName()} -> {newBiome.GetDisplayName()}");
    }

    private void UpdateBiomeDisplay(BiomeType biome)
    {
        _displayedBiome = biome;
        _biomeName = biome.GetDisplayName();

        // Get biome data for color and danger level
        var biomeData = BiomeRegistry.Get(biome);
        _biomeColor = new Color(biomeData.Color.R, biomeData.Color.G, biomeData.Color.B);
        _dangerLevel = biomeData.DangerLevel;
    }

    public void OnScreenResize(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
    }

    public void Update(float deltaTime)
    {
        // Update transition animation
        if (_transitionTimer > 0)
        {
            _transitionTimer -= deltaTime;

            // Fade in during first part of transition
            if (_transitionTimer > TRANSITION_DURATION - FADE_DURATION)
            {
                float fadeProgress = (TRANSITION_DURATION - _transitionTimer) / FADE_DURATION;
                _transitionAlpha = MathHelper.Clamp(fadeProgress, 0f, 1f);
            }
            else
            {
                _transitionAlpha = 1f;
            }
        }

        // Update extended info timer
        if (_extendedInfoTimer > 0)
        {
            _extendedInfoTimer -= deltaTime;
            if (_extendedInfoTimer <= 0)
            {
                _showExtendedInfo = false;
            }
        }

        // Periodically sync with biome manager (in case detection updated)
        var currentBiome = _biomeManager.CurrentBiome;
        if (currentBiome != _displayedBiome && _transitionTimer <= 0)
        {
            UpdateBiomeDisplay(currentBiome);
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
    {
        // Position: Top-right corner, below any other HUD elements
        int x = _screenWidth - 200 - PADDING;
        int y = PADDING + 35;  // Below the top row of HUD
        int width = 200;

        // Calculate alpha for fade effect
        float alpha = _transitionAlpha;

        // Background with biome-tinted border
        DrawBackground(spriteBatch, pixelTexture, x, y, width, BAR_HEIGHT, alpha);

        // Biome name
        DrawBiomeName(spriteBatch, pixelTexture, x, y, width, alpha);

        // Danger indicator
        DrawDangerIndicator(spriteBatch, pixelTexture, x, y, width, alpha);

        // Extended info (shown briefly after biome change)
        if (_showExtendedInfo)
        {
            float extendedAlpha = alpha * MathHelper.Clamp(_extendedInfoTimer / 0.5f, 0f, 1f);
            DrawExtendedInfo(spriteBatch, pixelTexture, x, y + BAR_HEIGHT + 4, width, extendedAlpha);
        }
    }

    private void DrawBackground(SpriteBatch spriteBatch, Texture2D pixelTexture, int x, int y, int width, int height, float alpha)
    {
        // Dark background
        spriteBatch.Draw(
            pixelTexture,
            new Rectangle(x, y, width, height),
            new Color(20, 20, 30) * (0.85f * alpha)
        );

        // Biome-colored border (left side accent)
        spriteBatch.Draw(
            pixelTexture,
            new Rectangle(x, y, 4, height),
            _biomeColor * alpha
        );

        // Top border
        spriteBatch.Draw(
            pixelTexture,
            new Rectangle(x, y, width, 1),
            _biomeColor * (0.5f * alpha)
        );

        // Bottom border
        spriteBatch.Draw(
            pixelTexture,
            new Rectangle(x, y + height - 1, width, 1),
            _biomeColor * (0.5f * alpha)
        );
    }

    private void DrawBiomeName(SpriteBatch spriteBatch, Texture2D pixelTexture, int x, int y, int width, float alpha)
    {
        // Biome icon (small colored square)
        int iconSize = 12;
        int iconX = x + 10;
        int iconY = y + (BAR_HEIGHT - iconSize) / 2;

        spriteBatch.Draw(
            pixelTexture,
            new Rectangle(iconX, iconY, iconSize, iconSize),
            _biomeColor * alpha
        );

        // Biome name text
        int textX = iconX + iconSize + 6;
        int textY = y + (BAR_HEIGHT - 8) / 2;

        Color textColor = Color.White * alpha;
        InventoryUI.DrawText(spriteBatch, pixelTexture, _biomeName, textX, textY, textColor);
    }

    private void DrawDangerIndicator(SpriteBatch spriteBatch, Texture2D pixelTexture, int x, int y, int width, float alpha)
    {
        // Danger skulls/dots on the right side
        int indicatorX = x + width - 8 - PADDING;
        int indicatorY = y + (BAR_HEIGHT - 8) / 2;

        for (int i = 0; i < Math.Min(_dangerLevel, 5); i++)
        {
            Color dangerColor = _dangerLevel switch
            {
                1 => Color.Green,
                2 => Color.YellowGreen,
                3 => Color.Yellow,
                4 => Color.Orange,
                _ => Color.Red
            };

            // Small danger indicator dot
            spriteBatch.Draw(
                pixelTexture,
                new Rectangle(indicatorX - (i * 10), indicatorY, 6, 8),
                dangerColor * alpha
            );
        }
    }

    private void DrawExtendedInfo(SpriteBatch spriteBatch, Texture2D pixelTexture, int x, int y, int width, float alpha)
    {
        var biomeData = BiomeRegistry.Get(_displayedBiome);

        // Extended info background
        int extHeight = 40;
        spriteBatch.Draw(
            pixelTexture,
            new Rectangle(x, y, width, extHeight),
            new Color(20, 20, 30) * (0.8f * alpha)
        );

        // Multiplier info
        string goldInfo = $"Gold: x{biomeData.GoldMultiplier:F1}";
        string xpInfo = $"XP: x{biomeData.XPMultiplier:F1}";
        string spawnInfo = $"Spawns: x{biomeData.SpawnRateMultiplier:F1}";

        Color infoColor = Color.LightGray * alpha;

        InventoryUI.DrawText(spriteBatch, pixelTexture, goldInfo, x + 8, y + 4,
            biomeData.GoldMultiplier > 1f ? Color.Gold * alpha : infoColor);
        InventoryUI.DrawText(spriteBatch, pixelTexture, xpInfo, x + 8, y + 14,
            biomeData.XPMultiplier > 1f ? Color.Cyan * alpha : infoColor);
        InventoryUI.DrawText(spriteBatch, pixelTexture, spawnInfo, x + 8, y + 24,
            biomeData.SpawnRateMultiplier > 1f ? Color.Orange * alpha : infoColor);

        // Category indicator
        string category = _displayedBiome.GetCategory() switch
        {
            BiomeCategory.Evil => "[EVIL]",
            BiomeCategory.Good => "[HALLOW]",
            BiomeCategory.Special => "[SPECIAL]",
            _ => ""
        };

        if (!string.IsNullOrEmpty(category))
        {
            Color categoryColor = _displayedBiome.GetCategory() switch
            {
                BiomeCategory.Evil => Color.Purple,
                BiomeCategory.Good => Color.Pink,
                BiomeCategory.Special => Color.Gold,
                _ => Color.White
            };

            InventoryUI.DrawText(spriteBatch, pixelTexture, category, x + width - 60, y + 14, categoryColor * alpha);
        }
    }
}