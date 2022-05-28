

#if H3VR_IMPORTED

using MeatKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

using FistVR;
using OtherLoader;

[CustomEditor(typeof(OtherLoaderBuildRoot), true)]
public class OtherLoaderBuildRootEditor : BuildItemEditor
{

    private PathNode pathRoot;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (ValidationMessages.Count > 0) return;

        pathRoot = new PathNode("");

        SerializedProperty firstList = serializedObject.FindProperty("BuildItemsFirst").Copy();
        PopulatePathTree(firstList);

        SerializedProperty anyList = serializedObject.FindProperty("BuildItemsAny").Copy();
        PopulatePathTree(anyList);

        SerializedProperty lastList = serializedObject.FindProperty("BuildItemsLast").Copy();
        PopulatePathTree(lastList);

        string pathString = GetPathString(pathRoot);

        EditorStyles.helpBox.richText = true;

        DrawHorizontalLine();
        EditorGUILayout.HelpBox("Warning Overview", MessageType.Info);
        DrawItemWarnings();

        DrawHorizontalLine();
        EditorGUILayout.HelpBox("Category Overview", MessageType.Info);
        EditorGUILayout.HelpBox(pathString.Trim(), MessageType.None);
    }


    private void DrawItemWarnings()
    {
        OtherLoaderBuildRoot buildRoot = serializedObject.targetObject as OtherLoaderBuildRoot;
        DrawItemWarnings(buildRoot.BuildItemsFirst);
        DrawItemWarnings(buildRoot.BuildItemsAny);
        DrawItemWarnings(buildRoot.BuildItemsLast);
    }

    private void DrawItemWarnings(List<OtherLoaderBuildItem> buildItems)
    {
        buildItems.ForEach(buildItem => DrawItemWarnings(buildItem));
    }

    private void DrawItemWarnings(OtherLoaderBuildItem buildItem)
    {
        buildItem.Prefabs.ForEach(gameObject => DrawItemWarnings(gameObject));
        buildItem.FVRObjects.ForEach(fvrObject => DrawItemWarnings(fvrObject));
        buildItem.SpawnerEntries.ForEach(spawnerEntry => DrawItemWarnings(spawnerEntry));
    }

    private void DrawItemWarnings(GameObject gameObject)
    {
        FVRPhysicalObject physicalObjectComp = gameObject.GetComponent<FVRPhysicalObject>();
        if(physicalObjectComp != null) DrawItemWarnings(physicalObjectComp);

        FVRFireArm firearmComp = gameObject.GetComponent<FVRFireArm>();
        if (firearmComp != null) DrawItemWarnings(firearmComp);
    }

    private void DrawItemWarnings(FVRObject fvrObject)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(fvrObject.m_anvilPrefab.AssetName) == null)
        {
            DrawItemWarningBox("Asset path does not point to prefab on FVRObject: " + fvrObject.name);
        }

        if(fvrObject.Category == FVRObject.ObjectCategory.Uncategorized)
        {
            DrawItemWarningBox("Category set to Uncategorized on FVRObject: " + fvrObject.name);
        }
    }

    private void DrawItemWarnings(ItemSpawnerEntry spawnerEntry)
    {
        if(spawnerEntry.Page == ItemSpawnerV2.PageMode.MainMenu)
        {
            DrawItemWarningBox("Page set to MainMenu for SpawnerEntry: " + spawnerEntry.name);
        }

        if (spawnerEntry.EntryIcon == null)
        {
            DrawItemWarningBox("Missing entry icon for SpawnerEntry: " + spawnerEntry.name);
        }

        if (spawnerEntry.EntryPath.EndsWith("/") && string.IsNullOrEmpty(spawnerEntry.MainObjectID))
        {
            DrawItemWarningBox("Possible custom category has trailing '/'. If you are making custom categories, ensure there is no trailing '/'. On SpawnerEntry: " + spawnerEntry.name);
        }
    }

    private void DrawItemWarnings(FVRPhysicalObject physicalObject)
    {
        if(physicalObject.ObjectWrapper == null)
        {
            DrawItemWarningBox("Object Wrapper field empty for object: " + physicalObject.name);
        }

        if(physicalObject.AttachmentMounts.Any(o => o == null))
        {
            DrawItemWarningBox("Empty attachment mount field on object: " + physicalObject.name);
        }

        if (!physicalObject.enabled)
        {
            DrawItemWarningBox("Physical object component is disabled on object: " + physicalObject.name);
        }

        if (physicalObject.MP.IsMeleeWeapon)
        {
            if (physicalObject.MP.HandPoint == null)
            {
                DrawItemWarningBox("MP HandPoint not assigned on melee object: " + physicalObject.name);
            }

            if (physicalObject.MP.EndPoint == null)
            {
                DrawItemWarningBox("MP EndPoint not assigned on melee object: " + physicalObject.name);
            }
        }
    }


    private void DrawItemWarnings(FVRFireArm firearmComp)
    {
        if (firearmComp.MuzzlePos == null)
        {
            DrawItemWarningBox("Muzzle not assigned for firearm: " + firearmComp.name);
        }

        if (firearmComp.AudioClipSet == null)
        {
            DrawItemWarningBox("AudioClipSet not assigned for firearm: " + firearmComp.name);
        }

        if (firearmComp.RecoilProfile == null)
        {
            DrawItemWarningBox("Recoil Profile not assigned for firearm: " + firearmComp.name);
        }

        if (firearmComp.UsesStockedRecoilProfile && firearmComp.RecoilProfileStocked == null)
        {
            DrawItemWarningBox("Stocked Recoil Profile not assigned for firearm: " + firearmComp.name);
        }

        if (firearmComp.HasActiveShoulderStock && firearmComp.StockPos == null)
        {
            DrawItemWarningBox("Active shoulder stock point not assigned for firearm: " + firearmComp.name);
        }

        if(firearmComp.GasOutEffects.Any(o => o.EffectPrefab == null))
        {
            DrawItemWarningBox("Gas Out Effect Prefab is missing for firearm: " + firearmComp.name);
        }
    }

    private void DrawItemWarningBox(string warning)
    {
        EditorGUILayout.HelpBox(warning, MessageType.Warning);
    }


    private void PopulatePathTree(SerializedProperty itemList)
    {
        int listCount = itemList.arraySize;
        for (int i = 0; i < listCount; i++)
        {
            //This is terrible, but it seems like it's the only way to get child properties
            //See here: https://answers.unity.com/questions/543010/odd-behavior-of-findpropertyrelative.html 
            SerializedObject entryList = new SerializedObject(itemList.GetArrayElementAtIndex(i).objectReferenceValue);

            SerializedProperty entries = entryList.FindProperty("SpawnerEntries");
            int entryCount = entries.arraySize;
            for (int j = 0; j < entryCount; j++)
            {
                SerializedObject entry = new SerializedObject(entries.GetArrayElementAtIndex(j).objectReferenceValue);

                SerializedProperty entryPath = entry.FindProperty("EntryPath");

                string path = entryPath.stringValue;
                string[] pathParts = path.Split('/');
                string currPath = "";

                PathNode currNode = pathRoot;
                for(int k = 0; k < pathParts.Length; k++)
                {
                    currPath += (k == 0?"":"/") + pathParts[k];
                    
                    PathNode nextNode = currNode.children.FirstOrDefault(o => currPath == o.path);

                    if(nextNode == null)
                    {
                        nextNode = new PathNode(currPath);
                        currNode.children.Add(nextNode);
                    }

                    if(k == 0)
                    {
                        if (Enum.IsDefined(typeof(ItemSpawnerV2.PageMode), pathParts[k]))
                        {
                            nextNode.declared = true;
                        }
                    }

                    if(k == 1)
                    {
                        if (Enum.IsDefined(typeof(ItemSpawnerID.ESubCategory), pathParts[k]))
                        {
                            nextNode.declared = true;
                        }
                    }

                    currNode = nextNode;
                }

                currNode.declared = true;
            }
        }
    }


    private string GetPathString(PathNode node, int currDepth = -1)
    {
        string pathString = "";
        if(currDepth >= 0)
        {
            string styleStart = "<b>";
            string styleEnd = "</b>";

            if(!node.declared)
            {
                styleStart = "<b><color=red>";
                styleEnd = "</color></b>";
            }

            pathString = styleStart + new string(' ', currDepth * 8) + node.path.Split('/').Last() + styleEnd + "\n";
        }
        
        foreach(PathNode child in node.children)
        {
            pathString += GetPathString(child, currDepth + 1);
        }

        return pathString;
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


    private class PathNode
    {
        public string path;

        public bool declared = false;

        public List<PathNode> children = new List<PathNode>();

        public PathNode(string path)
        {
            this.path = path;
        }
    }

}

#endif
