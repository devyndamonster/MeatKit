using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class ReflectionUtils  {


	public static void MapFieldsToActions(System.Object target, Dictionary<Type, Action<FieldInfo, System.Object>> actionMap)
    {
		BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default;

		FieldInfo[] finfos = target.GetType().GetFields(flags);
		foreach (var finfo in finfos)
		{
			if (actionMap.ContainsKey(finfo.FieldType))
			{
				actionMap[finfo.FieldType](finfo, target);
			}
		}
	}

	public static void MapPropertiesToActions(System.Object target, Dictionary<Type, Action<PropertyInfo, System.Object>> actionMap)
	{
		BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default;

		PropertyInfo[] pinfos = target.GetType().GetProperties(flags);
		foreach (var pinfo in pinfos)
		{
			if (actionMap.ContainsKey(pinfo.PropertyType))
			{
				actionMap[pinfo.PropertyType](pinfo, target);
			}
		}
	}


}
