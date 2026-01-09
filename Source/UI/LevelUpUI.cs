using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terrascent.Core;
using Terrascent.Progression;

namespace Terrascent.UI;

/// <summary>
/// UI panel for Vampire Survivors-style level-up choices.
/// Shows 3-4 upgrade cards with reroll and banish options.
/// </summary>
public class LevelUpUI
{
    private readonly LevelUpManager _levelUpManager;

    // Screen dimensions
    private int _screenWidth;
    private int _screenHeight;

    // Layout constants
    private const int CARD_WIDTH = 200;
    private const int CARD_HEIGHT = 280;
    private const int CARD_SPACING = 20;
    private const int BUTTON_WIDTH = 120;
    private const int BUTTON_HEIGHT = 36;

    // State
    private int _hoveredCard = -1;
    private bool _hoveredReroll;
    private bool _hoveredSkip;
    private int _hoveredBanish = -1;  // Index of card being hovered for banish

    // Animation
    private float _animationTimer;
    private float _cardAnimationOffset;
    private const float ANIMATION_SPEED = 5f;

    // Colors
    private static readonly Color BackgroundColor = new(0, 0, 0, 200);
    private static readonly Color CardBackground = new(30, 30, 40);
    private static readonly Color CardHovered = new(50, 50, 70);
    private static readonly Color CardBorder = new(80, 80, 100);
    private static readonly Color TitleColor = new(255, 220, 100);
    private static readonly Color ButtonNormal = new(60, 60, 80);
    private static readonly Color ButtonHovered = new(80, 80, 120);
    private static readonly Color ButtonDisabled = new(40, 40, 50);

    public LevelUpUI(LevelUpManager levelUpManager, int screenWidth, int screenHeight)
    {
        _levelUpManager = levelUpManager;
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
    /// Update UI state.
    /// </summary>
    public void Update(InputManager input, float deltaTime)
    {
        // Animation
        _animationTimer += deltaTime * ANIMATION_SPEED;
        _cardAnimationOffset = MathF.Sin(_animationTimer) * 2f;

        var mousePos = input.MousePositionV;
        bool shiftHeld = input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift);

        // Reset hover states
        _hoveredCard = -1;
        _hoveredReroll = false;
        _hoveredSkip = false;
        _hoveredBanish = -1;

        // Calculate card positions
        int totalWidth = _levelUpManager.CurrentChoices.Count * (CARD_WIDTH + CARD_SPACING) - CARD_SPACING;
        int startX = (_screenWidth - totalWidth) / 2;
        int cardY = (_screenHeight - CARD_HEIGHT) / 2 - 30;

        // Check card hover
        for (int i = 0; i < _levelUpManager.CurrentChoices.Count; i++)
        {
            int cardX = startX + i * (CARD_WIDTH + CARD_SPACING);
            var cardRect = new Rectangle(cardX, cardY, CARD_WIDTH, CARD_HEIGHT);

            if (cardRect.Contains(mousePos))
            {
                if (shiftHeld && _levelUpManager.BanishesRemaining > 0)
                {
                    _hoveredBanish = i;
                }
                else
                {
                    _hoveredCard = i;
                }
                break;
            }
        }

        // Check button hover
        int buttonY = cardY + CARD_HEIGHT + 40;
        int rerollX = (_screenWidth / 2) - BUTTON_WIDTH - 10;
        int skipX = (_screenWidth / 2) + 10;

        var rerollRect = new Rectangle(rerollX, buttonY, BUTTON_WIDTH, BUTTON_HEIGHT);
        var skipRect = new Rectangle(skipX, buttonY, BUTTON_WIDTH, BUTTON_HEIGHT);

        if (rerollRect.Contains(mousePos))
            _hoveredReroll = true;
        else if (skipRect.Contains(mousePos))
            _hoveredSkip = true;

        // Handle clicks
        if (input.IsLeftMousePressed())
        {
            input.ConsumeMousePress(left: true);

            if (_hoveredBanish >= 0)
            {
                // Banish upgrade (Shift+Click)
                _levelUpManager.BanishUpgrade(_hoveredBanish);
                _levelUpManager.GenerateChoices(1);
            }
            else if (_hoveredCard >= 0)
            {
                // Select upgrade
                _levelUpManager.SelectUpgrade(_hoveredCard);
            }
            else if (_hoveredReroll && _levelUpManager.RerollsRemaining > 0)
            {
                _levelUpManager.Reroll(1);
            }
            else if (_hoveredSkip)
            {
                _levelUpManager.Skip();
            }
        }

        // Keyboard shortcuts
        if (input.IsKeyPressed(Keys.D1) && _levelUpManager.CurrentChoices.Count >= 1)
        {
            input.ConsumeKeyPress(Keys.D1);
            _levelUpManager.SelectUpgrade(0);
        }
        if (input.IsKeyPressed(Keys.D2) && _levelUpManager.CurrentChoices.Count >= 2)
        {
            input.ConsumeKeyPress(Keys.D2);
            _levelUpManager.SelectUpgrade(1);
        }
        if (input.IsKeyPressed(Keys.D3) && _levelUpManager.CurrentChoices.Count >= 3)
        {
            input.ConsumeKeyPress(Keys.D3);
            _levelUpManager.SelectUpgrade(2);
        }
        if (input.IsKeyPressed(Keys.D4) && _levelUpManager.CurrentChoices.Count >= 4)
        {
            input.ConsumeKeyPress(Keys.D4);
            _levelUpManager.SelectUpgrade(3);
        }
        if (input.IsKeyPressed(Keys.R) && _levelUpManager.RerollsRemaining > 0)
        {
            input.ConsumeKeyPress(Keys.R);
            _levelUpManager.Reroll(1);
        }
    }

    /// <summary>
    /// Draw the level-up UI.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, Vector2 mousePosition)
    {
        // Draw darkened background
        spriteBatch.Draw(pixelTexture,
            new Rectangle(0, 0, _screenWidth, _screenHeight),
            BackgroundColor);

        // Draw title
        string title = "LEVEL UP!";
        int titleWidth = title.Length * 12;  // Larger font
        int titleX = (_screenWidth - titleWidth) / 2;
        int titleY = 60;
        DrawLargeText(spriteBatch, pixelTexture, title, titleX, titleY, TitleColor);

        // Draw subtitle
        string subtitle = "Choose an upgrade:";
        int subtitleWidth = subtitle.Length * 6;
        int subtitleX = (_screenWidth - subtitleWidth) / 2;
        InventoryUI.DrawText(spriteBatch, pixelTexture, subtitle, subtitleX, titleY + 30, Color.White);

        // Draw cards
        int totalWidth = _levelUpManager.CurrentChoices.Count * (CARD_WIDTH + CARD_SPACING) - CARD_SPACING;
        int startX = (_screenWidth - totalWidth) / 2;
        int cardY = (_screenHeight - CARD_HEIGHT) / 2 - 30;

        for (int i = 0; i < _levelUpManager.CurrentChoices.Count; i++)
        {
            var choice = _levelUpManager.CurrentChoices[i];
            int cardX = startX + i * (CARD_WIDTH + CARD_SPACING);

            // Apply animation offset to hovered card
            float yOffset = (_hoveredCard == i) ? -5f + _cardAnimationOffset : 0f;

            DrawCard(spriteBatch, pixelTexture, choice, cardX, (int)(cardY + yOffset),
                _hoveredCard == i, _hoveredBanish == i, i + 1);
        }

        // Draw buttons
        int buttonY = cardY + CARD_HEIGHT + 40;
        int rerollX = (_screenWidth / 2) - BUTTON_WIDTH - 10;
        int skipX = (_screenWidth / 2) + 10;

        DrawButton(spriteBatch, pixelTexture, $"REROLL ({_levelUpManager.RerollsRemaining})",
            rerollX, buttonY, _hoveredReroll, _levelUpManager.RerollsRemaining > 0);
        DrawButton(spriteBatch, pixelTexture, "SKIP",
            skipX, buttonY, _hoveredSkip, true);

        // Draw instructions
        string instructions = "Press 1-4 to select, R to reroll, Shift+Click to banish";
        int instWidth = instructions.Length * 6;
        int instX = (_screenWidth - instWidth) / 2;
        InventoryUI.DrawText(spriteBatch, pixelTexture, instructions, instX, buttonY + 50, Color.Gray);

        // Draw banish info if holding shift
        if (_hoveredBanish >= 0)
        {
            string banishText = $"BANISH ({_levelUpManager.BanishesRemaining} remaining)";
            int banishWidth = banishText.Length * 6;
            int banishX = (_screenWidth - banishWidth) / 2;
            InventoryUI.DrawText(spriteBatch, pixelTexture, banishText, banishX, buttonY + 70, Color.Red);
        }
    }

    /// <summary>
    /// Draw an upgrade choice card.
    /// </summary>
    private void DrawCard(SpriteBatch spriteBatch, Texture2D pixelTexture,
        UpgradeChoice choice, int x, int y, bool isHovered, bool isBanishHovered, int number)
    {
        var upgrade = choice.Upgrade;
        Color rarityColor = GetRarityColor(upgrade.Rarity);

        // Card background
        Color bgColor = isBanishHovered ? new Color(80, 30, 30) :
                       isHovered ? CardHovered : CardBackground;

        spriteBatch.Draw(pixelTexture, new Rectangle(x, y, CARD_WIDTH, CARD_HEIGHT), bgColor);

        // Rarity border
        int borderWidth = 3;
        Color borderColor = isBanishHovered ? Color.Red : rarityColor;
        DrawBorder(spriteBatch, pixelTexture, x, y, CARD_WIDTH, CARD_HEIGHT, borderWidth, borderColor);

        // Number indicator (top left)
        string numText = number.ToString();
        spriteBatch.Draw(pixelTexture, new Rectangle(x + 5, y + 5, 20, 20), new Color(20, 20, 30));
        InventoryUI.DrawText(spriteBatch, pixelTexture, numText, x + 12, y + 11, Color.White);

        // NEW badge for first-time upgrades
        if (choice.IsNew)
        {
            string newText = "NEW";
            int newX = x + CARD_WIDTH - 35;
            spriteBatch.Draw(pixelTexture, new Rectangle(newX - 2, y + 5, 32, 16), Color.Gold);
            InventoryUI.DrawText(spriteBatch, pixelTexture, newText, newX, y + 9, Color.Black);
        }

        // Upgrade icon area
        int iconSize = 48;
        int iconX = x + (CARD_WIDTH - iconSize) / 2;
        int iconY = y + 35;

        spriteBatch.Draw(pixelTexture, new Rectangle(iconX, iconY, iconSize, iconSize), GetCategoryColor(upgrade.Category));
        DrawBorder(spriteBatch, pixelTexture, iconX, iconY, iconSize, iconSize, 2, rarityColor);

        // Category symbol in icon
        string categorySymbol = GetCategorySymbol(upgrade.Category);
        int symbolX = iconX + (iconSize - categorySymbol.Length * 6) / 2;
        InventoryUI.DrawText(spriteBatch, pixelTexture, categorySymbol, symbolX, iconY + 20, Color.White);

        // Upgrade name (centered)
        int nameY = iconY + iconSize + 15;
        string name = upgrade.Name.ToUpper();
        int nameWidth = name.Length * 6;
        int nameX = x + (CARD_WIDTH - nameWidth) / 2;
        InventoryUI.DrawText(spriteBatch, pixelTexture, name, nameX, nameY, rarityColor);

        // Rarity label
        int rarityY = nameY + 15;
        string rarityText = upgrade.Rarity.ToString().ToUpper();
        int rarityWidth = rarityText.Length * 6;
        int rarityX = x + (CARD_WIDTH - rarityWidth) / 2;
        InventoryUI.DrawText(spriteBatch, pixelTexture, rarityText, rarityX, rarityY, rarityColor * 0.7f);

        // Description (wrapped)
        int descY = rarityY + 20;
        string desc = upgrade.GetDescription(choice.NextStacks);
        DrawWrappedText(spriteBatch, pixelTexture, desc, x + 10, descY, CARD_WIDTH - 20, Color.LightGray);

        // Stack count (bottom)
        if (upgrade.MaxStacks > 0)
        {
            int stackY = y + CARD_HEIGHT - 30;
            string stackText = $"{choice.CurrentStacks}/{upgrade.MaxStacks}";

            if (choice.CurrentStacks > 0)
            {
                stackText = $"{choice.CurrentStacks} -> {choice.NextStacks}/{upgrade.MaxStacks}";
            }

            int stackWidth = stackText.Length * 6;
            int stackX = x + (CARD_WIDTH - stackWidth) / 2;

            // Background for stack count
            spriteBatch.Draw(pixelTexture,
                new Rectangle(stackX - 5, stackY - 2, stackWidth + 10, 14),
                new Color(20, 20, 30));

            Color stackColor = choice.CurrentStacks > 0 ? Color.Cyan : Color.Gray;
            InventoryUI.DrawText(spriteBatch, pixelTexture, stackText, stackX, stackY, stackColor);
        }

        // Category label at bottom
        int catY = y + CARD_HEIGHT - 15;
        string catText = upgrade.Category.ToString();
        int catWidth = catText.Length * 6;
        int catX = x + (CARD_WIDTH - catWidth) / 2;
        InventoryUI.DrawText(spriteBatch, pixelTexture, catText, catX, catY, Color.Gray * 0.6f);
    }

    /// <summary>
    /// Draw a button.
    /// </summary>
    private void DrawButton(SpriteBatch spriteBatch, Texture2D pixelTexture,
        string text, int x, int y, bool isHovered, bool isEnabled)
    {
        Color bgColor = !isEnabled ? ButtonDisabled :
                       isHovered ? ButtonHovered : ButtonNormal;

        spriteBatch.Draw(pixelTexture, new Rectangle(x, y, BUTTON_WIDTH, BUTTON_HEIGHT), bgColor);

        if (isEnabled)
        {
            DrawBorder(spriteBatch, pixelTexture, x, y, BUTTON_WIDTH, BUTTON_HEIGHT, 2,
                isHovered ? Color.White : Color.Gray);
        }

        int textWidth = text.Length * 6;
        int textX = x + (BUTTON_WIDTH - textWidth) / 2;
        int textY = y + (BUTTON_HEIGHT - 7) / 2;

        Color textColor = isEnabled ? (isHovered ? Color.White : Color.LightGray) : Color.DarkGray;
        InventoryUI.DrawText(spriteBatch, pixelTexture, text, textX, textY, textColor);
    }

    /// <summary>
    /// Draw a border around a rectangle.
    /// </summary>
    private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixelTexture,
        int x, int y, int width, int height, int thickness, Color color)
    {
        // Top
        spriteBatch.Draw(pixelTexture, new Rectangle(x, y, width, thickness), color);
        // Bottom
        spriteBatch.Draw(pixelTexture, new Rectangle(x, y + height - thickness, width, thickness), color);
        // Left
        spriteBatch.Draw(pixelTexture, new Rectangle(x, y, thickness, height), color);
        // Right
        spriteBatch.Draw(pixelTexture, new Rectangle(x + width - thickness, y, thickness, height), color);
    }

    /// <summary>
    /// Draw text with word wrapping.
    /// </summary>
    private void DrawWrappedText(SpriteBatch spriteBatch, Texture2D pixelTexture,
        string text, int x, int y, int maxWidth, Color color)
    {
        int charWidth = 6;
        int lineHeight = 12;
        int maxChars = maxWidth / charWidth;

        string[] words = text.Split(' ');
        string currentLine = "";
        int currentY = y;

        foreach (string word in words)
        {
            string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;

            if (testLine.Length > maxChars && !string.IsNullOrEmpty(currentLine))
            {
                // Draw current line and start new one
                InventoryUI.DrawText(spriteBatch, pixelTexture, currentLine, x, currentY, color);
                currentY += lineHeight;
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        // Draw remaining text
        if (!string.IsNullOrEmpty(currentLine))
        {
            InventoryUI.DrawText(spriteBatch, pixelTexture, currentLine, x, currentY, color);
        }
    }

    /// <summary>
    /// Draw large text (2x scale).
    /// </summary>
    private void DrawLargeText(SpriteBatch spriteBatch, Texture2D pixelTexture,
        string text, int x, int y, Color color)
    {
        // Simple 2x scale by drawing offset pixels
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            int charX = x + i * 12;  // 2x spacing

            // Draw each character at 2x scale
            DrawLargeChar(spriteBatch, pixelTexture, c, charX, y, color);
        }
    }

    /// <summary>
    /// Draw a single character at 2x scale.
    /// </summary>
    private void DrawLargeChar(SpriteBatch spriteBatch, Texture2D pixelTexture,
        char c, int x, int y, Color color)
    {
        // Use InventoryUI patterns but doubled
        bool[,] pattern = GetCharPattern(c);

        for (int row = 0; row < 7; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                if (pattern[row, col])
                {
                    spriteBatch.Draw(pixelTexture,
                        new Rectangle(x + col * 2, y + row * 2, 2, 2),
                        color);
                }
            }
        }
    }

    /// <summary>
    /// Get character pattern for large text.
    /// </summary>
    private static bool[,] GetCharPattern(char c)
    {
        return char.ToUpper(c) switch
        {
            'L' => new bool[,] { { true, false, false, false, false }, { true, false, false, false, false }, { true, false, false, false, false }, { true, false, false, false, false }, { true, false, false, false, false }, { true, false, false, false, false }, { true, true, true, true, true } },
            'E' => new bool[,] { { true, true, true, true, true }, { true, false, false, false, false }, { true, false, false, false, false }, { true, true, true, true, false }, { true, false, false, false, false }, { true, false, false, false, false }, { true, true, true, true, true } },
            'V' => new bool[,] { { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { false, true, false, true, false }, { false, false, true, false, false } },
            'U' => new bool[,] { { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { false, true, true, true, false } },
            'P' => new bool[,] { { true, true, true, true, false }, { true, false, false, false, true }, { true, false, false, false, true }, { true, true, true, true, false }, { true, false, false, false, false }, { true, false, false, false, false }, { true, false, false, false, false } },
            '!' => new bool[,] { { false, false, true, false, false }, { false, false, true, false, false }, { false, false, true, false, false }, { false, false, true, false, false }, { false, false, true, false, false }, { false, false, false, false, false }, { false, false, true, false, false } },
            ' ' => new bool[7, 5],
            _ => new bool[7, 5]
        };
    }

    /// <summary>
    /// Get color for upgrade rarity.
    /// </summary>
    private static Color GetRarityColor(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Common => Color.White,
            UpgradeRarity.Uncommon => new Color(100, 255, 100),    // Green
            UpgradeRarity.Rare => new Color(100, 150, 255),        // Blue
            UpgradeRarity.Epic => new Color(200, 100, 255),        // Purple
            UpgradeRarity.Legendary => new Color(255, 180, 50),    // Orange/Gold
            _ => Color.White
        };
    }

    /// <summary>
    /// Get color for upgrade category.
    /// </summary>
    private static Color GetCategoryColor(UpgradeCategory category)
    {
        return category switch
        {
            UpgradeCategory.Offense => new Color(180, 50, 50),     // Red
            UpgradeCategory.Defense => new Color(50, 100, 180),    // Blue
            UpgradeCategory.Mobility => new Color(50, 180, 100),   // Green
            UpgradeCategory.Survival => new Color(180, 100, 50),   // Orange
            UpgradeCategory.OnHit => new Color(180, 180, 50),      // Yellow
            UpgradeCategory.OnKill => new Color(100, 50, 100),     // Purple
            UpgradeCategory.Utility => new Color(100, 100, 100),   // Gray
            UpgradeCategory.Weapon => new Color(150, 80, 50),      // Brown
            _ => new Color(80, 80, 80)
        };
    }

    /// <summary>
    /// Get symbol for upgrade category.
    /// </summary>
    private static string GetCategorySymbol(UpgradeCategory category)
    {
        return category switch
        {
            UpgradeCategory.Offense => "ATK",
            UpgradeCategory.Defense => "DEF",
            UpgradeCategory.Mobility => "SPD",
            UpgradeCategory.Survival => "RGN",
            UpgradeCategory.OnHit => "HIT",
            UpgradeCategory.OnKill => "KIL",
            UpgradeCategory.Utility => "UTL",
            UpgradeCategory.Weapon => "WPN",
            _ => "GEN"
        };
    }
}