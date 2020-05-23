using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static akr.Unity.Windows.NativeMethods;

namespace akr.Unity.Windows
{
    public class WindowManagerEx : MonoBehaviour
    {
        private uint defaultWindowStyle;
        private uint defaultExWindowStyle;
        private Color defaultBackgroundColor;

        public Renderer BackgroundRenderer;

        private void Awake()
        {
            defaultWindowStyle = GetWindowLong(GetUnityWindowHandle(), GWL_STYLE);
            defaultExWindowStyle = GetWindowLong(GetUnityWindowHandle(), GWL_EXSTYLE);
            defaultBackgroundColor = BackgroundRenderer.material.color;
        }

        public void SetWindowAlwaysTopMost(bool v) {
            if (v)
            {
                SetUnityWindowTopMost(true);
            }
            else {
                SetUnityWindowTopMost(false);
            }

        }

        public void SetWindowBackgroundTransparent(bool v, Color c)
        {
            if (v)
            {
                BackgroundRenderer.sharedMaterial.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
                SetDwmTransparent(true);
            }
            else
            {
                BackgroundRenderer.sharedMaterial.color = c;
                SetDwmTransparent(false);
            }
        }

        public void SetWindowBorder(bool v)
        {
            if (v)
            {
                SetWindowLong(GetUnityWindowHandle(), GWL_STYLE, WS_POPUP | WS_VISIBLE); //ウインドウ枠の削除
            }
            else
            {
                SetWindowLong(GetUnityWindowHandle(), GWL_STYLE, defaultWindowStyle);
            }
        }

        public void SetThroughMouseClick(bool v)
        {
            if (v)
            {
                SetWindowLong(GetUnityWindowHandle(), GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT); //クリックを透過する
            }
            else
            {
                SetWindowLong(GetUnityWindowHandle(), GWL_EXSTYLE, defaultExWindowStyle);
            }
        }

        void Update()
        {
        }
    }
}
