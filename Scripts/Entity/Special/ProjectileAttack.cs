using System.Collections;
using SaintsField;
using SaintsField.Playa;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class ProjectileAttack : MonoBehaviour
{
    [LayoutStart("References", ELayout.FoldoutBox)]
    [ReadOnly][SerializeField] bool canAttack;
    [ReadOnly][SerializeField] float canAttackTimer;
    [ReadOnly][SerializeField] GameObject tempProjectile;
    [ReadOnly][SerializeField] Entity entity;
    [ReadOnly][SerializeField] OverheadFakeVertical fakeVertical;
    [HideInInspector] UnityEvent attack = new UnityEvent();

    public GameObject CreateTempProjPrefab(GameObject projectile)
    {
        return null; //PrefabUtility.SaveAsPrefabAsset(projectile, "Assets/Temp/TempProjectile.prefab");
    }

    /// <summary>
    /// Something that you might want to update, but not usually (mainly to give projectiles an
    /// origin object so that there is no need to reassign every instantiation)
    /// </summary>
    /// <param name="projectile">the projectile prefab</param>
    public virtual void SetUpProj(GameObject projectile)
    {
        projectile.GetComponentInChildren<HitBox>().originObject = transform.root.gameObject;
        projectile.GetComponent<Projectile>().firedBy = this;
    }

    /// <summary>
    /// (Re)builds the temp projectile prefab from the entity's current runtimeStats and pushes those
    /// stats onto it via Projectile.SetUpProjectile - once. Must be called any time runtimeStats'
    /// base changes (weapon swap, new passive), not per shot, since every projectile fired from
    /// SendAttack is just an Instantiate() copy of this one already-configured prefab.
    /// </summary>
    public void RebuildTempProjectile()
    {
        if (entity == null)
            entity = GetComponent<Entity>();

        if (entity.weaponStats == null)
            return;

        tempProjectile.GetComponent<Projectile>().SetUpProjectile(
            entity.runtimeStats.projectileStatistics,
            entity.runtimeStats.specialProjectileStatistics);
    }

    public void CreateProjectile(GameObject projectile, Vector2 position, Vector2 direction)
    {
        GameObject newProjectile = Instantiate(projectile, position, projectile.transform.rotation);
        SetUpProj(newProjectile);
        newProjectile.GetComponent<Projectile>().StartProjectile(direction);
    }

    #region Unity Messages
    void Awake()
    {
        entity = GetComponent<Entity>();
        fakeVertical = GetComponent<OverheadFakeVertical>();
        tempProjectile = CreateTempProjPrefab(entity.weaponStats.projectilePrefab);
    }

    void Update()
    {
        if (!canAttack)
        {
            // increment
            canAttackTimer += Time.deltaTime;

            WeaponStatistics weaponStats = entity.runtimeStats.weaponStatistics;

            // invert attack speed so that it is usable in seconds
            float attackSpeedTime = 1 / weaponStats.speed;
            // need to add burst time aswell, so that you can burst constantly (but also checks if we even have burst)
            float burstTime = weaponStats.burstGap * weaponStats.burstCount;

            if (canAttackTimer >= attackSpeedTime + burstTime)
            {
                canAttack = true;
            }
        }
    }
    #endregion

    public void SendAttack()
    {
        // can't attack while airborne (e.g. mid elevator-launch jump)
        if (fakeVertical != null && fakeVertical.inAir)
            return;

        if (canAttack)
        {
            attack.Invoke();
            canAttack = false;
            canAttackTimer = 0;

            // get direction to the mouse
            Vector2 direction = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
            direction = direction.normalized;

            WeaponStatistics weaponStats = entity.runtimeStats.weaponStatistics;

            SendMessage("PlayParticle", new ParticleInformation("PSSendAttack", transform.position + (0.5f * (Vector3)direction), direction));

            // do different things depending on modifiers
            if (weaponStats.burstCount > 1)
            {
                StartCoroutine(BurstShot(direction));
            }
            else if (weaponStats.projectileCount > 1)
            {
                CleanMultiShot(direction);
            }
            else
            {
                CreateProjectile(tempProjectile, transform.position + (0.5f * (Vector3)direction), direction);
            }
        }
    }

    #region Burst + Multi
    public IEnumerator BurstShot(Vector2 direction)
    {
        WeaponStatistics weaponStats = entity.runtimeStats.weaponStatistics;

        for (int i = 0; i < weaponStats.burstCount; i++)
        {
            if (weaponStats.projectileCount > 1)
                CleanMultiShot(direction);
            else
                CreateProjectile(tempProjectile, transform.position + (0.5f * (Vector3)direction), direction);
            yield return new WaitForSeconds(weaponStats.burstGap);
        }
    }

    public void CleanMultiShot(Vector2 direction)
    {
        WeaponStatistics weaponStats = entity.runtimeStats.weaponStatistics;

        // angle the player aims at
        float aimAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // angle between each projectile
        float individualSpreadAngle = weaponStats.projectileSpread / weaponStats.projectileCount;

        // iterate through each projectile to fire
        for (int i = 0; i < weaponStats.projectileCount; i++)
        {
            // start at the bottom edge of the spread cone, offset half a slice so each projectile
            // sits at the center of its own slice - this is what keeps the middle projectile (for an
            // odd projectileCount) pointed exactly at aimAngle instead of drifting off it
            float tempAngle = aimAngle - (weaponStats.projectileSpread / 2) + (individualSpreadAngle / 2);

            // go up for each projectile
            tempAngle += i * individualSpreadAngle;

            // convert tempAngle into a vector
            Vector2 newDirection = new Vector2(Mathf.Cos(tempAngle * Mathf.Deg2Rad), Mathf.Sin(tempAngle * Mathf.Deg2Rad));

            // create projectile and fire
            CreateProjectile(tempProjectile, transform.position + (0.5f * (Vector3)direction), newDirection);
        }
    }
    #endregion
}
