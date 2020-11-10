using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Collada
{
    public class Test : MonoBehaviour
    {
        [SerializeField]
        private string _filename;

        private void Start()
        {
            Parser.Load(Path.Combine(Application.streamingAssetsPath, _filename));
        }
    }
}