using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Yurowm.Coroutines;
using Yurowm.Extensions;
using Yurowm.Integrations;
using Yurowm.Serialization;

namespace Yurowm.Services {
    public class YurowmAPIIntegration : APIIntegration {
        public const string hostDataKey = "yurowm_host";
        public string hostDebug;
        public bool debug = true;
        
        public string secret;

        protected override string GetHost() {
            var result = string.Empty;
            
            if (Debug.isDebugBuild && debug && !hostDebug.IsNullOrEmpty())
                result = hostDebug;
            else
                Get<DataProviderIntegration>()?.GetData(hostDataKey, out result);
            
            return result;
        }
        
        protected override async UniTask Initialize() {
            if (debug)
                isConfigured = true;
            else {
                var dataProvider = Get<DataProviderIntegration>();
                if (dataProvider != null) {
                    isConfigured = false;
                    dataProvider
                        .WaitReady()
                        .ContinueWith(() => isConfigured = true)
                        .Forget();
                } 
            }

            
            // connection = new HubConnectionBuilder()
            //     .WithUrl($"{host}ymthub")
            //     .Build();
            //
            // connection.On<string, string>("ReceiveMessage", 
            //     (user, message) => Debug.Log($"{user}: {message}"));
            //
            // connection.StartAsync();
            //
            // App.onQuit += () => StopHub();
        }

        // async Task StopHub() {
        //     if (connection == null)
        //         return;
        //     
        //     await connection.StopAsync();
        //     await connection.DisposeAsync();
        //     
        //     connection = null;
        // }

        public override string GetName() {
            return "Yurowm API";
        }

        protected override IEnumerable<(string, string)> GetHeaders(string body) {
            var expires = DateTime.UtcNow.AddMinutes(3).Ticks;
            
            yield return ("End", expires.ToString());
            yield return ("DeviceID", SystemInfo.deviceUniqueIdentifier);
            
            body ??= string.Empty;
            
            if (!secret.IsNullOrEmpty()) {
                var sign = $"{secret}.{expires}.{body.CheckSum()}";

                yield return ("Sign", sign.CheckSum().ToString());
            }
        }
        
        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            writer.Write("secret", secret);
            writer.Write("debug", debug);
            writer.Write("hostDebug", hostDebug);
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            reader.Read("secret", ref secret);
            reader.Read("debug", ref debug);
            reader.Read("hostDebug", ref hostDebug);
        }
    }
}