using System.Collections.Generic;
using SaintsField;
using SaintsField.Playa;
using UnityEngine;

[RequireComponent(typeof(Entity))]
public class OverheadFakeVertical : MonoBehaviour
{
    Entity entity;
    [LayoutStart("Modifiers", ELayout.FoldoutBox)]
    public float gravity;
    public float maxHeight;
    public float jumpForce;
    public bool floorDeath;
    public float shadowScaling;
    [LayoutStart("Stats", ELayout.FoldoutBox)]
    [ReadOnly] public bool inAir;
    [ReadOnly] public Vector3 initialShadowSize;
    [ReadOnly] public bool shadowInitialized;
    [SerializeField] public float airHeight;
    [ReadOnly] public float airVelocity;
    [LayoutStart("References", ELayout.FoldoutBox)]
    public Transform sprite;
    public SpriteMask shadow;
    void Awake()
    {
        entity = GetComponent<Entity>();
        EnsureShadowInitialized();
    }

    void EnsureShadowInitialized()
    {
        if (shadowInitialized) return;
        initialShadowSize = shadow.gameObject.transform.localScale;
        shadowInitialized = true;
    }
    void Update()
    {
        if (sprite.transform.localPosition.y > 0.2)
        {
            inAir = true;
        }

        if (inAir)
        {
            InAirUpdate();
        }
    }

    public void Jump()
    {
        UpLaunch(jumpForce);
    }

    public void UpLaunch(float force)
    {
        inAir = true;
        airVelocity = force;
    }

    public void SetHeight(float height)
    {
        airHeight = Mathf.Clamp(height, 0, maxHeight);
        inAir = airHeight > 0f;
        sprite.transform.localPosition = airHeight * Vector2.up;
        ShadowUpdate();
    }

    public void InAirUpdate()
    {
        float newY = Mathf.Clamp(airHeight + (airVelocity * Time.deltaTime), 0, maxHeight);
        airHeight = newY;
        airVelocity += gravity * Time.deltaTime;
        sprite.transform.localPosition = airHeight * Vector2.up;
        if (newY == 0)
        {
            Land();
            if (floorDeath)
            {
                SendMessage("Death");
            }
        }

        ShadowUpdate();
    }

    public void ShadowUpdate()
    {
        EnsureShadowInitialized();
        shadow.transform.localScale = initialShadowSize * Mathf.Clamp(airHeight / shadowScaling, 1, 9999);
    }

    public void Land()
    {
        SendMessage("PlayParticle", new ParticleInformation("PSLand"));
        inAir = false;
    }
}