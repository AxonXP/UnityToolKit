using SaintsField;
using UnityEngine;

/// <summary>Base for anything a DropCollector can pick up. readyToCollect distinguishes the first
/// touch (which should knock the pickup back and start magnetizing it toward the collector) from a
/// later touch once it's already homing in (which should actually collect and consume it).</summary>
public class Pickup : MonoBehaviour
{
    [ReadOnly] public bool readyToCollect;
}
