using Terrascent.Items;

namespace Terrascent.Combat;

/// <summary>
/// Manages a player's weapon collection and equipped weapon.
/// Tracks weapon levels and XP across the game.
/// </summary>
public class WeaponManager
{
    // All weapons the player has used (persists levels/XP)
    private readonly Dictionary<ItemType, Weapon> _weaponInstances = new();

    /// <summary>Currently equipped weapon (null if none).</summary>
    public Weapon? EquippedWeapon { get; private set; }

    /// <summary>Item type of equipped weapon.</summary>
    public ItemType? EquippedType => EquippedWeapon?.ItemType;

    /// <summary>
    /// Get or create a weapon instance for an item type.
    /// Weapon levels persist even when switching weapons.
    /// </summary>
    public Weapon GetWeapon(ItemType itemType)
    {
        if (!WeaponRegistry.IsWeapon(itemType))
            throw new ArgumentException($"{itemType} is not a weapon");

        if (!_weaponInstances.TryGetValue(itemType, out var weapon))
        {
            weapon = new Weapon(itemType);
            _weaponInstances[itemType] = weapon;
        }

        return weapon;
    }

    /// <summary>
    /// Equip a weapon by item type.
    /// </summary>
    public bool Equip(ItemType itemType)
    {
        if (!WeaponRegistry.IsWeapon(itemType))
        {
            EquippedWeapon = null;
            return false;
        }

        EquippedWeapon = GetWeapon(itemType);
        System.Diagnostics.Debug.WriteLine($"Equipped: {EquippedWeapon}");
        return true;
    }

    /// <summary>
    /// Unequip the current weapon.
    /// </summary>
    public void Unequip()
    {
        EquippedWeapon?.CancelCharge();
        EquippedWeapon = null;
    }

    /// <summary>
    /// Check if any weapon is equipped.
    /// </summary>
    public bool HasWeaponEquipped => EquippedWeapon != null;

    /// <summary>
    /// Get all weapons the player has used.
    /// </summary>
    public IEnumerable<Weapon> GetAllWeapons() => _weaponInstances.Values;

    /// <summary>
    /// Get total weapon mastery level (sum of all weapon levels).
    /// </summary>
    public int TotalWeaponMastery => _weaponInstances.Values.Sum(w => w.Level);

    #region Serialization

    /// <summary>
    /// Save all weapon data.
    /// </summary>
    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(_weaponInstances.Count);

        foreach (var weapon in _weaponInstances.Values)
        {
            weapon.SaveTo(writer);
        }

        // Save equipped weapon type
        writer.Write(EquippedWeapon != null);
        if (EquippedWeapon != null)
        {
            writer.Write((ushort)EquippedWeapon.ItemType);
        }
    }

    /// <summary>
    /// Load all weapon data.
    /// </summary>
    public void LoadFrom(BinaryReader reader)
    {
        _weaponInstances.Clear();

        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            var weapon = Weapon.LoadFrom(reader);
            _weaponInstances[weapon.ItemType] = weapon;
        }

        // Load equipped weapon
        bool hasEquipped = reader.ReadBoolean();
        if (hasEquipped)
        {
            var equippedType = (ItemType)reader.ReadUInt16();
            if (_weaponInstances.TryGetValue(equippedType, out var weapon))
            {
                EquippedWeapon = weapon;
            }
        }
    }

    #endregion
}