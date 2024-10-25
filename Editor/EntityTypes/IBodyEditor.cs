using Yurowm.ObjectEditors;

namespace Yurowm.Spaces {
    public class IBodyEditor : ObjectEditor<IBody> {
        public override void OnGUI(IBody item, object context = null) {
            BaseTypesEditor.SelectBody(item);
        }
    }
}