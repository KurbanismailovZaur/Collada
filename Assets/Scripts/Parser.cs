using Collada.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Xml;
using UnityEngine;
using UnityEngine.UIElements;

namespace Collada
{
    public static class Parser
    {
        public static GameObject Load(string filename)
        {
            XmlDocument document = new XmlDocument();
            document.Load(filename);

            var root = document.DocumentElement;

            var effects = new Dictionary<string, Color>();
            foreach (XmlNode effect in root.Element("library_effects"))
                effects[effect.Id()] = effect.Element("profile_COMMON", "technique", "lambert", "diffuse", "color").InnerText.Split().Select<string, (float channel, int index)>((channel, index) => (Convert.ToSingle(channel, CultureInfo.InvariantCulture), index)).GroupBy(pair => pair.index / 4).Select(g => { var gChannels = g.Select(gData => gData.channel).ToArray(); return new Color(gChannels[0], gChannels[1], gChannels[2], gChannels[3]); }).First();

            var materials = new Dictionary<string, Material>();
            foreach (XmlNode material in root.Element("library_materials"))
            {
                var mat = new Material(Shader.Find("Standard"))
                {
                    color = effects[material.Element("instance_effect").Url()]
                };

                materials[material.Id()] = mat;
            }

            var meshes = new Dictionary<string, Mesh>();
            foreach (XmlNode geometry in root.Element("library_geometries"))
            {
                var mesh = geometry.Element("mesh");

                var sourcesVerified = new List<string>();

                foreach (var triangles in mesh.Elements("triangles"))
                {

                }
                

                //meshes[geometry.Id()] = 
            }

            return null;
        }

        private static void Parse()
        {
            //var filename = Path.Combine(Application.streamingAssetsPath, _filename);
            string filename = null;

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
        }
    }
}