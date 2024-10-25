using System.Linq;
using Yurowm.Coroutines;
using Yurowm.UI;

namespace Yurowm.Spaces {
    public class SpaceLink : Behaviour {

        [TypeSelector.Target(typeof (Space))]
        public TypeSelector spaceType;

        public Space space = null;
        
        public bool waitUI = false;
        
        void OnEnable() {
            Space.Show(spaceType.GetSelectedType(), s => space = s);
        }

        public override void OnKill() {
            if (space) {
                space.Destroy();
                space = null;
            }
        }

        void OnDisable() {
            if (waitUI) 
                Page.WaitAnimation()
                    .ContinueWith(() => Space.Hide(spaceType.GetSelectedType()))
                    .Run();
            else 
                Space.Hide(spaceType.GetSelectedType());
        }
        
        
        public void Pause() {
            Space.all
                .FirstOrDefault(s => spaceType.GetSelectedType().IsInstanceOfType(s))?
                .Pause();
        }
        
        public void Unpause() {
            Space.all
                .FirstOrDefault(s => spaceType.GetSelectedType().IsInstanceOfType(s))?
                .Unpause();
        }
    }
}