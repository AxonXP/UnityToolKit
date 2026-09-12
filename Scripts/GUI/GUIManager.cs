using System.Collections.Generic;
using UnityEngine;

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
