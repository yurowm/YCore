using System;

namespace Yurowm.Spaces {
    public interface IBody {
        Type BodyType { get; }
        string bodyName { get; set; }
    }
}