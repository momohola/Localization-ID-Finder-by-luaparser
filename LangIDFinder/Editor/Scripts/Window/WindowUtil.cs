using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace LangIDFinder.Editor.Scripts
{
    public class WindowUtil
    {
        public static T GetWindow<T>(string windowName, int x = 200, int y = 200, int width = 400, int height = 500) where T: EditorWindow
        {
            T win = EditorWindow.GetWindowWithRect<T>(new Rect(x, y, width, height));
            win.titleContent = new GUIContent(windowName);
            win.Show();
            return win;
        }
    }
}