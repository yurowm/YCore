using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Scripting;
using Yurowm.Extensions;
using Yurowm.Serialization;
using Yurowm.Utilities;

namespace Yurowm.DebugTools {
	public static class DebugPanel {
		public static Action onLog = delegate {};
		public static Action<Entry> onNewEntry = delegate {};
		public static Action<Entry> onNewMessage = delegate {};

		#region Entries

		static Dictionary<int, Entry> entries = new();
		
		static Dictionary<string, List<Entry>> groups = new();
		
		public class Entry {
			public readonly string name;
			public readonly string group;
			public readonly int hash;
			
			public IMessage message;
			public readonly LogPoint logPoint;

			public Entry(string name, string group) {
				this.name = name;
				this.group = group;
				logPoint = LogPoint.Emit();
				
				hash = GetHashCode(name, group);
			}

			public static int GetHashCode(string name, string group) {
				unchecked {
					var hashCode = (name != null ? name.GetHashCode() : 0);
					hashCode = (hashCode * 397) ^ (group != null ? group.GetHashCode() : 0);
					return hashCode;
				}
			}

			public override int GetHashCode() {
				return hash;
			}

			public class LogPoint {
				public readonly string path;
				public readonly int line;
				
				static Regex lineParser = new Regex(@"in (?<path>.*\.cs):(?<line>\d+)");
				
				LogPoint(string path, int line) {
					this.path = path;
					this.line = line;
				}

				public static LogPoint Emit() {
					#if UNITY_EDITOR
					var stackTrace = System.Environment.StackTrace;

					bool onLog = false;
					
					foreach (var line in stackTrace.Split('\n')) {
						if (line.Contains("Yurowm.DebugTools.DebugPanel.Log")) {
							onLog = true;
							continue;
						}		
						if (!onLog)
							continue;
						
						var match = lineParser.Match(line);
						
						if (match.Success)
							return new LogPoint(match.Groups["path"].Value, int.Parse(match.Groups["line"].Value));
					}
					#endif
					
					return null;
				}
			}
		}

		#region Active

		static List<object> listeners = new List<object>();

		
		public static bool IsActive => !listeners.IsEmpty();

		public static void AddListener(object listener) {
			if (!listeners.Contains(listener))
				listeners.Add(listener);
		} 
		
		public static void RemoveListener(object listener) {
			listeners.Remove(listener);
		} 
		
		#endregion
		
		static Entry GetEntry(string name, string group) {
			var hash = Entry.GetHashCode(name, group);
			
			if (!entries.TryGetValue(hash, out var entry)) {
				entry = new Entry(name, group);
				
				entries.Add(entry.hash, entry);
				
				if (!groups.TryGetValue(group, out var list)) {
					list = new List<Entry>();
					groups.Add(group, list);
				}
				
				list.Add(entry);
				onNewEntry?.Invoke(entry);
			}
			
			return entry;
		}
		
		public static void RemoveEntry(string name, string group) {
			var hash = Entry.GetHashCode(name, group);
			
			if (entries.TryGetValue(hash, out var entry)) 
				entries.Remove(entry.hash);
			
			if (groups.TryGetValue(group, out var list))
				list.Add(entry);
		}
		
		public static void RemoveGroup(string group) {
			if (groups.TryGetValue(group, out var list)) {
				groups.Remove(group);

				foreach (var entry in list)
					entries.Remove(entry.hash);
			}
		}
		
		public static IEnumerable<Entry> GetEntries() {
			return entries.Values;
		}
		
		public static IEnumerable<Entry> GetEntries(string group) {
			if (group == null)
				throw new NullReferenceException(nameof(group));
			
			if (groups.TryGetValue(group, out var list))
				foreach (var entry in list)
					yield return entry;
		}
		
		public static void Clear() {
			entries.Clear();
			groups.Clear();
		}
		
		#endregion
		
		#region Log

		public static void Log(string text) {
			Log(text.GetHashCode().ToString(), null, text);
		}

		public static void Log(string name, Action obj) {
			Log(name, (object) obj);
		}

		public static void Log(string name, object obj) {
			Log(name, null, obj);
		}
		
		public static void Log(string name, string group, Action obj) {
			Log(name, group, (object) obj);
		}
		
		public static void Log(string name, string group, object obj) {
			if (name.IsNullOrEmpty())
				throw new NullReferenceException(nameof(name));
			
			if (group.IsNullOrEmpty())
				group = "Untitled";
			
			var entry = GetEntry(name, group);
			
			if (entry.message == null || !entry.message.Update(obj)) {
				entry.message = EmitMessage(obj);
				onNewMessage?.Invoke(entry);
			}
			
			onLog.Invoke();
		}
		
		#endregion

		#region Messages

		static IMessage[] baseMessages;

		[Preserve]
		public interface IMessage {
			int CastPriority {get;}
			
			IMessage TryToEmitFor(object value);
			
			bool Update(object obj);
			bool IsExtendable();
		}

		static IMessage EmitMessage(object raw) {
			if (baseMessages == null) {
				baseMessages = typeof(IMessage)
					.FindInheritorTypes(true)
					.Where(t => t.IsInstanceReadyType())
					.Select(Activator.CreateInstance)
					.CastIfPossible<IMessage>()
					.OrderBy(m => m.CastPriority)
					.ToArray();
			}
			
			return baseMessages
				.Select(m => m.TryToEmitFor(raw))
				.FirstOrDefault(m => m != null);
		}
		
		public class NullMessage : IMessage {
			public int CastPriority => 0;
			
			public NullMessage() {}

			public IMessage TryToEmitFor(object value) {
				if (value == null)
					return new NullMessage();
				return null;
			}

			public bool Update(object obj) {
				return obj == null;
			}

			public bool IsExtendable() => false;
		}
		
		public abstract class TypeBasedMessage<T> : IMessage {
			
			public T Value;
			
			public TypeBasedMessage() {}
			
			public TypeBasedMessage(T value) {
				Value = value;
			}
			
			public int CastPriority => 1;

			public abstract IMessage TryToEmitFor(object value);
			
			public bool Update(object obj) {
				if (obj == null)
					return false;
				
				if (obj is T t) {
					Value = t;
					return true;
				}
				
				return false;
			}
			
			public abstract bool IsExtendable();
		}
		
		public class OtherMessage : IMessage {
			
			public string text;
			Type type;
			
			public OtherMessage() {}
			
			public OtherMessage(object obj) {
				Set(obj);
			}
			
			public int CastPriority => int.MaxValue;

			public IMessage TryToEmitFor(object value) {
				return new OtherMessage(value);
			}
			
			void Set(object obj) {
				text = obj.ToString();
				type = obj.GetType();
			}
			
			public bool Update(object obj) {
				if (obj == null)
					return false;
				
				if (obj.GetType() == type) {
					text = obj.ToString();
					return true;
				}
				
				return false;
			}

			public bool IsExtendable() {
				return text.Length > 20;
			}
		}
		
		public class TextMessage : TypeBasedMessage<string> {
			
			public TextMessage() { }
			
			public TextMessage(string text) : base(text) { }
			
			public override IMessage TryToEmitFor(object value) {
				if (value is string text)
					return new TextMessage(text);
				return null;
			}

			public override bool IsExtendable() => Value.Length > 20;
		}

		public class ActionMessage : TypeBasedMessage<Action> {
			
			public ActionMessage() { }
			
			public ActionMessage(Action value) : base(value) { }

			public override IMessage TryToEmitFor(object value) {
				if (value is Action action)
					return new ActionMessage(action);
				return null;
			}
			
			public override bool IsExtendable() => false;
		}
		
		#endregion

		#region Colors
		
		static readonly Color errorColor = new(1f, .5f, .5f);
		static readonly Color systemColor = new(.5f, 1f, .5f);
		static readonly Color warningColor = new(1f, 1f, .3f);
		static readonly Color logColor = new(0.5f, 1f, 1f);
		static readonly Color untitledColor = new(0.8f, 0.8f, 0.8f);
		static readonly Color defaultColor = Color.white;
		
		public static Color GroupToColor(string name) {
			switch (name) {
				case "Error": return errorColor;
				case "Exception": return errorColor;
				case "System": return systemColor;
				case "Warning": return warningColor;
				case "Log": return logColor;
				case "Untitled": return untitledColor;
				default: return defaultColor;
			}
		}

		#endregion
	}
	
	public class DebugPanelParameters: GameParameters.Module {
		public string password;
		
		public override string GetName() => "Debug Panel";

		public override void Serialize(IWriter writer) {
			writer.Write("password", password.Encrypt());
		}

		public override void Deserialize(IReader reader) {
			password = reader.Read<string>("password").Decrypt();
		}
	}
}