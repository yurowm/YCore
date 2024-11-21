using System;
using System.Collections;
using System.Collections.Generic;
using Yurowm.Coroutines;
using Yurowm.Extensions;
using Yurowm.Localizations;
using Yurowm.Nodes;
using Yurowm.Serialization;
using Yurowm.Utilities;

namespace Yurowm.Core {
    public class UserPath : NodeSystem, ISerializableID, ILocalized {
        
        [PreloadStorage]
        public static Storage<UserPath> storage = new Storage<UserPath>("UserPath", TextCatalog.StreamingAssets);
        
        [OnLaunch(1)]
        static void Initialize() {
            storage.items.ForEach(p => p.Launch());            
        }
        
        public override IEnumerable<Type> GetSupportedNodeTypes() {
            yield return typeof(BasicNode);
            yield return typeof(UserPathState);
            yield return typeof(IUserPathNode);
        }
        
        public string ID { get; set; }
            
        public void Launch() {
            nodes
                .CastIfPossible<UserPathState>()
                .ForEach(s => s.Initialize());
        }

        public override void Serialize(IWriter writer) {
            writer.Write("ID", ID);
            base.Serialize(writer);
        }

        public void Deserialize(Reader reader) {
            ID = reader.Read<string>("ID");
            base.Deserialize(reader);
        }
        
        [LocalizationKeysProvider]
        static IEnumerable GetKeys() {
            return storage.items.CastIfPossible<ILocalized>();
        }
        
        public IEnumerable GetLocalizationKeys() {
            return nodes.CastIfPossible<ILocalized>();
        }
        
        public class AppEvent : UserPathSource {
            [Flags]
            public enum Event {
                FirstLaunch = 1 << 0,
                Launch = 1 << 1,
                Focus = 1 << 2,
                Unfocus = 1 << 3
            }
            
            public Event events = Event.Launch;

            public override void Initialize() {
                base.Initialize();
                if (events.HasFlag(Event.FirstLaunch)) App.onFirstLaunch += Start;
                if (events.HasFlag(Event.Launch)) App.onLaunch += Start;
                if (events.HasFlag(Event.Focus)) App.onFocus += Start;
                if (events.HasFlag(Event.Unfocus)) App.onUnfocus += Start;
            }

            public override IEnumerator Logic() {
                Push(outputPort);
                yield break;
            }

            public override void Serialize(IWriter writer) {
                base.Serialize(writer);
                writer.Write("events", events);
            }

            public override void Deserialize(IReader reader) {
                base.Deserialize(reader);
                reader.Read("events", ref events);
            }
        }
    }
    
    public interface IUserPathNode {}
    
    public class UserPathData : GameData.Module, IServerDataModule {
        Dictionary<string, int> states = new();
        
        public int GetState(string ID) {
            if (states.TryGetValue(ID, out var value))
                return value;
            return -1;
        }
        
        public void SetState(string ID, int value) {
            value = value.ClampMin(-1);
            var currentValue = GetState(ID);
            if (currentValue != value) {
                if (value < 0)
                    states.Remove(ID);
                else
                    states[ID] = value;
                SetDirty();
            }
        }
        
        public override void Serialize(IWriter writer) {
            writer.Write("states", states);
        }

        public override void Deserialize(IReader reader) {
            states.Reuse(reader.ReadDictionary<int>("states"));
        }
    }
    
    public abstract class UserPathState : Node {

        protected UserPath path => system as UserPath;
        
        public virtual void Initialize() {}
        
        public void Start() { 
            _Logic().Run();
        }
        
        protected virtual void OnStart() {}
        protected virtual void OnEnd() {}
        
        IEnumerator _Logic() {
            OnStart();
            yield return Logic();
            OnEnd();
        }

        public abstract IEnumerator Logic();
    }
    
    public abstract class UserPathSource : UserPathState {
        
        public readonly Port outputPort = new Port(1, "Output", Port.Info.Output, Side.Bottom);
        
        public override IEnumerable GetPorts() {
            yield return outputPort;
        }
    }
    
    public abstract class UserPathValueProvider : UserPathSource {
        public override IEnumerator Logic() {
            yield break;
        }
        
        public override IEnumerable<object> OnPortPulled(Port sourcePort, Port targetPort) {
            if (targetPort == outputPort)
                if (TryGetValue(out var result))
                    if (result is object[] array) {
                        foreach (var element in array) {
                            yield return element;
                        }
                    } else
                        yield return result;
        }

        protected abstract bool TryGetValue(out object value);
    }
    
    public abstract class UserPathFilter : UserPathState {
        
        public readonly Port inputPort = new Port(0, "Input", Port.Info.Input, Side.Top);
        public readonly Port outputPort = new Port(1, "Output", Port.Info.Output, Side.Bottom);
        
        public override IEnumerable GetPorts() {
            yield return inputPort;
            yield return outputPort;
        }

        protected override void OnEnd() {
            base.OnEnd();
            Push(outputPort);
        }

        public override void OnPortPushed(Port sourcePort, Port targetPort, object[] args) {
            base.OnPortPushed(sourcePort, targetPort, args);
            if (targetPort == inputPort)
                Start();
        }
    }
}