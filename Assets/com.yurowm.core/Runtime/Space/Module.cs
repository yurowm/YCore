using System;
using Yurowm.Serialization;

namespace Yurowm {
    public abstract class Module: ISerializable {
        
        public static M Emit<M>() where M : Module => Activator.CreateInstance<M>();
        
        public GameEntity gameEntity {
            private set;
            get;
        }
        
        public void Link(GameEntity gameEntity) {
            this.gameEntity ??= gameEntity;
        }

        public virtual void Serialize(IWriter writer) { }

        public virtual void Deserialize(IReader reader) { }
    }
    
    public interface IEnableModuleHandler {
        void OnEnable();
    }
    
    public interface IDisableModuleHandler {
        void OnDisable();
    }
}