using System;
using System.Collections;
using System.Linq;
using UnityEngine;
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

        [OnLaunch(INITIALIZE_ORDER)]
        public static IEnumerator InitializeOnLoad() {
            if (OnceAccess.GetAccess("Integration"))
                foreach (var integration in storage.items) {
                    if (!integration.active || integration.HasIssues())
                        continue;
                    yield return integration.Initialize();
                }
        }
        
        public bool active = true;
        
        protected virtual IEnumerator Initialize() {
            yield break;
        }

        public abstract string GetName();

        public static I Get<I>() where I : Integration {
            return storage.Items<I>().FirstOrDefault(i => i.active && !i.HasIssues());
        }
        
        public static T CastOne<T>() {
            return storage
                .Items<Integration>()
                .Where(i => i.active && !i.HasIssues())
                .CastOne<T>();
        }

        public static void Catch<I>(Action<I> action) where I : Integration {
            if (action == null)
                return;
            
            var integration = Get<I>();
            if (integration != null)
                action.Invoke(integration);
        }
        
        #region ISerializable

        public virtual void Serialize(IWriter writer) {
            writer.Write("active", active);
            PlatformExpression.Serialize(writer, this);
        }

        public virtual void Deserialize(IReader reader) {
            reader.Read("active", ref active);
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