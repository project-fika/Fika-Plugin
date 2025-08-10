// © 2025 Lacyway All Rights Reserved

using UnityEngine.UI;

namespace Fika.Core.Main.Components
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
