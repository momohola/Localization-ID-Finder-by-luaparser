using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;


namespace LangIDFinder.Editor.Scripts
{
    public static class LuaCodeFinder
    {
        private static string pyLuaCodeFinder = "luaCodeLangIDFinder";
        private static string luaFinderIndex = "luaFinderIndex";
        
        public static void UpdateLuaIndex(string functionName)
        {
            string path =Application.dataPath + Util.FindFilePath(pyLuaCodeFinder).Replace("Assets","");
            string projectLuaPath = Application.dataPath + "/Lua";
            string outputPath = Path.GetDirectoryName(path) + "\\cacheFile\\";
            string command = path + " " + projectLuaPath.Replace("/", "\\") + " " + outputPath + luaFinderIndex+".json" + " " + functionName;
            // string command = "ipconfig";
            ExecuteCommandAsync(command);
            Debug.Log(command);
        }

        public static Dictionary<int, Dictionary<string, List<int>>> Finder(HashSet<int> langIDSet)
        {
            // 反序列化
            string luaFinderIndexPath = Util.FindFilePath(luaFinderIndex);
            if (luaFinderIndexPath == null || luaFinderIndexPath.Equals(""))
            {
                return null;
            }
            
            string luaCallLang = ReadFile(luaFinderIndexPath);
            var luaFileInfo = JsonConvert.DeserializeObject<Dictionary<string, LuaFileInfo>>(luaCallLang);
            Dictionary<int, Dictionary<string, List<int>>> resDic = new Dictionary<int, Dictionary<string, List<int>>>();
            foreach (var item in luaFileInfo)   //遍历文件
            {
                foreach (var luaLangCallIndex in item.Value.langFuncList)            //遍历每一个函数
                {
                    //luaLangCallIndex key:文件名  value：luaLangCallIndex
                    foreach (var args in luaLangCallIndex.langFuncArgs)         //遍历每一个函数的参数
                    {
                        // 如果找到了
                        if (langIDSet.Contains(args))
                        {
                            if (!resDic.ContainsKey(args))
                            {
                                resDic[args] = new Dictionary<string, List<int>>();
                            }
            
                            if (!resDic[args].ContainsKey(item.Key))
                            {
                                resDic[args][item.Key] = new List<int>();
                            }
                            
                            resDic[args][item.Key].Add(luaLangCallIndex.langFuncLine);
                        }
                    }
                }
            }
            return resDic;
        }

        private static string ReadFile(string path)
        {
            string str1 = File.ReadAllText(path);
            return str1;
        }

        public static void ExecuteCommandAsync(string command)
        {
            Thread thread = new Thread(() => ExecuteCommand(command));
            thread.Start();
        }

        private static void ExecuteCommand(string command)
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/K " + command,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = true,
                CreateNoWindow = false,
            };

            process.StartInfo = startInfo;

            // 启动进程
            process.Start();
        }
    }
    
    public class LuaLangCallIndex
    {
        public int langFuncLine { get; set; }
        public int[] langFuncArgs { get; set; }
    }
    
    public class LuaFileInfo
    {
        public string md5 { get; set; }
        public LuaLangCallIndex[] langFuncList { get; set; }
    }
}

