using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yurowm.Extensions;

namespace Yurowm.Utilities {
    public class SceneAutoloader : MonoBehaviour {
        public string sceneName;
        
        void Awake() {
            if (!sceneName.IsNullOrEmpty()) 
                SceneManager.LoadScene(sceneName);
        }
    }
}