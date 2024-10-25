using UnityEditor;
using Yurowm.GUIHelpers;
using Yurowm.ObjectEditors;
using Yurowm.Serialization;

namespace Yurowm.DeveloperTools {
    public class BundleIDOptimization : Optimization {
        
        public string teamID;
        public string projectID;
        
        public string bundleID => $"com.{teamID}.{projectID}";
        
        public override bool DoAnalysis() {
            return bundleID == PlayerSettings.applicationIdentifier;
        }

        public override bool CanBeAutomaticallyFixed() {
            return true;
        }
        
        public override void Fix() {
            PlayerSettings.applicationIdentifier = bundleID;
            
            AssetDatabase.SaveAssets();
        }

        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            writer.Write("teamID", teamID);
            writer.Write("projectID", projectID);
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            reader.Read("teamID", ref teamID);
            reader.Read("projectID", ref projectID);
        }
    }
    
    public class BundleIDOptimizationEditor : ObjectEditor<BundleIDOptimization> {
        
        public override void OnGUI(BundleIDOptimization optimization, object context = null) {
            using (GUIHelper.IndentLevel.Start()) {
                optimization.teamID = EditorGUILayout.TextField("Team ID", optimization.teamID);
                optimization.projectID = EditorGUILayout.TextField("Project ID", optimization.projectID);
            }
            
            EditorGUILayout.LabelField("Bundle ID", optimization.bundleID);
        }
    }
}