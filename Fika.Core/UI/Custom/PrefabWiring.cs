using System;
using System.Linq;
using System.Reflection;
using Fika.Core.Main.Utils;
using UnityEngine;

namespace Fika.Core.UI.Custom;

[AttributeUsage(AttributeTargets.Field)]
public sealed class PrefabPathAttribute(string path) : Attribute
{
    public string Path { get; } = path;
}

/// <summary>
/// Attaches and wires the components that a Fika prefab cannot carry
/// </summary>
public static class PrefabWiring
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    /// <summary>
    /// Adds <typeparamref name="T"/> to an instantiated prefab and fills its object fields from the hierarchy
    /// </summary>
    /// <param name="root">The object to add the component to</param>
    /// <returns>The component, or <see langword="null"/> if a field could not be wired</returns>
    public static T Attach<T>(GameObject root) where T : Component
    {
        var component = root.AddComponent<T>();
        var wired = true;

        foreach (var field in typeof(T).GetFields(Fields))
        {
            var type = field.FieldType;
            if (type != typeof(GameObject) && !typeof(Component).IsAssignableFrom(type))
            {
                continue;
            }

            if (!IsSerialized(field))
            {
                continue;
            }

            var value = Resolve(root, field, type);
            if (value == null)
            {
                FikaGlobals.LogError($"{root.name}: could not wire {typeof(T).Name}.{field.Name} of type {type.Name}");
                wired = false;
                continue;
            }

            field.SetValue(component, value);
        }

        return wired ? component : null;
    }

    private static bool IsSerialized(FieldInfo field)
    {
        return !field.IsNotSerialized && !field.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute));
    }

    private static object Resolve(GameObject root, FieldInfo field, Type type)
    {
        var path = field.GetCustomAttribute<PrefabPathAttribute>()?.Path;
        if (path != null)
        {
            var child = root.transform.Find(path);
            if (child == null)
            {
                return null;
            }

            return type == typeof(GameObject) ? child.gameObject : child.GetComponent(Il2CppInterop.Runtime.Il2CppType.From(type));
        }

        var name = field.Name.TrimStart('_');
        foreach (var candidate in root.GetComponentsInChildren<Transform>(true))
        {
            if (!string.Equals(candidate.name, name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (type == typeof(GameObject))
            {
                return candidate.gameObject;
            }

            var component = candidate.GetComponent(Il2CppInterop.Runtime.Il2CppType.From(type));
            if (component != null)
            {
                return component;
            }
        }

        if (type == typeof(GameObject))
        {
            return null;
        }
        
        var matches = root.GetComponentsInChildren(Il2CppInterop.Runtime.Il2CppType.From(type), true);
        return matches.Count == 1 ? matches[0] : null;
    }
}
