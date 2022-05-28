#if H3VR_IMPORTED
using FistVR;
#endif
using MeatKit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class TextureToolsWindow : EditorWindow
{

	public Texture2D SelectedTexture;

	[MenuItem("Tools/Texture Tools")]
	public static void Open()
	{
		GetWindow<TextureToolsWindow>("Texture Tools").Show();
	}

	#if H3VR_IMPORTED

	private void OnGUI()
	{
		EditorGUILayout.LabelField("Selected GameObject", EditorStyles.boldLabel);

		EditorGUI.BeginChangeCheck();
		SelectedTexture = EditorGUILayout.ObjectField(SelectedTexture, typeof(Texture2D), true) as Texture2D;

		if (!SelectedTexture)
		{
			GUILayout.Label("Please select a texture");
			return;
		}

		EditorGUILayout.Space();

		if (GUILayout.Button("Convert normals from directX to openGL"))
		{
			ConvertToOpenGLButtonPressed();
		}

		if (GUILayout.Button("Calculate normal blue channel"))
		{
			CalculateBlueChannelNormals();
		}
	}

	private void ConvertToOpenGLButtonPressed()
    {
		string assetPath = AssetDatabase.GetAssetPath(SelectedTexture);
		string extension = Path.GetExtension(assetPath);
		string pathWithoutExtension = assetPath.Replace(extension, "");
		string newAssetPath = pathWithoutExtension + "_openGL" + extension;

		ImportNormalTexture(assetPath);

		Texture2D openGLNormal = GetOpenGLFromDirectXNormal(SelectedTexture);

		WriteTextureToAssets(newAssetPath, openGLNormal);
		ImportNormalTexture(newAssetPath);
	}

	private void CalculateBlueChannelNormals()
    {
		string assetPath = AssetDatabase.GetAssetPath(SelectedTexture);
		string extension = Path.GetExtension(assetPath);
		string pathWithoutExtension = assetPath.Replace(extension, "");
		string newAssetPath = pathWithoutExtension + "_withBlue" + extension;

		ImportNormalTexture(assetPath);

		Texture2D normalWithBlue = GetNormalWithCalculatedBlueChannel(SelectedTexture);

		WriteTextureToAssets(newAssetPath, normalWithBlue);
		ImportNormalTexture(newAssetPath);
	}


	private void ImportNormalTexture(string assetPath)
    {
		TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(assetPath);

		importer.isReadable = true;
		importer.textureType = TextureImporterType.NormalMap;
		importer.spriteImportMode = SpriteImportMode.Single;
		importer.alphaSource = TextureImporterAlphaSource.FromInput;
		importer.alphaIsTransparency = false;
		importer.mipmapEnabled = true;
		importer.wrapMode = TextureWrapMode.Repeat;

		EditorUtility.SetDirty(importer);
		importer.SaveAndReimport();
	}

	private void WriteTextureToAssets(string assetPath, Texture2D texture)
    {
		Debug.Log(assetPath);
		string realFilePath = Application.dataPath + assetPath.Replace("Assets", "");
		Debug.Log(realFilePath);

		byte[] bytes = texture.EncodeToPNG();
		File.WriteAllBytes(realFilePath, bytes);

		AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
		AssetDatabase.Refresh();
	}

	private Texture2D GetOpenGLFromDirectXNormal(Texture2D directXNormal)
    {
		Texture2D openGLNormal = new Texture2D(directXNormal.width, directXNormal.height);

		for(int y = 0; y < directXNormal.height; y++)
        {
			for(int x = 0; x < directXNormal.width; x++)
            {
				Color pixel = directXNormal.GetPixel(x, y);
				pixel.g = 1 - pixel.g;
				openGLNormal.SetPixel(x, y, pixel);
            }
        }

		openGLNormal.Apply();
		return openGLNormal;
    }


	private Texture2D GetNormalWithCalculatedBlueChannel(Texture2D normalInput)
	{
		Texture2D normalWithBlue = new Texture2D(normalInput.width, normalInput.height);

		for (int y = 0; y < normalInput.height; y++)
		{
			for (int x = 0; x < normalInput.width; x++)
			{
				Color pixel = normalInput.GetPixel(x, y);

				pixel.r = pixel.r * 2 - 1;
				pixel.g = pixel.g * 2 - 1;
				pixel.b = Mathf.Sqrt(1 - pixel.r * pixel.r - pixel.g * pixel.g);

				normalWithBlue.SetPixel(x, y, pixel);
			}
		}

		normalWithBlue.Apply();
		return normalWithBlue;
	}



#endif
}





