// © 2026 Lacyway All Rights Reserved

using UnityEngine.UI;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.Components;

internal class FikaPing : MonoBehaviour
{
    public FikaPing(IntPtr pointer) : base(pointer)
    {
    }

    Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    private void Update()
    {

    }
}
