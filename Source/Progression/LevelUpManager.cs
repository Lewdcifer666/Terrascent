namespace Terrascent.Progression;

/// <summary>
/// Manages the Vampire Survivors-style level-up system.
/// Generates weighted random choices, handles rerolls, banishes, and synergy bonuses.
/// </summary>
public class LevelUpManager
{
    // === State ===

    /// <summary>Is the level-up UI currently active?</summary>
    public bool IsLevelUpActive { get; private set; }

    /// <summary>Current upgrade choices being offered.</summary>
    public List<UpgradeChoice> CurrentChoices { get; } = new();

    /// <summary>Number of rerolls remaining this run.</summary>
    public int RerollsRemaining { get; private set; }

    /// <summary>Maximum rerolls per run.</summary>
    public int MaxRerolls { get; private set; } = 3;

    /// <summary>Number of banishes remaining this run.</summary>
    public int BanishesRemaining { get; private set; }

    /// <summary>Maximum banishes per run.</summary>
    public int MaxBanishes { get; private set; } = 5;

    /// <summary>Number of choices offered per level-up.</summary>
    public int ChoiceCount { get; private set; } = 3;

    // === Tracking ===

    /// <summary>Upgrades the player has selected (ID -> stack count).</summary>
    private readonly Dictionary<string, int> _selectedUpgrades = new();

    /// <summary>Upgrades permanently removed from the pool.</summary>
    private readonly HashSet<string> _banishedUpgrades = new();

    /// <summary>Queue of pending level-ups (for multiple levels at once).</summary>
    private int _pendingLevelUps = 0;

    // === Synergy ===

    /// <summary>Synergy bonus multiplier per matching upgrade.</summary>
    private const float SYNERGY_BONUS = 0.25f;

    /// <summary>Category synergy bonus multiplier.</summary>
    private const float CATEGORY_SYNERGY_BONUS = 0.10f;

    // === Random ===

    private readonly Random _random;

    // === Events ===

    /// <summary>Fired when level-up choices should be displayed.</summary>
    public event Action<List<UpgradeChoice>>? OnLevelUpStarted;

    /// <summary>Fired when an upgrade is selected. Args: (upgrade, new stack count)</summary>
    public event Action<Upgrade, int>? OnUpgradeSelected;

    /// <summary>Fired when choices are rerolled.</summary>
    public event Action<List<UpgradeChoice>>? OnRerolled;

    /// <summary>Fired when an upgrade is banished.</summary>
    public event Action<Upgrade>? OnUpgradeBanished;

    /// <summary>Fired when level-up is completed (UI should close).</summary>
    public event Action? OnLevelUpCompleted;

    public LevelUpManager(int seed = 0)
    {
        _random = seed == 0 ? new Random() : new Random(seed);
        Reset();
    }

    /// <summary>
    /// Reset for a new run.
    /// </summary>
    public void Reset()
    {
        IsLevelUpActive = false;
        CurrentChoices.Clear();
        _selectedUpgrades.Clear();
        _banishedUpgrades.Clear();
        RerollsRemaining = MaxRerolls;
        BanishesRemaining = MaxBanishes;
        _pendingLevelUps = 0;
    }

    /// <summary>
    /// Queue a level-up. Call when player levels up.
    /// </summary>
    public void QueueLevelUp()
    {
        _pendingLevelUps++;

        // If not currently in level-up UI, start it
        if (!IsLevelUpActive)
        {
            StartLevelUp();
        }
    }

    /// <summary>
    /// Start the level-up process.
    /// </summary>
    private void StartLevelUp()
    {
        if (_pendingLevelUps <= 0) return;

        IsLevelUpActive = true;
        GenerateChoices(1); // Use level 1 as base, we track upgrades not level

        OnLevelUpStarted?.Invoke(CurrentChoices);

        System.Diagnostics.Debug.WriteLine($"Level-up started! Showing {CurrentChoices.Count} choices.");
    }

    /// <summary>
    /// Generate weighted random upgrade choices.
    /// </summary>
    public void GenerateChoices(int playerLevel)
    {
        CurrentChoices.Clear();

        // Get all available upgrades
        var availableUpgrades = GetAvailableUpgrades(playerLevel);

        if (availableUpgrades.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("No available upgrades!");
            return;
        }

        // Calculate weights for each upgrade
        var weightedUpgrades = new List<(Upgrade upgrade, float weight)>();

        foreach (var upgrade in availableUpgrades)
        {
            float weight = CalculateWeight(upgrade);
            weightedUpgrades.Add((upgrade, weight));
        }

        // Select upgrades using weighted random
        int numChoices = Math.Min(ChoiceCount, weightedUpgrades.Count);

        for (int i = 0; i < numChoices; i++)
        {
            var selected = SelectWeightedRandom(weightedUpgrades);
            if (selected == null) break;

            int currentStacks = GetUpgradeStacks(selected.Id);
            CurrentChoices.Add(new UpgradeChoice
            {
                Upgrade = selected,
                CurrentStacks = currentStacks,
                NextStacks = currentStacks + 1
            });

            // Remove from available to prevent duplicates
            weightedUpgrades.RemoveAll(w => w.upgrade.Id == selected.Id);
        }
    }

    /// <summary>
    /// Get all upgrades available for selection.
    /// </summary>
    private List<Upgrade> GetAvailableUpgrades(int playerLevel)
    {
        var available = new List<Upgrade>();

        foreach (var upgrade in UpgradeRegistry.GetAll())
        {
            // Skip banished upgrades
            if (_banishedUpgrades.Contains(upgrade.Id))
                continue;

            // Skip if at max stacks
            int currentStacks = GetUpgradeStacks(upgrade.Id);
            if (upgrade.MaxStacks > 0 && currentStacks >= upgrade.MaxStacks)
                continue;

            // Skip if below min level
            if (playerLevel < upgrade.MinLevel)
                continue;

            // Check required upgrades
            bool hasRequirements = true;
            foreach (var requiredId in upgrade.RequiredUpgrades)
            {
                if (GetUpgradeStacks(requiredId) <= 0)
                {
                    hasRequirements = false;
                    break;
                }
            }
            if (!hasRequirements)
                continue;

            available.Add(upgrade);
        }

        return available;
    }

    /// <summary>
    /// Calculate the weighted selection chance for an upgrade.
    /// </summary>
    private float CalculateWeight(Upgrade upgrade)
    {
        // Start with rarity-based weight
        float weight = upgrade.Rarity switch
        {
            UpgradeRarity.Common => 100f,
            UpgradeRarity.Uncommon => 60f,
            UpgradeRarity.Rare => 30f,
            UpgradeRarity.Epic => 15f,
            UpgradeRarity.Legendary => 5f,
            _ => upgrade.BaseWeight
        };

        // Apply synergy bonuses from owned upgrades
        foreach (var synergyId in upgrade.SynergyWith)
        {
            int synergyStacks = GetUpgradeStacks(synergyId);
            if (synergyStacks > 0)
            {
                // Each stack of synergy upgrade adds bonus
                weight *= 1f + (SYNERGY_BONUS * synergyStacks);
            }
        }

        // Apply category synergy bonus
        int categoryCount = CountUpgradesInCategory(upgrade.Category);
        if (categoryCount > 0)
        {
            weight *= 1f + (CATEGORY_SYNERGY_BONUS * categoryCount);
        }

        return weight;
    }

    /// <summary>
    /// Select a random upgrade based on weights.
    /// </summary>
    private Upgrade? SelectWeightedRandom(List<(Upgrade upgrade, float weight)> weighted)
    {
        if (weighted.Count == 0) return null;

        float totalWeight = weighted.Sum(w => w.weight);
        float roll = (float)_random.NextDouble() * totalWeight;

        float cumulative = 0f;
        foreach (var (upgrade, weight) in weighted)
        {
            cumulative += weight;
            if (roll <= cumulative)
            {
                return upgrade;
            }
        }

        // Fallback to last
        return weighted[^1].upgrade;
    }

    /// <summary>
    /// Select an upgrade choice.
    /// </summary>
    public void SelectUpgrade(int choiceIndex)
    {
        if (choiceIndex < 0 || choiceIndex >= CurrentChoices.Count)
        {
            System.Diagnostics.Debug.WriteLine($"Invalid choice index: {choiceIndex}");
            return;
        }

        var choice = CurrentChoices[choiceIndex];
        var upgrade = choice.Upgrade;

        // Add to selected upgrades
        if (!_selectedUpgrades.ContainsKey(upgrade.Id))
            _selectedUpgrades[upgrade.Id] = 0;

        _selectedUpgrades[upgrade.Id]++;

        int newStacks = _selectedUpgrades[upgrade.Id];
        System.Diagnostics.Debug.WriteLine($"Selected upgrade: {upgrade.Name} (now {newStacks} stacks)");

        OnUpgradeSelected?.Invoke(upgrade, newStacks);

        // Complete this level-up
        CompleteLevelUp();
    }

    /// <summary>
    /// Reroll the current choices.
    /// </summary>
    public bool Reroll(int playerLevel)
    {
        if (RerollsRemaining <= 0)
        {
            System.Diagnostics.Debug.WriteLine("No rerolls remaining!");
            return false;
        }

        RerollsRemaining--;
        GenerateChoices(playerLevel);

        System.Diagnostics.Debug.WriteLine($"Rerolled! {RerollsRemaining} rerolls remaining.");
        OnRerolled?.Invoke(CurrentChoices);

        return true;
    }

    /// <summary>
    /// Banish an upgrade from the pool permanently.
    /// </summary>
    public bool BanishUpgrade(int choiceIndex)
    {
        if (BanishesRemaining <= 0)
        {
            System.Diagnostics.Debug.WriteLine("No banishes remaining!");
            return false;
        }

        if (choiceIndex < 0 || choiceIndex >= CurrentChoices.Count)
        {
            System.Diagnostics.Debug.WriteLine($"Invalid choice index for banish: {choiceIndex}");
            return false;
        }

        var upgrade = CurrentChoices[choiceIndex].Upgrade;

        BanishesRemaining--;
        _banishedUpgrades.Add(upgrade.Id);

        System.Diagnostics.Debug.WriteLine($"Banished: {upgrade.Name}. {BanishesRemaining} banishes remaining.");
        OnUpgradeBanished?.Invoke(upgrade);

        return true;
    }

    /// <summary>
    /// Skip the current level-up without selecting.
    /// </summary>
    public void Skip()
    {
        System.Diagnostics.Debug.WriteLine("Skipped level-up choice.");
        CompleteLevelUp();
    }

    /// <summary>
    /// Complete the current level-up and check for pending ones.
    /// </summary>
    private void CompleteLevelUp()
    {
        _pendingLevelUps--;
        CurrentChoices.Clear();

        if (_pendingLevelUps > 0)
        {
            // More level-ups pending, continue
            StartLevelUp();
        }
        else
        {
            // All done
            IsLevelUpActive = false;
            OnLevelUpCompleted?.Invoke();
            System.Diagnostics.Debug.WriteLine("Level-up completed!");
        }
    }

    /// <summary>
    /// Get the number of stacks for an upgrade.
    /// </summary>
    public int GetUpgradeStacks(string upgradeId)
    {
        return _selectedUpgrades.GetValueOrDefault(upgradeId, 0);
    }

    /// <summary>
    /// Count upgrades owned in a category.
    /// </summary>
    private int CountUpgradesInCategory(UpgradeCategory category)
    {
        int count = 0;
        foreach (var (upgradeId, stacks) in _selectedUpgrades)
        {
            var upgrade = UpgradeRegistry.Get(upgradeId);
            if (upgrade != null && upgrade.Category == category)
            {
                count += stacks;
            }
        }
        return count;
    }

    /// <summary>
    /// Get all selected upgrades with their stack counts.
    /// </summary>
    public IEnumerable<(Upgrade upgrade, int stacks)> GetSelectedUpgrades()
    {
        foreach (var (id, stacks) in _selectedUpgrades)
        {
            var upgrade = UpgradeRegistry.Get(id);
            if (upgrade != null)
            {
                yield return (upgrade, stacks);
            }
        }
    }

    /// <summary>
    /// Calculate the total value of a stat from all upgrades.
    /// </summary>
    public float GetStatBonus(UpgradeStatType statType)
    {
        float total = 0f;

        foreach (var (id, stacks) in _selectedUpgrades)
        {
            var upgrade = UpgradeRegistry.Get(id);
            if (upgrade != null && upgrade.StatType == statType)
            {
                total += upgrade.Value * stacks;
            }
        }

        return total;
    }

    /// <summary>
    /// Add a reroll (from items/events).
    /// </summary>
    public void AddReroll(int count = 1)
    {
        RerollsRemaining += count;
        System.Diagnostics.Debug.WriteLine($"Gained {count} reroll(s). Now have {RerollsRemaining}.");
    }

    /// <summary>
    /// Add a banish (from items/events).
    /// </summary>
    public void AddBanish(int count = 1)
    {
        BanishesRemaining += count;
        System.Diagnostics.Debug.WriteLine($"Gained {count} banish(es). Now have {BanishesRemaining}.");
    }

    /// <summary>
    /// Set number of choices per level-up.
    /// </summary>
    public void SetChoiceCount(int count)
    {
        ChoiceCount = Math.Clamp(count, 2, 6);
    }

    // === Serialization ===

    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(RerollsRemaining);
        writer.Write(BanishesRemaining);
        writer.Write(_pendingLevelUps);

        // Save selected upgrades
        writer.Write(_selectedUpgrades.Count);
        foreach (var (id, stacks) in _selectedUpgrades)
        {
            writer.Write(id);
            writer.Write(stacks);
        }

        // Save banished upgrades
        writer.Write(_banishedUpgrades.Count);
        foreach (var id in _banishedUpgrades)
        {
            writer.Write(id);
        }
    }

    public void LoadFrom(BinaryReader reader)
    {
        RerollsRemaining = reader.ReadInt32();
        BanishesRemaining = reader.ReadInt32();
        _pendingLevelUps = reader.ReadInt32();

        // Load selected upgrades
        _selectedUpgrades.Clear();
        int selectedCount = reader.ReadInt32();
        for (int i = 0; i < selectedCount; i++)
        {
            string id = reader.ReadString();
            int stacks = reader.ReadInt32();
            _selectedUpgrades[id] = stacks;
        }

        // Load banished upgrades
        _banishedUpgrades.Clear();
        int banishedCount = reader.ReadInt32();
        for (int i = 0; i < banishedCount; i++)
        {
            _banishedUpgrades.Add(reader.ReadString());
        }
    }
}

/// <summary>
/// Represents a single upgrade choice in the level-up UI.
/// </summary>
public class UpgradeChoice
{
    /// <summary>The upgrade being offered.</summary>
    public Upgrade Upgrade { get; init; } = null!;

    /// <summary>Current number of stacks the player has.</summary>
    public int CurrentStacks { get; init; }

    /// <summary>Stacks after selecting this upgrade.</summary>
    public int NextStacks { get; init; }

    /// <summary>Is this the first time selecting this upgrade?</summary>
    public bool IsNew => CurrentStacks == 0;
}