using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Collada
{
    public static class TransformExtensions
    {
        public static void FromMatrix(this Transform transform, Matrix4x4 matrix)
        {
            transform.localPosition = matrix.GetPosition();
            transform.localRotation = matrix.GetRotation();
            transform.localScale = matrix.GetScale();
        }
    }
}