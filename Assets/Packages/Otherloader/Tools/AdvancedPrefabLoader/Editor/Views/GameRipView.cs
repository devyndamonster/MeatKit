﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
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

        for (int i = 0; i < elements.Count; i++)
        {
            SerializedUnityElement element = elements[i];
            string elementType = element.GetElementType();

            Debug.Log("Loading element type: " + elementType);

            //If this is a prefab element, remove it (since element is being added to scene)
            if (elementType == "Prefab")
            {
                elements.RemoveAt(i);
                i -= 1;
                continue;
            }

            //If this is a monobehaviour we must patch the script references
            else if (elementType == "MonoBehaviour")
            {
                string scriptGUID = element.GetValueFromStruct(element.GetValue("m_Script"), "guid");
                if (_state.GUIDToFilePath.ContainsKey(scriptGUID))
                {
                    string scriptName = Path.GetFileNameWithoutExtension(_state.GUIDToFilePath[scriptGUID]);
                    Type type = PrefabPostProcess.GetTypeFromName(scriptName);
                    element.PatchScriptReference(type);
                }
            }

            element.ConvertFromPrefab();
        }

        //Get all dependancy elements and load them
        List<string> dependancyGUIDs = GetAllDependanciesFromElements(elements).Where(guid => _state.GUIDToFilePath.ContainsKey(guid)).ToList();

        //Load any assets this prefab depends on
        foreach (string dependancyGUID in dependancyGUIDs)
        {
            Debug.Log("Dependancy guid: " + dependancyGUID);
            Debug.Log("Dependancy path: " + _state.GUIDToFilePath[dependancyGUID]);

            foreach(SerializedUnityElement element in elements)
            {
                if(element.elementLines.Any(line => line.Contains(dependancyGUID)))
                {
                    string newGUID = LoadAssetFromGUID(dependancyGUID);
                    element.ReplaceText(dependancyGUID, newGUID);
                }
            }
        }

        CorrectElementFileIDs(elements, elements.First());
        PrefabPostProcess.AddUnityElementsToScene(elements);
    }


    private static List<string> GetAllDependanciesFromElements(List<SerializedUnityElement> elements)
    {
        List<string> dependancies = new List<string>();

        foreach (SerializedUnityElement element in elements)
        {
            foreach(string guid in element.GetDependancyGUIDs())
            {
                if (!dependancies.Contains(guid))
                {
                    dependancies.Add(guid);
                }
            }
        }

        return dependancies;
    }

    private static string LoadAssetFromGUID(string guid)
    {
        string assetPath = _state.GUIDToFilePath[guid];

        if (assetPath.EndsWith(".png")) return LoadSprite(assetPath);



        if (assetPath.EndsWith(".asset"))
        {

        }

        return guid;
    }

    private static string LoadSprite(string originalFilePath)
    {
        string imageName = Path.GetFileName(originalFilePath);
        string sceneAssetPath = EditorSceneManager.GetActiveScene().path;
        string sceneFolderPath = sceneAssetPath.Substring(0, sceneAssetPath.LastIndexOf('/'));
        string textureFolderPath = sceneFolderPath + "/" + "Textures";
        string copyAssetPath = textureFolderPath + "/" + imageName;
        string copyFilePath = GetRealFilePathFromAssetPath(copyAssetPath);

        if (!AssetDatabase.IsValidFolder(textureFolderPath)) AssetDatabase.CreateFolder(sceneFolderPath, "Textures");

        if (AssetDatabase.LoadAssetAtPath<Texture2D>(copyAssetPath) != null) return AssetDatabase.AssetPathToGUID(copyAssetPath);

        byte[] bytes = File.ReadAllBytes(originalFilePath);
        File.WriteAllBytes(copyFilePath, bytes);

        AssetDatabase.ImportAsset(copyAssetPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        return AssetDatabase.AssetPathToGUID(copyAssetPath);
    }

    private static string GetRealFilePathFromAssetPath(string assetPath)
    {
        string realFilePath = Application.dataPath + assetPath.Replace("Assets", "");
        return realFilePath;
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
