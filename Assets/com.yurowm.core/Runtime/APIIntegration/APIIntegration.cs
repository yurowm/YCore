using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Yurowm.Coroutines;
using Yurowm.Integrations;
using Yurowm.Serialization;

namespace Yurowm.Services {
    public abstract class APIIntegration: Integration {
        
        HttpClient httpClient = null;
        protected abstract string GetHost();
        
        protected bool isConfigured = true;
        
        public async UniTask Request<T>(string name, T body, MethodType method,  Action<T> success, Action<string> failed = null) {
            await Request<T, T>(name, body, method, success, failed);
        }
        
        public async UniTask Request<B, R>(string name, B body, MethodType method, Action<R> success, Action<string> failed = null) {
            #if UNITY_WEBGL
            await RequestUnity(name, body, method, success, failed);
            #else
            await RequestHTTPClient(name, body, method, success, failed);
            #endif
        }

        async UniTask RequestUnity<B, R>(string name, B body, MethodType method, Action<R> success, Action<string> failed = null) {
            while (!isConfigured)
                await UniTask.Yield();
        
            var request = new UnityWebRequest(Path.Combine(GetHost(), name), method.ToString());
            Debug.Log($"Request: {request.url}: {method}");
            
            var serializedBody = body == null ? "" : JsonConvert.SerializeObject(body);
            var bodyRaw = Encoding.UTF8.GetBytes(serializedBody);
            
            if (method.HasBody())
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);

            foreach (var header in GetHeaders(serializedBody)) 
                request.SetRequestHeader(header.Item1, header.Item2);
            
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            await request.SendWebRequest();
        
            if (request.result == UnityWebRequest.Result.Success) {
                var result = JsonConvert.DeserializeObject<R>(request.downloadHandler.text);
                success?.Invoke(result);
            } else {
                var errorMsg = $"{request.result}: {request.error} ({request.responseCode})";
                Debug.LogError(errorMsg);
                failed?.Invoke(errorMsg);
            }
        }
        
        public async UniTask RequestHTTPClient<B, R>(string name, B body, MethodType method, Action<R> success, Action<string> failed = null) {
            while (!isConfigured)
                await UniTask.Yield(); 
            
            if (httpClient == null) {
                httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(GetHost());
            }
            
            HttpResponseMessage message = null;
            
            var serializedBody = body == null ? "" : JsonConvert.SerializeObject(body);
            var content = new StringContent(serializedBody, Encoding.UTF8, "application/json");
            
            foreach (var header in GetHeaders(serializedBody)) 
                content.Headers.Add(header.Item1, header.Item2);
            
            switch (method) {
                case MethodType.GET: 
                    message = await httpClient.GetAsync(name);
                    break;
                case MethodType.POST: 
                    message = await httpClient.PostAsync(name, content);
                    break;
                case MethodType.PUT: 
                    message = await httpClient.PutAsync(name, content);
                    break;
                case MethodType.DELETE:
                    message = await httpClient.DeleteAsync(name);
                    break;
                default: throw new NotSupportedException($"The method type {method} is not supported.");
            }

            if (message == null)
                return;

            var responseContent = await message.Content.ReadAsStringAsync();

            if (message.IsSuccessStatusCode) {
                var result = JsonConvert.DeserializeObject<R>(responseContent);
                success?.Invoke(result);
            } else
                failed?.Invoke(responseContent);
        }
        
        protected virtual IEnumerable<(string, string)> GetHeaders(string body) {
            yield break;
        }
    }
    
    public enum MethodType {
        GET,
        POST,
        PUT,
        DELETE
    }
    
    public static class MethodTypeExtensions {
        public static bool HasBody(this MethodType type) {
            switch (type) {
                case MethodType.GET:
                case MethodType.DELETE:
                    return false;
                case MethodType.PUT:
                case MethodType.POST:
                    return true;
                default: 
                    return false;
            }
        }
    }
    
}