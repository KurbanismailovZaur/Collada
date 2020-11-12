using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using System.Linq;
using System.Xml.Linq;
using System;

namespace Collada.Extensions.Xml
{
    public static class ColladaXmlExtensions
    {
        public static XmlNode Element(this XmlNode node, params string[] names)
        {
            int index = 0;

        NAMES:
            while (index < names.Length)
            {
                foreach (XmlNode child in node)
                {
                    if (child.Name == names[index])
                    {
                        node = child;
                        index++;

                        goto NAMES;
                    }
                }

                return null;
            }

            return node;
        }

        public static List<XmlNode> Elements(this XmlNode node, string name)
        {
            var nodes = new List<XmlNode>();

            foreach (XmlNode child in node)
            {
                if (child.Name == name)
                    nodes.Add(child);
            }

            return nodes;
        }

        public static string Attribute(this XmlNode node, string name) => node.Attributes.GetNamedItem(name).InnerText;

        public static string Id(this XmlNode node) => Attribute(node, "id");

        public static string Url(this XmlNode node) => Attribute(node, "url").Substring(1);

        public static string Source(this XmlNode node) => Attribute(node, "source").Substring(1);

        public static int Offset(this XmlNode node) => Convert.ToInt32(Attribute(node, "offset"));

        public static int Count(this XmlNode node) => Convert.ToInt32(Attribute(node, "count"));

        public static XmlNode ElementWithAttribute(this XmlNode node, string name, string value)
        {
            foreach (XmlNode element in node)
            {
                foreach (XmlAttribute attribute in element.Attributes)
                {
                    if (attribute.Name == name && attribute.Value == value)
                        return element;
                }
            }

            return null;
        }

        public static XmlNode ElementWithId(this XmlNode node, string id) => ElementWithAttribute(node, "id", id);

        public static XmlNode ElementWithSemantic(this XmlNode node, string semantic) => ElementWithAttribute(node, "semantic", semantic);
    }
}