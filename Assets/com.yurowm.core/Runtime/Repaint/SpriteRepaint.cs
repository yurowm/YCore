using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Yurowm.Coroutines;
using Yurowm.Extensions;

namespace Yurowm.Colors {
    public class SpriteRepaint : Repaint, IRepaintSetSprite {
        Image image;
        
        public void SetSprite(Sprite sprite) {
            if (image || this.SetupComponent(out image))
                image.sprite = sprite;
        }
    }
}