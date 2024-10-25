﻿using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
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
        static IEnumerator LoadTextCommand(string path) {
            string result = null;
            yield return LoadTextRoutine(path, r => result = r);
            
            if (result.IsNullOrEmpty())
                yield return YConsole.Error("The file is empty or doesn't exist");
            else
                yield return YConsole.Alias(result);
        }
        
        static string GetFullPath(string path, TextCatalog catalog) {
            if (!HasReadAccess(catalog))
                throw new Exception("No read access for the catalog: " + catalog);

            var fullPath = GetPath(catalog);
            
            if (fullPath.IsNullOrEmpty())
                return null;
            
            return Path.Combine(fullPath, path);
        }
        
        static IEnumerator LoadTextAsyncInternal(string path, Action<string> getResult) {
            using var request = UnityWebRequest.Get(path);
            
            yield return request.SendWebRequest();

            while (request.result == UnityWebRequest.Result.InProgress)
                yield return null;

            switch (request.result) {
                case UnityWebRequest.Result.Success:
                    getResult?.Invoke(request.downloadHandler.text); 
                    yield break;
                default:
                    Debug.LogError($"Text is not loaded: {request.result}: {request.error}\n{path}");
                    break;
            }
            
            getResult?.Invoke(null); 
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
        
        public static IEnumerator LoadTextRoutine(string path, Action<string> getResult, TextCatalog catalog = TextCatalog.StreamingAssets) {
            path = GetFullPath(path, catalog);

            if (!Application.isEditor && path.Contains("://")) {
                yield return LoadTextAsyncInternal(path, getResult);
            } else {
                var result = LoadTextFromFile(path);
                getResult?.Invoke(result);
            }
        }
    }
    
    public enum TextCatalog {
        StreamingAssets,
        EditorDefaultResources,
        Persistent,
        ProjectFolder
    }
}
