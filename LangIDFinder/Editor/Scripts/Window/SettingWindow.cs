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
    public class SettingWindow : OdinEditorWindow
    {
        [LabelText("国际化函数名")]
        public string functionName;
        
        public static SettingWindow WindowStart()
        {
            SettingWindow win = WindowUtil.GetWindow<SettingWindow>("LangID查找器_设置",0,0,250,150);
            return win;
        }

        private void OnEnable()
        {
            functionName = PlayerPrefs.GetString("LangIDFinderLangIDFunctionName", "lang.Get");
        }
        
        private void OnDestroy()
        {
            PlayerPrefs.SetString("LangIDFinderLangIDFunctionName", functionName);
        }
    }
}





