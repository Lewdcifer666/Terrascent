using Microsoft.Xna.Framework;
using Terrascent.Combat;
using Terrascent.Entities;
using Terrascent.Entities.Enemies;
using Terrascent.World;

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
    private Rectangle _currentAttackBox;
    private bool _hasHitThisAttack;  // Prevent multiple hits per attack

    /// <summary>Is an attack currently in progress?</summary>
    public bool IsAttacking => _isAttacking;

    /// <summary>Current attack progress (0-1).</summary>
    public float AttackProgress => _isAttacking ? _attackTimer / _currentAttack.Duration : 0f;

    /// <summary>The attack currently being performed.</summary>
    public ChargeAttack CurrentAttack => _currentAttack;

    /// <summary>Current attack hitbox in world space.</summary>
    public Rectangle CurrentAttackBox => _currentAttackBox;

    /// <summary>Event fired when an attack is executed.</summary>
    public event Action<AttackEventArgs>? OnAttack;

    /// <summary>Event fired when damage is dealt.</summary>
    public event Action<DamageEventArgs>? OnDamageDealt;

    // References for combat integration
    private EnemyManager? _enemyManager;
    private ChunkManager? _chunkManager;

    /// <summary>
    /// Set the enemy manager for combat integration.
    /// </summary>
    public void SetEnemyManager(EnemyManager enemyManager)
    {
        _enemyManager = enemyManager;
    }

    /// <summary>
    /// Set the chunk manager for line of sight checks.
    /// </summary>
    public void SetChunkManager(ChunkManager chunkManager)
    {
        _chunkManager = chunkManager;
    }

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

            // Update attack box position (follows player)
            _currentAttackBox = CalculateAttackBox(player, equippedWeapon.GetRange(_pendingChargeLevel));

            // Check for enemy hits during attack window
            if (!_hasHitThisAttack && _attackTimer >= 0.05f && _attackTimer <= _currentAttack.Duration * 0.8f)
            {
                TryHitEnemies(player, equippedWeapon);
            }

            if (_attackTimer >= _currentAttack.Duration)
            {
                // Attack finished
                _isAttacking = false;
                _attackTimer = 0f;
                _hasHitThisAttack = false;
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
        _hasHitThisAttack = false;

        int damage = weapon.GetDamage(chargeLevel);
        float range = weapon.GetRange(chargeLevel);

        // Calculate attack hitbox based on player facing direction
        _currentAttackBox = CalculateAttackBox(player, range);

        // Fire attack event
        OnAttack?.Invoke(new AttackEventArgs
        {
            Player = player,
            Weapon = weapon,
            ChargeLevel = chargeLevel,
            Attack = _currentAttack,
            Damage = damage,
            AttackBox = _currentAttackBox,
        });

        // Add weapon XP for using the attack
        int xpGain = 1 + chargeLevel;
        weapon.AddExperience(xpGain);
    }

    /// <summary>
    /// Try to hit enemies with the current attack.
    /// </summary>
    /// <summary>
    /// Try to hit enemies with the current attack.
    /// </summary>
    private void TryHitEnemies(Player player, Weapon weapon)
    {
        if (_enemyManager == null) return;

        int damage = weapon.GetDamage(_pendingChargeLevel);
        float knockback = 150f + _pendingChargeLevel * 50f;

        // Apply player damage modifiers
        bool isCrit = player.Stats.RollProc(player.Stats.CritChance);
        damage = player.Stats.CalculateDamage(damage, isCrit, out _);

        if (isCrit)
        {
            System.Diagnostics.Debug.WriteLine("CRITICAL HIT!");
            knockback *= 1.5f;
        }

        // Use line of sight check if chunk manager is available
        if (_chunkManager != null)
        {
            _enemyManager.DamageEnemy(_currentAttackBox, damage, knockback, player.Center, _chunkManager);
        }
        else
        {
            _enemyManager.DamageEnemy(_currentAttackBox, damage, knockback, player.Center);
        }

        // Fire damage dealt event (for damage numbers UI, etc.)
        OnDamageDealt?.Invoke(new DamageEventArgs
        {
            Source = player,
            Target = null!,  // Would need enemy reference for specific target
            Damage = damage,
            Knockback = knockback,
            KnockbackDirection = new Vector2(player.FacingDirection, 0)
        });

        _hasHitThisAttack = true;
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
    /// </summary>
    public bool TryHitEntity(Rectangle targetHitbox, int targetHealth, out int damageDealt)
    {
        damageDealt = 0;

        if (!_isAttacking)
            return false;

        if (!_currentAttackBox.Intersects(targetHitbox))
            return false;

        damageDealt = _currentAttack.DamageMultiplier > 0
            ? (int)(_currentAttack.DamageMultiplier * 10)
            : 10;

        return true;
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