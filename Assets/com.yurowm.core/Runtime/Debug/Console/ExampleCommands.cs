using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Yurowm.Console {
    public class HelloWorld : ICommand {
        public override async UniTask Execute(params string[] args) {
            YConsole.WriteLine("Hello buddy! :)");
            foreach (string arg in args) {
                YConsole.WriteLine(arg);
                await UniTask.WaitForSeconds(1);
            }
        }

        public override string GetName() {
            return "hello";
        }
    }

    public class SceneResearch : ICommand {

        Dictionary<string, Func<string[], UniTask>> sublogics;

        public SceneResearch() {
            sublogics = new Dictionary<string, Func<string[], UniTask>>();
            sublogics.Add("list", ListOfChilds);
            sublogics.Add("select", SelectChild);
            sublogics.Add("details", PrintDetails);
            sublogics.Add("destroy", DestroySelected);
        }

        public override string Help() {
            StringBuilder builder = new StringBuilder();
            string format = GetName() + " {0} - {1}";
            builder.AppendLine(string.Format(format, "list", "show list of child objects"));
            builder.AppendLine(string.Format(format, "select @3", "select third child object"));
            builder.AppendLine(string.Format(format, "select @root", "select root of the scene"));
            builder.AppendLine(string.Format(format, "select @parent", "select parent object"));
            builder.AppendLine(string.Format(format, "select ABC", "select child object with ABC name"));
            builder.AppendLine(string.Format(format, "details", "show details of selected object"));
            builder.AppendLine(string.Format(format, "destroy", "destroy selected object"));
            return builder.ToString();
        }

        public override async UniTask Execute(params string[] args) {
            if (args.Length > 0 && sublogics.ContainsKey(args[0]))
                await sublogics[args[0]](args.Skip(1).ToArray());
            else
                await sublogics["help"](args.Skip(1).ToArray());
        }

        GameObject currentObject = null;
        async UniTask ListOfChilds(params string[] args) {
            YConsole.WriteLineColored(string.Format("Childs of {0}", currentObject ? currentObject.name : "@Root"), Color.green, true);

            var childs = Childs(currentObject);
            if (childs.Length == 0)
                YConsole.WriteLine("None...");
            else {
                for (int i = 0; i < childs.Length; i++)
                    YConsole.WriteLine(i + ". " + childs[i].name);
            }
        }

        async UniTask DestroySelected(params string[] args) {
            if (currentObject) {
                Transform parent = currentObject.transform.parent;
                MonoBehaviour.Destroy(currentObject);
                YConsole.Success(currentObject.name + " is removed");
                currentObject = parent ? parent.gameObject : null;
                YConsole.Success((currentObject ? currentObject.name : "@Root") + " is selected");
            } else 
                YConsole.Error("@Root can't be removed");
        }

        async UniTask SelectChild(params string[] args) {
            if (args.Length == 0) {
                YConsole.Error(
                    "scene select @1 - to select a child with index # 1"
                    + "\nscene select @root - to select the root"
                    + "\nscene select @parent - to select a parent"
                    + "\nscene select ABC - to select a child with ABC name");
                return;
            }

            foreach (var arg in args) {
				var childs = Childs(currentObject);
				
                if (childs.Length == 0) {
					YConsole.Error("Current object doesn't have any childs");
                    return;
                }

				if (arg.StartsWith("@")) {
					string substring = arg.Substring(1).ToLower();
					switch (substring) {
						case "root": {
								currentObject = null;
								YConsole.Success((currentObject ? currentObject.name : "@Root") + " is selected");
							} break;
						case "parent": {
								if (currentObject && currentObject.transform.parent) {
									currentObject = currentObject.transform.parent.gameObject;
                                    YConsole.Success((currentObject ? currentObject.name : "@Root") + " is selected");
                                } else {
                                    YConsole.Error("@Root is already selected");
                                    return;
                                }
							} break;
						default: {
								int index = -1;
								if (int.TryParse(substring, out index)) {
									if (index >= 0 && index < childs.Length) {
										currentObject = childs[index].gameObject;
                                        YConsole.Success(currentObject.name + " is selected");
                                    } else {
                                        YConsole.Error("Out or range!");
                                        return;
                                    }
                                } else {
                                    YConsole.Error("Wrong format!");
                                    return;
                                }
							} break;
					}
				} else {
					string name = arg;
					GameObject newChild = childs.FirstOrDefault(c => c.name == name);
					
					if (newChild) {
						currentObject = newChild;
                        YConsole.Success(currentObject.name + " is selected");
                    } else {
						YConsole.Error("The child is not found");
                        return;
					}
				}
            }
        }

        async UniTask PrintDetails(params string[] args) {
            if (currentObject == null)
                YConsole.Error("Can't show details of @Root");
            else {
                YConsole.Success("Details of " + currentObject.name);
                Type type;
                var components = currentObject.GetComponents<Component>();
                List<string> lines = new List<string>();
                foreach (var component in components) {
                    type = component.GetType();
                    YConsole.Alias(type.Name);
                    foreach (FieldInfo info in type.GetFields()) {
                        var value = info.GetValue(component);
                        lines.Add("   " + info.Name + ": " + (value == null ? "null" : value.ToString()));
                    }
                    foreach (PropertyInfo info in type.GetProperties()) {
                        if (info.GetIndexParameters().Length == 0)
                            try {
                                var value = info.GetValue(component, new object[0]);
                                lines.Add("   " + info.Name + ": " + (value == null ? "null" : value.ToString()));
                            } catch (Exception) {}
                    }
                    YConsole.WriteLine(string.Join("\n", lines.ToArray()));
                    lines.Clear();
                }
            }
        }

        GameObject[] Childs(GameObject gameObject) {
            if (gameObject == null)
                return SceneManager.GetActiveScene().GetRootGameObjects();
            else {
                Transform transform = gameObject.transform;
                GameObject[] result = new GameObject[transform.childCount];
                for (int i = 0; i < transform.childCount; i++)
                    result[i] = transform.GetChild(i).gameObject;
                return result;
            }
        }

        public override string GetName() {
            return "scene";
        }
    }

    public class SetCommands : ICommand {

        Dictionary<string, Func<string[], UniTask>> sublogics;

        Vector2Int defaultResolution;

        public override string Help() {
            StringBuilder builder = new StringBuilder();
            string format = GetName() + " {0} - {1}";
            builder.AppendLine(string.Format(format, "resolution default", "change screen resolution to default"));
            builder.AppendLine(string.Format(format, "resolution 480 600", "change screen resolution to 480x600"));
            builder.AppendLine(string.Format(format, "framerate default", "change target FPS to default (60)"));
            builder.AppendLine(string.Format(format, "framerate 20", "change target FPS to 20"));
            builder.AppendLine(string.Format(format, "vsync 1", "change VSycn settings to 1 (variants: 0, 1 or 2)"));
            return builder.ToString();
        }

        public SetCommands() {
            sublogics = new Dictionary<string, Func<string[], UniTask>>();
            sublogics.Add("resolution", SetResolution);
            sublogics.Add("framerate", SetFramerate);
            sublogics.Add("vsync", SetVSync);
            defaultResolution = new Vector2Int(Screen.width, Screen.height);
        }

        public override async UniTask Execute(params string[] args) {
            if (args.Length > 0 && sublogics.ContainsKey(args[0]))
                await sublogics[args[0]](args.Skip(1).ToArray());
        }

        async UniTask SetResolution(params string[] args) {
            if (args.Length > 0 && args[0] == "default") {
                Screen.SetResolution(defaultResolution.x, defaultResolution.y, true);
                YConsole.Success(string.Format("Resolution is set to {0}x{1}", defaultResolution.x, defaultResolution.y));
            }

            if (args.Length != 2) {
                YConsole.Error("set resolution 480 600 (example)");
                return;
            }

            int width;
            int height;

            if (int.TryParse(args[0], out width) && int.TryParse(args[1], out height)) {
                Screen.SetResolution(width, height, true);
                YConsole.Success($"Resolution is set to {width}x{height}");
            } else
                YConsole.Error("Error of parsing. Use only integer values.");
        }

        async UniTask SetFramerate(params string[] args) {
            if (args.Length > 0 && args[0] == "default") {
                Application.targetFrameRate = 60;
                YConsole.Success($"Frame rate is set to {Application.targetFrameRate}");
            }

            if (args.Length != 1) {
                YConsole.Error("set framerate 30 (example)");
                return;
            }

            int target;

            if (int.TryParse(args[0], out target)) {
                Application.targetFrameRate = target;
                YConsole.Success($"Frame rate is set to {Application.targetFrameRate}");
            } else
                YConsole.Error("Error of parsing. Use only integer values.");
        }

        async UniTask SetVSync(params string[] args) {
            if (args.Length != 1) {
                YConsole.Error("set vsync 1 (example)");
                return;
            }

            if (int.TryParse(args[0], out var target)) {
                target = target.Clamp(0, 2);
                QualitySettings.vSyncCount = target;
                YConsole.Success($"VSync is set to {target}");
            } else
                YConsole.Error("Error of parsing. Use only 0, 1 and 2 values.");
        }

        public override string GetName() {
            return "set";
        }
    }
}