using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using War.UI;

namespace LangIDFinder.Editor.Scripts
{
    public static class PrefabFinder
    {
        public static Dictionary<int ,Dictionary<string, List<string>>> Finder(string prefabPath, HashSet<int> langIDSet)
        {
            List<string> allPrefabDir = Util.GetAllPrefabDir(prefabPath);
            Dictionary<int ,Dictionary<string, List<string>>> resDic = new Dictionary<int ,Dictionary<string, List<string>>>();
            foreach (var dir in allPrefabDir)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(dir);
                if (prefab != null)
                {
                    LocalizationText[] localizeTextList = prefab.GetComponentsInChildren<LocalizationText>(true);
                    foreach (var localizeText in localizeTextList)
                    {
                        int compLangID = localizeText.TextKey;
                        // 如果找到了
                        if (langIDSet.Contains(compLangID))
                        {
                            if (!resDic.ContainsKey(compLangID))
                            {
                                resDic[compLangID] = new Dictionary<string, List<string>>();
                            }

                            if (!resDic[compLangID].ContainsKey(dir))
                            {
                                resDic[compLangID][dir] = new List<string>();
                            }
                            
                            resDic[compLangID][dir].Add(Util.GetRoute(localizeText.transform));
                        }
                    }
                }
            }

            return resDic;
        }
    }
}