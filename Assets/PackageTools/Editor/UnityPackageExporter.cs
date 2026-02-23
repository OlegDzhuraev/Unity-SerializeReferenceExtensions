using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace InsaneOne.PackageTools.Editor
{
    public static class UnityPackageExporter
    {
        // The name of the unitypackage to output.
        const string PackageName = "SerializeReference-Extensions";

        // The path to the package under the `Assets/` folder.
        const string PackagePath = "InsaneOne";

        // Path to export to.
        const string ExportPath = "Build";

        const string SearchPattern = "*";
        const string PackageToolsFolderName = "PackageTools";
        const string ResourcesFolderName = "Resources";

        [MenuItem("Tools/SerializeReference Extensions/Export Package")]
        public static void Export ()
        {
            ExportPackage($"{ExportPath}/{PackageName}.unitypackage");
        }
		
        public static string ExportPackage (string exportPath)
        {
            var dir = new FileInfo(exportPath).Directory;
            if (dir != null && !dir.Exists)
                dir.Create();

            AssetDatabase.ExportPackage(
                GetAssetPaths(),
                exportPath,
                ExportPackageOptions.Default
            );

            return Path.GetFullPath(exportPath);
        }

        public static string[] GetAssetPaths ()
        {
            var path = Path.Combine(Application.dataPath,PackagePath);
            var assets = Directory.EnumerateFiles(path,SearchPattern,SearchOption.AllDirectories)
                .Where(x => !x.Contains(PackageToolsFolderName) && !x.Contains(ResourcesFolderName))
                .Select(x => "Assets" + x.Replace(Application.dataPath,"").Replace(@"\","/"))
                .ToArray();
            return assets;
        }
    }
}