using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using System.Linq;
using System.Xml.Linq;
using System;

namespace Collada.Xml
{
    public static class XmlExtensions
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

                throw new Exception($"Element with name {names[index]} was not found!");
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
    }
}