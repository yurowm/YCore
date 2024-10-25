using System.Collections;
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
                SetColorFade(color).Run();
                return;
            }
            
            if (!spriteRenderer && !this.SetupComponent(out spriteRenderer))
                return;
            
            spriteRenderer.color = TransformColor(color);
        }
        
        IEnumerator SetColorFade(Color color) {
            if (!spriteRenderer && !this.SetupComponent(out spriteRenderer))
                yield break;
            
            var currentColor = GetColor();
            
            color = TransformColor(color);
            
            for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime / fadeDuration) {
                spriteRenderer.color = Color.Lerp(currentColor, color, t);
                yield return null;
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