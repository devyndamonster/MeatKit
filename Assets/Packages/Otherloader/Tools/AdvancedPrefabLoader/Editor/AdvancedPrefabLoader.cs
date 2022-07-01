using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using FistVR;
using HarmonyLib;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;


public class AdvancedPrefabLoader : EditorWindow
{
    private Vector2 scrollPos;
    private PrefabLoaderState _state;
    
    [MenuItem("Tools/Advanced Prefab Loader")]
    private static void Init()
    {
        GetWindow<AdvancedPrefabLoader>("AdvPrefabLoader").Show();
    }

    private void InitState()
    {
        if (_state == null)
        {
            string fileName = "PrefabLoaderState.json";
            if (File.Exists(SaveState.GetStateFilePath(fileName)))
            {
                Debug.Log("File Name " + SaveState.GetStateFilePath(fileName));
                _state = SaveState.LoadStateFromFile<PrefabLoaderState>(fileName);
            }
            else
            {
                _state = new PrefabLoaderState(fileName);
            }
        } 
    }

    private void OnGUI()
    {
        InitState();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(Screen.width), GUILayout.Height(Screen.height - 50));

        //DrawFileModeSelector();

        if(_state.PrefabLoaderFileMode == PrefabLoaderFileType.AssetBundle)
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
        _state.PrefabLoaderFileMode = (PrefabLoaderFileType)GUILayout.Toolbar((int)_state.PrefabLoaderFileMode, Enum.GetNames(typeof(PrefabLoaderFileType)));
    }
}
