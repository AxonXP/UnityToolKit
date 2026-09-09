using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives arbitrary HUD elements purely from inspector-configured bindings to other components'
/// fields/properties (see GUIElementBinding) - mirrors AnimationManager/AnimatorBinding so new HUD
/// hookups (hearts, ammo counters, boss bars, ...) never need a line of code, only a new list entry.
/// </summary>
public class GUIManager : MonoBehaviour
{
    [SerializeField] List<GUIElementBinding> elements = new();

    void Awake()
    {
        foreach (GUIElementBinding element in elements)
            element.Initialize();
    }

    void Update()
    {
        foreach (GUIElementBinding element in elements)
            element.Apply();
    }
}
