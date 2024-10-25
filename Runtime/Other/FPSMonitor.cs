using System;
using UnityEngine;

namespace Yurowm {

    public class FPSMonitor {
        float FPS = 30; 
        bool isDefined = false; 
        
        public bool GetFPS(out float FPS) {
            FPS = this.FPS;
            return isDefined;
        }
        
        DateTime lastTime = DateTime.Now; 
        int frames = 0;

        public void Frame() {
            frames++;
            
            if (frames >= 10) {
                FPS = Mathf.RoundToInt(1f * frames / (float) (DateTime.Now - lastTime).TotalSeconds);
                isDefined = FPS >= 1;
                
                frames = 0;
                lastTime = DateTime.Now;
            }
        }
    }
}