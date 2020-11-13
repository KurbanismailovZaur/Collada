using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Collada
{
	public class Wire : MonoBehaviour
	{
        public string Mark { get; set; }

        public string StartBlock { get; set; }

        public int StartBlockIndex { get; set; }

        public string[] EndBlocks { get; set; }

        public int[] EndBlocksIndexes { get; set; }

        public Vector3[] Points { get; set; }

        public int[] Indexes { get; set; }
    }
}