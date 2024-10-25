using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yurowm.Coroutines;
using Yurowm.DebugTools;
using Yurowm.Extensions;

namespace Yurowm.Console {
    public class YConsole : MonoBehaviour {
        static YConsole _Instance = null;
        public static YConsole Instance {
            get {
                if (!_Instance && Application.isPlaying) {
                    _Instance = FindObjectOfType<YConsole>();
                    if (!_Instance) {
                        _Instance = Resources.Load<YConsole>("YConsole");
                        if (_Instance) {
                            _Instance = Instantiate(_Instance.gameObject).GetComponent<YConsole>();
                            _Instance.transform.localPosition = Vector3.zero;
                            _Instance.transform.localRotation = Quaternion.identity;
                            _Instance.transform.localScale = Vector3.one;
                            _Instance.gameObject.SetActive(false);
                            _Instance.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild;
                            _Instance.name = "YConsole";
                        } 
                    }
                }
                return _Instance;
            }
        }

        public StringBuilder builder = new StringBuilder();

        public TMP_Text output;
        public InputField input;
        public Button enter;
        public Button cancel;
        public RectTransform layout;

        [RuntimeInitializeOnLoadMethod]
        public static void InitializeOnLoad() {
            DebugPanel.Log("YConsole", "System", () => {
                DebugPanelUI.Hide();
                Instance.gameObject.SetActive(true);
            });
        }

        void Awake() {
            if (!_Instance) _Instance = this;

            enter.onClick.SetSingleListner(OnSubmit);
            cancel.onClick.SetSingleListner(OnCancel);
            output.text = "";
            input.text = "";
            input.onSubmit.AddListener(OnSubmit);
            
            Hello();
        }

        void OnEnable() {
			enter.gameObject.SetActive(true);
			cancel.gameObject.SetActive(false);
            cancelRequest = false;
            
            LayoutUpdate();
        }

        public void Hello() {
            WriteLine(ColorizeText("Write 'help' to see the command list.", Color.gray));
            WriteLine(ColorizeText("Write 'hide' to close the console.", Color.gray));
        }

		bool isFocused = false;
        void Update() {
            if (isFocused != input.isFocused) {
                isFocused = !isFocused;
                LayoutUpdate();
            }
        }
        
        void LayoutUpdate() {
            if (!layout || !input) return;

            layout.anchorMin = new Vector2(0f, input.isFocused && Application.isMobilePlatform ? 0.5f : 0f);
            layout.anchorMax = new Vector2(1f, 1f);
            layout.offsetMin = default;
            layout.offsetMax = default;
        }

        void OnSubmit() {
            OnSubmit(input.text);
        }

        public void OnSubmit(string command) {
            input.text = "";
            input.Select();
            input.ActivateInputField();
            
            if (!Application.isPlaying)
                return;
            
            command = command.Trim();
            if (string.IsNullOrEmpty(command))
                return;
            WriteLine("<i>> " + command + "</i>");
            Execute(command).Run();
        }

        bool cancelRequest = false;
        void OnCancel() {
            cancelRequest = true;
        }

        IEnumerator Execute(string command) {
            enter.gameObject.SetActive(false);
            cancel.gameObject.SetActive(true);

            cancelRequest = false;

            var logic = Commands.Execute(command, WriteLine);

            while (logic.MoveNext() && !cancelRequest)
                yield return logic.Current;
   
            cancelRequest = false;

            enter.gameObject.SetActive(true);
            cancel.gameObject.SetActive(false);
        }

        public void WriteLine(string command) {
            builder.AppendLine(command);
            var text = builder.ToString().Trim();
            // if (text.Length > 5000)
            //     text = text.Substring(text.Length - 5000, 5000);
            
            output.text = text;
        }

        public static string Error(string text) {
            return ColorizeText(text, Color.red, false, true);
        }

        public static string Success(string text) {
            return ColorizeText(text, Color.green, true);
        }

        public static string Alias(string text) {
            return ColorizeText(text, Color.cyan);
        }

        public static string Warning(string text) {
            return ColorizeText(text, Color.yellow, false, true);
        }

        public static string ColorizeText(string text, Color? color = null, bool bold = false, bool italic = false) {
            StringBuilder builder = new StringBuilder();
            if (color.HasValue)
                builder.Append(string.Format("<color=#{0:X2}{1:X2}{2:X2}{3:X2}>",
                    (byte) (255 * color.Value.r),
                    (byte) (255 * color.Value.g),
                    (byte) (255 * color.Value.b),
                    (byte) (255 * color.Value.a)));
            if (bold) builder.Append("<b>");
            if (italic) builder.Append("<i>");

            builder.Append(text);

            if (italic) builder.Append("</i>");
            if (bold) builder.Append("</b>");
            if (color.HasValue) builder.Append("</color>");

            return builder.ToString();
        }
    }
}