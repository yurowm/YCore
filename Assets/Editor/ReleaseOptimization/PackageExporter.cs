using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Yurowm.DeveloperTools {
    public static class PackageExporter {

        static string[] projectContent;
        static string _name;
    
        public static void Export(PassType type) {
            projectContent = AssetDatabase.GetAllAssetPaths()
                .Where(x => PassAsset(x, type)).ToArray();
            
            // string projectSettings_original = null;
            // string projectSettingsFilePath = projectContent.FirstOrDefault(x => x.EndsWith("ProjectSettings.asset"));
            // if (!string.IsNullOrEmpty(projectSettingsFilePath)) {
            //     FileInfo file = new FileInfo(projectSettingsFilePath);
            //     if (file.Exists) {
            //         projectSettings_original = File.ReadAllText(projectSettingsFilePath);
            //
            //         string newSettings = projectSettings_original;
            //         newSettings = new Regex(@"^\s*cloudProjectId:.+$", RegexOptions.Multiline).Replace(newSettings, " cloudProjectId: ");
            //         newSettings = new Regex(@"^\s*projectName:.+$", RegexOptions.Multiline).Replace(newSettings, " projectName: ");
            //         newSettings = new Regex(@"^\s*organizationId:.+$", RegexOptions.Multiline).Replace(newSettings, " organizationId: ");
            //
            //         File.WriteAllText(projectSettingsFilePath, newSettings);
            //     }
            // }

            try {
                if (type == PassType.Backup) {
                    _name = Application.productName + "_backup";
                } else {
                    _name = Application.productName + (type == PassType.Full ? "_Full" : "") + "_" + DateTime.Now.ToString(CultureInfo.InvariantCulture);
                    _name = _name.Replace('/', '-').Replace(' ', '_').Replace(':', '-');
                }

                _name += ".unitypackage";

                EditorUtility.DisplayProgressBar("Exporting", "Exporting the package", 0.3f);

                AssetDatabase.ExportPackage(projectContent, _name);
        
                EditorUtility.ClearProgressBar();

                DirectoryInfo path = new DirectoryInfo(Application.dataPath).Parent;
                FileInfo file = null;

                if (type == PassType.Backup) {
                    DirectoryInfo backupFolder = new DirectoryInfo(Path.Combine(path.FullName, "Backup"));
                    if (!backupFolder.Exists) backupFolder.Create();

                    file = new FileInfo(Path.Combine(backupFolder.FullName, _name));
                    if (file.Exists) file.Delete();

                    File.Move(Path.Combine(path.FullName, _name), file.FullName);

                    Debug.Log("Backed up success!");
                } else
                    file = new FileInfo(Path.Combine(path.FullName, _name));

                EditorUtility.RevealInFinder(file.FullName);
            } catch (Exception e) {
                Debug.LogException(e);
            }

            // if (!string.IsNullOrEmpty(projectSettingsFilePath) && !string.IsNullOrEmpty(projectSettings_original))
            //     File.WriteAllText(projectSettingsFilePath, projectSettings_original);
        }

        public enum PassType {Customer, Backup, Full}

        static bool PassAsset(string path, PassType type) {
            if (type == PassType.Customer) {
                if (path.StartsWith("Assets/AssetStoreTools/")) return false;
                if (path.StartsWith("Assets/DeveloperTools/")) return false;
            }
            if (path.StartsWith("Assets/")) return true;
            if (path.StartsWith("ProjectSettings/")) return true;
            return false;
        }
    }
}
