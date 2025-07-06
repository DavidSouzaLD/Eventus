using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eventus.Editor
{
    public static class EditorUtils
    {
        private static Categories _cachedCategories;

        public static VisualTreeAsset LoadUXML(string fileName)
        {
            var guids = AssetDatabase.FindAssets($"{fileName} t:VisualTreeAsset");
            if (guids.Length == 0)
            {
                Debug.LogError($"UXML file '{fileName}.uxml' not found.");
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
        }

        public static Categories FindCategories()
        {
            if (_cachedCategories != null) return _cachedCategories;

            var guids = AssetDatabase.FindAssets("t:Categories");

            switch (guids.Length)
            {
                case 0:
                    Debug.LogError(
                        "[Eventus] CRITICAL: No 'Categories.asset' found in the project. Please create one via the menu: 'Assets > Create > Eventus > Categories'.");
                    return null;
                case > 1:
                    Debug.LogWarning(
                        "[Eventus] Multiple 'Categories.asset' files found. Using the first one. It is highly recommended to have only one registry per project to ensure consistency.");
                    break;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _cachedCategories = AssetDatabase.LoadAssetAtPath<Categories>(path);

            return _cachedCategories;
        }

        public static string FindChannelScriptPath()
        {
            var guids = AssetDatabase.FindAssets("Channel t:Script");
            return guids.Length == 0
                ? null
                : guids.Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(path => Path.GetFileName(path) == "Channel.cs");
        }
    }
}