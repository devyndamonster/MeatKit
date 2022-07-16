using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TextureUtils {

	public static Texture2D LoadTextureFromPath(string filePath)
	{
		Texture2D tex = null;
		byte[] fileData;

		if (File.Exists(filePath))
		{
			fileData = File.ReadAllBytes(filePath);
			tex = new Texture2D(2, 2);
			tex.LoadImage(fileData);
		}
		return tex;
	}

	public static void WriteTextureToAssets(string assetPath, Texture2D texture)
	{
		string realFilePath = AssetUtils.GetRealFilePathFromAssetPath(assetPath);

		byte[] bytes = texture.EncodeToPNG();
		File.WriteAllBytes(realFilePath, bytes);

		AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
		AssetDatabase.Refresh();
	}

	public static void ImportTexture(string assetPath, TextureImporterSettings settings)
	{
		TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(assetPath);
		importer.SetTextureSettings(settings);
		importer.SaveAndReimport();
	}

	public static TextureImporterSettings GetTextureSettings(string assetPath)
	{
		TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(assetPath);
		TextureImporterSettings settings = new TextureImporterSettings();
		importer.ReadTextureSettings(settings);
		return settings;
	}

}
