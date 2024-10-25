using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;
using UnityEngine.Serialization;
using Yurowm.Extensions;
using Yurowm.Serialization;
using Yurowm.UI;
using Yurowm.Utilities;
using Yurowm.YJSONSerialization;

namespace Yurowm {
    public class GameParameters : IPropertyStorage {
        
        static GameParameters Instance => PropertyStorage.GetInstance<GameParameters>();

        public string FileName => "GameParameters" + Serializer.FileExtension;
        public TextCatalog Catalog => TextCatalog.StreamingAssets;
        
        List<Module> modules = null;
        Type[] moduleTypes = Utils
            .FindInheritorTypes<Module>(true)
            .Where(t => !t.IsAbstract) 
            .ToArray();
        
        public IEnumerable<Module> GetModules() {
            if (modules == null)
                modules = moduleTypes 
                    .Select(Activator.CreateInstance)
                    .Cast<Module>()
                    .ToList();
            
            foreach (var module in modules)
                yield return module;
        }
        
        public static M GetModule<M>() where M : Module {
            return Instance?.GetModules().CastOne<M>();
        }

        [Preserve]
        public abstract class Module : ISerializable {
            public abstract string GetName();
            public abstract void Serialize(IWriter writer);
            public abstract void Deserialize(IReader reader);
        }
        
        public void Serialize(IWriter writer) {
            writer.Write("modules", modules.ToArray());
        }

        public void Deserialize(IReader reader) {
            if (modules == null)
                modules = new List<Module>();
            else 
                modules.Clear();
            modules.AddRange(reader.ReadCollection<Module>("modules"));
            foreach (var moduleType in moduleTypes)
                if (modules.All(m => !moduleType.IsInstanceOfType(m)))
                    modules.Add((Module) Activator.CreateInstance(moduleType));
        }
    }
    
    public class GameParametersGeneral : GameParameters.Module {
        public string supportEmail;
        public string privacyPolicyURL;
        public string termsOfUseURL;
        
        public float maxDeltaTime = 1f / 30;
        public bool userRestoreInEditor = false;
        public LayoutPreset.Layout forceLayout;
        
        public string fakeDeviceID;

        [ReferenceValue("HasSupportEmail")]
        static int HasSupportEmail() => GameParameters.GetModule<GameParametersGeneral>().supportEmail.IsNullOrEmpty() ? 0 : 1;
        
        [ReferenceValue("HasPrivacyPolicy")]
        static int HasPrivacyPolicy() => GameParameters.GetModule<GameParametersGeneral>().privacyPolicyURL.IsNullOrEmpty() ? 0 : 1;
        
        [ReferenceValue("HasTermsOfUse")]
        static int HasTermsOfUse() => GameParameters.GetModule<GameParametersGeneral>().termsOfUseURL.IsNullOrEmpty() ? 0 : 1;

        public override string GetName() {
            return "General";
        }

        public override void Serialize(IWriter writer) {
            writer.Write("supportEmail", supportEmail);
            writer.Write("privacyPolicy", privacyPolicyURL);
            writer.Write("termsOfUseURL", termsOfUseURL);
            
            writer.Write("maxDeltaTime", maxDeltaTime);
            writer.Write("userRestoreInEditor", userRestoreInEditor);
            writer.Write("fakeDeviceID", fakeDeviceID);
            
            writer.Write("forceOrientation", forceLayout);
        }

        public override void Deserialize(IReader reader) {
            reader.Read("supportEmail", ref supportEmail);
            reader.Read("privacyPolicy", ref privacyPolicyURL);
            reader.Read("termsOfUseURL", ref termsOfUseURL);
            
            reader.Read("maxDeltaTime", ref maxDeltaTime);
            reader.Read("userRestoreInEditor", ref userRestoreInEditor);
            reader.Read("fakeDeviceID", ref fakeDeviceID);
            
            reader.Read("forceOrientation", ref forceLayout);
        }
    }
}