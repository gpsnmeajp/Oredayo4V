using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static akr.Unity.Windows.NativeMethods;

namespace akr.Unity.Windows
{
    public class WindowManager : MonoBehaviour
    {
        private uint defaultWindowStyle;
        private uint defaultExWindowStyle;
        private Color defaultBackgroundColor;

        public Renderer BackgroundRenderer;

        public KeyCode WindowAlwaysTopMostKey = KeyCode.W;
        public KeyCode WindowBackgroundTransparentKey = KeyCode.T;
        public KeyCode WindowBorderVisibleKey = KeyCode.B;
        public KeyCode WindowThroughMouseClickKey = KeyCode.M;
        public KeyCode DisableKey = KeyCode.LeftShift;

        private void Awake()
        {
            defaultWindowStyle = GetWindowLong(GetUnityWindowHandle(), GWL_STYLE);
            defaultExWindowStyle = GetWindowLong(GetUnityWindowHandle(), GWL_EXSTYLE);
            defaultBackgroundColor = BackgroundRenderer.material.color;
        }


        void Update()
        {

            var disableKey = Input.GetKey(DisableKey);

            var keyDownDictionary = new Dictionary<KeyCode, bool>();
            keyDownDictionary[WindowAlwaysTopMostKey] = false;
            keyDownDictionary[WindowBackgroundTransparentKey] = false;
            keyDownDictionary[WindowBorderVisibleKey] = false;
            keyDownDictionary[WindowThroughMouseClickKey] = false;
            foreach (var key in new List<KeyCode>(keyDownDictionary.Keys))
            {
                keyDownDictionary[key] = Input.GetKeyDown(key);
            }

            //Window Always TopMost
            if (disableKey && keyDownDictionary[WindowAlwaysTopMostKey])
            {
                SetUnityWindowTopMost(false);
            }
            else if (keyDownDictionary[WindowAlwaysTopMostKey])
            {
                SetUnityWindowTopMost(true);
            }

            //Window Background Transparent
            if (disableKey && keyDownDictionary[WindowBackgroundTransparentKey])
            {
                BackgroundRenderer.material.color = defaultBackgroundColor;
                SetDwmTransparent(false);
            }
            else if (keyDownDictionary[WindowBackgroundTransparentKey])
            {
                BackgroundRenderer.material.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
                SetDwmTransparent(true);
            }

            //Window Border
            if (disableKey && keyDownDictionary[WindowBorderVisibleKey])
            {
                SetWindowLong(GetUnityWindowHandle(), GWL_STYLE, defaultWindowStyle);
            }
            else if (keyDownDictionary[WindowBorderVisibleKey])
            {
                SetWindowLong(GetUnityWindowHandle(), GWL_STYLE, WS_POPUP | WS_VISIBLE); //ウインドウ枠の削除
            }

            //Through Mouse Click
            if (disableKey && keyDownDictionary[WindowThroughMouseClickKey])
            {
                SetWindowLong(GetUnityWindowHandle(), GWL_EXSTYLE, defaultExWindowStyle);
            }
            else if (keyDownDictionary[WindowThroughMouseClickKey])
            {
                SetWindowLong(GetUnityWindowHandle(), GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT); //クリックを透過する
            }
        }
    }
}
