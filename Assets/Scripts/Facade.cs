using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Exploring.FileSystem;
using UnityEngine.UI;

namespace Collada
{
    public class Facade : MonoBehaviour
    {
        private GameObject _model;

        private List<Wire> _wires;

        [SerializeField]
        private Button _calculateButton;

        public void OpenCollada() => StartCoroutine(OpenColladaRoutine());

        private IEnumerator OpenColladaRoutine()
        {
            yield return FileExplorer.Instance.OpenFile("Открыть файл", filters: "dae");

            if (FileExplorer.Instance.LastResult == null)
                yield break;

            _model?.GetComponentsInChildren<MeshFilter>().ToList().ForEach(filter => Destroy(filter.sharedMesh));
            Destroy(_model);

            Resources.UnloadUnusedAssets();

            (_model, _wires) = Parser.Load(FileExplorer.Instance.LastResult);

            _calculateButton.interactable = true;
        }

        public void Calculate()
        {
            var jWires = new JArray();

            foreach (var wire in _wires)
            {
                var jWire = new JObject();

                jWire["name"] = wire.name;
                jWire["mark"] = wire.Mark;
                jWire["start_block"] = wire.StartBlock;
                jWire["start_block_index"] = wire.StartBlockIndex;
                jWire["end_blocks"] = new JArray(wire.EndBlocks);
                jWire["end_blocks_indexes"] = new JArray(wire.EndBlocksIndexes);
                jWire["points"] = new JArray(wire.Points.Select(p => new JArray(p.x, p.y, p.z)).ToArray());
                jWire["indexes"] = new JArray(wire.Indexes);

                jWires.Add(jWire);
            }

            File.WriteAllText("wiredata.json", jWires.ToString());
        }
    }
}