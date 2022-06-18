using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Valve.Newtonsoft;
using Valve.Newtonsoft.Json;

public static class GameRipView {

    private static PrefabLoaderGameRipState _state;

    private static void InitState()
    {
        if (_state == null)
        {
            string fileName = "PrefabLoaderGameRipState.json";
            if (File.Exists(SaveState.GetStateFilePath(fileName)))
            {
                _state = SaveState.LoadStateFromFile<PrefabLoaderGameRipState>(fileName);
            }
            else
            {
                _state = new PrefabLoaderGameRipState(fileName);
            }
        }
    }

    public static void Draw()
    {
        InitState();

        if (GUILayout.Button("Select Select Game Rip", new GUILayoutOption[] { GUILayout.Height(35) }))
        {
            SelectNewGameRipFolder();
        }

        if (!string.IsNullOrEmpty(_state.SelectedPath))
        {
            GUILayout.Label("Selected Game Rip Folder : " + _state.SelectedPath);
        }
    }

    private static void SelectNewGameRipFolder()
    {
        string previousGameRipFolder = _state.SelectedPath;
        string gameRipPath = EditorUtility.OpenFolderPanel("Select Game Rip Folder", previousGameRipFolder, string.Empty);

        Debug.Log("Selected path: " + gameRipPath);

        if (!string.IsNullOrEmpty(gameRipPath))
        {
            if(_state.SelectedPath != gameRipPath)
            {
                PopulateFileIndexesForFolder(gameRipPath);
            }

            _state.SelectedPath = gameRipPath;
        }
    }


    private static void PopulateFileIndexesForFolder(string folderPath)
    {
        Debug.Log("Populating indexes for folder: " + folderPath);
        foreach(string dir in Directory.GetDirectories(folderPath))
        {
            PopulateFileIndexesForFolder(dir);
        }

        foreach(string metaFile in Directory.GetFiles(folderPath, "*.meta"))
        {
            string guid = GetGUIDFromMetaFile(metaFile);
            string realFile = metaFile.Replace(".meta", "");
            _state.GUIDToFilePath[guid] = realFile;
        }
    }

    private static string GetGUIDFromMetaFile(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);
        return lines.FirstOrDefault(o => o.Contains("guid: ")).Replace("guid: ", "").Trim();
    }

}
