using System;

namespace InventorySystem.Items.Firearms.Extensions;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PresetPrefabExtensionAttribute : Attribute
{
	public readonly string PrefabName;

	public readonly Type ExtensionType;

	public PresetPrefabExtensionAttribute(string prefabName, Type extensionType)
	{
		this.PrefabName = prefabName;
		this.ExtensionType = extensionType;
	}

	public PresetPrefabExtensionAttribute(string prefabName, bool isWorldmodel)
	{
		this.PrefabName = prefabName;
		this.ExtensionType = (isWorldmodel ? typeof(IWorldmodelExtension) : typeof(IViewmodelExtension));
	}
}
