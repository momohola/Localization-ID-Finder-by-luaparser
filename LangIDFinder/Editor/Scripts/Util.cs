using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Application = UnityEngine.Application;

namespace LangIDFinder.Editor.Scripts
{
    public class Util
    {
        /// <summary>
        /// 获取指定路径下所有预制体的路径
        /// </summary>
        /// <param name="dirPath">文件夹的相对路径</param>
        /// <returns>预制体路径列表</returns>
        public static List<string> GetAllPrefabDir(string dirPath)
        {
            List<string> fileList = AccessAllFileAndDir(dirPath, ".prefab");
            return fileList;
        }
        
        /// <summary>
        /// 遍历指定目录下的所有指定扩展名的文件路径
        /// </summary>
        /// <param name="dirPath">路径</param>
        /// <param name="extension">文件扩展名</param>
        /// <returns>文件的相对路径</returns>
        public static List<string> AccessAllFileAndDir(string dirPath, string extension)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(dirPath);
            FileSystemInfo[] fileSystemInfos = directoryInfo.GetFileSystemInfos("*",SearchOption.AllDirectories);
            List<string> result = new List<string>();
            for (int i = 0; i < fileSystemInfos.Length; i++)
            {
                FileSystemInfo fileSystemInfo = fileSystemInfos[i];
                if (fileSystemInfo.Attributes != FileAttributes.Directory && fileSystemInfo.Extension.Equals(extension))
                {
                    result.Add("Assets" + GetRelativePath(fileSystemInfo.FullName));
                }
            }

            return result;
        }

        /// <summary>
        /// 获取相对路径
        /// </summary>
        /// <param name="path">绝对路径</param>
        /// <returns>相对路径</returns>
        public static string GetRelativePath(string path)
        {
            // 获取当前工作目录的全路径
            string currentDirectory = Application.dataPath;
        
            // 使用 Path 类的 GetFullPath 方法来确保路径正确，并获取绝对路径
            string fullCurrentDirectory = Path.GetFullPath(currentDirectory);
            string fullPathToFile = Path.GetFullPath(path);
        
            // 计算相对路径
            return fullPathToFile.Substring(fullCurrentDirectory.Length);
        }
        
        /// <summary>
        /// 输出子节点的路径
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="splitter"></param>
        /// <returns></returns>
        public static string GetRoute(Transform transform, string splitter = "/")
        {
            var result = transform.name;
            var parent = transform.parent;
            while (parent != null)
            {
                result = $"{parent.name}{splitter}{result}";
                parent = parent.parent;
            }
            return result;
        }

        /// <summary>
        /// 通过路径获取文件名
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns></returns>
        public static string GetFileNameByPath(string path)
        {
            string fileName = path.Split('\\').Last();
            if (fileName.Contains("."))
            {
                return fileName.Split('.')[0];
            }
            else
            {
                return fileName;
            }
        }
        
        /// <summary>
        /// 通过文件名获取资源路径
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string FindFilePath(string fileName)
        {
            // 获取所有资源路径
            string[] guids = AssetDatabase.FindAssets(fileName);
        
            if (guids.Length > 0)
            {
                foreach (var guid in guids)
                {
                    // 根据 guid 获取文件的路径
                    string filePath = AssetDatabase.GUIDToAssetPath(guid);
                    return filePath;
                }
            }
            else
            {
                Debug.Log("未找到指定文件");
            }
            return null;
        }

        /// <summary>
        /// 选择文件
        /// </summary>
        /// <param name="flag">本地存储标记</param>
        /// <returns></returns>
        public static string SelectInExplore(string flag)
        {
            
            //记录上次选择目录
            string folderPath = PlayerPrefs.GetString(flag);
            string searchPath = EditorUtility.OpenFilePanel("select path", folderPath, "");
 
            if (!searchPath.Equals(""))
            {
                PlayerPrefs.SetString(flag,searchPath);
                // PlayerPrefs.Save();
                return searchPath;
            }

            return "";
        }
        
        public static string OpenFolderPicker(string flag)
        {
            string folderPath = PlayerPrefs.GetString(flag);
            // 打开文件夹选择窗口
            string path = EditorUtility.OpenFolderPanel("选择文件夹", folderPath, "");

            // 检查用户是否选择了一个文件夹
            if (!string.IsNullOrEmpty(path))
            {
                PlayerPrefs.SetString(flag,path);
                return path;
            }
            else
            {
                return "";
            }
        }
        
        /// <summary>
        /// 将DataTable中数据写入到CSV文件中
        /// </summary>
        /// <param name="filePath">路径</param>
        /// <param name="dt"></param>
        public static void SaveCSV(string filePath,DataTable dt)
        {
            FileInfo fi = new FileInfo(filePath);
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }
            else
            {
                fi.Delete();
                fi.Directory.Create();
            }
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8))
                {
                    string data = "";
                    //写入表头
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        data += dt.Columns[i].ColumnName.ToString();
                        if (i < dt.Columns.Count - 1)
                        {
                            data += ",";
                        }
                    }
                    sw.WriteLine(data);
                    //写入每一行每一列的数据
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        data = "";
                        for (int j = 0; j < dt.Columns.Count; j++)
                        {
                            string str = dt.Rows[i][j].ToString();
                            data += str;
                            if (j < dt.Columns.Count - 1)
                            {
                                data += ",";
                            }
                        }
                        sw.WriteLine(data);
                    }
                    sw.Close();
                    fs.Close();
                }
            }
        }
        
        /// <summary>
        /// 读取CSV文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static DataTable OpenCSV(string filePath)
        {
            DataTable dt = new DataTable();
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                {
                    //记录每次读取的一行记录
                    string strLine = "";
                    //记录每行记录中的各字段内容
                    string[] aryLine = null;
                    string[] tableHead = null;
                    //标示列数
                    int columnCount = 0;
                    //标示是否是读取的第一行
                    bool IsFirst = true;
                    //逐行读取CSV中的数据
                    while ((strLine = sr.ReadLine()) != null)
                    {
                        if (IsFirst == true)
                        {
                            tableHead = strLine.Split(',');
                            IsFirst = false;
                            columnCount = tableHead.Length;
                            //创建列
                            for (int i = 0; i < columnCount; i++)
                            {
                                DataColumn dc = new DataColumn(tableHead[i]);
                                dt.Columns.Add(dc);
                            }
                        }
                        else
                        {
                            aryLine = strLine.Split(',');
                            DataRow dr = dt.NewRow();
                            for (int j = 0; j < columnCount; j++)
                            {
                                dr[j] = aryLine[j];
                            }
                            dt.Rows.Add(dr);
                        }
                    }
                    if (aryLine != null && aryLine.Length > 0)
                    {
                        dt.DefaultView.Sort = tableHead[0] + " " + "asc";
                    }
                    sr.Close();
                    fs.Close();
                    return dt;
                }
            }
        }
        
        
        
        
        
        
        
        
    }
}