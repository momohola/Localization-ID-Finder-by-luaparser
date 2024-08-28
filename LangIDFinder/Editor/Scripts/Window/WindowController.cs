using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace LangIDFinder.Editor.Scripts
{
    public class WindowController : OdinEditorWindow
    {
        private static string prefabPath = "/UI/Prefabs";
        private static string luaCodePath = "/Lua";
        [MenuItem("Tools/LangID查找器 &i")]
        public static void WindowStart()
        {
            WindowController win = WindowUtil.GetWindow<WindowController>("LangID查找器",0,0,750,620);
        }
        
        [HorizontalGroup("Input")]
        public string langID;
        
        [HorizontalGroup("Input")]
        [Button("从CSV文件导入")]
        public void ImportFromCSV()
        {
            string excelPath = Util.SelectInExplore("LangIDFinder_ImportFromExcel");
            if (Path.GetExtension(excelPath).Equals(".csv"))
            {
                var dt = Util.OpenCSV(excelPath);
                foreach (DataRow row in dt.Rows)
                {
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        string value = row[i].ToString();
                        langID = langID + value + ";";
                    }
                }
            }
        }

        [Space]
        [TableList(IsReadOnly = true, AlwaysExpanded = true)]
        public List<LangIDSearchResult> LangIDSearchResults = new List<LangIDSearchResult>();
        
        [Button("查找")]
        public void SearchLangID()
        {
            if (langID == null || langID.Equals(""))
            {
                ShowNotification(new GUIContent("请输入LangID"));
                return;
            }
            
            // 将输入字符串进行切割
            HashSet<int> langIDSet = new HashSet<int>(); 
            string[] langIDs = langID.Split(';');
            foreach (var id in langIDs)
            {
                if (!id.Equals("") && id.All(char.IsDigit))
                {
                    langIDSet.Add(int.Parse(id));
                }
            }

            LangIDSearchResults.Clear();
            string pPath = Application.dataPath + prefabPath;
            // 查找预制体
            Dictionary<int, Dictionary<string, List<string>>> prefabDic = PrefabFinder.Finder(pPath, langIDSet);
            // 查找lua代码
            Dictionary<int, Dictionary<string, List<int>>> luaDic = LuaCodeFinder.Finder(langIDSet);
            if (prefabDic.Count == 0 && luaDic.Count == 0)
            {
                ShowNotification(new GUIContent("未查找到id"));
            }
            else
            {
                DrawResultTable(langIDSet, prefabDic, luaDic);
            }
        }
        
        [Button("将结果导出为CSV文件")]
        public void Export2CSV()
        {
            if (LangIDSearchResults.Count == 0)
            {
                ShowNotification(new GUIContent("未查询到结果，无法导出"));
            }
            else
            {
                string excelPath = Util.OpenFolderPicker("LangIDFinder_Export2CSV");
                excelPath += "\\LangID.csv";
                DataTable dt = new DataTable("Sheet1");
                dt.Columns.Add("LangID");
                dt.Columns.Add("预制体");
                dt.Columns.Add("lua文件");
                dt.Columns.Add("lua行数");
                foreach (var item in LangIDSearchResults)
                {
                    List<DataRow> rows = new List<DataRow>();
                    foreach (var prefabInfo in item.PrefabInfo)
                    {
                        DataRow dr = dt.NewRow();
                        dr[1] = prefabInfo.prefabNodePath;
                        rows.Add(dr);
                    }

                    for (int i = 0; i < item.LuaInfo.Count; i++)
                    {
                        if (i < rows.Count)
                        {
                            rows[i][2] = item.LuaInfo[i].luaFileName;
                            rows[i][3] = item.LuaInfo[i].luaLine;
                        }
                        else
                        {
                            DataRow dr = dt.NewRow();
                            dr[2] = item.LuaInfo[i].luaFileName;
                            dr[3] = item.LuaInfo[i].luaLine;
                            rows.Add(dr);
                        }
                    }

                    foreach (var row in rows)
                    {
                        row[0] = item.langID;
                        dt.Rows.Add(row);
                    }
                    // 插入一个空行
                    DataRow spaceRow = dt.NewRow();
                    dt.Rows.Add(spaceRow);
                }                
                
                Util.SaveCSV(excelPath, dt);
                ShowNotification(new GUIContent("导出成功"));
            }
        }
        
        [Button("点击更新Lua代码索引")]
        [InfoBox("长时间(一周以上)未使用此工具，需要更新Lua代码索引文件，否则查找的结果会不准确。(打开工具快捷键：Alt+I)")]
        public void UpdateLuaCodeIndexFile()
        {
            var functionName = PlayerPrefs.GetString("LangIDFinderLangIDFunctionName", "lang.Get");
            LuaCodeFinder.UpdateLuaIndex(functionName);
        }
        

        [Button("设置")]
        public void OpenSettingWindow()
        { 
            SettingWindow.WindowStart();
        }
        
        private void DrawResultTable(HashSet<int> langIDSet, Dictionary<int,Dictionary<string, List<string>>> prefabDic,Dictionary<int, Dictionary<string, List<int>>> luaDic)
        {
            List<LangIDSearchResult> langIDTable = new List<LangIDSearchResult>();
            Dictionary<int, List<LangIDSearchResult>> tempTable = new Dictionary<int, List<LangIDSearchResult>>();
            foreach (int langID in langIDSet)
            {
                if (prefabDic.ContainsKey(langID) || luaDic.ContainsKey(langID))
                {
                    LangIDSearchResult tempLangIDSearchResult = new LangIDSearchResult();
                    tempLangIDSearchResult.langID = langID.ToString();
                    if (prefabDic.ContainsKey(langID))
                    {
                        List<PrefabView> tempPrefabView = new List<PrefabView>();
                        foreach (var fileInfo in prefabDic[langID])
                        {
                            Color color;
                            if (fileInfo.Value.Count == 1)
                            {
                                color = Color.white;
                            }
                            else
                            {
                                color = Color.cyan;
                            }
                            foreach (var nodePath in fileInfo.Value)
                            {
                                PrefabView temp = new PrefabView();
                                temp.prefabNodePath = nodePath;
                                temp.PrefabPath = fileInfo.Key;
                                temp.Color = color;
                                tempPrefabView.Add(temp);
                            }
                        }

                        tempLangIDSearchResult.PrefabInfo = tempPrefabView;
                    }

                    if (luaDic.ContainsKey(langID))
                    {
                        List<LuaCode> luaCodeList = new List<LuaCode>();
                        foreach (var fileInfo in luaDic[langID])
                        {
                            Color color;
                            if (fileInfo.Value.Count == 1)
                            {
                                color = Color.white;
                            }
                            else
                            {
                                color = Color.yellow;
                            }
                            
                            // fileInfo key:文件名   value：这个文件中那些行数调用了当前的langid
                            foreach (var callIndex in fileInfo.Value)
                            {
                                LuaCode temp = new LuaCode();
                                temp.luaLine = callIndex.ToString();
                                temp.luaFileName = fileInfo.Key;
                                temp.Color = color;
                                luaCodeList.Add(temp);
                            }
                        }

                        tempLangIDSearchResult.LuaInfo = luaCodeList;
                    }
                    
                    langIDTable.Add(tempLangIDSearchResult);
                }
            }
            LangIDSearchResults = langIDTable;
        }

        private int LastColorCode = 0;
    }
    

    [Serializable]
    public class LangIDSearchResult
    {
        [TableColumnWidth(70,Resizable = false)]
        public string langID;
        
        [HideLabel]
        [ReadOnly]
        [TableList(IsReadOnly = true, AlwaysExpanded = false)]
        public List<PrefabView> PrefabInfo;
        
        
        [HideLabel]
        [ReadOnly]
        [TableList(IsReadOnly = true, AlwaysExpanded = false)]
        public List<LuaCode> LuaInfo= new List<LuaCode>();
    }

    [Serializable]
    public class LuaCode
    {
        public Color Color
        {
            get;
            set;
        }
        
        [HideLabel]
        [GUIColor("@this.Color")]
        public string luaFileName;
        
        [InlineButton("OpenCode", "打开")]
        [HideLabel]
        public string luaLine;
        
        public void OpenCode()
        {
            string path = Util.FindFilePath(Util.GetFileNameByPath(luaFileName));
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<Object>(path));
        }
    }
    
    [Serializable]
    public class PrefabView
    {
        public string PrefabPath
        {
            get;
            set;
        }

        public Color Color
        {
            get;
            set;
        }
        
        [HideLabel]
        [InlineButton("OpenPrefab", "打开")]
        [GUIColor("@this.Color")]
        public string prefabNodePath;
        
        public void OpenPrefab()
        {
            if (PrefabPath == null)
            {
                EditorUtility.DisplayDialog("Error", "预制提路径为空！", "OK");
                return;
            }
            
            GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            AssetDatabase.OpenAsset(uiPrefab);
        }
    }
}





