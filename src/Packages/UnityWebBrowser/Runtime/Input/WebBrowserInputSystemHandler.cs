// UnityWebBrowser (UWB)
// Copyright (c) 2021-2022 Voltstro-Studios
// 
// This project is under the MIT license. See the LICENSE.md file for more details.

#if ENABLE_INPUT_SYSTEM

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VoltstroStudios.UnityWebBrowser.Shared;


namespace VoltstroStudios.UnityWebBrowser.Input
{
    /// <summary>
    ///     Input handler using Unity's new input system
    /// </summary>
    [CreateAssetMenu(fileName = "Input System Handler", menuName = "UWB/Inputs/Input System Handler")]
    public sealed class WebBrowserInputSystemHandler : WebBrowserInputHandler
    {
        [Header("Scroll Input")] public InputAction scrollInput;

        public float scrollValue = 0.2f;

        [Header("Pointer Position")] public InputAction pointPosition;

        private readonly List<WindowsKey> keysDown = new();
        private readonly List<WindowsKey> keysUp = new();
        private string inputBuffer = string.Empty;

        private Keyboard keyboard;
        private IMECompositionMode compositionMode;

        public override float GetScroll()
        {
            //Mouse scroll wheel in the new input system is fucked, its value is either 120 or -120,
            //no in-between or -1.0 to 1.0 like the old input system. Why Unity.
            //While there are forum post talking about this, nothing is from Unity themselves about the issue.
            UnityEngine.XR.InputDevice rightHandDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            Vector2 joystickValue;
            float yValue = 0;
            if (rightHandDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out joystickValue))
            {
                // Extract the Y value from the Vector2 (up/down movement)
                yValue = joystickValue.y;
                Debug.Log(yValue);
            }
            float scroll = Mathf.Clamp(yValue > 0.4 ? yValue * 0.25f : yValue, -scrollValue, scrollValue);

            return scroll;
        }

        public override Vector2 GetCursorPos()
        {
            XRRayInteractor ray = GameObject.Find("Right Controller").GetComponent<XRRayInteractor>();
            if (ray.TryGetCurrent3DRaycastHit(out RaycastHit hitInfo))
            {
                // Get the world position of the hit UI element
                Vector3 worldPosition = hitInfo.point;
                // Convert the world position to screen space
                Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
                return screenPosition;
            }
            return Vector2.zero;
        }
        public override WindowsKey[] GetDownKeys()
        {
            keysDown.Clear();
            foreach (KeyControl key in keyboard.allKeys)
                try
                {
                    if (key.wasPressedThisFrame)
                        keysDown.Add(key.keyCode.UnityKeyToWindowKey());
                }
                catch (ArgumentOutOfRangeException)
                {
                    //Safe to ignore
                }

            return keysDown.ToArray();
        }

        public override WindowsKey[] GetUpKeys()
        {
            keysUp.Clear();
            foreach (KeyControl key in keyboard.allKeys)
                try
                {
                    if (key.wasReleasedThisFrame)
                        keysUp.Add(key.keyCode.UnityKeyToWindowKey());
                }
                catch (ArgumentOutOfRangeException)
                {
                    //Safe to ignore
                }

            return keysUp.ToArray();
        }

        public override string GetFrameInputBuffer()
        {
            string buffer = inputBuffer;
            inputBuffer = string.Empty;
            return buffer;
        }

        public override void OnStart()
        {
            keyboard = Keyboard.current;
            
            //Don't bother initializing some stuff if we can't get a keyboard
            if (keyboard != null)
            {
                keyboard.onTextInput += OnTextEnter;
                inputBuffer = string.Empty;
            }
            
            scrollInput.Enable();
            pointPosition.Enable();
            
            keysDown.Clear();
            keysUp.Clear();
        }

        private void OnTextEnter(char character)
        {
            inputBuffer += character;
        }

        public override void OnStop()
        {
            //Keyboard might actually already be destroyed by now
            if (keyboard != null)
            {
                keyboard.onTextInput -= OnTextEnter;
                keyboard = null;
            }
            
            scrollInput.Disable();
            pointPosition.Disable();
        }

        public override void EnableIme(Vector2 location)
        {
            //Appears we still have to set UnityEngine.Input.imeCompositionMode?
            compositionMode = UnityEngine.Input.imeCompositionMode;
            UnityEngine.Input.imeCompositionMode = IMECompositionMode.On;
            
            keyboard.SetIMEEnabled(true);
            keyboard.SetIMECursorPosition(location);
        }

        public override void DisableIme()
        {
            UnityEngine.Input.imeCompositionMode = compositionMode;
            switch (compositionMode)
            {
                case IMECompositionMode.Auto:
                case IMECompositionMode.On:
                    keyboard.SetIMEEnabled(true);
                    break;
                case IMECompositionMode.Off:
                    keyboard.SetIMEEnabled(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

#endif