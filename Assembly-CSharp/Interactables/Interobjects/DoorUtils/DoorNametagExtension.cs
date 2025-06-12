using System.Collections.Generic;
using UnityEngine;

namespace Interactables.Interobjects.DoorUtils;

public class DoorNametagExtension : DoorVariantExtension
{
	public static readonly Dictionary<string, DoorNametagExtension> NamedDoors = new Dictionary<string, DoorNametagExtension>();

	[SerializeField]
	private string _nametag;

	public string GetName => this._nametag;

	private void Start()
	{
		this.UpdateName(this._nametag);
		if (base.TryGetComponent<DoorVariant>(out var component))
		{
			component.DoorName = this._nametag;
		}
	}

	private void FixedUpdate()
	{
	}

	public void UpdateName(string newName)
	{
		if (string.IsNullOrEmpty(newName))
		{
			Debug.LogError("Nametag of " + base.transform.parent.name + "/" + base.name + " has not been set", base.gameObject);
		}
		else
		{
			this._nametag = newName;
			DoorNametagExtension.NamedDoors[newName] = this;
		}
	}
}
