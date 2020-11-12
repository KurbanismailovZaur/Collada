using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Collada.Extensions
{
	public static class Matrix4x4Extensions
	{
		public static Vector3 GetPosition(this Matrix4x4 matrix) => matrix.GetColumn(3);

		public static Vector3 GetScale(this Matrix4x4 matrix) => new Vector3(matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);
	}
}