using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class SpriteRipper
{

	private static Dictionary<Type, Action<PropertyInfo, System.Object>> GetSpritePropertyActionMap(string exportPath)
    {
		return new Dictionary<Type, Action<PropertyInfo, System.Object>>()
		{
			{ typeof(Texture), (pinfo, target) => { RipSpriteFromProperty(pinfo, target, exportPath); } },
			{ typeof(List<Texture>), (pinfo, target) => { RipSpritesFromListProperty(pinfo, target, exportPath); } }
		};
	} 


    public static void RipAndReplaceSprites(GameObject spawned, string outputFolderPath)
    {
		var propertyActionMap = GetSpritePropertyActionMap(outputFolderPath);

		foreach (Component comp in spawned.GetComponentsInChildren(typeof(Component), true))
        {
			ReflectionUtils.MapPropertiesToActions(comp, propertyActionMap);
        }
    }


	private static void RipSpritesFromMaterial(Material material, string exportPath)
    {

    }

	private static void RipSpriteFromProperty(PropertyInfo pinfo, System.Object target, string exportPath)
    {
		try
		{
			Debug.Log("Property is texture: " + pinfo.Name);
			Texture2D originalTexture = (Texture2D)pinfo.GetValue(target, null);
			Texture2D newTexture = new Texture2D(originalTexture.width, originalTexture.height);
			Graphics.CopyTexture(originalTexture, newTexture);

			string textureAssetPath = exportPath + "/" + originalTexture.name + ".png";
			if (!AssetUtils.DoesFileExistInAssets(textureAssetPath))
			{
				TextureUtils.WriteTextureToAssets(textureAssetPath, newTexture);
			}

			pinfo.SetValue(target, AssetDatabase.LoadAssetAtPath<Texture2D>(textureAssetPath), null);
		}
		catch (Exception e)
		{
			Debug.LogError("Could not read property: " + pinfo.Name);
			Debug.LogError(e.ToString());

		}
	}

	private static void RipSpritesFromField(PropertyInfo pinfo, System.Object target, string exportPath)
	{

	}

	private static void RipSpritesFromListProperty(PropertyInfo pinfo, System.Object target, string exportPath)
    {
		Debug.Log("Cool list!");
    }


	private static void RipSpritesFromComponent(System.Object asset, string exportPath)
	{
		Type type = asset.GetType();
		BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default;

		FieldInfo[] finfos = type.GetFields(flags);
		foreach (var finfo in finfos)
		{
			try
			{
				if (finfo.FieldType == typeof(Texture))
				{
					Debug.Log("Field is Texture2D: " + finfo.Name);
					Texture2D originalTexture = (Texture2D)finfo.GetValue(asset);

					string textureAssetPath = exportPath + "/" + originalTexture.name + ".png";
					if (!AssetUtils.DoesFileExistInAssets(textureAssetPath))
					{
						TextureUtils.WriteTextureToAssets(textureAssetPath, originalTexture);
					}
				}

				if(finfo.FieldType == typeof(List<Material>) || finfo.FieldType == typeof(Material[]))
                {

                }
			}
			catch (Exception e)
			{
				Debug.LogError("Could not read field: " + finfo.Name);

			}
		}

		PropertyInfo[] pinfos = type.GetProperties(flags);
		foreach (var pinfo in pinfos)
		{
			
			try
            {
				if(pinfo.PropertyType == typeof(Texture))
                {
					Debug.Log("Property is Texture: " + pinfo.Name);
					Texture2D originalTexture = (Texture2D)pinfo.GetValue(asset, null);

					Texture2D newTexture = new Texture2D(originalTexture.width, originalTexture.height);

					Graphics.CopyTexture(originalTexture, newTexture);

					string textureAssetPath = exportPath + "/" + originalTexture.name + ".png";
					if (!AssetUtils.DoesFileExistInAssets(textureAssetPath))
					{
						TextureUtils.WriteTextureToAssets(textureAssetPath, newTexture);
					}
				}
			}
			catch(Exception e)
            {
				Debug.LogError("Could not read property: " + pinfo.Name);
				Debug.LogError(e.ToString());

			}
			
		}

	}
}
