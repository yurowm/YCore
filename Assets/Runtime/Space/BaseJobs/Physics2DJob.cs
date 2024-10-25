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
    public interface IPhysic2DSimulated {
        void BeforeSimulatePhysic2D(float deltaTime);
        void AfterSimulatePhysic2D();
    }
    
    public class Physics2DJob : Job<IPhysic2DSimulated>, ISpaceJob {
    
        public Space space { get; set; }

        [OnLaunch()]
        static void Initialize() {
            if (OnceAccess.GetAccess("Physics2D")) 
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
            #if PHYSICS_2D
            
            float lastSimulate = Time.time;
                
            if (Physics2D.simulationMode != SimulationMode2D.Script)
                yield break;
            
            while (true) {
                DeltaTime = (Time.time - lastSimulate) * TimeScale;
                lastSimulate = Time.time;
                
                stage = Stage.BeforeSimulate;
                onSimulate.Invoke();

                using (YProfiler.Area("Physics2D Simulate"))
                    Physics2D.Simulate(DeltaTime);
                
                stage = Stage.AfterSimulate;
                onSimulate.Invoke();
                
                yield return null;
            }
            
            #else
            
            yield break;
            
            #endif
        }
        
        bool active = false;

        public override void OnSubscribe(IPhysic2DSimulated subscriber) {
            base.OnSubscribe(subscriber);
            if (!active && subscribers.Count > 0) {
                onSimulate += Do;
                active = true;
            }
        }

        public override void OnUnsubscribe(IPhysic2DSimulated subscriber) {
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
                        s.BeforeSimulatePhysic2D(DeltaTime);
                    break;
                }
                case Stage.AfterSimulate: {
                    foreach (var s in subscribers)
                        s.AfterSimulatePhysic2D();
                    break;
                }
            }
        }
    }

    #if PHYSICS_2D
    
    public class Raycast2DHitBuffer : IDisposable {
        RaycastHit2D[] array;
        
        int Size => array.Length;

        Raycast2DHitBuffer(int size) {
            array = new RaycastHit2D[size.ClampMin(1)];
        }
        
        public void Dispose() {
            buffers.Add(this);
        }
        
        static List<Raycast2DHitBuffer> buffers = new List<Raycast2DHitBuffer>();
        
        public static Raycast2DHitBuffer Get(int size, out RaycastHit2D[] buffer) {
            if (size <= 0) {
                buffer = null;
                return null;
            }
            
            Raycast2DHitBuffer result = null;
            if (buffers.IsEmpty()) {
                result = new Raycast2DHitBuffer(size);
            } else {
                foreach (var b in buffers) {
                    if (b.Size < size) continue;
                    if (result == null || b.Size < result.Size) {
                        result = b;
                        if (result.Size == size) break;
                    }
                }
                if (result == null)
                    result = new Raycast2DHitBuffer(size);
                else
                    buffers.Remove(result);
            }
            
            buffer = result.array;                   
            return result;
        }
    }

    #endif
    
    public class Physics2DSymbol : ScriptingDefineSymbolAuto {
        public override string GetSybmol() {
            return "PHYSICS_2D";
        }

        public override IEnumerable<string> GetRequiredPackageIDs() {
            yield return "com.unity.modules.physics2d";
        }
    }
    
}