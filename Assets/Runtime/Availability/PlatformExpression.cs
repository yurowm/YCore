using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
#if MXPARSER
using org.mariuszgromada.math.mxparser;
#endif
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.Integrations;
using Yurowm.Serialization;

namespace Yurowm {
    public static class PlatformExpression {
        
        static string[] values;
        
        const string debugValue = "Debug";
        const string releaseValue = "Release";
        const string editorValue = "Editor";
        const string runtimeValue = "Runtime";
        const string trueValue = "True";
        const string falseValue = "False";
        
        public static IEnumerable<string> AllValues() {
            yield return debugValue;
            yield return releaseValue;
            yield return editorValue;
            yield return runtimeValue;
            yield return trueValue;
            yield return falseValue;
        }
        
        static void BuildValues() {
            var result = new List<string>();
                
            result.Add(trueValue);
            
            #if UNITY_EDITOR
            result.Add(UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString());
            #else
            result.Add(Core.ProjectSettings.Instance.buildTarget);
            #endif
            
            if (Debug.isDebugBuild)
                result.Add(debugValue);
            else
                result.Add(releaseValue);
            
            if (Application.isEditor)
                result.Add(editorValue);
            else
                result.Add(runtimeValue);
            
            values = result.Select(v => v.ToUpper()).ToArray();
        }
        
        public static bool Evaluate(this IPlatformExpression ipe) => Evaluate(ipe.platformExpression);

        public static bool Evaluate(string exprssion) {
            
            if (exprssion.IsNullOrEmpty())
                return true;
            
            #if MXPARSER
            try {
                if (values == null)
                    BuildValues();
                
                var expression = exprssion;
                foreach (Match match in Regex.Matches(exprssion, @"\w+")) 
                    expression = expression.Replace(match.Value, values.Contains(match.Value.ToUpper()) ? "1" : "0");
                
                var exp = new Expression(expression);
                
                if (exp.calculate() != 1)
                    return false;
            }
            catch (Exception e) {
                Debug.LogException(e);
                return false;
            }
            #else
            Debug.LogError("Platform expression feature requires MXParser library");
            #endif
            
            return true;
        }

        public static void Serialize(IWriter writer, IPlatformExpression ipe) {
            var value = ipe.platformExpression;
            if (!value.IsNullOrEmpty())
                writer.Write("platformExpression", value);
        }

        public static void Deserialize(IReader reader, IPlatformExpression ipe) {
            ipe.platformExpression = reader.Read<string>("platformExpression");
        }
    }
    
    public interface IPlatformExpression {
        public string platformExpression {get; set;}
    }
}