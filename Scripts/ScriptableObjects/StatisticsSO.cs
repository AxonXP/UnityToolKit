using SaintsField;
using SaintsField.Playa;
using UnityEngine;

[CreateAssetMenu(fileName = "Statistics", menuName = "ScriptableObjects/Statistics")]
public class StatisticsSO : ScriptableObject
{
    /// <summary>Every statistics grouping a PassiveEffect (or anything else) can target. Adding a
    /// new stat to an existing category's class needs no changes here - this only grows when a
    /// whole new category of statistics is introduced.</summary>
    public enum StatCategory
    {
        Entity = 0,
        Weapon = 1,
        Projectile = 2,
        SpecialProjectile = 3,
        Movement = 4,
    }

    [LayoutStart("Entity", ELayout.FoldoutBox)]
    [SerializeField] public EntityStatistics entityStatistics = new();
    [LayoutStart("Weapon", ELayout.FoldoutBox)]
    [SerializeField] public WeaponStatistics weaponStatistics = new();
    [LayoutStart("Projectile", ELayout.FoldoutBox)]
    [SerializeField] public ProjectileStatistics projectileStatistics = new();
    [LayoutStart("Special Projectile", ELayout.FoldoutBox)]
    [SerializeField] public SpecialProjectileStatistics specialProjectileStatistics = new();
    [LayoutStart("Movement", ELayout.FoldoutBox)]
    [SerializeField] public MovementStatistics movementStatistics = new();

    public object GetCategoryObject(StatCategory category)
    {
        switch (category)
        {
            case StatCategory.Entity: return entityStatistics;
            case StatCategory.Weapon: return weaponStatistics;
            case StatCategory.Projectile: return projectileStatistics;
            case StatCategory.SpecialProjectile: return specialProjectileStatistics;
            case StatCategory.Movement: return movementStatistics;
            default: return null;
        }
    }

    public static System.Type GetCategoryType(StatCategory category)
    {
        switch (category)
        {
            case StatCategory.Entity: return typeof(EntityStatistics);
            case StatCategory.Weapon: return typeof(WeaponStatistics);
            case StatCategory.Projectile: return typeof(ProjectileStatistics);
            case StatCategory.SpecialProjectile: return typeof(SpecialProjectileStatistics);
            case StatCategory.Movement: return typeof(MovementStatistics);
            default: return null;
        }
    }

    /// <summary>Creates an in-memory copy so callers (e.g. Entity.finalStats) can hold per-run
    /// runtime values without ever mutating this shared asset.</summary>
    public StatisticsSO Clone()
    {
        StatisticsSO clone = CreateInstance<StatisticsSO>();
        clone.entityStatistics = StatFieldUtility.Clone(entityStatistics);
        clone.weaponStatistics = StatFieldUtility.Clone(weaponStatistics);
        clone.projectileStatistics = StatFieldUtility.Clone(projectileStatistics);
        clone.specialProjectileStatistics = StatFieldUtility.Clone(specialProjectileStatistics);
        clone.movementStatistics = StatFieldUtility.Clone(movementStatistics);
        return clone;
    }

    /// <summary>Merges two StatisticsSO (e.g. entity base stats + weapon stats) into a new instance,
    /// summing every matching field per category - adding a stat needs no changes here either.</summary>
    public static StatisticsSO operator +(StatisticsSO a, StatisticsSO b)
    {
        StatisticsSO result = a.Clone();
        StatFieldUtility.MergeInto(result.entityStatistics, b.entityStatistics);
        StatFieldUtility.MergeInto(result.weaponStatistics, b.weaponStatistics);
        StatFieldUtility.MergeInto(result.projectileStatistics, b.projectileStatistics);
        StatFieldUtility.MergeInto(result.specialProjectileStatistics, b.specialProjectileStatistics);
        StatFieldUtility.MergeInto(result.movementStatistics, b.movementStatistics);
        return result;
    }

    /// <summary>Adds a WeaponSO's stat contributions into this StatisticsSO in place - weapons only
    /// carry weapon/projectile/specialProjectile stats (no entityStatistics), so that category is
    /// left untouched.</summary>
    public void MergeWeapon(WeaponSO weapon)
    {
        StatFieldUtility.MergeInto(weaponStatistics, weapon.weaponStatistics);
        StatFieldUtility.MergeInto(projectileStatistics, weapon.projectileStatistics);
        StatFieldUtility.MergeInto(specialProjectileStatistics, weapon.specialProjectileStatistics);
    }
}
