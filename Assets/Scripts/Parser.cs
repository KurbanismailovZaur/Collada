using Collada.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Xml;
using UnityEngine;
using Collada.Extensions.Xml;
using System.Runtime.Remoting.Messaging;
using System.Net.Http.Headers;

namespace Collada
{
    public static class Parser
    {
        private static Dictionary<string, Mesh> _meshes = new Dictionary<string, Mesh>();

        private static Dictionary<string, Material> _materials = new Dictionary<string, Material>();

        private static string _defaultMaterialID;

        private static bool _leftHandedAxes;

        private static Func<IEnumerable<float>, Vector3> _axesCorrector;

        public static GameObject Load(string filename)
        {
            XmlDocument document = new XmlDocument();
            document.Load(filename);

            _defaultMaterialID = Guid.NewGuid().ToString();

            var defaultMaterial = new Material(Shader.Find("Standard"));
            defaultMaterial.name = "Default";

            _materials.Add(_defaultMaterialID, defaultMaterial);

            var rootElement = document.DocumentElement;
            var root = new GameObject(Path.GetFileNameWithoutExtension(filename));

            _leftHandedAxes = rootElement.Element("asset", "up_axis").InnerText != "Z_UP";

            if (_leftHandedAxes)
                _axesCorrector = v => new Vector3(v.ElementAt(0), v.ElementAt(1), v.ElementAt(2));
            else
                _axesCorrector = v => new Vector3(v.ElementAt(0), v.ElementAt(2), v.ElementAt(1));

            foreach (XmlNode sceneElement in rootElement.Elements("scene"))
            {
                var sceneId = sceneElement.Element("instance_visual_scene")?.Url();

                if (sceneId == null)
                    continue;

                var visualSceneElement = rootElement.Element("library_visual_scenes").ElementWithId(sceneId);
                var visualScene = CreateGameObjectWithParent(sceneId, root.transform);

                foreach (XmlNode node in visualSceneElement.Elements("node"))
                {
                    var gameObject = CreateNodeGameObject(rootElement, node);
                    gameObject.transform.parent = visualScene.transform;
                }
            }

            _meshes.Clear();

            _materials.Clear();
            _defaultMaterialID = null;

            _axesCorrector = null;
            _leftHandedAxes = false;

            return root;
        }

        private static GameObject CreateGameObjectWithParent(string name, Transform parent)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.parent = parent;

            return gameObject;
        }

        private static GameObject CreateNodeGameObject(XmlElement root, XmlNode node)
        {
            var gameObject = new GameObject(node.Attribute("name"));

            foreach (XmlNode child in node.Elements("node"))
                CreateNodeGameObject(root, child).transform.SetParent(gameObject.transform, false);

            #region Transform
            var columns = node.Element("matrix")?.InnerText.Split().Select((stringValue, index) => (value: Convert.ToSingle(stringValue, CultureInfo.InvariantCulture), index)).GroupBy(pair => pair.index % 4).Select(group => group.Select(pair => pair.value)).Select(col => new Vector4(col.ElementAt(0), col.ElementAt(1), col.ElementAt(2), col.ElementAt(3))) ?? throw new Exception("\"matrix\" element was not finded.");
            var matrix = new Matrix4x4(columns.ElementAt(0), columns.ElementAt(1), columns.ElementAt(2), columns.ElementAt(3));

            if (_leftHandedAxes)
            {
                gameObject.transform.localPosition = matrix.GetPosition();
                gameObject.transform.localRotation = matrix.rotation;
                gameObject.transform.localScale = matrix.GetScale();
            }
            else
            {
                var pos = matrix.GetPosition();
                
                var temp = pos.y;
                pos.y = pos.z;
                pos.z = temp;

                gameObject.transform.localPosition = pos;
                gameObject.transform.localRotation = new Quaternion(-matrix.rotation.x, -matrix.rotation.z, -matrix.rotation.y, matrix.rotation.w);

                var scale = matrix.GetScale();

                temp = scale.y;
                scale.y = scale.z;
                scale.z = temp;

                gameObject.transform.localScale = scale;
            }

            #endregion

            var instanceGeometry = node.Element("instance_geometry");

            if (instanceGeometry == null)
                return gameObject;

            string[] trianglesMaterials = null;

            #region Geometry
            var geometryElement = root.Element("library_geometries").ElementWithId(instanceGeometry.Url());
            if (!_meshes.ContainsKey(geometryElement.Id()))
            {
                var meshElement = geometryElement.Element("mesh");

                var uniqueVertices = new Dictionary<string, Vector3[]>();
                var uniqueNormals = new Dictionary<string, Vector3[]>();
                var uniqueUVs = new Dictionary<string, Vector3[]>();

                var uniqueArrays = new Dictionary<string, Dictionary<string, Vector3[]>>
                {
                    { "VERTEX", uniqueVertices },
                    { "NORMAL", uniqueNormals },
                    { "TEXCOORD", uniqueUVs }
                };

                int trianglesCount = meshElement.Elements("triangles").Select(t => t.Count()).Sum();
                int arraysSize = trianglesCount * 3;

                var vertices = new Vector3[arraysSize];
                var normals = new Vector3[arraysSize];
                var uvs = new Vector3[arraysSize];

                var arrays = new Dictionary<string, Array>
                {
                    { "VERTEX", vertices },
                    { "NORMAL", normals },
                    { "TEXCOORD", uvs }
                };

                var trianglesArray = new int[meshElement.Elements("triangles").Count][];
                trianglesMaterials = new string[trianglesArray.Length];

                int arraysIndex = 0;
                int trianglesIndex = 0;
                foreach (var trianglesElement in meshElement.Elements("triangles"))
                {
                    var vertexSource = trianglesElement.ElementWithSemantic("VERTEX")?.Source();
                    var verticesSourceElement = meshElement.ElementWithId((vertexSource != null ? meshElement.ElementWithId(vertexSource) : meshElement.Element("vertices")).ElementWithSemantic("POSITION").Source());
                    HandleGeometryVector3Source(uniqueVertices, verticesSourceElement);

                    var normalsSource = trianglesElement.ElementWithSemantic("NORMAL")?.Source();
                    if (normalsSource != null)
                    {
                        var normalsSourceElement = meshElement.ElementWithId(normalsSource);
                        HandleGeometryVector3Source(uniqueNormals, normalsSourceElement);
                    }

                    var uvsSource = trianglesElement.ElementWithSemantic("TEXCOORD")?.Source();
                    if (uvsSource != null)
                    {
                        var uvsSourceElement = meshElement.ElementWithId(uvsSource ?? meshElement.Element("vertices").ElementWithSemantic("TEXCOORD").Source());
                        HandleGeometryVector2Source(uniqueUVs, uvsSourceElement);
                    }

                    var inputs = new List<(string semantic, string source, int offset)>();
                    inputs.Add(("VERTEX", meshElement.Element("vertices").ElementWithSemantic("POSITION").Source(), 0));

                    var normalInput = trianglesElement.ElementWithSemantic("NORMAL");
                    if (normalInput != null)
                        inputs.Add(("NORMAL", normalInput.Source(), normalInput.Offset()));

                    var texcoordInput = trianglesElement.ElementWithSemantic("TEXCOORD");
                    if (texcoordInput != null)
                        inputs.Add(("TEXCOORD", texcoordInput.Source(), texcoordInput.Offset()));

                    int offset = inputs.Select(pair => pair.offset).Max() + 1;
                    var indexes = trianglesElement.Element("p").InnerText.Split().Select(sIndex => Convert.ToInt32(sIndex)).ToArray();

                    trianglesArray[trianglesIndex] = new int[trianglesElement.Count() * 3];
                    int currentTrianglesIndex = 0;

                    for (int i = 0; i < indexes.Length; i += offset)
                    {
                        for (int j = 0; j < inputs.Count; j++)
                            arrays[inputs[j].semantic].SetValue(uniqueArrays[inputs[j].semantic][inputs[j].source][indexes[i + inputs[j].offset]], arraysIndex);

                        trianglesArray[trianglesIndex][currentTrianglesIndex++] = arraysIndex++;
                    }

                    #region Materials
                    var triangleMaterialSymbol = trianglesElement.Attribute("material");
                    var materialID = (triangleMaterialSymbol == null) ? _defaultMaterialID : instanceGeometry.Element("bind_material")?.Element("technique_common").ElementWithAttribute("symbol", triangleMaterialSymbol).Attribute("target").Substring(1);

                    if (materialID != null && !_materials.ContainsKey(materialID))
                    {
                        var materialElement = root.Element("library_materials").ElementWithId(materialID);
                        var color = root.Element("library_effects").ElementWithId(materialElement.Element("instance_effect").Url()).Element("profile_COMMON", "technique", "lambert", "diffuse", "color").InnerText.Split().Select((sColor, index) => (value: Convert.ToSingle(sColor, CultureInfo.InvariantCulture), index)).GroupBy(pair => pair.index / 4).Select(g => { var values = g.Select(pair => pair.value); return new Color(values.ElementAt(0), values.ElementAt(1), values.ElementAt(2), values.ElementAt(3)); }).First();
                        var material = new Material(Shader.Find("Standard"))
                        {
                            name = materialElement.Attribute("name"),
                            color = color
                        };

                        _materials.Add(materialID, material);
                    }

                    trianglesMaterials[trianglesIndex++] = materialID;
                    #endregion
                }

                if (!_leftHandedAxes)
                {
                    for (int i = 0; i < trianglesArray.Length; i++)
                    {
                        for (int j = 0; j < trianglesArray[i].Length; j += 3)
                        {
                            var temp = trianglesArray[i][j + 1];
                            trianglesArray[i][j + 1] = trianglesArray[i][j + 2];
                            trianglesArray[i][j + 2] = temp;
                        }
                    }
                }

                var mesh = new Mesh();
                mesh.vertices = vertices;
                mesh.normals = normals;
                mesh.uv = uvs.Select(uv => (Vector2)uv).ToArray();

                mesh.subMeshCount = trianglesArray.Length;
                for (int i = 0; i < trianglesArray.Length; i++)
                    mesh.SetTriangles(trianglesArray[i], i);

                _meshes.Add(geometryElement.Id(), mesh);
            }
            #endregion

            gameObject.AddComponent<MeshFilter>().mesh = _meshes[geometryElement.Id()];

            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.materials = trianglesMaterials.Select(id => _materials[id]).ToArray();

            return gameObject;
        }

        private static void HandleGeometryVector3Source(Dictionary<string, Vector3[]> dict, XmlNode arraySource)
        {
            if (dict.ContainsKey(arraySource.Id()))
                return;

            var accessorElement = arraySource.Element("technique_common", "accessor") ?? throw new Exception("\"accessor\" element was not finded.");
            var paramElements = accessorElement.Elements("param");

            if (accessorElement.Attribute("stride") != "3" || paramElements.Count != 3 || paramElements.Where(p => p.Attributes.GetNamedItem("name") != null).Count() != 3)
                throw new Exception($"Wrong accessor in element: {arraySource.InnerXml}");

            var array = arraySource.ElementWithId(accessorElement.Source()).InnerText.Split().Select((sValue, index) => (value: Convert.ToSingle(sValue, CultureInfo.InvariantCulture), index)).GroupBy(pair => pair.index / 3).Select(g => { var values = g.Select(pair => pair.value); return _axesCorrector(values); }).ToArray();
            dict.Add(arraySource.Id(), array);
        }

        private static void HandleGeometryVector2Source(Dictionary<string, Vector3[]> dict, XmlNode arraySource)
        {
            if (dict.ContainsKey(arraySource.Id()))
                return;

            var accessorElement = arraySource.Element("technique_common", "accessor") ?? throw new Exception("\"accessor\" element was not finded.");
            var paramElements = accessorElement.Elements("param");

            if (accessorElement.Attribute("stride") != "2" || paramElements.Count != 2 || paramElements.Where(p => p.Attributes.GetNamedItem("name") != null).Count() != 2)
                throw new Exception($"Wrong accessor in element: {arraySource.InnerXml}");

            var array = arraySource.ElementWithId(accessorElement.Source()).InnerText.Split().Select((sValue, index) => (value: Convert.ToSingle(sValue, CultureInfo.InvariantCulture), index)).GroupBy(pair => pair.index / 2).Select(g => { var values = g.Select(pair => pair.value); return new Vector3(values.ElementAt(0), values.ElementAt(1), 0f); }).ToArray();
            dict.Add(arraySource.Id(), array);
        }
    }
}