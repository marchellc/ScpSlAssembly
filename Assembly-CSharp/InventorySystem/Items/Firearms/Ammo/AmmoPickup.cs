using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Ammo
{
	public class AmmoPickup : ItemPickupBase
	{
		public int MaxAmmo
		{
			get
			{
				return this._maxDisplayedValue;
			}
		}

		private Material GetDigitMaterial(int digit)
		{
			Dictionary<int, Material> dictionary;
			if (!AmmoPickup.DigitMaterials.TryGetValue(this.Info.ItemId, out dictionary))
			{
				dictionary = new Dictionary<int, Material>();
				AmmoPickup.DigitMaterials.Add(this.Info.ItemId, dictionary);
			}
			Material material;
			if (!dictionary.TryGetValue(digit, out material))
			{
				material = new Material(this._targetDigitMaterial)
				{
					mainTextureOffset = Vector2.up * (float)digit / 10f
				};
				dictionary.Add(digit, material);
			}
			return material;
		}

		private void Update()
		{
			if (this._roundingValue == 0 || this.SavedAmmo == this._prevAmmo)
			{
				return;
			}
			int num = Mathf.Clamp((int)this.SavedAmmo, this._minDisplayedValue, this._maxDisplayedValue);
			while (num % this._roundingValue != 0)
			{
				num++;
			}
			Material digitMaterial = this.GetDigitMaterial(Mathf.FloorToInt((float)num / 10f));
			Material digitMaterial2 = this.GetDigitMaterial(num % 10);
			foreach (Renderer renderer in this._firstDigits)
			{
				renderer.sharedMaterial = digitMaterial;
				if (this._hideFirstDigitBelow10)
				{
					renderer.gameObject.SetActive(num >= 10);
				}
			}
			Renderer[] array = this._secondDigits;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].sharedMaterial = digitMaterial2;
			}
			this._prevAmmo = this.SavedAmmo;
		}

		public override bool Weaved()
		{
			return true;
		}

		public ushort NetworkSavedAmmo
		{
			get
			{
				return this.SavedAmmo;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<ushort>(value, ref this.SavedAmmo, 2UL, null);
			}
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				writer.WriteUShort(this.SavedAmmo);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 2UL) != 0UL)
			{
				writer.WriteUShort(this.SavedAmmo);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<ushort>(ref this.SavedAmmo, null, reader.ReadUShort());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 2L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<ushort>(ref this.SavedAmmo, null, reader.ReadUShort());
			}
		}

		[SyncVar]
		public ushort SavedAmmo;

		[SerializeField]
		private int _minDisplayedValue;

		[SerializeField]
		private int _maxDisplayedValue;

		[SerializeField]
		private int _roundingValue;

		[SerializeField]
		private bool _hideFirstDigitBelow10;

		[SerializeField]
		private Material _targetDigitMaterial;

		[SerializeField]
		private Renderer[] _firstDigits;

		[SerializeField]
		private Renderer[] _secondDigits;

		private static readonly Dictionary<ItemType, Dictionary<int, Material>> DigitMaterials = new Dictionary<ItemType, Dictionary<int, Material>>();

		private ushort _prevAmmo;
	}
}
