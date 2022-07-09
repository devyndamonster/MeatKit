﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SerializedUnityElement {

	public List<string> elementLines = new List<string>();

	public string GetElementType()
    {
		return elementLines[1].Replace(":", "").Trim();
    }

	public string GetFileID()
    {
		return elementLines[0].Substring(elementLines[0].LastIndexOf('&') + 1).Trim();
    }

	public string GetScriptGUID()
    {
		return elementLines
			.First(o => o.Contains("m_Script"))
			.Replace("m_Script:", "")
			.Replace("{", "")
			.Replace("}", "")
			.Split(',')
			.First(o => o.Contains("guid"))
			.Replace("guid:", "")
			.Trim();
    }

	public string GetValue(string field)
    {
		return GetValueFromLine(elementLines.FirstOrDefault(o => o.Contains(field + ":")));
    }

	public bool HasValue(string field)
    {
		return elementLines.Any(o => o.Contains(field + ":"));
    }

	public List<string> GetDependancyGUIDs()
    {
		List<string> dependancyGUIDs = new List<string>();

		foreach(string line in elementLines)
        {
            if (line.Contains("guid:"))
            {
				dependancyGUIDs.Add(GetValueFromStruct(GetValueFromLine(line), "guid"));
            }
        }

		return dependancyGUIDs;
    }

	public string GetValueFromLine(string line)
    {
		if (!line.Contains(":")) throw new ArgumentException("Line argument must be line with value");

		return line.Substring(line.IndexOf(":") + 1).Trim();
    }

	public string GetValueFromStruct(string structValue, string field)
    {
		if (!structValue.Contains(field + ":")) throw new ArgumentException("Struct value argument does not have field: " + field);

		string targetField = structValue
			.Replace("{", "")
			.Replace("}", "")
			.Split(',')
			.First(value => value.Contains(field + ":"));

		return GetValueFromLine(targetField);
    }



	public void PatchScriptReference(Type scriptType)
    {
        if (PrefabPostProcess.DoesObjectHaveManagedDLL(scriptType))
        {
			string newScriptReference = PrefabPostProcess.GetScriptMetaTag(scriptType);
			int scriptIndex = elementLines.FindIndex(o => o.Contains("m_Script"));
			string scriptLine = elementLines[scriptIndex];

			Debug.Log("Line Before: " + elementLines[scriptIndex]);

			elementLines[scriptIndex] = elementLines[scriptIndex]
				.Remove(scriptLine.IndexOf('{'))
				+ newScriptReference;

			Debug.Log("Line After: " + elementLines[scriptIndex]);
		}
    }

	public void ConvertFromPrefab()
	{
		elementLines.Insert(2, "  m_PrefabInternal: {fileID: 0}");
		elementLines.Insert(2, "  m_PrefabParentObject: {fileID: 0}");
		elementLines.Insert(2, "  m_ObjectHideFlags: 0");
	}

	public void ReplaceText(string originalText, string newText)
    {
		for(int i = 0; i < elementLines.Count; i++)
        {
			elementLines[i] = elementLines[i].Replace(originalText, newText);
        }
    }

	public void ReplaceFileIds(string originalFileId, string newFileId)
    {
		Debug.Log("Original file id: " + originalFileId);
		for (int i = 0; i < elementLines.Count; i++)
        {
			if (elementLines[i].Contains(originalFileId))
            {
				Debug.Log("Line: " + elementLines[i]);
				elementLines[i] = elementLines[i].Replace(originalFileId, newFileId);
            }
        }
    }

	public List<string> GetComponentFileIds()
    {
		return elementLines
			.Where(o => o.Contains("- component:"))
			.Select(o => o.Replace("- component: {fileID:", "").Replace("}","").Trim())
			.ToList();
    }

	public List<string> GetTransformChildrenFileIds()
    {
		return elementLines
			.Where(o => o.Contains("- {fileID:"))
			.Select(o => o.Replace("- {fileID:", "").Replace("}", "").Trim())
			.ToList();
	}

	public string GetGameObjectFileId()
    {
		return elementLines
			.FirstOrDefault(o => o.Contains("m_GameObject:"))
			.Replace("m_GameObject: {fileID:", "")
			.Replace("}", "")
			.Trim();
    }

}
