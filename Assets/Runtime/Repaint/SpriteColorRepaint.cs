using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Yurowm.Coroutines;
using Yurowm.Extensions;

namespace Yurowm.Colors {
    [RequireComponent(typeof (SpriteRenderer))]
    public class SpriteColorRepaint : RepaintColor {
        
        SpriteRenderer spriteRenderer;
        
        public float fadeDuration = 0;

        public override void SetColor(Color color) {
            if (fadeDuration > 0 && gameObject.activeInHierarchy) {
                SetColorFade(color).Forget();
                return;
            }
            
            if (!spriteRenderer && !this.SetupComponent(out spriteRenderer))
                return;
            
            spriteRenderer.color = TransformColor(color);
        }
        
        async UniTask SetColorFade(Color color) {
            if (!spriteRenderer && !this.SetupComponent(out spriteRenderer))
                return;
            
            var currentColor = GetColor();
            
            color = TransformColor(color);
            
            for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime / fadeDuration) {
                spriteRenderer.color = Color.Lerp(currentColor, color, t);
                await UniTask.Yield();
            }

            spriteRenderer.color = color;
        }

        public override Color GetColor() {
            if (!spriteRenderer && !this.SetupComponent(out spriteRenderer))
                return default;
            
            return spriteRenderer.color;
        }
    }
}