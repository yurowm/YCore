using System;
using UnityEditor;

namespace Yurowm.DeveloperTools {
    public class VersionNameOptimization : Optimization {
        string GetRightVersionName() {
            var today = DateTime.Today;
            return $"{today.Year}.{today.Month:00}";
        }

        public override bool DoAnalysis() {
            return PlayerSettings.bundleVersion == GetRightVersionName();
        }

        public override bool CanBeAutomaticallyFixed() {
            return true;
        }

        public override void Fix() {
            PlayerSettings.bundleVersion = GetRightVersionName();
        }
    }
}