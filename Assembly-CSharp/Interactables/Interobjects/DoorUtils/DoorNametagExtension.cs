using System;
using System.Collections.Generic;
using UnityEngine;

namespace Interactables.Interobjects.DoorUtils
{
	public class DoorNametagExtension : DoorVariantExtension
	{
		public string GetName
		{
			get
			{
				return this._nametag;
			}
		}

		private void Start()
		{
			this.UpdateName(this._nametag);
			DoorVariant doorVariant;
			if (!base.TryGetComponent<DoorVariant>(out doorVariant))
			{
				return;
			}
			doorVariant.DoorName = this._nametag;
		}

		private void FixedUpdate()
		{
		}

		public void UpdateName(string newName)
		{
			if (string.IsNullOrEmpty(newName))
			{
				Debug.LogError(string.Concat(new string[]
				{
					"Nametag of ",
					base.transform.parent.name,
					"/",
					base.name,
					" has not been set"
				}), base.gameObject);
				return;
			}
			this._nametag = newName;
			DoorNametagExtension.NamedDoors[newName] = this;
		}

		public static readonly Dictionary<string, DoorNametagExtension> NamedDoors = new Dictionary<string, DoorNametagExtension>();

		[SerializeField]
		private string _nametag;
	}
}
