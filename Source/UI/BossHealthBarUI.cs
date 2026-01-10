using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terrascent.Entities.Bosses;

namespace Terrascent.UI;

/// <summary>
/// UI component for displaying boss health bar at top of screen.
/// Shows boss name, health, phase indicator, and enrage timer.
/// </summary>
public class BossHealthBarUI
{
    private readonly BossManager _bossManager;
    private int _screenWidth;
    private int _screenHeight;

    // Layout constants
    private const int BAR_WIDTH = 500;
    private const int BAR_HEIGHT = 24;
    private const int TOP_MARGIN = 120;  // Below player health bar and XP bar
    private const int PADDING = 4;

    // Animation
    private float _displayedHealthPercent = 1f;
    private float _healthAnimationSpeed = 3f;
    private float _shakeTimer;
    private float _shakeIntensity;
    private float _pulseTimer;

    // Visibility
    private float _fadeTimer;
    private const float FADE_DURATION = 0.5f;
    private bool _wasVisible;

    public BossHealthBarUI(BossManager bossManager, int screenWidth, int screenHeight)
    {
        _bossManager = bossManager;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
    }

    /// <summary>
    /// Handle screen resize.
    /// </summary>
    public void OnScreenResize(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
    }

    /// <summary>
    /// Update UI animations.
    /// </summary>
    public void Update(float deltaTime)
    {
        var boss = _bossManager.ActiveBoss;

        // Handle visibility fading
        bool isVisible = boss != null && !boss.IsDead;
        if (isVisible != _wasVisible)
        {
            _wasVisible = isVisible;
            _fadeTimer = 0f;
        }
        _fadeTimer = MathF.Min(_fadeTimer + deltaTime, FADE_DURATION);

        if (boss == null) return;

        // Animate health bar
        float targetHealth = boss.HealthPercent;
        if (MathF.Abs(_displayedHealthPercent - targetHealth) > 0.001f)
        {
            float direction = targetHealth < _displayedHealthPercent ? -1f : 1f;
            _displayedHealthPercent += direction * _healthAnimationSpeed * deltaTime;

            // Clamp to target
            if ((direction < 0 && _displayedHealthPercent < targetHealth) ||
                (direction > 0 && _displayedHealthPercent > targetHealth))
            {
                _displayedHealthPercent = targetHealth;
            }

            // Shake when taking damage
            if (direction < 0)
            {
                _shakeTimer = 0.2f;
                _shakeIntensity = 3f;
            }
        }

        // Update shake
        if (_shakeTimer > 0)
        {
            _shakeTimer -= deltaTime;
        }

        // Update pulse
        _pulseTimer += deltaTime;
    }

    /// <summary>
    /// Draw the boss health bar.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
    {
        var boss = _bossManager.ActiveBoss;
        if (boss == null && _fadeTimer >= FADE_DURATION) return;

        // Calculate fade alpha
        float alpha = _wasVisible
            ? MathF.Min(_fadeTimer / FADE_DURATION, 1f)
            : 1f - MathF.Min(_fadeTimer / FADE_DURATION, 1f);

        if (alpha <= 0.01f) return;

        // Use cached boss data if fading out
        if (boss == null) return;

        // Calculate position
        int centerX = _screenWidth / 2;
        int barX = centerX - BAR_WIDTH / 2;
        int barY = TOP_MARGIN;

        // Apply shake offset
        float shakeX = 0;
        float shakeY = 0;
        if (_shakeTimer > 0)
        {
            shakeX = (float)(Random.Shared.NextDouble() - 0.5) * _shakeIntensity * 2f;
            shakeY = (float)(Random.Shared.NextDouble() - 0.5) * _shakeIntensity * 2f;
        }

        barX += (int)shakeX;
        barY += (int)shakeY;

        // Draw background panel
        int panelHeight = BAR_HEIGHT + 40;  // Extra space for name and phase
        DrawRect(spriteBatch, pixelTexture,
            barX - PADDING, barY - 20 - PADDING,
            BAR_WIDTH + PADDING * 2, panelHeight + PADDING * 2,
            new Color(0, 0, 0, (int)(200 * alpha)));

        // Draw boss name (centered)
        string bossName = boss.Data.Name;
        int nameWidth = bossName.Length * 6;  // 5 char + 1 spacing
        InventoryUI.DrawText(spriteBatch, pixelTexture, bossName,
            centerX - nameWidth / 2, barY - 16,
            Color.White * alpha);

        // Draw phase indicator
        string phaseText = GetPhaseText(boss);
        Color phaseColor = GetPhaseColor(boss);
        int phaseWidth = phaseText.Length * 6;
        InventoryUI.DrawText(spriteBatch, pixelTexture, phaseText,
            barX + BAR_WIDTH - phaseWidth, barY - 14,
            phaseColor * alpha);

        // Draw health bar background
        DrawRect(spriteBatch, pixelTexture,
            barX, barY,
            BAR_WIDTH, BAR_HEIGHT,
            new Color(40, 0, 0, (int)(255 * alpha)));

        // Draw health bar fill
        int fillWidth = (int)(BAR_WIDTH * _displayedHealthPercent);
        Color healthColor = boss.GetHealthBarColor() * alpha;

        // Pulsing effect when enraged
        if (boss.IsEnraged)
        {
            float pulse = MathF.Sin(_pulseTimer * 5f) * 0.2f + 0.8f;
            healthColor *= pulse;
        }

        DrawRect(spriteBatch, pixelTexture,
            barX, barY,
            fillWidth, BAR_HEIGHT,
            healthColor);

        // Draw health bar border
        DrawRectOutline(spriteBatch, pixelTexture,
            barX - 1, barY - 1,
            BAR_WIDTH + 2, BAR_HEIGHT + 2,
            Color.White * alpha * 0.5f);

        // Draw health text
        string healthText = $"{boss.CurrentHealth:N0} / {boss.MaxHealth:N0}";
        int healthTextWidth = healthText.Length * 6;
        InventoryUI.DrawText(spriteBatch, pixelTexture, healthText,
            centerX - healthTextWidth / 2, barY + 8,
            Color.White * alpha);

        // Draw enrage timer (if not already enraged)
        if (!boss.IsEnraged)
        {
            int timeLeft = (int)boss.TimeToEnrage;
            int minutes = timeLeft / 60;
            int seconds = timeLeft % 60;
            string timerText = $"Enrage: {minutes}:{seconds:D2}";

            // Color warning when close to enrage
            Color timerColor = timeLeft < 60 ? Color.Red : (timeLeft < 120 ? Color.Yellow : Color.Gray);

            InventoryUI.DrawText(spriteBatch, pixelTexture, timerText,
                barX, barY + BAR_HEIGHT + 4,
                timerColor * alpha);
        }
        else
        {
            // Show ENRAGED text with pulsing effect
            float pulse = MathF.Sin(_pulseTimer * 8f) * 0.3f + 0.7f;
            InventoryUI.DrawText(spriteBatch, pixelTexture, "ENRAGED",
                barX, barY + BAR_HEIGHT + 4,
                Color.Red * pulse * alpha);
        }

        // Draw boss announcement (if any)
        string announcement = _bossManager.CurrentAnnouncement;
        if (!string.IsNullOrEmpty(announcement))
        {
            int announcementWidth = announcement.Length * 6;
            InventoryUI.DrawText(spriteBatch, pixelTexture, announcement,
                centerX - announcementWidth / 2, barY + BAR_HEIGHT + 20,
                Color.Gold * alpha);
        }
    }

    /// <summary>
    /// Get display text for current phase.
    /// </summary>
    private string GetPhaseText(Boss boss)
    {
        return boss.CurrentPhase switch
        {
            BossPhase.Phase1 => "Phase 1",
            BossPhase.Phase2 => "Phase 2",
            BossPhase.Phase3 => "Phase 3",
            BossPhase.Enraged => "ENRAGED",
            BossPhase.Dead => "DEFEATED",
            BossPhase.Despawning => "FLEEING",
            _ => ""
        };
    }

    /// <summary>
    /// Get color for current phase.
    /// </summary>
    private Color GetPhaseColor(Boss boss)
    {
        return boss.CurrentPhase switch
        {
            BossPhase.Phase1 => Color.LimeGreen,
            BossPhase.Phase2 => Color.Yellow,
            BossPhase.Phase3 => Color.Orange,
            BossPhase.Enraged => Color.Red,
            BossPhase.Dead => Color.Gray,
            BossPhase.Despawning => Color.Gray,
            _ => Color.White
        };
    }

    #region Drawing Helpers

    private void DrawRect(SpriteBatch spriteBatch, Texture2D texture, int x, int y, int width, int height, Color color)
    {
        spriteBatch.Draw(texture, new Rectangle(x, y, width, height), color);
    }

    private void DrawRectOutline(SpriteBatch spriteBatch, Texture2D texture, int x, int y, int width, int height, Color color)
    {
        // Top
        spriteBatch.Draw(texture, new Rectangle(x, y, width, 1), color);
        // Bottom
        spriteBatch.Draw(texture, new Rectangle(x, y + height - 1, width, 1), color);
        // Left
        spriteBatch.Draw(texture, new Rectangle(x, y, 1, height), color);
        // Right
        spriteBatch.Draw(texture, new Rectangle(x + width - 1, y, 1, height), color);
    }

    #endregion
}
