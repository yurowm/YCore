using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Yurowm.Coroutines;
using Yurowm.Extensions;
using Yurowm.Localizations;
using Yurowm.Serialization;
using Yurowm.Utilities;

namespace Yurowm.Integrations {
    public abstract class Integration : ISerializable, IPlatformExpression {
        
        [PreloadStorage(-1)]
        public static Storage<Integration> storage = new("Integrations", TextCatalog.StreamingAssets);
        
        public const int INITIALIZE_ORDER = -900;
        
        public string platformExpression { get; set; }
        
        public string name;
        
        static bool isInitialized = false;
        static Action<Integration> onInitialize = delegate {};

        [OnLaunch(INITIALIZE_ORDER)]
        public static IEnumerator InitializeOnLoad() {
            if (!OnceAccess.GetAccess("Integration")) yield break;
            
            yield return storage.items
                .Where(i => i.active && i.AvailabilityFilter())
                .ToArray()
                .Select(i => i.Initialize())
                .Parallel();
            
            isInitialized = true;
            
            storage.items
                .Where(i => i.active && i.AvailabilityFilter())
                .ForEach(i => onInitialize(i));
        }
        
        public bool active = true;
        
        protected virtual IEnumerator Initialize() {
            yield break;
        }

        public abstract string GetName();

        public static I Get<I>() where I : Integration {
            return storage.Items<I>().FirstOrDefault(i => i.active && i.AvailabilityFilter());
        }
        
        public static T CastOne<T>() {
            return storage
                .Items<Integration>()
                .Where(i => i.active && !i.HasIssues())
                .CastOne<T>();
        }
        
        public static void Catch<I>(Func<I, bool> action) where I : Integration {
            if (action == null)
                return;
            
            if (isInitialized) {
                foreach (var integration in storage.items.CastIfPossible<I>())
                    if (action(integration))
                        return;
            } else {
                void OnInitialize(Integration integration) {
                    if (integration is I i && action(i)) 
                        onInitialize -= OnInitialize;
                }
                onInitialize += OnInitialize;
            }
        }
        
        #region ISerializable

        public virtual void Serialize(IWriter writer) {
            writer.Write("active", active);
            writer.Write("name", name);
            PlatformExpression.Serialize(writer, this);
        }

        public virtual void Deserialize(IReader reader) {
            reader.Read("active", ref active);
            reader.Read("name", ref name);
            PlatformExpression.Deserialize(reader, this);
        }
        
        #endregion
        
        public bool AvailabilityFilter() {
            return !HasIssues() && PlatformExpression.Evaluate(platformExpression);
        }
        
        [Flags]
        public enum Issue {
            None = 0,
            SDK = 1 << 0,
            Platform = 1 << 1
        }
        
        public bool HasIssues() => GetIssues() != Issue.None;
        
        public virtual Issue GetIssues() => Issue.None;

        public virtual string GetIntegrationType() => string.Empty;
    }
}