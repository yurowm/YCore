using System.Collections;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
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
            output.text = "";
            input.text = "";
            input.onSubmit.AddListener(OnSubmit);
            
            Hello();
        }

        void OnEnable() {
			enter.gameObject.SetActive(true);
			cancel.gameObject.SetActive(false);
            
            LayoutUpdate();
        }

        public void Hello() {
            WriteLineColored("Write 'help' to see the command list.", Color.gray);
            WriteLineColored("Write 'hide' to close the console.", Color.gray);
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
            Execute(command).Forget();
        }

        async UniTask Execute(string command) {
            enter.gameObject.SetActive(false);
            cancel.gameObject.SetActive(true);

            
            await Commands.Execute(command);

            enter.gameObject.SetActive(true);
            cancel.gameObject.SetActive(false);
        }

        public static void WriteLine(string command) {
            Instance.builder.AppendLine(command);
            var text = Instance.builder.ToString().Trim();
            // if (text.Length > 5000)
            //     text = text.Substring(text.Length - 5000, 5000);
            
            Instance.output.text = text;
        }

        public static void Error(string text) {
            WriteLineColored(text, Color.red, false, true);
        }

        public static void Success(string text) {
            WriteLineColored(text, Color.green, true);
        }

        public static void Alias(string text) {
            WriteLineColored(text, Color.cyan);
        }

        public static void Warning(string text) {
            WriteLineColored(text, Color.yellow, false, true);
        }

        public static void WriteLineColored(string text, Color? color = null, bool bold = false, bool italic = false) {
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

            WriteLine(builder.ToString());
        }
    }
}