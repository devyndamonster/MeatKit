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

namespace Tools.AdvancedPrefabLoader
{
    public class AdvancedPrefabLoader : EditorWindow
    {
        private Vector2 scrollPos;
        private List<string> visibleFolders = new List<string>();

        [MenuItem("Tools/Advanced Prefab Loader")]
        private static void Init()
        {
            GetWindow<AdvancedPrefabLoader>("AdvPrefabLoader").Show();
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(Screen.width), GUILayout.Height(Screen.height - 50));

            if (GUILayout.Button("Select Asset Bundle", new GUILayoutOption[] { GUILayout.Height(35) }))
            {
                SelectNewAssetBundle();
            }

            if(PrefabLoaderState.Instance.AssetNames.Count() > 0)
            {
                GUILayout.Label("Selected Bundle : " + Path.GetFileName(PrefabLoaderState.Instance.SelectedBundlePath));

                DrawHorizontalLine();
                DrawSpawmModeSelector();
                DrawHorizontalLine();
                DrawFavorites();
                DrawHorizontalLine();

                if (PrefabLoaderState.Instance.CurrentAssetPath.Contains("/") && GUILayout.Button("<- Go Back", new GUILayoutOption[] { GUILayout.Height(35) }))
                {
                    PrefabLoaderState.Instance.CurrentAssetPath = PrefabLoaderState.Instance.CurrentAssetPath.Substring(0, PrefabLoaderState.Instance.CurrentAssetPath.LastIndexOf("/"));
                }

                DrawPrefabSpawnButtons();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPrefabSpawnButtons()
        {
            visibleFolders.Clear();

            EditorGUILayout.BeginVertical();
            
            GUIStyle leftButtonStyle = new GUIStyle(GUI.skin.button);
            leftButtonStyle.alignment = TextAnchor.MiddleLeft;

            Color folderColor = new Color(220f / 255, 220f / 255, 220f / 255);

            for (int i = 0; i < PrefabLoaderState.Instance.AssetNames.Length; i++)
            {
                string prefabName = PrefabLoaderState.Instance.AssetNames[i];

                if (string.IsNullOrEmpty(PrefabLoaderState.Instance.CurrentAssetPath))
                {
                    PrefabLoaderState.Instance.CurrentAssetPath = prefabName.Split('/')[0];
                }

                if (IsAssetVisible(prefabName))
                {
                    if (IsAssetSpawnable(prefabName))
                    {
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(prefabName.Substring(prefabName.LastIndexOf('/') + 1), leftButtonStyle, new GUILayoutOption[] { GUILayout.Height(25) }))
                        {
                            SpawnAssetFromBundle(prefabName, PrefabLoaderState.Instance.SelectedBundlePath);
                        }

                        if (PrefabLoaderState.Instance.IsFavorited(prefabName, PrefabLoaderState.Instance.SelectedBundlePath))
                        {
                            if (GUILayout.Button("Unfavorite", new GUILayoutOption[] { GUILayout.Height(25), GUILayout.Width(100) }))
                            {
                                PrefabLoaderState.Instance.UnfavoritePrefab(prefabName, PrefabLoaderState.Instance.SelectedBundlePath);
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("Favorite", new GUILayoutOption[] { GUILayout.Height(25), GUILayout.Width(100) }))
                            {
                                PrefabLoaderState.Instance.FavoritePrefab(prefabName, PrefabLoaderState.Instance.SelectedBundlePath);
                            }
                        }

                        GUILayout.EndHorizontal();
                    }

                    else
                    {
                        string folderPath = PrefabLoaderState.Instance.CurrentAssetPath + "/" + prefabName.Replace(PrefabLoaderState.Instance.CurrentAssetPath, "").Trim('/').Split('/')[0];
                        if (!visibleFolders.Contains(folderPath))
                        {
                            visibleFolders.Add(folderPath);

                            string folderName = folderPath.Substring(folderPath.LastIndexOf('/') + 1);

                            Color prevColor = GUI.backgroundColor;
                            GUI.backgroundColor = folderColor;
                            if (GUILayout.Button(folderName + " ->", leftButtonStyle, new GUILayoutOption[] { GUILayout.Height(25) }))
                            {
                                PrefabLoaderState.Instance.CurrentAssetPath = folderPath;
                            }
                            GUI.backgroundColor = prevColor;
                        }
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSpawmModeSelector()
        {
            PrefabLoaderState.Instance.CurrentSpawnMode = (PrefabLoaderSpawnMode)GUILayout.Toolbar((int)PrefabLoaderState.Instance.CurrentSpawnMode, Enum.GetNames(typeof(PrefabLoaderSpawnMode)));
        }


        private void DrawFavorites()
        {
            GUILayout.Label("Favorites");

            GUIStyle leftButtonStyle = new GUIStyle(GUI.skin.button);
            leftButtonStyle.alignment = TextAnchor.MiddleLeft;

            for(int i = 0; i < PrefabLoaderState.Instance.Favorites.Count(); i++)
            {
                FavoritedAsset favorite = PrefabLoaderState.Instance.Favorites[i];

                GUILayout.BeginHorizontal();

                if (GUILayout.Button(favorite.AssetName.Substring(favorite.AssetName.LastIndexOf('/') + 1), leftButtonStyle, new GUILayoutOption[] { GUILayout.Height(25) }))
                {
                    SpawnAssetFromBundle(favorite.AssetName, favorite.AssetBundlePath);
                }
                if (GUILayout.Button("Unfavorite", new GUILayoutOption[] { GUILayout.Height(25), GUILayout.Width(100) }))
                {
                    PrefabLoaderState.Instance.UnfavoritePrefab(favorite.AssetName, favorite.AssetBundlePath);
                    i -= 1;
                }

                GUILayout.EndHorizontal();
            }
        }

        private bool IsAssetSpawnable(string prefabName)
        {
            string remainingPath = prefabName.Replace(PrefabLoaderState.Instance.CurrentAssetPath, "").Trim('/');
            return !remainingPath.Contains("/");
        }

        private bool IsAssetVisible(string prefabName)
        {
            return prefabName.Contains(PrefabLoaderState.Instance.CurrentAssetPath);
        }

        private void SelectNewAssetBundle()
        {
            string assetBundlePath = EditorUtility.OpenFilePanel("Select Asset Bundle", string.Empty, string.Empty);

            if (!string.IsNullOrEmpty(assetBundlePath))
            {
                AssetBundle _bundle = AssetBundle.LoadFromFile(assetBundlePath);

                if (_bundle != null)
                {
                    PrefabLoaderState.Instance.SetSelectedAssetBundle(assetBundlePath, _bundle.GetAllAssetNames());
                    _bundle.Unload(true);
                }
            }
        }

        private void SpawnAssetFromBundle(string assetName, string bundlePath)
        {
            AssetBundle _bundle = AssetBundle.LoadFromFile(bundlePath);

            UnityEngine.Object spawned = Instantiate(_bundle.LoadAsset(assetName));

            _bundle.Unload(false);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            if (spawned is GameObject && PrefabLoaderState.Instance.CurrentSpawnMode == PrefabLoaderSpawnMode.DeepCopy)
            {
                PrefabPostProcess.ProcessSpawnedObject((GameObject)spawned);
            }
        }

        private void DrawHorizontalLine()
        {
            EditorGUILayout.Space();
            var rect = EditorGUILayout.BeginHorizontal();
            Handles.color = Color.gray;
            Handles.DrawLine(new Vector2(rect.x - 15, rect.y), new Vector2(rect.width + 15, rect.y));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }
    }
}
