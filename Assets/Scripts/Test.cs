using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Collada
{
    public class Test : MonoBehaviour
    {
        [SerializeField]
        private string _inputFile;

        [SerializeField]
        private string _outputFile;

        private void Start()
        {
            var (model, wires) = Parser.Load(Path.Combine(Application.streamingAssetsPath, _inputFile));

            
        }
    }
}