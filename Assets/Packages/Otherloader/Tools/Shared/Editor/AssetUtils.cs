using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AssetUtils {

	public static string GetRealFilePathFromAssetPath(string assetPath)
	{
		string realFilePath = Application.dataPath + assetPath.Replace("Assets", "");
		return realFilePath;
	}

	public static bool DoesFileExistInAssets(string assetPath)
    {
		return File.Exists(GetRealFilePathFromAssetPath(assetPath));
    }

}
