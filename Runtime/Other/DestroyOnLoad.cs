using UnityEngine;

namespace Yurowm.Utilities {
    public class DestroyOnLoad : MonoBehaviour {
        void Awake() {
            Destroy(gameObject);        
        }
    }
}