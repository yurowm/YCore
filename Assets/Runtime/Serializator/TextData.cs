using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Yurowm.Console;
using Yurowm.Coroutines;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.Serialization {
    public static class TextData {
        static bool HasReadAccess(TextCatalog catalog) {
            #if UNITY_EDITOR
            return true;
            #else
            return catalog != TextCatalog.EditorDefaultResources && catalog != TextCatalog.ProjectFolder;
            #endif
        }
        
        static bool HasWriteAccess(TextCatalog catalog) {
            #if UNITY_EDITOR
            return true;
            // #if UNITY_WEBGL
            // return false;
            // #endif
            #else
            return catalog == TextCatalog.Persistent;
            #endif
        }
        
        public static string GetPath(TextCatalog catalog) {
            switch (catalog) {
                case TextCatalog.StreamingAssets: return Application.streamingAssetsPath;
                case TextCatalog.Persistent: return Application.persistentDataPath;
                case TextCatalog.ProjectFolder: return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Storage");
                case TextCatalog.EditorDefaultResources: return Path.Combine(Application.dataPath, "Editor Default Resources");
            }
            
            Debug.LogError("Unknown type of catalog");
            return null;
        }
        
        public static void SaveText(string path, string text, TextCatalog catalog = TextCatalog.StreamingAssets) {
            if (!HasWriteAccess(catalog))
                throw new Exception("No write access for the catalog: " + catalog);
            
            var fullPath = GetPath(catalog);
            
            if (fullPath.IsNullOrEmpty())
                return;
            
            fullPath = Path.Combine(fullPath, path);
            
            var file = new FileInfo(fullPath);
            
            if (!file.Directory.Exists)
                file.Directory.Create();
            
            File.WriteAllText(file.FullName, text);
        }
        
        public static void RemoveText(string path, TextCatalog catalog = TextCatalog.StreamingAssets) {
            if (!HasWriteAccess(catalog))
                throw new Exception("No write access for the catalog: " + catalog);
            
            
            var fullPath = GetPath(catalog);
            
            if (fullPath.IsNullOrEmpty())
                return;
            
            fullPath = Path.Combine(fullPath, path);

            var file = new FileInfo(fullPath);

            if (file.Exists) 
                File.Delete(file.FullName);
        }
        
        [QuickCommand("loadtext", "Data/Pages.json", "Load StreamingAssets/Data/Pages.ys file and show text")]
        static async UniTask LoadTextCommand(string path) {
            var result = await LoadTextTask(path);
            
            if (result.IsNullOrEmpty())
                YConsole.Error("The file is empty or doesn't exist");
            else
                YConsole.Alias(result);
        }
        
        static string GetFullPath(string path, TextCatalog catalog) {
            if (!HasReadAccess(catalog))
                throw new Exception("No read access for the catalog: " + catalog);

            var fullPath = GetPath(catalog);
            
            if (fullPath.IsNullOrEmpty())
                return null;
            
            return Path.Combine(fullPath, path);
        }
        
        static async UniTask<string> LoadTextAsyncInternal(string path) {
            using var request = UnityWebRequest.Get(path);
            
            await request.SendWebRequest();

            while (request.result == UnityWebRequest.Result.InProgress)
                await UniTask.Yield();
            
            switch (request.result) {
                case UnityWebRequest.Result.Success:
                    return request.downloadHandler.text; 
                default:
                    Debug.LogError($"Text is not loaded: {request.result}: {request.error}\n{path}");
                    break;
            }
            
            return null;
        }
        
        static string LoadTextFromFile(string path) {
            if (!File.Exists(path))
                return null;
            
            return File.ReadAllText(path);
        }
        
        public static string LoadTextInEditor(string path, TextCatalog catalog = TextCatalog.StreamingAssets) {
            #if UNITY_EDITOR
            return LoadTextFromFile(GetFullPath(path, catalog));
            #else
            return null;
            #endif
        }
        
        public static async UniTask<string> LoadTextTask(string path, TextCatalog catalog = TextCatalog.StreamingAssets) {
            path = GetFullPath(path, catalog);

            if (!Application.isEditor && path.Contains("://"))
                return await LoadTextAsyncInternal(path);
            else
                return LoadTextFromFile(path);
        }
    }
    
    public enum TextCatalog {
        StreamingAssets,
        EditorDefaultResources,
        Persistent,
        ProjectFolder
    }
}
