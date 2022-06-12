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

namespace Tools.AdvancedPrefabLoader
{
    public class GameObjectTester : EditorWindow
    {
        public GameObject SelectedGameObject;

        [MenuItem("Tools/GameObjectTester")]
        private static void Init()
        {
            GetWindow<GameObjectTester>().Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Process GameObject", EditorStyles.boldLabel);
            SelectedGameObject = EditorGUILayout.ObjectField(SelectedGameObject, typeof(GameObject), true) as GameObject;

            if (SelectedGameObject != null && GUILayout.Button("Process GameObject"))
            {
                PrefabPostProcess.ProcessSpawnedObject(SelectedGameObject);
            }
        }
    }
}
