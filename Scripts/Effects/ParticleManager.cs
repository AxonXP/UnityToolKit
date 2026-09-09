using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for Linq operations like FirstOrDefault

public class ParticleInformation
{
    public string name;
    public Vector3 position;
    public Vector2 direction;
    public ParticleInformation(string name)
    {
        this.name = name;
    }
    public ParticleInformation(string name, Vector3 position)
    {
        this.name = name;
        this.position = position;
    }
    public ParticleInformation(string name, Vector3 position, Vector2 direction)
    {
        this.name = name;
        this.position = position;
        this.direction = direction;
    }
}

public class ParticleManager : MonoBehaviour
{
    [Tooltip("Drag In GameObjects with ParticleSystems on them")]
    public List<GameObject> particleSystems = new List<GameObject>();

    /// <summary>
    /// Play particle (will restart it if it is currently playing)
    /// </summary>
    public void PlayParticle(ParticleInformation info)
    {
        GameObject currentPS = particleSystems.Find(x => x != null && x.name == info.name);
        if (currentPS == null)
        {
            Debug.LogWarning($"ParticleManager on {name}: no particle system named '{info.name}' found (missing/destroyed reference?)", this);
            return;
        }
        if (currentPS.activeInHierarchy) //not a prefab
        {
            // apply direction
            if (info.direction != Vector2.zero)
                currentPS.transform.eulerAngles = new Vector3(0, 0, Mathf.Atan2(info.direction.y, info.direction.x) * Mathf.Rad2Deg - 90);

            // apply position
            if (info.position != Vector3.zero)
                currentPS.transform.position = info.position;

            currentPS.GetComponent<ParticleSystem>().Play();
        }
        else //prefab
        {
            GameObject newPS = Instantiate(currentPS, transform.position + info.position, Quaternion.identity);

            // apply direction
            if (info.direction != Vector2.zero)
                newPS.transform.eulerAngles = new Vector3(0, 0, Mathf.Atan2(info.direction.y, info.direction.x) * Mathf.Rad2Deg - 90);
        }
    }

    /// <summary>
    /// Play particle only if it is not currently playing
    /// </summary>
    public void StartParticle(ParticleInformation info)
    {
        GameObject currentPS = particleSystems.Find(x => x != null && x.name == info.name);
        if (currentPS == null)
        {
            Debug.LogWarning($"ParticleManager on {name}: no particle system named '{info.name}' found (missing/destroyed reference?)", this);
            return;
        }
        if (!currentPS.GetComponent<ParticleSystem>().isPlaying)
            PlayParticle(info);
    }

    /// <summary>
    /// Stops the particle system if it is playing (not applicable to prefab creations)
    /// </summary>
    public void StopParticle(ParticleInformation info)
    {
        GameObject currentPS = particleSystems.Find(x => x != null && x.name == info.name);
        if (currentPS == null)
        {
            Debug.LogWarning($"ParticleManager on {name}: no particle system named '{info.name}' found (missing/destroyed reference?)", this);
            return;
        }
        ParticleSystem ps = currentPS.GetComponent<ParticleSystem>();
        if (!ps.isPlaying)
            ps.Stop();
    }
}