using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.Coroutines;
using Yurowm.Extensions;
using Yurowm.Profiling;
using Yurowm.Spaces;
using Yurowm.Utilities;
using Space = Yurowm.Spaces.Space;

namespace Yurowm.Jobs {
    public interface IPhysic3DSimulated {
        void BeforeSimulatePhysic3D(float deltaTime);
        void AfterSimulatePhysic3D();
    }
    
    public class Physics3DJob : Job<IPhysic3DSimulated>, ISpaceJob {
    
        public Space space { get; set; }

        [OnLaunch()]
        static void Initialize() {
            if (OnceAccess.GetAccess("Physics3D")) 
                Simulate().Run();
        }

        static Action onSimulate = delegate {};
        
        public static float DeltaTime {get; private set;} = 0;
        public static float TimeScale = 1;
        static Stage stage;
        
        enum Stage {
            BeforeSimulate,
            AfterSimulate,
        }
        
        static IEnumerator Simulate() {
            #if PHYSICS_3D
            float lastSimulate = Time.time;
                
            if (Physics.autoSimulation)
                yield break;
            
            while (true) {
                DeltaTime = (Time.time - lastSimulate) * TimeScale;
                lastSimulate = Time.time;
                
                stage = Stage.BeforeSimulate;
                onSimulate.Invoke();

                using (YProfiler.Area("Physics3D Simulate"))
                    Physics.Simulate(DeltaTime);
                
                stage = Stage.AfterSimulate;
                onSimulate.Invoke();
                
                yield return null;
            }
            #else
            yield break;
            #endif
        }
        
        bool active = false;

        public override void OnSubscribe(IPhysic3DSimulated subscriber) {
            base.OnSubscribe(subscriber);
            if (!active && subscribers.Count > 0) {
                onSimulate += Do;
                active = true;
            }
        }

        public override void OnUnsubscribe(IPhysic3DSimulated subscriber) {
            base.OnUnsubscribe(subscriber);
            if (active && subscribers.Count == 0) {
                onSimulate -= Do;
                active = false;
            }
        }

        public override void ToWork() {
            switch (stage) {
                case Stage.BeforeSimulate: {
                    foreach (var s in subscribers)
                        s.BeforeSimulatePhysic3D(DeltaTime);
                    break;
                }
                case Stage.AfterSimulate: {
                    foreach (var s in subscribers)
                        s.AfterSimulatePhysic3D();
                    break;
                }
            }
        }
    }

    
    #if PHYSICS_3D
    
    public class Raycast3DHitBuffer : IDisposable {
        RaycastHit[] array;
        
        int Size => array.Length;

        Raycast3DHitBuffer(int size) {
            array = new RaycastHit[size.ClampMin(1)];
        }
        
        public void Dispose() {
            buffers.Add(this);
        }
        
        static List<Raycast3DHitBuffer> buffers = new List<Raycast3DHitBuffer>();
        
        public static Raycast3DHitBuffer Get(int size, out RaycastHit[] buffer) {
            if (size <= 0) {
                buffer = null;
                return null;
            }
            
            Raycast3DHitBuffer result = null;
            if (buffers.IsEmpty()) {
                result = new Raycast3DHitBuffer(size);
            } else {
                foreach (var b in buffers) {
                    if (b.Size < size) continue;
                    if (result == null || b.Size < result.Size) {
                        result = b;
                        if (result.Size == size) break;
                    }
                }
                if (result == null)
                    result = new Raycast3DHitBuffer(size);
                else
                    buffers.Remove(result);
            }
            
            buffer = result.array;                   
            return result;
        }
    }
    
    #endif

    public class Physics3DSymbol : ScriptingDefineSymbolAuto {
        public override string GetSybmol() {
            return "PHYSICS_3D";
        }

        public override IEnumerable<string> GetRequiredPackageIDs() {
            yield return "com.unity.modules.physics";
        }
    }
}