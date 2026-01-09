using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terrascent.Progression;

namespace Terrascent.UI;

/// <summary>
/// UI component for displaying player's XP bar and level.
/// Positioned directly below the health bar at top center.
/// </summary>
public class XPBarUI
{
    private readonly XPSystem _xpSystem;

    // Bar dimensions - matches health bar width (200), smaller height
    public int Width { get; set; } = 200;   // Same as health bar
    public int Height { get; set; } = 10;   // Smaller than health bar (20)

    // Screen dimensions for positioning
    private int _screenWidth;

    // Colors
    public Color BackgroundColor { get; set; } = new Color(0, 0, 40);      // Dark blue
    public Color BorderColor { get; set; } = new Color(0, 0, 0, 200);      // Black border
    public Color FillColorStart { get; set; } = new Color(0, 180, 255);    // Cyan
    public Color FillColorEnd { get; set; } = new Color(100, 255, 220);    // Teal
    public Color TextColor { get; set; } = Color.White;

    // Animation
    private float _displayProgress;
    private float _animationSpeed = 8f;
    private float _flashTimer;
    private float _levelUpFlashDuration = 1.5f;
    private bool _showLevelUpText;
    private float _levelUpTextTimer;

    // Position constants - matches health bar layout
    // Health bar: y=10, height=20, invincibility bar at y=32 height=3
    // XP bar starts at y=38
    private const int TOP_Y = 38;

    public XPBarUI(XPSystem xpSystem, int screenWidth, int screenHeight)
    {
        _xpSystem = xpSystem;
        _screenWidth = screenWidth;

        // Initialize display progress
        _displayProgress = _xpSystem.LevelProgress;

        // Subscribe to level-up for flash effect
        _xpSystem.OnLevelUp += (level, overflow) =>
        {
            _flashTimer = _levelUpFlashDuration;
            _displayProgress = 0f;  // Reset bar on level up
            _showLevelUpText = true;
            _levelUpTextTimer = 2.5f;
        };
    }

    /// <summary>
    /// Get the X position (centered on screen).
    /// </summary>
    private int GetX() => (_screenWidth - Width) / 2;

    /// <summary>
    /// Update XP bar animations.
    /// </summary>
    public void Update(float deltaTime)
    {
        // Smooth progress animation
        float targetProgress = _xpSystem.LevelProgress;
        _displayProgress = MathHelper.Lerp(_displayProgress, targetProgress, _animationSpeed * deltaTime);

        // Level-up flash timer
        if (_flashTimer > 0)
        {
            _flashTimer -= deltaTime;
        }

        // Level-up text timer
        if (_levelUpTextTimer > 0)
        {
            _levelUpTextTimer -= deltaTime;
            if (_levelUpTextTimer <= 0)
            {
                _showLevelUpText = false;
            }
        }
    }

    /// <summary>
    /// Reposition the XP bar when screen size changes.
    /// </summary>
    public void OnScreenResize(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
    }

    /// <summary>
    /// Draw the XP bar with pixel-based text (uses InventoryUI.DrawText).
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
    {
        int x = GetX();
        int y = TOP_Y;

        // Draw border/background
        spriteBatch.Draw(
            pixelTexture,
            new Rectangle(x - 2, y - 2, Width + 4, Height + 4),
            BorderColor
        );

        spriteBatch.Draw(
            pixelTexture,
            new Rectangle(x, y, Width, Height),
            BackgroundColor
        );

        // Draw fill bar
        int fillWidth = (int)(Width * _displayProgress);
        if (fillWidth > 0)
        {
            // Gradient effect based on progress
            Color fillColor = Color.Lerp(FillColorStart, FillColorEnd, _displayProgress);

            // Level-up flash effect
            if (_flashTimer > 0)
            {
                float flashIntensity = _flashTimer / _levelUpFlashDuration;
                fillColor = Color.Lerp(fillColor, Color.White, flashIntensity * 0.6f);
            }

            spriteBatch.Draw(
                pixelTexture,
                new Rectangle(x, y, fillWidth, Height),
                fillColor
            );

            // Add subtle highlight at top
            spriteBatch.Draw(
                pixelTexture,
                new Rectangle(x, y, fillWidth, 2),
                Color.White * 0.3f
            );
        }

        // Draw XP text centered on bar: "Lv.X  XP/Total"
        string xpText = $"LV{_xpSystem.Level} {_xpSystem.CurrentXP}/{_xpSystem.XPToNextLevel}";
        int textWidth = xpText.Length * 6;  // ~6px per character
        int textX = x + (Width - textWidth) / 2;
        int textY = y + 2;  // Center vertically in bar

        // Use the pixel-based text drawing from InventoryUI
        InventoryUI.DrawText(spriteBatch, pixelTexture, xpText, textX, textY, TextColor);

        // Draw level-up notification floating above health bar
        if (_showLevelUpText && _levelUpTextTimer > 0)
        {
            float alpha = Math.Min(1f, _levelUpTextTimer);
            float yOffset = (2.5f - _levelUpTextTimer) * 15f;  // Float upward

            string levelUpText = $"LEVEL UP! LV{_xpSystem.Level}";
            int levelUpWidth = levelUpText.Length * 6;
            int levelUpX = (_screenWidth - levelUpWidth) / 2;
            int levelUpY = (int)(y - 20 - yOffset);

            // Draw with color based on level tier
            Color levelColor = GetLevelColor(_xpSystem.Level);
            levelColor *= alpha;

            InventoryUI.DrawText(spriteBatch, pixelTexture, levelUpText, levelUpX, levelUpY, levelColor);
        }
    }

    /// <summary>
    /// Get color based on player level tier.
    /// </summary>
    private Color GetLevelColor(int level)
    {
        if (level >= 50) return new Color(255, 200, 50);   // Gold
        if (level >= 30) return new Color(200, 100, 255);  // Purple
        if (level >= 20) return new Color(50, 150, 255);   // Blue
        if (level >= 10) return new Color(50, 255, 100);   // Green
        return new Color(255, 255, 255);                    // White
    }

    /// <summary>
    /// Get string summary for debug display.
    /// </summary>
    public string GetDebugText()
    {
        return $"Lv.{_xpSystem.Level} ({_xpSystem.CurrentXP}/{_xpSystem.XPToNextLevel} XP)";
    }
}