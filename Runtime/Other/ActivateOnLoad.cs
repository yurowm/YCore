using System.Linq;
using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.Utilities {
    public class ActivateOnLoad : MonoBehaviour {
        public GameObject[] activate;
        public GameObject[] deactivate;
        
        void Awake() {
            activate
                .Where(go => go != null)
                .ForEach(go => go.SetActive(true));
            deactivate
                .Where(go => go != null)
                .ForEach(go => go.SetActive(false));
        }
    }
}