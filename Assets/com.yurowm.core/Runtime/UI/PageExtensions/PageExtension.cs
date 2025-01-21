using Yurowm.Serialization;

namespace Yurowm.UI {
    public abstract class PageExtension : ISerializable {
        
        public virtual void OnShow(Page page) {}
        
        public virtual void OnHide(Page page) {}
        
        public virtual void Serialize(IWriter writer) {}

        public virtual void Deserialize(IReader reader) {}
    }
}