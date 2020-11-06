using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Collada
{
    public static class MatrixExtensions
    {
        public static Vector3 GetPosition(this Matrix4x4 matrix) => matrix.GetColumn(3);

        public static void SetPosition(this Matrix4x4 matrix, Vector3 position) => matrix.SetColumn(3, position);

        public static Quaternion GetRotation(this Matrix4x4 matrix) => Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));

        public static void SetRotation(this Matrix4x4 matrix, Quaternion rotation)
        {
            matrix.SetColumn(2, rotation * Vector3.forward);
            matrix.SetColumn(1, rotation * Vector3.up);
        }

        public static Vector3 GetScale(this Matrix4x4 matrix) => new Vector3(matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);

        public static void SetScale(this Matrix4x4 matrix, Vector3 scale)
        {
            matrix.SetColumn(0, new Vector4((Vector4.one * scale.x).magnitude, (Vector4.one * scale.y).magnitude, (Vector4.one * scale.z).magnitude));
        }
    }
}