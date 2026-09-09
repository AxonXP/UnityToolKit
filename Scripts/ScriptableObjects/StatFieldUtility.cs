using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Generic reflection-backed access into the plain statistics classes (EntityStatistics,
/// WeaponStatistics, ProjectileStatistics, SpecialProjectileStatistics, ...) so callers like
/// PassiveEffect can get/set/merge a stat by name without a switch statement per stat - adding a
/// new float field to one of those classes needs no changes here.
/// </summary>
public static class StatFieldUtility
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    static readonly Dictionary<Type, FieldInfo[]> fieldCache = new();

    static FieldInfo[] GetFloatFields(Type type)
    {
        if (type == null)
            return Array.Empty<FieldInfo>();

        if (!fieldCache.TryGetValue(type, out FieldInfo[] fields))
        {
            List<FieldInfo> floatFields = new();
            foreach (FieldInfo field in type.GetFields(Flags))
            {
                if (field.FieldType == typeof(float))
                    floatFields.Add(field);
            }
            fieldCache[type] = fields = floatFields.ToArray();
        }
        return fields;
    }

    /// <summary>Names of every float field on the given statistics type - used to populate the
    /// stat-name dropdown in the inspector.</summary>
    public static string[] GetFieldNames(Type type)
    {
        FieldInfo[] fields = GetFloatFields(type);
        string[] names = new string[fields.Length];
        for (int i = 0; i < fields.Length; i++)
            names[i] = fields[i].Name;
        return names;
    }

    public static float GetValue(object statObject, string fieldName)
    {
        FieldInfo field = FindField(statObject, fieldName);
        return field == null ? 0f : (float)field.GetValue(statObject);
    }

    public static void SetValue(object statObject, string fieldName, float value)
    {
        FieldInfo field = FindField(statObject, fieldName);
        field?.SetValue(statObject, value);
    }

    static FieldInfo FindField(object statObject, string fieldName)
    {
        if (statObject == null || string.IsNullOrEmpty(fieldName))
            return null;

        FieldInfo field = statObject.GetType().GetField(fieldName, Flags);
        if (field == null)
            Debug.LogWarning($"STAT FIELD NOT FOUND: {fieldName} on {statObject.GetType().Name}");
        return field;
    }

    /// <summary>Adds every float field of source into target, in place.</summary>
    public static void MergeInto(object target, object source)
    {
        foreach (FieldInfo field in GetFloatFields(target.GetType()))
            field.SetValue(target, (float)field.GetValue(target) + (float)field.GetValue(source));
    }

    /// <summary>Creates a new T with each of source's float field values copied in - used so runtime
    /// copies (e.g. Entity.finalStats) never share storage with the source asset.</summary>
    public static T Clone<T>(T source) where T : new()
    {
        T clone = new T();
        foreach (FieldInfo field in GetFloatFields(typeof(T)))
            field.SetValue(clone, field.GetValue(source));
        return clone;
    }
}
