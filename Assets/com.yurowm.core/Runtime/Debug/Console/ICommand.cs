using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.Console {
    public abstract class ICommand {
        public abstract string GetName();
        public abstract UniTask Execute(params string[] args);
        public virtual string Help() {
            return null;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class QuickCommand : Attribute {
        MethodInfo method = null;
        Regex regex;
        string commandBody;
        string parametersExample;
        string description;

        static readonly Regex scapeReplaceRegex = new Regex(@"\s+");

        public QuickCommand(string commandBody, string parametersExample = null, string description = null) {
            commandBody = scapeReplaceRegex.Replace(commandBody.Trim(), " ");
            this.commandBody = commandBody;
            this.parametersExample = parametersExample;
            this.description = description;
        }

        public void SetMethod(MethodInfo method) {
            this.method = method;
            int index = -1;
            string expression = commandBody;
            foreach (var parameter in method.GetParameters()) {
                index++;
                if (parameter.ParameterType == typeof(string))
                    expression += " (?<arg" + index + @">\S+)";
                else if (parameter.ParameterType == typeof(int))
                    expression += " (?<arg" + index + @">\d+)";
                else if (parameter.ParameterType == typeof(float))
                    expression += " (?<arg" + index + @">[\d\.\,]+)";
                else
                    throw new Exception(method.Name + " has parameters with unsupported types. You can use only int, float and string.");
            }

            regex = new Regex(expression);
        }

        public async UniTask<bool> TryExecute(string command) {
            Match match = regex.Match(command);
            Group group = null;
            if (regex.IsMatch(command)) {
                #region Extruct Parameters
                var parameters = new List<object>();
                foreach (var parameter in method.GetParameters()) {
                    group = match.Groups["arg" + parameters.Count];

                    if (parameter.ParameterType == typeof(string))
                        parameters.Add(group.Value);
                    
                    else if(parameter.ParameterType == typeof (int))
                        parameters.Add(int.Parse(group.Value));

                    else if (parameter.ParameterType == typeof(float))
                        parameters.Add(float.Parse(group.Value));
                }
                #endregion
                if (method.ReturnType == typeof(UniTask)) {
                    try {
                        await (UniTask) method.Invoke(null, parameters.ToArray());
                    } catch (Exception e) {
                        Debug.LogException(e);
                        return false;
                    }
                } else
                    await Execute(() => method.Invoke(null, parameters.ToArray()));
                return true;
            }
            return false;
        }

        async UniTask Error(string text) {
            YConsole.Error(text);
        }

        async UniTask Execute(Action action) {
            try {
                action.Invoke();
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        public string Help() {
            return description == null ? null :
                commandBody + (string.IsNullOrEmpty(parametersExample) ? "" : " " + parametersExample) + " - " + description;
        }
    }
    
    public static class Commands {
        public readonly static Dictionary<string, ICommand> commands;
        public readonly static List<QuickCommand> quickCommands;
        static Commands() {
            commands = new Dictionary<string, ICommand>();
            Type reference = typeof(ICommand);
            foreach (Type type in reference.FindInheritorTypes(true, true)) {
                if (type.IsAbstract) continue;
                ICommand command = (ICommand) Activator.CreateInstance(type);
                commands[command.GetName().ToLower()] = command;
            }
            quickCommands = new List<QuickCommand>();
            foreach (var pair in UnityUtils.GetAllMethodsWithAttribute<QuickCommand>(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
                pair.Value.SetMethod(pair.Key);
                quickCommands.Add(pair.Value);
            }
        }

        static readonly Regex wordSplitter = new(@"\s+");
        public static async UniTask Execute(string command) {
            
            string[] words = wordSplitter.Split(command);
            if (words.Length > 0) {
                if (commands.ContainsKey(words[0].ToLower())) {
                    ICommand c = commands[words[0].ToLower()];
                    try {
                        await c.Execute(words.Skip(1).ToArray());
                    } catch (Exception e) {
                        YConsole.Error(e.ToString());
                        throw;
                    }
                } else {
                    foreach (var c in quickCommands)
                        if (await c.TryExecute(command))
                            return;

                    YConsole.Error("The command is not found");
                }
            }
        }
    }

    public class ClearCommand : ICommand {
        public override async UniTask Execute(params string[] args) {
            YConsole.Instance.builder = new System.Text.StringBuilder();
            YConsole.Instance.output.text = "";
            YConsole.Instance.Hello();
        }

        public override string GetName() {
            return "clear";
        }

        public override string Help() {
            return GetName() + " - clear the console";
        }
    }

    public class HideCommand : ICommand {
        public override async UniTask Execute(params string[] args) {
            YConsole.Instance.gameObject.SetActive(false);
        }

        public override string GetName() {
            return "hide";
        }

        public override string Help() {
            return GetName() + " - close the console";
        }
    }

    public class HelpCommand : ICommand {
        public override async UniTask Execute(params string[] args) {
            Commands.commands.Values
                .Select(c => c.Help())
                .Concat(Commands.quickCommands
                    .Select(c => c.Help()))
                .Where(h => !h.IsNullOrEmpty())
                .SelectMany(h => h.Split('\n'))
                .Where(h => !h.IsNullOrEmpty())
                .Select(h => h.Trim())
                .OrderBy(h => h)
                .ForEach(YConsole.WriteLine);
        }

        public override string GetName() {
            return "help";
        }

        public override string Help() {
            return GetName() + " - show the list of commands";
        }
    }
}
