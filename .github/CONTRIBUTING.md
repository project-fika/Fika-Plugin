# Contributing & Fika Code Formatting

### Keep the scope clear

This is ok:
```cs
if (!something)
{
    return;
}
```
This is not:
```cs
if (!something)
    return;
```

### Use boolean negations

This is ok:
```cs
if (!boolean)
{
    // do something
}
```
This is not:
```cs
if (boolean == false)
{
    // do something
}
```

### Avoid LINQ

Use `for` or `foreach` loops rather than LINQ statements where applicable, especially in `Update()` or methods that run often.

### Do not use null propagation or null-coalescing operators

They do not work with `Unity.GameObject`, more info [here](https://discussions.unity.com/t/usage-of-null-propagation-on-unity-objects-is-incorrect/880951).

This is ok:
```cs
if (myGameObject != null)
{
    myGameObject.Shoot();
}

if (myGameObject == null)
{
    myGameObject = MyGameObject.Create();
}
return myGameObject;
```
This is not:
```cs
myGameObject?.Shoot();

myGameObject ??= MyGameObject.Create();
return myGameObject;
```

### Use `Traverse`

Use `Traverse` when getting fields, properties, etc. that are private. If you are modifying/getting several things on the same `object`, save a reference to the `Traverse` and re-use it.

```cs
Traverse playerTraverse = Traverse.Create(this);

IVaultingComponent vaultingComponent = playerTraverse.Field<IVaultingComponent>("_vaultingComponent").Value;
if (vaultingComponent != null)
{
    UpdateEvent -= vaultingComponent.DoVaultingTick;
}

playerTraverse.Field("_vaultingComponent").SetValue(null);
playerTraverse.Field("_vaultingComponentDebug").SetValue(null);
playerTraverse.Field("_vaultingParameters").SetValue(null);
playerTraverse.Field("_vaultingGameplayRestrictions").SetValue(null);
playerTraverse.Field("_vaultAudioController").SetValue(null);
playerTraverse.Field("_sprintVaultAudioController").SetValue(null);
playerTraverse.Field("_climbAudioController").SetValue(null);
```

### AI-Generated Code Policy

We do not allow AI-generated code in contributions. We reserve the right to reject any submissions we suspect to be AI-generated.
The only exception is using AI to generate comments or documentation for code you write yourself.

### Ownership of Contributions

By submitting code to this repository, you agree that your contributions are licensed to the project under the terms of the [LICENSE](../Licenses/LICENSE.md) file. This gives the project the right to use, modify, and redistribute your contributions.
Once submitted, contributions cannot be removed or retracted, and the project may continue to use your code even if you later wish to withdraw it.
