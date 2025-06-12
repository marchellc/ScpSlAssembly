using System.Collections.Generic;
using System.Runtime.InteropServices;
using InventorySystem.Items.Pickups;
using InventorySystem.Searching;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Ammo;

public class AmmoPickup : ItemPickupBase
{
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

	public int MaxAmmo => this._maxDisplayedValue;

	public ushort NetworkSavedAmmo
	{
		get
		{
			return this.SavedAmmo;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.SavedAmmo, 2uL, null);
		}
	}

	public override PickupSearchCompletor GetPickupSearchCompletor(SearchCoordinator coordinator, float sqrDistance)
	{
		return new AmmoSearchCompletor(coordinator.Hub, this, sqrDistance);
	}

	private Material GetDigitMaterial(int digit)
	{
		if (!AmmoPickup.DigitMaterials.TryGetValue(base.Info.ItemId, out var value))
		{
			value = new Dictionary<int, Material>();
			AmmoPickup.DigitMaterials.Add(base.Info.ItemId, value);
		}
		if (!value.TryGetValue(digit, out var value2))
		{
			value2 = new Material(this._targetDigitMaterial)
			{
				mainTextureOffset = Vector2.up * digit / 10f
			};
			value.Add(digit, value2);
		}
		return value2;
	}

	private void Update()
	{
		if (this._roundingValue == 0 || this.SavedAmmo == this._prevAmmo)
		{
			return;
		}
		int i;
		for (i = Mathf.Clamp(this.SavedAmmo, this._minDisplayedValue, this._maxDisplayedValue); i % this._roundingValue != 0; i++)
		{
		}
		Material digitMaterial = this.GetDigitMaterial(Mathf.FloorToInt((float)i / 10f));
		Material digitMaterial2 = this.GetDigitMaterial(i % 10);
		Renderer[] firstDigits = this._firstDigits;
		foreach (Renderer renderer in firstDigits)
		{
			renderer.sharedMaterial = digitMaterial;
			if (this._hideFirstDigitBelow10)
			{
				renderer.gameObject.SetActive(i >= 10);
			}
		}
		firstDigits = this._secondDigits;
		for (int j = 0; j < firstDigits.Length; j++)
		{
			firstDigits[j].sharedMaterial = digitMaterial2;
		}
		this._prevAmmo = this.SavedAmmo;
	}

	public override bool Weaved()
	{
		return true;
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
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteUShort(this.SavedAmmo);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.SavedAmmo, null, reader.ReadUShort());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.SavedAmmo, null, reader.ReadUShort());
		}
	}
}
