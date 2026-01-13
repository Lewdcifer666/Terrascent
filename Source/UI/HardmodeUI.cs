using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terrascent.World.Hardmode;

namespace Terrascent.UI;

/// <summary>
/// UI component for displaying Hardmode status and transformation messages.
/// Shows a notification when hardmode activates and displays current hardmode state.
/// </summary>
public class HardmodeUI
{
    private readonly HardmodeManager _hardmodeManager;
    private Texture2D? _pixelTexture;
    private SpriteFont? _font;

    // Screen dimensions
    private int _screenWidth;
    private int _screenHeight;

    // Notification state
    private string _currentMessage = "";
    private float _messageTimer;
    private float _messageAlpha;
    private const float MESSAGE_DURATION = 4f;
    private const float FADE_DURATION = 0.5f;

    // Hardmode indicator settings
    private bool _showIndicator = true;
    private const int INDICATOR_SIZE = 24;
    private const int INDICATOR_PADDING = 10;

    // Colors
    private static readonly Color HardmodeColor = new(200, 100, 255);
    private static readonly Color HallowColor = new(255, 200, 100);
    private static readonly Color EvilColor = new(100, 50, 150);
    private static readonly Color TransformBgColor = new(20, 10, 40, 200);

    public HardmodeUI(HardmodeManager hardmodeManager)
    {
        _hardmodeManager = hardmodeManager;

        // Subscribe to events
        _hardmodeManager.OnHardmodeActivated += HandleHardmodeActivated;
        _hardmodeManager.OnTransformationProgress += HandleTransformationProgress;
        _hardmodeManager.OnTransformationComplete += HandleTransformationComplete;
    }

    /// <summary>
    /// Initialize the UI with graphics resources.
    /// </summary>
    public void Initialize(GraphicsDevice graphics, int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        _pixelTexture = new Texture2D(graphics, 1, 1);
        _pixelTexture.SetData([Color.White]);
    }

    /// <summary>
    /// Set the font for text rendering.
    /// </summary>
    public void SetFont(SpriteFont font)
    {
        _font = font;
    }

    /// <summary>
    /// Update the UI state.
    /// </summary>
    public void Update(float deltaTime)
    {
        // Update message timer
        if (_messageTimer > 0)
        {
            _messageTimer -= deltaTime;

            // Calculate alpha for fade in/out
            if (_messageTimer > MESSAGE_DURATION - FADE_DURATION)
            {
                // Fade in
                _messageAlpha = 1f - (_messageTimer - (MESSAGE_DURATION - FADE_DURATION)) / FADE_DURATION;
            }
            else if (_messageTimer < FADE_DURATION)
            {
                // Fade out
                _messageAlpha = _messageTimer / FADE_DURATION;
            }
            else
            {
                _messageAlpha = 1f;
            }
        }
        else
        {
            _messageAlpha = 0f;
        }
    }

    /// <summary>
    /// Draw the hardmode UI elements.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        if (_pixelTexture == null) return;

        // Draw transformation progress if active
        if (_hardmodeManager.IsTransforming)
        {
            DrawTransformationProgress(spriteBatch);
        }

        // Draw notification message
        if (_messageAlpha > 0 && !string.IsNullOrEmpty(_currentMessage))
        {
            DrawNotification(spriteBatch);
        }

        // Draw hardmode indicator
        if (_hardmodeManager.IsHardmode && _showIndicator && !_hardmodeManager.IsTransforming)
        {
            DrawHardmodeIndicator(spriteBatch);
        }
    }

    /// <summary>
    /// Draw the transformation progress bar and status.
    /// </summary>
    private void DrawTransformationProgress(SpriteBatch spriteBatch)
    {
        if (_font == null) return;

        // Semi-transparent background
        int bgWidth = 400;
        int bgHeight = 100;
        int bgX = (_screenWidth - bgWidth) / 2;
        int bgY = _screenHeight / 3;

        spriteBatch.Draw(_pixelTexture,
            new Rectangle(bgX, bgY, bgWidth, bgHeight),
            TransformBgColor);

        // Border
        DrawBorder(spriteBatch, new Rectangle(bgX, bgY, bgWidth, bgHeight), HardmodeColor, 2);

        // Title
        string title = "HARDMODE ACTIVATED";
        Vector2 titleSize = _font.MeasureString(title);
        Vector2 titlePos = new(bgX + (bgWidth - titleSize.X) / 2, bgY + 10);
        spriteBatch.DrawString(_font, title, titlePos, HardmodeColor);

        // Current status message
        if (!string.IsNullOrEmpty(_currentMessage))
        {
            Vector2 msgSize = _font.MeasureString(_currentMessage);
            Vector2 msgPos = new(bgX + (bgWidth - msgSize.X) / 2, bgY + 35);
            spriteBatch.DrawString(_font, _currentMessage, msgPos, Color.White);
        }

        // Progress bar
        int barWidth = bgWidth - 40;
        int barHeight = 20;
        int barX = bgX + 20;
        int barY = bgY + bgHeight - barHeight - 15;

        // Background
        spriteBatch.Draw(_pixelTexture,
            new Rectangle(barX, barY, barWidth, barHeight),
            new Color(30, 30, 30));

        // Progress fill
        float progress = _hardmodeManager.TransformationProgress;
        int fillWidth = (int)(barWidth * progress);

        // Gradient from evil to hallow
        Color progressColor = Color.Lerp(EvilColor, HallowColor, progress);
        spriteBatch.Draw(_pixelTexture,
            new Rectangle(barX, barY, fillWidth, barHeight),
            progressColor);

        // Border
        DrawBorder(spriteBatch, new Rectangle(barX, barY, barWidth, barHeight), Color.White, 1);

        // Percentage text
        string percentText = $"{(int)(progress * 100)}%";
        Vector2 percentSize = _font.MeasureString(percentText);
        Vector2 percentPos = new(barX + (barWidth - percentSize.X) / 2, barY + (barHeight - percentSize.Y) / 2);
        spriteBatch.DrawString(_font, percentText, percentPos, Color.White);
    }

    /// <summary>
    /// Draw notification message.
    /// </summary>
    private void DrawNotification(SpriteBatch spriteBatch)
    {
        if (_font == null) return;

        Color textColor = HardmodeColor * _messageAlpha;
        Color shadowColor = Color.Black * _messageAlpha * 0.7f;

        Vector2 textSize = _font.MeasureString(_currentMessage);
        Vector2 position = new((_screenWidth - textSize.X) / 2, _screenHeight / 4);

        // Shadow
        spriteBatch.DrawString(_font, _currentMessage, position + new Vector2(2, 2), shadowColor);

        // Main text
        spriteBatch.DrawString(_font, _currentMessage, position, textColor);
    }

    /// <summary>
    /// Draw hardmode indicator in corner.
    /// </summary>
    private void DrawHardmodeIndicator(SpriteBatch spriteBatch)
    {
        int x = _screenWidth - INDICATOR_SIZE - INDICATOR_PADDING;
        int y = INDICATOR_PADDING;

        // Background
        spriteBatch.Draw(_pixelTexture,
            new Rectangle(x - 2, y - 2, INDICATOR_SIZE + 4, INDICATOR_SIZE + 4),
            new Color(0, 0, 0, 150));

        // Indicator (pulsing color between hallow and evil)
        float pulse = (float)Math.Sin(Environment.TickCount64 / 500.0) * 0.5f + 0.5f;
        Color indicatorColor = Color.Lerp(EvilColor, HallowColor, pulse);

        spriteBatch.Draw(_pixelTexture,
            new Rectangle(x, y, INDICATOR_SIZE, INDICATOR_SIZE),
            indicatorColor);

        // "H" text if font available
        if (_font != null)
        {
            string h = "H";
            Vector2 hSize = _font.MeasureString(h);
            Vector2 hPos = new(x + (INDICATOR_SIZE - hSize.X) / 2, y + (INDICATOR_SIZE - hSize.Y) / 2);
            spriteBatch.DrawString(_font, h, hPos, Color.White);
        }
    }

    /// <summary>
    /// Draw a rectangle border.
    /// </summary>
    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        // Top
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        // Bottom
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        // Left
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        // Right
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    /// <summary>
    /// Show a notification message.
    /// </summary>
    public void ShowMessage(string message, float duration = MESSAGE_DURATION)
    {
        _currentMessage = message;
        _messageTimer = duration;
    }

    /// <summary>
    /// Toggle the hardmode indicator visibility.
    /// </summary>
    public void ToggleIndicator()
    {
        _showIndicator = !_showIndicator;
    }

    // Event handlers

    private void HandleHardmodeActivated()
    {
        ShowMessage("The ancient spirits of light and dark have been released...", 5f);
    }

    private void HandleTransformationProgress(string status)
    {
        _currentMessage = status;
    }

    private void HandleTransformationComplete()
    {
        ShowMessage("HARDMODE TRANSFORMATION COMPLETE!", 5f);
    }
}
