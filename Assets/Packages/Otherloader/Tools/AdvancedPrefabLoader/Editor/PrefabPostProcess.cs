using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.IO;

namespace Tools.AdvancedPrefabLoader
{
    public class PrefabPostProcess
    {
        private static int GetFileIDForObject(UnityEngine.Object spawnedObject)
        {
            PropertyInfo inspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
            SerializedObject serializedObject = new SerializedObject(spawnedObject);
            inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);
            SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");

            return localIdProp.intValue;
        }

        public static void LogScriptMeta(GameObject spawned)
        {
            Debug.Log("Gameobject FileID: " + GetFileIDForObject(spawned));

            foreach (var component in spawned.GetComponents<Component>())
            {
                Debug.Log("Component: " + component.GetType());
                Debug.Log("Computed FileID: " + FileIDUtil.Compute(component.GetType()));
                Debug.Log("Component FileID: " + GetFileIDForObject(component));
            }
        }

        public static void ProcessSpawnedObject(GameObject spawned)
        {
            //Debug.Log("---Script Meta Before Save---");
            //LogScriptMeta(spawned);

            SaveScene();

            //Debug.Log("---Script Meta After Save---");
            //LogScriptMeta(spawned);

            Debug.Log(GetSceneFilePath());
            List<int> fileIDs = GetMonoBehaviourFileIDs(spawned);
            PrintSceneFile();   
        }

        public static List<int> GetMonoBehaviourFileIDs(GameObject spawned)
        {
            List<int> fileIDs = new List<int>();

            foreach (var component in spawned.GetComponentsInChildren<MonoBehaviour>())
            {
                Debug.Log("MonoBehavior: " + component.GetType());

                int fileID = GetFileIDForObject(component);
                Debug.Log("MonoBehavior FileID: " + fileID);
                Debug.Log("MonoBehavior Assembly Name: " + GetAssemblyNameForObject(component));
                Debug.Log("GUID of assembly: " + AssetDatabase.AssetPathToGUID("Assets/MeatKit/Managed/" + GetAssemblyNameForObject(component) + ".dll"));
                Debug.Log("Computed FileID for script: " + FileIDUtil.Compute(component.GetType()));
                fileIDs.Add(fileID);
            }

            return fileIDs;
        }

        public static void SaveScene()
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        public static string GetSceneFilePath()
        {
            return Path.Combine(Path.GetDirectoryName(Application.dataPath), EditorSceneManager.GetActiveScene().path);
        }

        public static string GetAssemblyNameForObject(UnityEngine.Object targetObject)
        {
            return targetObject.GetType().Assembly.GetName().Name;
        }

        public static void PrintSceneFile()
        {
            foreach(string line in File.ReadAllLines(GetSceneFilePath()))
            {
                //Debug.Log(line);
            }
        }
    }
}
