using System.Collections;
using System.Collections.Generic;
using Yurowm.Core;
using Yurowm.Extensions;
using Yurowm.Serialization;

namespace Yurowm.UI {
    public class WaitPageNode : UserPathFilter {
        public List<string> pageNames = new();
        public bool immediate = false;

        public override IEnumerator Logic() {

            bool wait = true;
            
            void OnShowPage(Page page) {
                if (pageNames.Contains(page.ID))    
                    wait = false;
            }
            
            Page.onShow += OnShowPage;

            while (wait)
                yield return null;

            Page.onShow -= OnShowPage;
            
            if (!immediate)
                yield return Page.WaitAnimation();
        }


        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            writer.Write("immediate", immediate);
            writer.Write("pages", pageNames.ToArray());
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            reader.Read("immediate", ref immediate);
            pageNames.Reuse(reader.ReadCollection<string>("pages"));
        }
    }
}