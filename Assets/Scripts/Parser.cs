using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;

namespace Collada
{
    public class Parser : MonoBehaviour
    {
        [SerializeField]
        private string _filename;

        private void Start()
        {
            var filename = Path.Combine(Application.streamingAssetsPath, _filename);

            XmlDocument document = new XmlDocument();
            document.Load(filename);

            var root = document.DocumentElement;

            #region Materials
            var effects = new Dictionary<string, Color>();
            foreach (XmlNode effect in root.ChildNodes[1])
            {
                var color = effect.ChildNodes[0].ChildNodes[0].ChildNodes[0].ChildNodes[1].ChildNodes[0].InnerText.Split().Select<string, (float channel, int index)>((channel, index) => (Convert.ToSingle(channel, CultureInfo.InvariantCulture), index)).GroupBy(pair => pair.index / 4).Select(g => { var gChannels = g.Select(gData => gData.channel).ToArray(); return new Color(gChannels[0], gChannels[1], gChannels[2], gChannels[3]); }).First();
                effects[effect.Attributes.GetNamedItem("id").InnerText] = color;
            }

            var materials = new Dictionary<string, string>();
            foreach (XmlNode material in root.ChildNodes[3])
                materials[material.Attributes.GetNamedItem("id").InnerText] = material.ChildNodes[0].Attributes.GetNamedItem("url").InnerText.Substring(1);
            #endregion

            #region Meshes
            var meshes = new Dictionary<string, Mesh>();

            foreach (XmlNode geometry in root.ChildNodes[4])
            {
                var meshData = geometry.ChildNodes[0].ChildNodes;

                var vertices = meshData[0].ChildNodes[0].InnerText.Split().Select((sValue, index) => (sValue, index)).GroupBy(pair => pair.index / 3).Select(g => { var sValues = g.Select(data => data.sValue).ToArray(); return new Vector3(Convert.ToSingle(sValues[0], CultureInfo.InvariantCulture), Convert.ToSingle(sValues[1], CultureInfo.InvariantCulture), Convert.ToSingle(sValues[2], CultureInfo.InvariantCulture)); }).ToArray();
                //var normals = meshData[1].ChildNodes[0].InnerText.Split().Select((sValue, index) => (sValue, index)).GroupBy(pair => pair.index / 3).Select(g => { var sValues = g.Select(data => data.sValue).ToArray(); return new Vector3(Convert.ToSingle(sValues[0], CultureInfo.InvariantCulture), Convert.ToSingle(sValues[1], CultureInfo.InvariantCulture), Convert.ToSingle(sValues[2], CultureInfo.InvariantCulture)); }).ToArray();
                var triangles = meshData[4].ChildNodes[3].InnerText.Split().Select(sIndex => Convert.ToInt32(sIndex)).ToArray();



                var mesh = new Mesh
                {
                    vertices = vertices,
                    //normals = normals
                    triangles = triangles
                };

                meshes[geometry.Attributes.GetNamedItem("id").InnerText] = mesh;
            }
            #endregion

            #region Scene
            Transform parent = new GameObject(Path.GetFileNameWithoutExtension(filename)).transform;

            foreach (XmlNode obj in root.ChildNodes[5].ChildNodes[0])
            {
                Transform go = new GameObject(obj.Attributes.GetNamedItem("name").InnerText).transform;
                go.parent = parent;

                var matrixValues = obj.ChildNodes[0].InnerText.Split().Select(sValue => Convert.ToSingle(sValue, CultureInfo.InvariantCulture)).ToArray();

                Matrix4x4 matrix = Matrix4x4.zero;
                for (int i = 0, j = 0; i < matrixValues.Length; i += 4)
                    matrix[i / 4, i % 4] = matrixValues[j++];

                Debug.LogWarning(matrix.ValidTRS());
                go.position = matrix.GetColumn(3);
                go.localScale = new Vector3(matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);

                matrix.SetPosition(Vector3.one);
                matrix.SetRotation(Quaternion.Euler(0f, 90f, 0f));
                go.FromMatrix(matrix);

                go.gameObject.AddComponent<MeshFilter>().mesh = meshes[obj.ChildNodes[1].Attributes.GetNamedItem("url").InnerText.Substring(1)];
                
                var renderer = go.gameObject.AddComponent<MeshRenderer>();
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.sharedMaterial.color = effects[materials[obj.ChildNodes[1].ChildNodes[0].ChildNodes[0].ChildNodes[0].Attributes.GetNamedItem("target").InnerText.Substring(1)]];
            }
            #endregion
        }
    }
}