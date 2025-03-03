﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Yurowm.Coroutines;
using Yurowm.Extensions;
using Yurowm.Utilities;
using Yurowm.YJSONSerialization;

namespace Yurowm.Serialization {

    public abstract class Storage : ISerializable {
        [OnLaunch(int.MinValue)]
        static async UniTask OnLaunch() {
            if (!OnceAccess.GetAccess("Storage"))
                return;
            
            foreach (var field in Utils
                         .GetAllFieldsWithAttribute<PreloadStorageAttribute>(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                         .OrderBy(t => t.Item2.order)
                         .Select(t => t.Item1)
                         .Cast<FieldInfo>())
                if (field.GetValue(null) is Storage storage)
                    await storage.Load();
        }
        
        bool isLoaded = false;
        public bool IsLoaded => isLoaded;

        protected string fileName;
        public string Name => fileName;
        
        TextCatalog catalog;
        ISerializer serializer;

        public Storage() { }
        
        public Storage(string fileName, TextCatalog catalog, ISerializer serializer = null) {
            this.fileName = fileName + Serializer.FileExtension;
            this.catalog = catalog;
            this.serializer = serializer ?? Serializer.Instance;
        }

        public async UniTask Apply() {
            if (!IsLoaded) 
                await Load();
            var raw = serializer.Serialize(this);
            if (catalog == TextCatalog.StreamingAssets && !Application.isEditor)
                raw = raw.Encrypt();
            TextData.SaveText(Path.Combine("Data", fileName), raw, catalog);
        }
        
        public async UniTask<string> GetSource() {
            var source = await TextData.LoadTextTask(Path.Combine("Data", fileName), catalog);
            
            if (!source.IsNullOrEmpty() && catalog == TextCatalog.StreamingAssets && !Application.isEditor)
                source = source.Decrypt();
            
            return source;
        }
        
        public ISerializer ser {
            set => serializer = value;
            get => serializer;
        }

        public virtual async UniTask Load() {
            isLoaded = true;
            var source = await GetSource();

            if (source.IsNullOrEmpty()) 
                return;
            
            try {
                serializer.Deserialize(this, source);
            } catch (Exception e) {
                Debug.LogException(e);
                isLoaded = false;
            }
        }
        
        public abstract void Serialize(IWriter writer);

        public abstract void Deserialize(IReader reader);
    }
    
    public class Storage<S> : Storage, IEnumerable<S> where S : ISerializable {

        List<S> _items = new();
        
        public List<S> items {
            get {
                if (!IsLoaded) 
                    Load().Complete();
                return _items;
            }
        }
        
        Action<Storage<S>> onLoad;
        
        public Func<S, int> sorter;
        
        public Storage(string fileName, TextCatalog catalog, ISerializer serializer = null) :
            base(fileName, catalog, serializer) {
            Initialize();
        }
        
        public Storage() : base() {
            Initialize();
        }
        
        bool hasIDs;

        protected void Initialize() {
            hasIDs = typeof(ISerializableID).IsAssignableFrom(typeof(S));
        }
        
        public override async UniTask Load() {
            await base.Load();
            
            if (!Application.isEditor) {
                items.RemoveAll(i => !i.CheckAvailability());
                items.RemoveAll(i => i is IPlatformExpression ipe && !ipe.Evaluate());
            }
                
            if (filter != null)
                items.RemoveAll(i => !filter(i));
                
            if (sorter != null)
                _items = items.OrderBy(sorter).ToList();
            else if (typeof(IComparable).IsAssignableFrom(typeof(S)) || typeof(IComparable<S>).IsAssignableFrom(typeof(S)))
                items.Sort();
            
            onLoad?.Invoke(this);
        }
        
        public Storage<S> OnLoad(Action<Storage<S>> onLoad) {
            if (onLoad == null)
                return this;
            
            if (IsLoaded)
                onLoad.Invoke(this);
            else
                this.onLoad += onLoad;
            
            return this;
        } 
        
        Func<S, bool> filter;
        
        public void SetLoadFilter(Func<S, bool> filter) {
            this.filter = filter;
            if (IsLoaded && filter != null)
                items.RemoveAll(i => !filter(i));
        }

        public static Storage<S> Load(string fileName, TextCatalog catalog) {
            return new Storage<S>(fileName, catalog);
        }

        public IEnumerable<S> Items() {
            if (!IsLoaded) {
                Debug.LogException(new Exception("Storage is not loaded"));
                Load().Complete();
            }
            return _items;
        }  
        
        public IEnumerable<T> Items<T>() where T : S => Items().CastIfPossible<T>();

        public T GetDefault<T>() where T : S, IStorageElementExtraData {
            return Items<T>()
                .FirstOrDefaultFiltered(
                    t => t.storageElementFlags.HasFlag(StorageElementFlags.DefaultElement),
                    t => true);
        } 

        public IEnumerable<T> GetAllDefault<T>() where T : S, IStorageElementExtraData {
            return Items<T>()
                .Where(t => t.storageElementFlags.HasFlag(StorageElementFlags.DefaultElement));
        }

        public T GetItem<T>(Func<T, bool> filter = null) where T : S {
            if (!IsLoaded) 
                Load().Complete();
            if (filter == null)
                return _items.CastOne<T>();
            return _items.CastIfPossible<T>().FirstOrDefault(filter);
        }
        
        public T GetItemByID<T>(string ID) where T : S {
            if (hasIDs)
                return (T) items
                    .CastIfPossible<T>()
                    .Cast<ISerializableID>()
                    .FirstOrDefault(isid => isid.ID == ID);
         
            return default;
        }
        
        public S GetItemByID(string ID) {
            return GetItemByID<S>(ID);
        }
        
        #region ISerializable
        public override void Deserialize(IReader reader) {
            _items = reader.ReadCollection<S>("items")
                .Where(i => i != null)
                .ToList();
        }

        public override void Serialize(IWriter writer) {
            writer.Write("items", _items);
        }
        #endregion

        #region IEnumerable<S>
        public IEnumerator<S> GetEnumerator() {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
        #endregion
    }
    
    public interface ISearchable {
        IEnumerable<string> GetSearchData();
    }
    
    public class RemoteStorage<S>: ISerializable where S : ISerializable {
        public string analyticIdentifier;
        
        public List<S> items = new();
        
        #region ISerializable
        
        public void Deserialize(IReader reader) {
            reader.Read("analyticIdentifier", ref analyticIdentifier);
            items.Reuse(reader.ReadCollection<S>("items")
                .Where(i => i != null));
        }

        public void Serialize(IWriter writer) {
            writer.Write("analyticIdentifier", analyticIdentifier);
            writer.Write("items", items);
        }
        
        #endregion
    }
    
    public class PreloadStorageAttribute : Attribute {
        public readonly int order;
        public PreloadStorageAttribute(int order = 0) {
            this.order = order;
        }
    }
    
    [Flags]
    public enum StorageElementFlags {
        DefaultElement = 1 << 1,
        WorkInProgress = 1 << 2,
    }
    
    public interface IStorageElementExtraData {
        StorageElementFlags storageElementFlags {get; set;}
    }
    
    public interface IStorageElementPath {
        string sePath {get; set;}
    }
    
    public interface ISerializableID : ISerializable {
        string ID {get; set;}
    }
}