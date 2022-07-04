using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Valve.Newtonsoft;
using Valve.Newtonsoft.Json;

public static class GameRipView {

    private static List<string> visibleFolders = new List<string>();

    private static PrefabLoaderGameRipState _state;

    private static System.Random random = new System.Random();

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

            if (FolderHasParent(_state.CurrentPath) && GUILayout.Button("<- Go Back", new GUILayoutOption[] { GUILayout.Height(35) }))
            {
                _state.CurrentPath = _state.CurrentPath.Substring(0, _state.CurrentPath.LastIndexOf("/"));
            }

            DrawPrefabSpawnButtons();
        }
    }

    private static void SelectNewGameRipFolder()
    {
        string previousGameRipFolder = _state.SelectedPath;
        string gameRipPath = EditorUtility.OpenFolderPanel("Select Game Rip Folder", previousGameRipFolder, string.Empty);

        if (!string.IsNullOrEmpty(gameRipPath))
        {
            if(_state.SelectedPath != gameRipPath)
            {
                _state.GUIDToFilePath.Clear();
                _state.FilePathToGUID.Clear();
                _state.FolderToSubfiles.Clear();
                _state.CurrentPath = "";
                PopulateFileIndexesForFolder(gameRipPath);
            }

            _state.SelectedPath = gameRipPath;
        }
    }


    private static void PopulateFileIndexesForFolder(string folderPath)
    {
        folderPath = folderPath.Replace("\\", "/");

        _state.FolderToSubfiles[folderPath] = new List<string>();
        _state.FolderToSubfolders[folderPath] = new List<string>();

        foreach(string subFolder in Directory.GetDirectories(folderPath).Select(o => o.Replace("\\", "/")))
        {
            _state.FolderToSubfolders[folderPath].Add(subFolder);
            PopulateFileIndexesForFolder(subFolder);
        }

        foreach(string metaFile in Directory.GetFiles(folderPath, "*.meta"))
        {
            string guid = GetGUIDFromMetaFile(metaFile);
            string realFile = metaFile.Replace(".meta", "").Replace('\\', '/');
            _state.GUIDToFilePath[guid] = realFile;
            _state.FilePathToGUID[realFile] = guid;
            _state.FolderToSubfiles[folderPath].Add(realFile);
        }
    }

    private static string GetGUIDFromMetaFile(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);
        return lines.FirstOrDefault(o => o.Contains("guid: ")).Replace("guid: ", "").Trim();
    }

    private static bool FolderHasParent(string folder)
    {
        return !string.IsNullOrEmpty(folder) && _state.FolderToSubfolders.ContainsKey(folder.Substring(0, folder.LastIndexOf('/')));
    }

    private static void DrawPrefabSpawnButtons()
    {
        visibleFolders.Clear();

        EditorGUILayout.BeginVertical();

        GUIStyle leftButtonStyle = new GUIStyle(GUI.skin.button);
        leftButtonStyle.alignment = TextAnchor.MiddleLeft;

        Color folderColor = new Color(220f / 255, 220f / 255, 220f / 255);

        if (string.IsNullOrEmpty(_state.CurrentPath))
        {
            _state.CurrentPath = _state.FolderToSubfolders.First().Key;
        }

        Debug.Log("Current Path: " + _state.CurrentPath);
        foreach(string subFolder in _state.FolderToSubfolders[_state.CurrentPath])
        {
            string folderName = subFolder.Substring(subFolder.LastIndexOf('/') + 1);
            Color prevColor = GUI.backgroundColor;

            GUI.backgroundColor = folderColor;
            if (GUILayout.Button(folderName + " ->", leftButtonStyle, new GUILayoutOption[] { GUILayout.Height(25) }))
            {
                _state.CurrentPath = subFolder;
            }
            GUI.backgroundColor = prevColor;
        }

        foreach (string filePath in _state.FolderToSubfiles[_state.CurrentPath])
        {
            if (GUILayout.Button(filePath.Substring(filePath.LastIndexOf('/') + 1), leftButtonStyle, new GUILayoutOption[] { GUILayout.Height(25) }))
            {
                LoadAsset(filePath);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private static void LoadAsset(string path)
    {
        Debug.Log("Loading asset: " + path);

        if (path.EndsWith(".prefab"))
        {
            LoadPrefab(path);
        }

    }

    private static void LoadPrefab(string path)
    {
        Debug.Log("Loading Prefab");

        List<SerializedUnityElement> elements = GetElementsFromPrefab(path);
        Dictionary<string, SerializedUnityElement> fileIdToElement = new Dictionary<string, SerializedUnityElement>();

        for(int i = 0; i < elements.Count; i++)
        {
            SerializedUnityElement element = elements[i];
            string elementType = element.GetElementType();

            if (elementType == "Prefab")
            {
                Debug.Log("Removing prefab instance");
                elements.RemoveAt(i);
                i -= 1;
                continue;
            }

            else if (elementType == "MonoBehaviour")
            {
                Debug.Log("Guid: " + element.GetScriptGUID());
                string scriptGUID = element.GetScriptGUID();
                if (_state.GUIDToFilePath.ContainsKey(scriptGUID))
                {
                    string scriptName = Path.GetFileNameWithoutExtension(_state.GUIDToFilePath[element.GetScriptGUID()]);
                    Debug.Log("Script Name: " + scriptName);

                    Type type = PrefabPostProcess.GetTypeFromName(scriptName);
                    Debug.Log("Script Type: " + type);
                    element.PatchScriptReference(type);
                }
            }

            element.ConvertFromPrefab();
            fileIdToElement[element.GetFileID()] = element;
        }

        CorrectElementFileIDs(elements, elements.First());
        PrefabPostProcess.AddUnityElementsToScene(elements);
    }


    private static void CorrectElementFileIDs(List<SerializedUnityElement> elements, SerializedUnityElement gameObjectElement)
    {
        int newFileId = UnityEngine.Random.Range(1000, int.MaxValue - 1000);
        string gameObjectFileId = gameObjectElement.GetFileID();
        string transformFileId = "";

        Debug.Log("New Random Value: " + newFileId.ToString());

        //First, replace our base gameobject file id with the new one
        foreach(SerializedUnityElement element in elements)
        {
            element.ReplaceFileIds(gameObjectFileId, newFileId.ToString());
        }

        //Now, go through every component and replace those child Ids
        foreach(string componentFileId in gameObjectElement.GetComponentFileIds())
        {
            SerializedUnityElement component = elements.FirstOrDefault(o => o.GetFileID() == componentFileId);
            newFileId += 1;

            Debug.Log("Added to Random Value: " + newFileId.ToString());

            foreach (SerializedUnityElement element in elements)
            {
                element.ReplaceFileIds(componentFileId, newFileId.ToString());
            }

            if (component.GetElementType() == "Transform" || component.GetElementType() == "RectTransform")
            {
                transformFileId = newFileId.ToString();
            }
        }

        //Finally go through children elements
        foreach (string childFileIds in elements.FirstOrDefault(o => o.GetFileID() == transformFileId).GetTransformChildrenFileIds())
        {
            string childGameObjectFileId = elements.FirstOrDefault(o => o.GetFileID() == childFileIds).GetGameObjectFileId();
            SerializedUnityElement childGameObjectElement = elements.FirstOrDefault(o => o.GetFileID() == childGameObjectFileId);
            CorrectElementFileIDs(elements, childGameObjectElement);
        }
    }

    private static List<SerializedUnityElement> GetElementsFromPrefab(string path)
    {
        string[] fileLines = File.ReadAllLines(path);
        List<SerializedUnityElement> elements = new List<SerializedUnityElement>();

        foreach (string line in fileLines)
        {
            if (line.StartsWith("---"))
            {
                elements.Add(new SerializedUnityElement());
            }

            if (elements.Count > 0)
            {
                elements.Last().elementLines.Add(line);
            }
        }

        return elements;
    }

    


}
