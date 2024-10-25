using System.Linq;
using Yurowm.ContentManager;
using Yurowm.Extensions;

namespace Yurowm.Spaces {
    public class SpaceObject : ContextedBehaviour {

        public SpacePhysicalItemBase item;
    
        public virtual void Destroying() {
            Kill();
        }
        
        public virtual void Prepare() {}
        
        public SpaceObject Clone() {
            var go = Instantiate(gameObject);
            
            go.name = gameObject.name;
            
            go.transform.SetParent(transform.parent);
            go.transform.position = transform.position;
            go.transform.rotation = transform.rotation;
            go.transform.localScale = transform.localScale;
            
            return go.GetComponent(GetType()) as SpaceObject;
        }
        
        #region IColorBlindAgent

        const string colorBlindTag = "ColorBlind";

        public void SetColorBlind(bool enabled) {
            transform
                .AllChild()
                .Where(t => t.CompareTag(colorBlindTag))
                .ForEach(t => t.gameObject.SetActive(enabled));
        }

        #endregion
    }
}