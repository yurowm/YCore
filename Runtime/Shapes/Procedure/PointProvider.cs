using UnityEngine;

namespace Yurowm {
    public interface IPointProvider {
        Vector2[] Points {get; set;}
        Vector3 TransformPoint(Vector2 point);
        Vector2 InverseTransformPoint(Vector3 point);
    }
}