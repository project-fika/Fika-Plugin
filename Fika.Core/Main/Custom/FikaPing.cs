// © 2025 Lacyway All Rights Reserved

using UnityEngine;
using UnityEngine.UI;

namespace Fika.Core.Main.Custom
{
    internal class FikaPing : MonoBehaviour
    {
        Image _image;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private void Update()
        {

        }
    }
}
