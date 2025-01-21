using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Text;
using UnityEditor;
using Yurowm.Extensions;
using Yurowm.ObjectEditors;
using Yurowm.Serialization;
using Yurowm.Utilities;

namespace Yurowm.DeveloperTools {
    public class CleanAssetsFolder : Optimization {
        
        public List<string> foldersToPack = new List<string>();
        public string packPath = "_Yurowm/UnpackToAssetsFolder";

        DirectoryInfo rootFolder;
        
        public override void OnInitialize() {
            rootFolder = new DirectoryInfo(Path.Combine(Application.dataPath));
            var projectFolder = new DirectoryInfo(Path.Combine(Application.dataPath, packPath));
        }

        public override bool DoAnalysis() {
            if (createUnpacker && !File.Exists(GetUnpackerPath())) 
                return false;

            return rootFolder.GetDirectories()
                .All(d => !foldersToPack.Contains(d.Name));
        }

        #region Unpacker

        public bool createUnpacker = true;
        
        const string unpackerFileName = "Unpacker.cs";
        
        #region Unpacker Code

        const string unpackerCode = @"#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class Unpacker#TYPENUMBER {
  
  const string directoryToUnpackPath = ""#PATH"";
  const string thisScriptName = ""#UNPACKER"";
  
  const string metaEx = "".meta"";
  
  const string popupMessage = ""Here is a few important folders that must be located in the Assets folder. Do you want to relocate them automaticaly?"";
  
  [InitializeOnLoadMethod]
  static void OnLoad() {
      switch (EditorUtility.DisplayDialogComplex(""Unpacker"", popupMessage, ""Relocate"", ""Cancel"", ""Don't ask again"")) {
          case 0: Unpack(); break;
          case 1: break;
          case 2: RemoveUnpacker(); break;
      }
  }
  
  static void Unpack() {
        var assetsDirectory = new DirectoryInfo(Application.dataPath);
        var directoryToUnpack = new DirectoryInfo(Path.Combine(assetsDirectory.FullName, directoryToUnpackPath));

        foreach (var directory in directoryToUnpack.GetDirectories()) {
            var newPath = Path.Combine(assetsDirectory.FullName, directory.Name);

            if (Directory.Exists(newPath)) {
                Directory.Delete(newPath, true);
                File.Delete(newPath + metaEx);
            }

            Directory.Move(directory.FullName, newPath);
            File.Move(directory.FullName + metaEx, newPath + metaEx);
        }

        RemoveUnpacker();

        directoryToUnpack.Delete(true);
        File.Delete(directoryToUnpack.FullName + metaEx);

        AssetDatabase.Refresh();
  }
  
  static void RemoveUnpacker() {
      var refresh = false;
      
      var file = new FileInfo(Path.Combine(Application.dataPath, directoryToUnpackPath, thisScriptName));
      if (file.Exists) {
          refresh = true;
          file.Delete();
      }
      
      file = new FileInfo(file.FullName + metaEx);
      if (file.Exists) file.Delete();
      
      if (refresh)
          AssetDatabase.Refresh();
  }
}

#endif 
";
        #endregion
        
        string GetUnpackerPath() {
            return Path.Combine(Application.dataPath, packPath, unpackerFileName);
        }
        
        #endregion
        
        public override bool CanBeAutomaticallyFixed() {
            return true;
        }

        public override void Fix() {
            var projectFolder = new DirectoryInfo(Path.Combine(Application.dataPath, packPath));

            var errors = new StringBuilder();
            
            projectFolder.Refresh();
            
            if (!projectFolder.Exists)
                projectFolder.Create();
            
            foreach (var directory in rootFolder.GetDirectories()) {
                if (!foldersToPack.Contains(directory.Name)) continue;
                
                var newPath = Path.Combine(projectFolder.FullName, directory.Name);
                
                if (Directory.Exists(newPath)) {
                    errors.AppendLine($"\"{newPath}\" already exists");
                    continue;
                }
                
                Directory.Move(directory.FullName, newPath);
                File.Move(directory.FullName + ".meta", newPath + ".meta");
            }

            if (errors.Length > 0)
                throw new Exception(errors.ToString());
            
            File.WriteAllText(GetUnpackerPath(), unpackerCode
                .Replace("#PATH", packPath)
                .Replace("#UNPACKER", unpackerFileName)
                .Replace("#TYPENUMBER", YRandom.main.Range(1, int.MaxValue).ToString()));
            
            AssetDatabase.Refresh();
        }

        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            writer.Write("packPath", packPath);
            writer.Write("createUnpacker", createUnpacker);
            writer.Write("foldersToPack", foldersToPack.ToArray());
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            reader.Read("packPath", ref packPath);
            reader.Read("createUnpacker", ref createUnpacker);
            foldersToPack.Reuse(reader.ReadCollection<string>("foldersToPack"));
        }
    }
    
    public class CleanAssetsFolderEditor : ObjectEditor<CleanAssetsFolder> {
        public override void OnGUI(CleanAssetsFolder optimization, object context = null) {
            optimization.createUnpacker = EditorGUILayout.Toggle("Create Unpacker", optimization.createUnpacker);
            optimization.packPath = EditorGUILayout.TextField("Pack Path", optimization.packPath);
            EditStringList("Folders To Pack", optimization.foldersToPack);
        }
    }

}