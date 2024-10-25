namespace Yurowm.Core {
    public abstract class SaveStateNode : UserPathFilter {

        public override void Initialize() {
            base.Initialize();
            if (ID == App.data.GetModule<UserPathData>().GetState(path.ID))
                Start();
        }

        protected override void OnStart() {
            base.OnStart();
            App.data.GetModule<UserPathData>().SetState(path.ID, ID);
        }

        protected override void OnEnd() {
            base.OnEnd();
            App.data.GetModule<UserPathData>().SetState(path.ID, -1);
        }
    }
}