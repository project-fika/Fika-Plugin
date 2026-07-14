using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using BepInEx.Bootstrap;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;

namespace Fika.Core.Main.Utils;

/// <summary>
/// Provides access to UI Fixes' multiselect functionality
/// </summary>
internal static class MultiSelect
{
    private static readonly Version _requiredVersion = new(2, 5);
    private static readonly Item[] _emptyArray = [];

    private static bool? _uIFixesLoaded;

    private static Type _multiSelectType;
    private static MethodInfo _getCountMethod;
    private static MethodInfo _getItemsMethod;
    private static MethodInfo _applyMethod;

    /// <summary>
    /// The number of items in the current selection, 0 if UI Fixes is not present.
    /// </summary>
    public static int Count
    {
        get
        {
            if (!Loaded)
            {
                return 0;
            }

            return (int)_getCountMethod.Invoke(null, null);
        }
    }

    /// <summary>
    /// Enumerable list of items in the current selection, empty if UI Fixes is not present
    /// </summary>
    public static IEnumerable<Item> Items
    {
        get
        {
            if (!Loaded)
            {
                return _emptyArray;
            }

            return (IEnumerable<Item>)_getItemsMethod.Invoke(null, null);
        }
    }

    /// <summary>
    /// This method takes an <c>Action</c> and calls it *sequentially* on each item in the current selection.
    /// Will no-op if UI Fixes is not present.
    /// </summary>
    /// <param name="action">The action to call on each item.</param>
    /// <param name="itemUiContext">Optional <c>ItemUiContext</c>; will use <c>ItemUiContext.Instance</c> if not provided.</param>
    public static void Apply(Action<Item> action, ItemUiContext itemUiContext = null)
    {
        if (!Loaded)
        {
            return;
        }

        Func<Item, Task> func = item =>
        {
            action(item);
            return Task.CompletedTask;
        };

        _applyMethod.Invoke(null, [func, itemUiContext]);
    }

    /// <summary>
    /// This method takes an <c>Func</c> that returns a <c>Task</c> and calls it *sequentially* on each item in the current selection.
    /// Will return a completed task immediately if UI Fixes is not present.
    /// </summary>
    /// <param name="func">The function to call on each item</param>
    /// <param name="itemUiContext">Optional <c>ItemUiContext</c>; will use <c>ItemUiContext.Instance</c> if not provided.</param>
    /// <returns>A <c>Task</c> that will complete when all the function calls are complete.</returns>
    public static Task Apply(Func<Item, Task> func, ItemUiContext itemUiContext = null)
    {
        if (!Loaded)
        {
            return Task.CompletedTask;
        }

        return (Task)_applyMethod.Invoke(null, [func, itemUiContext]);
    }

    private static bool Loaded
    {
        get
        {
            if (!_uIFixesLoaded.HasValue)
            {
                var present = Chainloader.PluginInfos.TryGetValue("Tyfon.UIFixes", out var pluginInfo);
                _uIFixesLoaded = present && pluginInfo.Metadata.Version >= _requiredVersion;

                if (_uIFixesLoaded.Value)
                {
                    _multiSelectType = Type.GetType("UIFixes.MultiSelectController, Tyfon.UIFixes");
                    if (_multiSelectType != null)
                    {
                        _getCountMethod = AccessTools.Method(_multiSelectType, "GetCount");
                        _getItemsMethod = AccessTools.Method(_multiSelectType, "GetItems");
                        _applyMethod = AccessTools.Method(_multiSelectType, "Apply");
                    }
                }
            }

            return _uIFixesLoaded.Value;
        }
    }
}