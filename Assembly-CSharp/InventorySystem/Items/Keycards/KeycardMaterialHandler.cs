using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem.Items.Keycards;

public class KeycardMaterialHandler
{
	private static readonly Dictionary<Material, Stack<Material>> Pool = new Dictionary<Material, Stack<Material>>();

	private readonly Material _template;

	public readonly Material Instance;

	public KeycardMaterialHandler(Renderer rend)
	{
		this._template = rend.sharedMaterial;
		if (!KeycardMaterialHandler.Pool.TryGetValue(this._template, out var value) || !value.TryPop(out this.Instance))
		{
			this.Instance = new Material(this._template);
		}
		rend.sharedMaterial = this.Instance;
	}

	public void Cleanup()
	{
		this.Instance.CopyPropertiesFromMaterial(this._template);
		KeycardMaterialHandler.Pool.GetOrAddNew(this._template).Push(this.Instance);
	}
}
