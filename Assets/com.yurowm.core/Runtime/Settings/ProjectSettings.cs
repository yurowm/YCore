using UnityEngine.Serialization;
using Yurowm.Extensions;
using Yurowm.Serialization;
using Yurowm.UI;
using Yurowm.YJSONSerialization;

namespace Yurowm.Core {
    public class ProjectSettings : IPropertyStorage {
        
        public static ProjectSettings Instance => PropertyStorage.GetInstance<ProjectSettings>();

        public string FileName => "ProjectSettings" + Serializer.FileExtension;
        public TextCatalog Catalog => TextCatalog.StreamingAssets;
        
        public bool increaseBuildCode;
        public bool autoVersionName;
        public string versionName;
        public string buildTarget;
        
        // public string Version => $"{versionName}.{buildCode}";

        public int buildCode;
        
        [ReferenceValue("Version")]
        static string GetVersionName() => Instance.versionName;

        public void Serialize(IWriter writer) {
            writer.Write("buildCode", buildCode);
            writer.Write("increaseBuildCode", increaseBuildCode);
            writer.Write("autoVersionName", autoVersionName);
            writer.Write("buildTarget", buildTarget);
            writer.Write("versionName", versionName);
        }

        public void Deserialize(IReader reader) {
            reader.Read("buildCode", ref buildCode);
            reader.Read("increaseBuildCode", ref increaseBuildCode);
            reader.Read("autoVersionName", ref autoVersionName);
            reader.Read("buildTarget", ref buildTarget);
            reader.Read("versionName", ref versionName);
        }
    }
}