using System;
using UnityEngine;
using UnityEngine.Rendering;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.Shapes {
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public abstract class Shape2DBehaviour : MonoBehaviour, IMaterialProvider {
        [SerializeField] Material m_Material = null;
        Material instance_Material;
        
        public SortingLayerAndOrder sorting;
        
        public bool hiddenComponents = true;
        
        protected MeshBuilder builder;
        
        public Material material {
            get {
                if (instance_Material) return instance_Material;
                if (m_Material) return m_Material;
                return defaultMaterial;
            }
            set {
                if (m_Material == value) return;
                m_Material = value;
                SetDirty();
            }
        }
        
        Material IMaterialProvider.material => InstanceMaterial;

        public Material InstanceMaterial {
            get {
                if (!instance_Material) {
                    if (m_Material)
                        instance_Material = m_Material;
                    else
                        instance_Material = defaultMaterial;
                    
                    instance_Material = Instantiate(instance_Material);
                    renderer.material = instance_Material;
                }

                return instance_Material;
            }
        }

        static Material _defaultMaterial;
        static Material defaultMaterial {
            get {
                if (!_defaultMaterial)
                    _defaultMaterial = new Material(Shader.Find("Sprites/Default"));
                return _defaultMaterial;
            }
        }
        
        MeshRenderer m_MeshRenderer;
        protected MeshRenderer renderer {
            get {
                #if UNITY_EDITOR
                if (gameObject.scene.name == null)
                    return null;
                #endif
                
                if (!m_MeshRenderer) {
                    if (!TryGetComponent(out m_MeshRenderer)) 
                        m_MeshRenderer = gameObject.AddComponent<MeshRenderer>();
                    
                    m_MeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                    m_MeshRenderer.lightProbeUsage = LightProbeUsage.Off;
                    m_MeshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    m_MeshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.Object | MotionVectorGenerationMode.Camera;
                    m_MeshRenderer.receiveShadows = false;
                    m_MeshRenderer.allowOcclusionWhenDynamic = false;
                    m_MeshRenderer.sharedMaterial = m_Material;
                }
                
                m_MeshRenderer.hideFlags = hiddenComponents ?
                    HideFlags.HideAndDontSave | HideFlags.HideInInspector :
                    HideFlags.None;
                
                return m_MeshRenderer;
            }
        }
        
        MeshFilter m_MeshFilter;
        protected MeshFilter filter {
            get {
                #if UNITY_EDITOR
                if (gameObject.scene.name == null)
                    return null;
                #endif
                
                if (!m_MeshFilter)
                    if (!TryGetComponent(out m_MeshFilter))
                        m_MeshFilter = gameObject.AddComponent<MeshFilter>();

                m_MeshFilter.hideFlags = hiddenComponents ?
                    HideFlags.HideAndDontSave | HideFlags.HideInInspector :
                    HideFlags.None;
                
                return m_MeshFilter;
            }
        }
        
        protected virtual void OnEnable() {
            renderer.enabled = true;
            RebuildImmediate();
        }

        protected virtual void OnDestroy() {
            #if UNITY_EDITOR
            if (Application.isPlaying)
                DestroyInPlaymode();
            else
                DestroyInEditor();
            #else
            DestroyInPlaymode();
            #endif
        }
        
        void DestroyInEditor() {
            if (renderer != null)
                DestroyImmediate(renderer);
            if (filter != null)
                DestroyImmediate(filter);
        }
            
        void DestroyInPlaymode() {
            Destroy(renderer);
            Destroy(filter);
        }

        protected virtual void OnDisable() {
            renderer.enabled = false;
        }

        protected virtual void Update() {
            if (!isDirty) return;
            RebuildImmediate();
        }
        
        public virtual void OnValidate() {
            if (!renderer) return;
            
            renderer.enabled = enabled;

            if (!enabled) return;
            
            if (this is IOnAnimateHandler)
                AnimateProperty.Update(this);

            RebuildImmediate();
        }
        
        bool isDirty = true;
        public void SetDirty() {
            isDirty = true;
            OnSetDirty();
        }

        protected virtual void OnSetDirty() {}
        protected virtual bool IsCached() => false;

        public void RebuildImmediate() {
            if (enabled && renderer) {
                if (builder == null) {
                    builder = new MeshBuilder();
                    builder.CreateMesh();
                }
                
                renderer.sharedMaterial = material;
                renderer.sortingLayerID = sorting.layerID;
                renderer.sortingOrder = sorting.order;
                
                if (!IsCached()) {
                    builder.Clear();
                    
                    using (ExtensionsUnityEngine.ProfileSample("Build Shape2D"))
                        FillMesh(builder);
                    
                    filter.mesh = builder.Build();
                }
            }

            isDirty = false;
        }
        
        public virtual void Clear() {
            builder?.Clear();
            filter.mesh = builder?.Build();
            isDirty = false;
        }
        
        public abstract void FillMesh(MeshBuilder builder);
        
        void OnDidApplyAnimationProperties() {
            SetDirty();
            Update();    
        }
    }
    
    public interface IMaterialProvider {
        Material material { get; }
    }
}