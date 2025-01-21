using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using Yurowm.Core;
using Yurowm.Extensions;
using Yurowm.Serialization;

namespace Yurowm.UI {
    public class WaitButtonClickNode : UserPathFilter {
        public string buttonID;
        
        public bool buttonLock;

        public override async UniTask Logic() {

            if (buttonID.IsNullOrEmpty())
                return;
            
            var buttons = Behaviour
                .GetAllByID<Button>(buttonID)
                .ToArray();
            
            if (!buttons.Any())
                return;
            
            bool wait = true;
            
            void OnButtonClick() => wait = false;

            var locker = buttonLock ? InputLock.Lock(buttonID) : null;
            
            buttons.ForEach(b => b.onClick.AddListener(OnButtonClick));
            
            while (wait)
                await UniTask.Yield();

            buttons.ForEach(b => b.onClick.RemoveListener(OnButtonClick));
            
            locker?.Dispose();
        }


        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            writer.Write("buttonID", buttonID);
            writer.Write("buttonLock", buttonLock);
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            reader.Read("buttonID", ref buttonID);
            reader.Read("buttonLock", ref buttonLock);
        }
    }
}