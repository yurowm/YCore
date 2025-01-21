using System;
using UnityEngine;

namespace Yurowm.Utilities {
    public class LauncherMB : MonoBehaviour {
        void Awake() {
            OnLaunchAttribute.Launch();
            Destroy(gameObject);
        }
    }
}
