using System;
using UnityEngine;
using UnityEngine.Rendering;
using Yurowm.Colors;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.Shapes {
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class Shape3D : MonoBehaviour, IMaterialProvider, IRepaintTarget, IMeshComponent {
        [SerializeField] Material m_Material = null;
        Material instance_Material;
        
        [SerializeField]
        Color m_Color = Color.white;
        
        public Color Color {
            get => m_Color;
            set {
                if (m_Color == value) return;
                m_Color = value;
                SetDirty();
            }
        }
        
        [SerializeField]
        float m_Scale = 1f;
        
        public float Scale {
            get => m_Scale;
            set {
                if (m_Scale == value) return;
                m_Scale = value;
                SetDirty();
            }
        }
        
        [SerializeField]
        Mesh m_Mesh;
        
        public Mesh mesh {
            get => m_Mesh;
            set {
                if (m_Mesh == value) return;
                m_Mesh = value;
                SetDirty();
            }
        }
        
        public SortingLayerAndOrder sorting;
        
        Mesh meshLocal;
        
        public Options options;
        
        [Flags]
        public enum Options {
            SolidColor = 1 << 0,
            NoAlpha = 1 << 1,
            MaterialInstancing = 1 << 2
        }
        
        public Material material {
            get {
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                    return m_Material ? m_Material : defaultMaterial;
                #endif
                
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
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                    return material;
                #endif
                
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
                
                if (m_MeshRenderer) {
                    m_MeshRenderer.hideFlags = HideFlags.HideAndDontSave;
                    return m_MeshRenderer;
                }
                
                if (!TryGetComponent(out m_MeshRenderer)) 
                    m_MeshRenderer = gameObject.AddComponent<MeshRenderer>();
                
                m_MeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                m_MeshRenderer.lightProbeUsage = LightProbeUsage.Off;
                m_MeshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                m_MeshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.Object | MotionVectorGenerationMode.Camera;
                m_MeshRenderer.receiveShadows = false;
                m_MeshRenderer.allowOcclusionWhenDynamic = false;
                m_MeshRenderer.material = m_Material;
                
                return renderer;
            }
        }        
        
        MeshFilter m_MeshFilter;
        protected MeshFilter filter {
            get {
                #if UNITY_EDITOR
                if (gameObject.scene.name == null)
                    return null;
                #endif
                
                if (m_MeshFilter) {
                    m_MeshFilter.hideFlags = HideFlags.HideAndDontSave;   
                    return m_MeshFilter;
                }
                
                if (!TryGetComponent(out m_MeshFilter))
                    m_MeshFilter = gameObject.AddComponent<MeshFilter>();
                
                return filter;
            }
        }
        
        protected virtual void OnEnable() {
            renderer.enabled = true;
            RebuildImmediate();
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

        public void RebuildImmediate() {
            if (enabled && renderer && m_Mesh) {
                if (meshLocal == null) 
                    meshLocal = new();
                
                if (options.HasFlag(Options.MaterialInstancing))
                    renderer.material = InstanceMaterial;

                renderer.material = material;
                renderer.sortingLayerID = sorting.layerID;
                renderer.sortingOrder = sorting.order;
                
                meshLocal.Clear();
                
                var solidColor = options.HasFlag(Options.SolidColor);
                var noAlpha = options.HasFlag(Options.NoAlpha);
                
                var color = m_Color;
                
                var colors = m_Mesh.colors;
                var vertices = m_Mesh.vertices;
                
                if (colors.Length != vertices.Length) {
                    colors = new Color[vertices.Length];
                    solidColor = true;
                }
                
                for (var i = 0; i < vertices.Length; i++) {
                    vertices[i] *= m_Scale;
                    var c = colors[i];
                    
                    if (solidColor)
                        c = color;
                    else
                        c *= color;
                    
                    if (noAlpha)
                        c.a = 1;
                    
                    colors[i] = c;
                }

                meshLocal.vertices = vertices;
                meshLocal.colors = colors;
                
                meshLocal.triangles = m_Mesh.triangles;
                meshLocal.normals = m_Mesh.normals;
                meshLocal.tangents = m_Mesh.tangents;
                meshLocal.uv = m_Mesh.uv;
                
                filter.mesh = meshLocal;
            }

            isDirty = false;
        }
        
        void OnDidApplyAnimationProperties() {
            SetDirty();
            Update();    
        }

        public void SetMesh(Mesh mesh) {
            m_Mesh = mesh;
            SetDirty();
        }
    }
    
    public interface IMeshComponent {
        public void SetMesh(Mesh mesh);
    }
}