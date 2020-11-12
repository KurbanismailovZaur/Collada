using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Collada
{
	public class Normals : MonoBehaviour
	{
        private Vector3[] _vertices;

        private Vector3[] _normals;

        [SerializeField]
        [Range(0f, 1f)]
        private float _length = 1;

        private void Start()
        {
            var mesh = GetComponent<MeshFilter>().sharedMesh;

            _vertices = mesh.vertices;
            _normals = mesh.normals;
        }

        private void Update()
        {
            for (int i = 0; i < _vertices.Length; i++)
            {
                Debug.DrawRay(transform.position + _vertices[i], _normals[i] * _length, Color.yellow);
            }
        }
    }
}