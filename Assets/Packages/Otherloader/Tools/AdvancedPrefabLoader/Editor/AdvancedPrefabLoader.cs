using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using FistVR;
using HarmonyLib;
using System.Reflection;
using System.Linq;
using AssetsTools.NET.Extra;
using System.Collections.Generic;


public class AdvancedPrefabLoader : EditorWindow
{
    private Vector2 scrollPos;
    
    [MenuItem("Tools/Advanced Prefab Loader")]
    private static void Init()
    {
        GetWindow<AdvancedPrefabLoader>("AdvPrefabLoader").Show();
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(Screen.width), GUILayout.Height(Screen.height - 50));

        DrawFileModeSelector();

        if(PrefabLoaderState.Instance.PrefabLoaderFileMode == PrefabLoaderFileType.AssetBundle)
        {
            AssetBundleView.Draw();
        }
        else
        {
            GameRipView.Draw();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawFileModeSelector()
    {
        PrefabLoaderState.Instance.PrefabLoaderFileMode = (PrefabLoaderFileType)GUILayout.Toolbar((int)PrefabLoaderState.Instance.PrefabLoaderFileMode, Enum.GetNames(typeof(PrefabLoaderFileType)));
    }
}
