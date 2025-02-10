﻿
using UnityEditor;
using UnityEngine;

namespace FileRenamer.Styles
{
    public static class FileRenamerStyleGUI
    {
        #region Fields 

        public const int DefaultLineLength = 30;

        private static GUIStyle _redLabel;
        private static GUIStyle _greenLabel;
        private static GUIStyle _yellowLabel;

        #endregion


        #region Properties

        public static Vector2 WindowMinSize => new Vector2(400f, 250f);
        public static GUIStyle RedLabel => _redLabel ??= CreateStyle(Color.red);
        public static GUIStyle GreenLabel => _greenLabel ??= CreateStyle(Color.green);
        public static GUIStyle YellowLabel => _yellowLabel ??= CreateStyle(Color.yellow);

        public static readonly GUILayoutOption[] ButtonsLayouts = new GUILayoutOption[]
        {
            GUILayout.Width(150),
            GUILayout.Height(24),
        };

        public static readonly GUILayoutOption[] ToggleLabelLayouts = new GUILayoutOption[]
        {
            GUILayout.Width(175),
        };

        #endregion


        #region Methods

        public static void DrawUILine(Color color, float thickness = 2f)
        {
            EditorGUILayout.Space();
            Rect rect = EditorGUILayout.GetControlRect(false, thickness);
            EditorGUI.DrawRect(rect, color);
            EditorGUILayout.Space();
        }

        private static GUIStyle CreateStyle(Color color)
        {
            var style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = color;
            style.hover.textColor = color;
            return style;
        }

        #endregion
    }
}
