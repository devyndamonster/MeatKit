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

		Texture2D texture = TextureUtils.LoadTextureFromPath(AssetUtils.GetRealFilePathFromAssetPath(assetPath));
		texture = InvertGreenChannel(texture);
		TextureUtils.WriteTextureToAssets(newAssetPath, texture);
		TextureUtils.ImportTexture(newAssetPath, TextureUtils.GetTextureSettings(assetPath));
	}

	private void CalculateBlueChannelNormals()
	{
		string assetPath = AssetDatabase.GetAssetPath(SelectedTexture);
		string extension = Path.GetExtension(assetPath);
		string pathWithoutExtension = assetPath.Replace(extension, "");
		string newAssetPath = pathWithoutExtension + "_withBlue" + extension;

		Texture2D texture = TextureUtils.LoadTextureFromPath(AssetUtils.GetRealFilePathFromAssetPath(assetPath));
		texture = CalculatedBlueChannel(texture);
		TextureUtils.WriteTextureToAssets(newAssetPath, texture);
		TextureUtils.ImportTexture(newAssetPath, TextureUtils.GetTextureSettings(assetPath));
	}

	private Texture2D InvertGreenChannel(Texture2D inputNormal)
	{
		for (int y = 0; y < inputNormal.height; y++)
		{
			for (int x = 0; x < inputNormal.width; x++)
			{
				Color pixel = inputNormal.GetPixel(x, y);
				pixel.g = 1 - pixel.g;
				inputNormal.SetPixel(x, y, pixel);
			}
		}

		inputNormal.Apply();
		return inputNormal;
	}


	private Texture2D CalculatedBlueChannel(Texture2D normalInput)
	{
		for (int y = 0; y < normalInput.height; y++)
		{
			for (int x = 0; x < normalInput.width; x++)
			{
				Color pixel = normalInput.GetPixel(x, y);

				pixel.r = pixel.r * 2 - 1;
				pixel.g = pixel.g * 2 - 1;
				pixel.b = Mathf.Sqrt(1 - pixel.r * pixel.r - pixel.g * pixel.g);

				normalInput.SetPixel(x, y, pixel);
			}
		}

		normalInput.Apply();
		return normalInput;
	}

#endif
}





