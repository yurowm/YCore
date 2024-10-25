using Yurowm.Profiling;
using Yurowm.Spaces;

namespace Yurowm.Jobs {
    public interface ISelfUpdate {
        int updateID { get; set; }
        bool readyForUpdate { get; set; }
        void UpdateFrame(Updater updater);
        void MakeUnupdated();
    }

    public class SelfUpdateJob : Job<ISelfUpdate>, ISpaceJob, IUpdateJob {
        Updater updater = new Updater();
        
        public Space space { get; set; }
        
        int frameID = int.MinValue;
        
        public override int GetPriority() {
            return base.GetPriority() - 1;
        }

        public override void ToWork() {
            frameID ++;
            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            using (YProfiler.Area($"SelfUpdate Work"))
                #endif
            foreach (var s in subscribers)
                if (s.readyForUpdate && s.updateID != frameID) {
                    updater.frameID = frameID;
                    #if DEVELOPMENT_BUILD || UNITY_EDITOR
                    using (YProfiler.Area($"SelfUpdate: {s}"))
                        #endif
                        s.UpdateFrame(updater);
                    s.updateID = frameID;
                }
        }

        public override void Do() {
            do {
                updater.repeate = false;
                base.Do();
            } while (updater.repeate);
        }
    }

    public class Updater {
        public bool repeate = false;
        public int frameID;
    }
}