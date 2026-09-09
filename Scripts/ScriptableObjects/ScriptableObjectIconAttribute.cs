using System;
using UnityEngine;

namespace CustomIcons
{
    // Marks a Sprite field to be used as the icon for this ScriptableObject in the Project view.
    // Only one field per ScriptableObject should have this attribute.
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ScriptableObjectIconAttribute : PropertyAttribute
    {
    }
}
