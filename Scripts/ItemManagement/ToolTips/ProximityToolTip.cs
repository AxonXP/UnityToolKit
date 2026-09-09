using SaintsField.Playa;
using UnityEditor;
using UnityEngine;

public class ProximityToolTip : ToolTip
{
    [LayoutStart("Proximity", ELayout.FoldoutBox)]
    public Vector3 detectionCircleCenterOffset;
    public float detectionCircleRadius;
    public LayerMask playerLayer;
    override protected void Awake()
    {
        base.Awake();
        CloseTip();
    }
    void Update()
    {
        if (openTimer > 0)
        {
            openTimer -= Time.deltaTime;
        }
        else
        {
            if (open) CloseTip();
        }
    }
    void FixedUpdate()
    {
        if (Physics2D.OverlapCircle(detectionCircleCenterOffset + transform.position, detectionCircleRadius, playerLayer))
        {
            if (!open) OpenTip();
            else openTimer = timeOpen;
        }
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + detectionCircleCenterOffset, detectionCircleRadius);
    }
}