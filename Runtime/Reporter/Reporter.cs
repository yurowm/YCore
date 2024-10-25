using System;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.InUnityReporting {
    public static class Reporter {
        static Dictionary<string, Report> actions = new Dictionary<string, Report>();
        
        public static void AddTextReport(string name, Func<string> action) {
            AddReport(name, new TextReport(action));
        }       
        
        public static void AddReport(string name, Report report) {
            actions[name] = report;
        }
        
        public static Report GetReport(string name) {
            try {
                var result = actions[name];
                if (result.Refresh())
                    return result;
                else
                    actions.Remove(name);
            } catch (Exception e) {
                Debug.LogException(e);
            }
            
            return null;
        }
        
        public static IEnumerable<string> GetActionsList() {
            foreach (string key in actions.Keys)
                yield return key;
        }
    }
        
    public abstract class Report {
        public abstract bool Refresh();
        public abstract bool OnGUI(params GUILayoutOption[] layoutOptions);
        public abstract string GetTextReport();
    }
        
    public class TextReport : Report {
        Func<string> refresher;
        string snapshot = null;
            
        public TextReport(Func<string> refresher) {
            this.refresher = refresher;
        }
            
        public override bool Refresh() {
            try {
                snapshot = refresher.Invoke().Replace("\t", "   ");
                if (snapshot.IsNullOrEmpty())
                    snapshot = "NULL";
                return true;
            } catch (Exception e) {
                Debug.LogException(e);
            }
            return false;
        }

        public override bool OnGUI(params GUILayoutOption[] layoutOptions) {
            return false;
        }
        
        public override string GetTextReport() {
            return snapshot;
        }
    }
}
