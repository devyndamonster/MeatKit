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

        public static void ProcessSpawnedObject(GameObject spawned)
        {
            ReplaceMeshes(spawned);
            SaveScene();
            Dictionary<int, string> scriptReferences = GetMonoBehaviorScriptReferenceDict(spawned);
            PatchSceneFile(scriptReferences);
        }


        public static Dictionary<int, string> GetMonoBehaviorScriptReferenceDict(GameObject spawned)
        {
            Dictionary<int, string> scriptReferenceDict = new Dictionary<int, string>();

            foreach (var component in spawned.GetComponentsInChildren<MonoBehaviour>())
            {
                if (DoesObjectHaveManagedDLL(component))
                {
                    int fileID = GetFileIDForObject(component);
                    string scriptReference = GetScriptMetaTag(component);
                    scriptReferenceDict[fileID] = scriptReference;
                }
            }

            return scriptReferenceDict;
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

        public static string GetScriptMetaTag(UnityEngine.Object targetObject)
        {
            string metadata = 
                "{fileID: " +
                FileIDUtil.Compute(targetObject.GetType()).ToString() + 
                ", guid: " +
                GetAssemblyGUIDForObject(targetObject) +
                ", type: 3}";

            return metadata;
        }

        public static bool DoesObjectHaveManagedDLL(UnityEngine.Object targetObject)
        {
            return !string.IsNullOrEmpty(GetAssemblyGUIDForObject(targetObject));
        }

        public static string GetAssemblyPathForObject(UnityEngine.Object targetObject)
        {
            return "Assets/MeatKit/Managed/" + GetAssemblyNameForObject(targetObject) + ".dll";
        }

        public static string GetAssemblyGUIDForObject(UnityEngine.Object targetObject)
        {
            return AssetDatabase.AssetPathToGUID(GetAssemblyPathForObject(targetObject));
        }

        public static void PatchSceneFile(Dictionary<int, string> scriptReferences)
        {
            Debug.Log("Patching Scene!");
            Debug.Log("Reference count: " + scriptReferences.Count);
            string replaceNextScript = "";
            string[] fileLines = File.ReadAllLines(GetSceneFilePath());

            for(int lineIndex = 0; lineIndex < fileLines.Length; lineIndex++) 
            {
                string line = fileLines[lineIndex];

                if (IsLineSceneAsset(line))
                {
                    int fileID = GetFileIDFromSceneLine(line);
                    if (scriptReferences.ContainsKey(fileID))
                    {
                        replaceNextScript = scriptReferences[fileID];
                    }
                }

                else if (!string.IsNullOrEmpty(replaceNextScript) && IsLineMonoBehaviourScript(line))
                {
                    line = line.Replace("{fileID: 0}", replaceNextScript);
                    replaceNextScript = "";
                    fileLines[lineIndex] = line;
                }
            }

            UnityEngine.SceneManagement.Scene currentScene = EditorSceneManager.GetActiveScene();
            string sceneFilePath = GetSceneFilePath();
            string sceneAssetPath = currentScene.path;
            
            UnityEngine.SceneManagement.Scene tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            EditorSceneManager.SaveScene(tempScene, "tempScene.unity");
            EditorSceneManager.OpenScene(tempScene.path);

            File.WriteAllLines(sceneFilePath, fileLines);

            EditorSceneManager.OpenScene(sceneAssetPath);
            SaveScene();
        }


        private static bool IsLineSceneAsset(string line)
        {
            return line.Contains("--- !u!");
        }

        private static bool IsLineMonoBehaviourScript(string line)
        {
            return line.Contains("m_Script:");
        }

        private static int GetFileIDFromSceneLine(string line)
        {
            string lineID = line.Substring(line.IndexOf('&') + 1);
            int lineIDValue = 0;
            int.TryParse(lineID, out lineIDValue);
            return lineIDValue;
        }

        private static void ReplaceMeshes(GameObject spawned)
        {
            foreach (MeshFilter meshFilter in spawned.GetComponentsInChildren<MeshFilter>())
            {
                Debug.Log("Replacing mesh: " + meshFilter.sharedMesh.name);
                string meshPath = "Assets/" + meshFilter.sharedMesh.name + ".asset";

                if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(meshPath)))
                {
                    Mesh currentMesh = meshFilter.sharedMesh;
                    Mesh newmesh = new Mesh();

                    newmesh.vertices = currentMesh.vertices;
                    newmesh.triangles = currentMesh.triangles;
                    newmesh.uv = currentMesh.uv;
                    newmesh.normals = currentMesh.normals;
                    newmesh.colors = currentMesh.colors;
                    newmesh.tangents = currentMesh.tangents;

                    AssetDatabase.CreateAsset(newmesh, meshPath);
                    AssetDatabase.Refresh();
                }

                meshFilter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            }

            foreach (MeshRenderer meshRenderer in spawned.GetComponentsInChildren<MeshRenderer>())
            {
                meshRenderer.materials = new Material[] { AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat") };
            }
        }
    }
}
