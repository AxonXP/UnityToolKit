using SaintsField;
using UnityEngine;

[System.Serializable]
public class EntityStatistics
{
    [SerializeField] float maxhealth;
    [SerializeField] float wealth;
}

public class Entity : MonoBehaviour
{
    [Expandable]
    [SerializeField] public StatisticsSO playerStats;
    [Expandable]
    [SerializeField] public WeaponSO weaponStats;
    [Expandable]
    [SerializeField] public StatisticsSO runtimeStats;

    public void Awake()
    {
        if (gameObject.tag == "Player")
            RecalculateRuntimeStats();
    }

    /// <summary>Rebuilds runtimeStats from scratch (playerStats + the currently equipped weapon's
    /// stats), then asks the ItemManager to reapply every held passive on top. Must be called any
    /// time the base changes (weapon swap, new passive) rather than tweaking runtimeStats in place,
    /// since a multiply/divide passive's result depends on what it's applied to - reapplying onto an
    /// already-modified value would compound instead of recompute.</summary>
    public void RecalculateRuntimeStats()
    {
        runtimeStats = playerStats.Clone();

        if (weaponStats != null)
            runtimeStats.MergeWeapon(weaponStats);

        if (TryGetComponent(out ItemManager itemManager))
            itemManager.ReapplyPassives(this);

        if (TryGetComponent(out ProjectileAttack projectileAttack))
            projectileAttack.RebuildTempProjectile();

        if (TryGetComponent(out Movement movement))
        {
            movement.currentMoveStats = runtimeStats.movementStatistics;
            movement.currentMoveStatsClaimed = true;
        }
    }

    /// <summary>Swaps the equipped weapon and recalculates runtimeStats to match. Unlike passives
    /// (stackable, tracked in a list), a weapon is single-slot, so this replaces weaponStats outright
    /// rather than adding to a collection.</summary>
    public void EquipWeapon(WeaponSO newWeapon)
    {
        weaponStats = newWeapon;
        RecalculateRuntimeStats();
    }

    public virtual void Death()
    {
        SendMessage("PlayParticle", new ParticleInformation("PSDeath"));
        Destroy(gameObject);
    }
}