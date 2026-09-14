using SaintsField;
using SaintsField.Playa;
using UnityEngine;

[System.Serializable]
public class WeaponStatistics
{
    [Tooltip("Speed at which the player can fire projectiles")]
    [SerializeField] public float speed;
    [Tooltip("Amount of projectiles fired")]
    [SerializeField] public float projectileCount;
    [Tooltip("Total angle for multiple projectiles")]
    [SerializeField] public float projectileSpread;
    [Tooltip("Amount of projectiles bursted (stacks with count)")]
    [SerializeField] public float burstCount;
    [Tooltip("Time in between each burst of projectiles")]
    [SerializeField] public float burstGap;
    [Tooltip("Amount of ammo per reload")]
    [SerializeField] public float magazineSize;
    [Tooltip("Time per reload")]
    [SerializeField] public float reloadTime;
}

[CreateAssetMenu(fileName = "Weapon", menuName = "ScriptableObjects/Weapon")]
public class WeaponSO : ItemSO
{
    [LayoutStart("Weapon Specific", ELayout.FoldoutBox)]
    [SerializeField] public GameObject projectilePrefab;
    [SerializeField] public WeaponStatistics weaponStatistics;
    [SerializeField] public ProjectileStatistics projectileStatistics;
    [SerializeField] public SpecialProjectileStatistics specialProjectileStatistics;
}
