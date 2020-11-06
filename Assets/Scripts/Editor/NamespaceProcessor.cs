using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using UnityEditor;

namespace Editor
{
    public class NamespaceProcessor : UnityEditor.AssetModificationProcessor
    {
        public static void OnWillCreateAsset(string path)
        {
            path = path.Substring(0, path.Length - ".meta".Length);

            if (Path.GetExtension(path) != ".cs")
                return;

            var fullpath = Path.Combine(Path.GetDirectoryName(Application.dataPath), path);
            var template = File.ReadAllText(fullpath);

            path = path.Substring(path.IndexOf(Path.DirectorySeparatorChar) + 1);

            var index = path.LastIndexOf(Path.DirectorySeparatorChar);
            if (index == -1)
                path = string.Empty;
            else
                path = path.Substring(0, index);

            index = path.LastIndexOf("Scripts");
            if (index != -1)
            {
                if (index + "Scripts".Length == path.Length)
                    path = string.Empty;
                else
                    path = path.Substring(index + "Scripts".Length + 1);
            }

            path = path.Replace(Path.DirectorySeparatorChar, '.');

            var _namespace = path == string.Empty ? Application.productName : $"{Application.productName}.{path}";

            template = template.Replace("#NAMESPACE#", _namespace);

            File.WriteAllText(fullpath, template);
            AssetDatabase.Refresh();
        }
    }
}