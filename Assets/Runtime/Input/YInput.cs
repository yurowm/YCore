using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Yurowm.Coroutines;
using Yurowm.Extensions;
using Yurowm.Utilities;
using I = UnityEngine.Input;

namespace Yurowm.Controls {
    public static class YInput {

        static List<Button> buttons = new List<Button>();
        
        public static void AddButton(Button button) {
            if (busy) {
                onComplete.Enqueue(() => AddButton(button));
                return;
            }
                
            if (buttons.Contains(button))
                return;
            
            buttons.Add(button);
            
            if (logicSeed == 0) {
                logicSeed = YRandom.main.Value();
                Logic().Forget();
            }
        }
        
        public static void RemoveButton(Button button) {
            if (busy) {
                onComplete.Enqueue(() => RemoveButton(button));
                return;
            }
            
            if (!buttons.Contains(button))
                return;
            
            buttons.Remove(button);
            
            if (logicSeed != 0 && buttons.IsEmpty())
                logicSeed = 0;
        }
        
        public static void ClearButton(KeyCode key) {
            if (busy) {
                onComplete.Enqueue(() => ClearButton(key));
                return;
            }
            
            buttons.RemoveAll(b => b.key == key);
            
            if (logicSeed != 0 && buttons.IsEmpty())
                logicSeed = 0;
        }
        
        static float logicSeed = 0;
        static bool busy = false;
        static Queue<Action> onComplete = new Queue<Action>();
        
        static async UniTask Logic() {
            var seed = logicSeed;
            while (seed == logicSeed) {
                busy = true;
                foreach (var button in buttons) {
                    if (!button.enabled || button.onClick == null || button.key == KeyCode.None)
                        continue;
                    
                    ButtonState state = 0;
                    
                    if (button.state.HasFlag(ButtonState.Down) && I.GetKeyDown(button.key))
                        state = state | ButtonState.Down;
                    if (button.state.HasFlag(ButtonState.Hold) && I.GetKey(button.key))
                        state = state | ButtonState.Hold;
                    if (button.state.HasFlag(ButtonState.Up) && I.GetKeyUp(button.key))
                        state = state | ButtonState.Up;
                    
                    if (state != 0)
                        try {
                            button.onClick(state);
                        } catch (Exception e) {
                            Debug.LogException(e);
                        }
                }
                busy = false;
                
                while (onComplete.Count > 0)
                    onComplete.Dequeue()();
                
                await UniTask.Yield();
            }
        }
        
        public class Button {
            public ButtonState state;
            public KeyCode key;
            public Action<ButtonState> onClick;
            public bool enabled = true;
            
            public Button(KeyCode key, ButtonState state = ButtonState.Down) {
                this.key = key;
                this.state = state;
            }

            public Button(GamepadButton gamepadButton, int gamepadNumber, ButtonState state = ButtonState.Down) 
                : this(GetGamepadKeyCode(gamepadButton, gamepadNumber), state) { }

            static KeyCode GetGamepadKeyCode(GamepadButton gpKey, int gamepadNumber) {
                switch (gpKey) {
                    case GamepadButton.A: return GetGamepadKeyCode(0, gamepadNumber);
                    case GamepadButton.B: return GetGamepadKeyCode(1, gamepadNumber);
                    case GamepadButton.X: return GetGamepadKeyCode(2, gamepadNumber);
                    case GamepadButton.Y: return GetGamepadKeyCode(3, gamepadNumber);
                    case GamepadButton.LeftBumper: return GetGamepadKeyCode(4, gamepadNumber);
                    case GamepadButton.RightBumper: return GetGamepadKeyCode(5, gamepadNumber);
                    case GamepadButton.Select: return GetGamepadKeyCode(6, gamepadNumber);
                    case GamepadButton.Start: return GetGamepadKeyCode(7, gamepadNumber);
                    case GamepadButton.LeftStick: return GetGamepadKeyCode(8, gamepadNumber);
                    case GamepadButton.RightStick: return GetGamepadKeyCode(9, gamepadNumber);
                    default: return KeyCode.None;
                }
            }
            
            static KeyCode GetGamepadKeyCode(int keyNumber, int gamepadNumber = 0) {
                gamepadNumber = gamepadNumber.Clamp(0, 8);
                var name = $"Joystick{(gamepadNumber == 0 ? string.Empty : gamepadNumber.ToString())}Button{keyNumber.Clamp(0, 19)}";
                if (Enum.TryParse(name, out KeyCode code))
                    return code;
                return KeyCode.None;
            }
        }
    }
    
    public enum ButtonState {
        Down = 1 << 1,
        Hold = 1 << 2,
        Up = 1 << 3
    }
    
    public enum GamepadButton {
        A, B, X, Y,
        LeftBumper, RightBumper,
        LeftStick, RightStick,
        Select, Start
    }
}