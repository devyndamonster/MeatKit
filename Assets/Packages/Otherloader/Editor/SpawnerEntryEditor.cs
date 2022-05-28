

#if H3VR_IMPORTED
using FistVR;
using OtherLoader;
#endif

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(OtherLoader.ItemSpawnerEntry), true)]
public class SpawnerEntryEditor : Editor
{
    public bool hasInit = false;
    public bool isItemIDEmpty = false;

#if H3VR_IMPORTED

    public override void OnInspectorGUI()
    {
        serializedObject.ApplyModifiedProperties();

        ItemSpawnerEntry entry = serializedObject.targetObject as ItemSpawnerEntry;

        if (!hasInit)
        {
            isItemIDEmpty = string.IsNullOrEmpty(entry.MainObjectID);
            hasInit = true;
        }

        var property = serializedObject.GetIterator();
        if (!property.NextVisible(true)) return;

        do
        {

            if (property.name == "MainObjectID")
            {
                if (entry.MainObjectObj != null)
                {
                    property.stringValue = entry.MainObjectObj.ItemID;
                }
            }

            if (property.name == "MainObjectObj" || property.name == "EntryIcon" || property.name == "UsesLargeSpawnPad")
            {
                DrawHorizontalLine();
            }

            if (property.name == "EntryPath")
            {
                DrawHorizontalLine();

                List<string> values = property.stringValue.Split('/').ToList();
                property.stringValue = ((ItemSpawnerV2.PageMode)serializedObject.FindProperty("Page").enumValueIndex).ToString();
                property.stringValue += "/";

                if (((ItemSpawnerID.ESubCategory)serializedObject.FindProperty("SubCategory").enumValueIndex) != ItemSpawnerID.ESubCategory.None)
                {
                    property.stringValue += ((ItemSpawnerID.ESubCategory)serializedObject.FindProperty("SubCategory").enumValueIndex).ToString();
                }
                else
                {
                    property.stringValue += values[1];
                }

                //Finally, add the end of the path based on the objectID
                string itemID = serializedObject.FindProperty("MainObjectID").stringValue;
                if (!string.IsNullOrEmpty(itemID))
                {
                    //If the itemID field is currently filled, but previously wasn't, we fill maintain all of the path and then add the itemID
                    if (isItemIDEmpty)
                    {
                        for (int i = 2; i < values.Count; i++)
                        {
                            property.stringValue += "/" + values[i];
                        }

                        isItemIDEmpty = false;
                    }


                    //If the itemID field was already filled previously, we can just draw everything until the itemID, and then add the itemID
                    else
                    {
                        for (int i = 2; i < values.Count - 1; i++)
                        {
                            property.stringValue += "/" + values[i];
                        }
                    }

                    property.stringValue += "/" + serializedObject.FindProperty("MainObjectID").stringValue;
                }

                else
                {
                    isItemIDEmpty = true;

                    for (int i = 2; i < values.Count; i++)
                    {
                        property.stringValue += "/" + values[i];
                    }
                }
            }

            DrawProperty(property);
        }
        while (property.NextVisible(false));
    }


    protected virtual void DrawProperty(SerializedProperty property)
    {
        EditorGUILayout.PropertyField(property, true);
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

#endif

}

