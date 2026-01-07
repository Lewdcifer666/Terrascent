using Microsoft.Xna.Framework;
using Terrascent.Combat;
using Terrascent.Entities;

namespace Terrascent.Systems;

/// <summary>
/// Handles weapon attacks, damage calculation, and combat interactions.
/// </summary>
public class CombatSystem
{
    // Attack state
    private bool _isAttacking;
    private float _attackTimer;
    private int _pendingChargeLevel;
    private ChargeAttack _currentAttack;

    /// <summary>Is an attack currently in progress?</summary>
    public bool IsAttacking => _isAttacking;

    /// <summary>Current attack progress (0-1).</summary>
    public float AttackProgress => _isAttacking ? _attackTimer / _currentAttack.Duration : 0f;

    /// <summary>The attack currently being performed.</summary>
    public ChargeAttack CurrentAttack => _currentAttack;

    /// <summary>Event fired when an attack is executed.</summary>
    public event Action<AttackEventArgs>? OnAttack;

    /// <summary>Event fired when damage is dealt.</summary>
    public event Action<DamageEventArgs>? OnDamageDealt;

    /// <summary>
    /// Update combat state. Call every fixed update.
    /// </summary>
    public void Update(float deltaTime, Player player, Weapon? equippedWeapon, bool attackPressed, bool attackHeld)
    {
        if (equippedWeapon == null)
            return;

        // If currently attacking, continue the attack
        if (_isAttacking)
        {
            _attackTimer += deltaTime;

            if (_attackTimer >= _currentAttack.Duration)
            {
                // Attack finished
                _isAttacking = false;
                _attackTimer = 0f;
            }
            return;
        }

        // Update weapon charging
        equippedWeapon.UpdateCharge(deltaTime, attackHeld);

        // Check for attack release
        if (!attackHeld && equippedWeapon.ChargeTime > 0)
        {
            // Player released attack button - execute attack
            int chargeLevel = equippedWeapon.ReleaseCharge();
            ExecuteAttack(player, equippedWeapon, chargeLevel);
        }
        else if (attackPressed && !attackHeld)
        {
            // Quick tap - basic attack (level 0)
            equippedWeapon.CancelCharge();
            ExecuteAttack(player, equippedWeapon, 0);
        }
    }

    /// <summary>
    /// Execute an attack at the specified charge level.
    /// </summary>
    private void ExecuteAttack(Player player, Weapon weapon, int chargeLevel)
    {
        _currentAttack = weapon.GetChargeAttack(chargeLevel);
        _isAttacking = true;
        _attackTimer = 0f;
        _pendingChargeLevel = chargeLevel;

        int damage = weapon.GetDamage(chargeLevel);
        float range = weapon.GetRange(chargeLevel);

        // Calculate attack hitbox based on player facing direction
        Rectangle attackBox = CalculateAttackBox(player, range);

        // Fire attack event
        OnAttack?.Invoke(new AttackEventArgs
        {
            Player = player,
            Weapon = weapon,
            ChargeLevel = chargeLevel,
            Attack = _currentAttack,
            Damage = damage,
            AttackBox = attackBox,
        });

        System.Diagnostics.Debug.WriteLine($"Attack: {_currentAttack.Name} (Lv.{chargeLevel}) - {damage} damage");

        // Add weapon XP for using the attack
        // More XP for higher charge levels
        int xpGain = 1 + chargeLevel;
        weapon.AddExperience(xpGain);
    }

    /// <summary>
    /// Calculate the attack hitbox based on player position and facing.
    /// </summary>
    private Rectangle CalculateAttackBox(Player player, float range)
    {
        int rangeInt = (int)range;
        int height = 32; // Attack height

        if (player.FacingDirection > 0)
        {
            // Facing right
            return new Rectangle(
                (int)(player.Position.X + player.Width),
                (int)(player.Position.Y + player.Height / 2 - height / 2),
                rangeInt,
                height
            );
        }
        else
        {
            // Facing left
            return new Rectangle(
                (int)(player.Position.X - rangeInt),
                (int)(player.Position.Y + player.Height / 2 - height / 2),
                rangeInt,
                height
            );
        }
    }

    /// <summary>
    /// Check if an entity is hit by the current attack and apply damage.
    /// Call this for each potential target during an attack.
    /// </summary>
    public bool TryHitEntity(Rectangle targetHitbox, int targetHealth, out int damageDealt)
    {
        damageDealt = 0;

        if (!_isAttacking)
            return false;

        // For now, just return if hitboxes intersect
        // TODO: Implement proper hit detection with i-frames

        return false;
    }
}

/// <summary>
/// Event data for when an attack is executed.
/// </summary>
public struct AttackEventArgs
{
    public Player Player;
    public Weapon Weapon;
    public int ChargeLevel;
    public ChargeAttack Attack;
    public int Damage;
    public Rectangle AttackBox;
}

/// <summary>
/// Event data for when damage is dealt.
/// </summary>
public struct DamageEventArgs
{
    public Entity Source;
    public Entity Target;
    public int Damage;
    public float Knockback;
    public Vector2 KnockbackDirection;
}