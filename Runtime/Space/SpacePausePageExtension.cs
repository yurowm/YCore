using System;
using System.Linq;
using Yurowm.Extensions;
using Yurowm.Serialization;
using Yurowm.UI;
using Yurowm.Utilities;

namespace Yurowm.Spaces {
    public class SpacePausePageExtension : PageExtension {
        public string spaceType;
        public bool pause;
        
        Type type;
        
        public override void OnShow(Page page) {
            base.OnShow(page);
            if (spaceType.IsNullOrEmpty()) return;
            
            if (type == null) {
                type = Utils
                    .FindInheritorTypes<Space>(true)
                    .FirstOrDefault(t => t.FullName == spaceType);
               
                if (type == null) 
                    return;
            }
            
            var space = Space.all.FirstOrDefault(s => type.IsInstanceOfType(s));

            if (space == null) return;
            
            if (pause)
                space.Pause();
            else
                space.Unpause();
        }

        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            writer.Write("spaceType", spaceType);
            writer.Write("pause", pause);
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            reader.Read("spaceType", ref spaceType);
            reader.Read("pause", ref pause);
        }
    }
}