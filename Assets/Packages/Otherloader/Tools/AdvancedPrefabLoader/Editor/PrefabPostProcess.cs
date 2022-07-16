using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.IO;


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


    private static string CreateFolderRelativeToScene(string name)
    {
        string scenePath = EditorSceneManager.GetActiveScene().path;
        string sceneFolderPath = scenePath.Substring(0, scenePath.LastIndexOf('/'));
        string meshFolderPath = sceneFolderPath + "/" + name;
        if (!AssetDatabase.IsValidFolder(meshFolderPath)) AssetDatabase.CreateFolder(sceneFolderPath, name);

        return meshFolderPath;
    }


    public static void ProcessSpawnedObject(GameObject spawned, PrefabLoaderAssetBundleState _state)
    {
        if (_state.RipMeshes)
        {
            string folderPath = CreateFolderRelativeToScene("Meshes");
            MeshRipper.RipAndReplaceMeshes(spawned, folderPath);
        }

        if (_state.RipSprites)
        {
            string folderPath = CreateFolderRelativeToScene("Textures");
            SpriteRipper.RipAndReplaceSprites(spawned, folderPath);
        }
        
        SaveScene();
        Dictionary<int, string> scriptReferences = GetMonoBehaviorScriptReferenceDict(spawned);
        PatchSceneFile(scriptReferences);
    }

    public static void AddUnityElementsToScene(List<SerializedUnityElement> elements)
    {
        Debug.Log("Adding unity elements to Scene!");
        SaveScene();

        List<string> fileLines = File.ReadAllLines(GetSceneFilePath()).ToList();

        foreach(SerializedUnityElement element in elements)
        {
            Debug.Log("Adding element: " + element.GetElementType());
            fileLines.AddRange(element.elementLines);
        }

        UnityEngine.SceneManagement.Scene currentScene = EditorSceneManager.GetActiveScene();
        string sceneFilePath = GetSceneFilePath();
        string sceneAssetPath = currentScene.path;

        UnityEngine.SceneManagement.Scene tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        EditorSceneManager.SaveScene(tempScene, "tempScene.unity");
        EditorSceneManager.OpenScene(tempScene.path);

        File.WriteAllLines(sceneFilePath, fileLines.ToArray());

        EditorSceneManager.OpenScene(sceneAssetPath);
        SaveScene();
    }


    public static Dictionary<int, string> GetMonoBehaviorScriptReferenceDict(GameObject spawned)
    {
        Dictionary<int, string> scriptReferenceDict = new Dictionary<int, string>();

        foreach (var component in spawned.GetComponentsInChildren<MonoBehaviour>())
        {
            if (DoesObjectHaveManagedDLL(component.GetType()))
            {
                int fileID = GetFileIDForObject(component);
                string scriptReference = GetScriptMetaTag(component.GetType());
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

    public static string GetScriptMetaTag(Type type)
    {
        string metadata =
            "{fileID: " +
            FileIDUtil.Compute(type).ToString() +
            ", guid: " +
            GetAssemblyGUIDFromType(type) +
            ", type: 3}";

        return metadata;
    }

    public static string GetAssemblyNameFromType(Type type)
    {
        return type.Assembly.GetName().Name;
    }

    public static bool DoesObjectHaveManagedDLL(Type type)
    {
        return !string.IsNullOrEmpty(GetAssemblyGUIDFromType(type));
    }

    public static string GetAssemblyPathFromType(Type type)
    {
        return "Assets/MeatKit/Managed/" + GetAssemblyNameFromType(type) + ".dll";
    }

    public static string GetAssemblyGUIDFromType(Type type)
    {
        return AssetDatabase.AssetPathToGUID(GetAssemblyPathFromType(type));
    }

    public static Assembly GetAssemblyFromTypeName(string typeName)
    {
        foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach(Type type in assembly.GetTypes())
            {
                if (type.Name.Contains(typeName))
                {
                    return assembly;
                }
            }
        }

        return null;
    }

    public static Type GetTypeFromName(string typeName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.Name.Contains(typeName))
                {
                    return type;
                }
            }
        }

        return null;
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

    
}
