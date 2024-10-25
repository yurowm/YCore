using System;
using System.Collections.Generic;
using System.Linq;
using Yurowm.Extensions;
using Yurowm.Serialization;

namespace Yurowm.UI {
    public class PageTag : PageExtension, ISearchable {
        public string tag;

        public bool Compare(string tag) {
            if (tag.IsNullOrEmpty())
                return false;
            return string.Equals(this.tag, tag, StringComparison.CurrentCultureIgnoreCase);
        }

        public IEnumerable<string> GetSearchData() {
            yield return tag;
        }

        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            writer.Write("tag", tag);
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            reader.Read("tag", ref tag);
        }
    }
    
    public static class PageTagExtensions {
        public static bool HasTag(this Page page, string tag) {
            if (tag.IsNullOrEmpty())
                return false;

            return page.extensions.CastIfPossible<PageTag>().Any(t => t.Compare(tag));
        } 
    }
}