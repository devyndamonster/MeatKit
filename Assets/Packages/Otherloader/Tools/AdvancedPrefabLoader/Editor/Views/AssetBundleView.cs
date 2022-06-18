using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AssetBundleView {

    private static List<string> visibleFolders = new List<string>();

    private static PrefabLoaderAssetBundleState _state;

    private static void InitState()
    {
        if (_state == null)
        {
            string fileName = "PrefabLoaderAssetBundleState.json";
            if (File.Exists(SaveState.GetStateFilePath(fileName)))
            {
                _state = SaveState.LoadStateFromFile<PrefabLoaderAssetBundleState>(fileName);
            }
            else
            {
                _state = new PrefabLoaderAssetBundleState(fileName);
            }
        }
    }

    public static void Draw()
    {
        InitState();

        if (GUILayout.Button("Select Asset Bundle", new GUILayoutOption[] { GUILayout.Height(35) }))
        {
            SelectNewAssetBundle();
        }

        if (_state.AssetNames.Count() > 0)
        {
            GUILayout.Label("Selected Bundle : " + Path.GetFileName(_state.SelectedPath));

            HorizontalLine.Draw();
            DrawSpawmModeSelector();
            DrawSpawnSettings();
            HorizontalLine.Draw();
            DrawFavorites();
            HorizontalLine.Draw();

            if (_state.CurrentAssetPath.Contains("/") && GUILayout.Button("<- Go Back", new GUILayoutOption[] { GUILayout.Height(35) }))
            {
                _state.CurrentAssetPath = _state.CurrentAssetPath.Substring(0, _state.CurrentAssetPath.LastIndexOf("/"));
            }

            DrawPrefabSpawnButtons();
        }
    }

    private static void SelectNewAssetBundle()
    {
        string previousAssetBundleFolder = "";
        try
        {
            previousAssetBundleFolder = Directory.GetParent(_state.SelectedPath).FullName;
        }
        catch (Exception e) { }
        
        string assetBundlePath = EditorUtility.OpenFilePanel("Select Asset Bundle", previousAssetBundleFolder, string.Empty);

        if (!string.IsNullOrEmpty(assetBundlePath))
        {
            AssetBundle _bundle = AssetBundle.LoadFromFile(assetBundlePath);

            if (_bundle != null)
            {
                _state.SetSelectedAssetBundle(assetBundlePath, _bundle.GetAllAssetNames());
                _bundle.Unload(true);
            }
        }
    }

    private static void DrawSpawmModeSelector()
    {
        _state.CurrentSpawnMode = (PrefabLoaderSpawnMode)GUILayout.Toolbar((int)_state.CurrentSpawnMode, Enum.GetNames(typeof(PrefabLoaderSpawnMode)));
    }

    private static void DrawSpawnSettings()
    {
        if (_state.CurrentSpawnMode == PrefabLoaderSpawnMode.DeepCopy)
        {
            DrawDeepCopySettings();
        }
    }

    private static void DrawDeepCopySettings()
    {
        _state.RipMeshes = EditorGUILayout.Toggle("Rip Meshes On Spawn", _state.RipMeshes);
    }

    private static void DrawFavorites()
    {
        GUILayout.Label("Favorites");

        GUIStyle leftButtonStyle = new GUIStyle(GUI.skin.button);
        leftButtonStyle.alignment = TextAnchor.MiddleLeft;

        for (int i = 0; i < _state.Favorites.Count(); i++)
        {
            FavoritedAsset favorite = _state.Favorites[i];

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(favorite.AssetName.Substring(favorite.AssetName.LastIndexOf('/') + 1), leftButtonStyle, new GUILayoutOption[] { GUILayout.Height(25) }))
            {
                SpawnAssetFromBundle(favorite.AssetName, favorite.AssetBundlePath);
            }
            if (GUILayout.Button("Unfavorite", new GUILayoutOption[] { GUILayout.Height(25), GUILayout.Width(100) }))
            {
                _state.UnfavoritePrefab(favorite.AssetName, favorite.AssetBundlePath);
                i -= 1;
            }

            GUILayout.EndHorizontal();
        }
    }

    private static void SpawnAssetFromBundle(string assetName, string bundlePath)
    {
        AssetBundle _bundle = AssetBundle.LoadFromFile(bundlePath);
        UnityEngine.Object spawned = GameObject.Instantiate(_bundle.LoadAsset(assetName));
        _bundle.Unload(false);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        if (spawned is GameObject && _state.CurrentSpawnMode == PrefabLoaderSpawnMode.DeepCopy)
        {
            PrefabPostProcess.ProcessSpawnedObject((GameObject)spawned, _state);
        }

        EditorGUIUtility.ExitGUI();
    }

    private static void DrawPrefabSpawnButtons()
    {
        visibleFolders.Clear();

        EditorGUILayout.BeginVertical();

        GUIStyle leftButtonStyle = new GUIStyle(GUI.skin.button);
        leftButtonStyle.alignment = TextAnchor.MiddleLeft;

        Color folderColor = new Color(220f / 255, 220f / 255, 220f / 255);

        for (int i = 0; i < _state.AssetNames.Length; i++)
        {
            string prefabName = _state.AssetNames[i];

            if (string.IsNullOrEmpty(_state.CurrentAssetPath))
            {
                _state.CurrentAssetPath = prefabName.Split('/')[0];
            }

            if (IsAssetVisible(prefabName))
            {
                if (IsAssetSpawnable(prefabName))
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(prefabName.Substring(prefabName.LastIndexOf('/') + 1), leftButtonStyle, new GUILayoutOption[] { GUILayout.Height(25) }))
                    {
                        SpawnAssetFromBundle(prefabName, _state.SelectedPath);
                    }

                    if (_state.IsFavoritede(prefabName, _state.SelectedPath))
                    {
                        if (GUILayout.Button("Unfavorite", new GUILayoutOption[] { GUILayout.Height(25), GUILayout.Width(100) }))
                        {
                            _state.UnfavoritePrefab(prefabName, _state.SelectedPath);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Favorite", new GUILayoutOption[] { GUILayout.Height(25), GUILayout.Width(100) }))
                        {
                            _state.FavoritePrefab(prefabName, _state.SelectedPath);
                        }
                    }

                    GUILayout.EndHorizontal();
                }

                else
                {
                    string folderPath = _state.CurrentAssetPath + "/" + prefabName.Replace(_state.CurrentAssetPath, "").Trim('/').Split('/')[0];
                    if (!visibleFolders.Contains(folderPath))
                    {
                        visibleFolders.Add(folderPath);

                        string folderName = folderPath.Substring(folderPath.LastIndexOf('/') + 1);

                        Color prevColor = GUI.backgroundColor;
                        GUI.backgroundColor = folderColor;
                        if (GUILayout.Button(folderName + " ->", leftButtonStyle, new GUILayoutOption[] { GUILayout.Height(25) }))
                        {
                            _state.CurrentAssetPath = folderPath;
                        }
                        GUI.backgroundColor = prevColor;
                    }
                }
            }
        }

        EditorGUILayout.EndVertical();
    }

    private static bool IsAssetSpawnable(string prefabName)
    {
        string remainingPath = prefabName.Replace(_state.CurrentAssetPath, "").Trim('/');
        return !remainingPath.Contains("/");
    }

    private static bool IsAssetVisible(string prefabName)
    {
        return prefabName.Contains(_state.CurrentAssetPath);
    }
}
