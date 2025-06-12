using System;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions;

public class WorldmodelMagazineExtension : MonoBehaviour, IWorldmodelExtension
{
	[Serializable]
	private class Magazine
	{
		private uint? _filter;

		private bool? _lastIsActive;

		public GameObject Target;

		public AttachmentLink[] Attachments;

		public void UpdateState(WorldmodelMagazineExtension extRef, bool magInserted)
		{
			if (magInserted)
			{
				if (!this._filter.HasValue)
				{
					this.SetFilter(extRef._lastId);
				}
				uint num = extRef._lastAttCode & this._filter.Value;
				this.SetVisibility(extRef._removalMode, num != 0);
			}
			else
			{
				this.SetVisibility(extRef._removalMode, isActive: false);
			}
		}

		private void SetVisibility(RemovalMode removalMode, bool isActive)
		{
			if (!this._lastIsActive.HasValue || this._lastIsActive != isActive)
			{
				switch (removalMode)
				{
				case RemovalMode.SetScaleZero:
					this.Target.transform.localScale = (isActive ? Vector3.one : Vector3.zero);
					break;
				case RemovalMode.DeactivateObject:
					this.Target.SetActive(isActive);
					break;
				}
				this._lastIsActive = isActive;
			}
		}

		private void SetFilter(ItemIdentifier id)
		{
			if (this.Attachments == null || this.Attachments.Length == 0)
			{
				this._filter = uint.MaxValue;
				return;
			}
			uint num = 0u;
			AttachmentLink[] attachments = this.Attachments;
			for (int i = 0; i < attachments.Length; i++)
			{
				if (attachments[i].TryGetFilter(id.TypeId, out var filter))
				{
					num |= filter;
				}
			}
			this._filter = num;
		}
	}

	private enum RemovalMode
	{
		DeactivateObject,
		SetScaleZero
	}

	private ItemIdentifier _lastId;

	private uint _lastAttCode;

	private bool _forceInserted;

	[SerializeField]
	private Magazine[] _magazines;

	[SerializeField]
	private RemovalMode _removalMode;

	public void SetupWorldmodel(FirearmWorldmodel worldmodel)
	{
		this._lastId = worldmodel.Identifier;
		this._lastAttCode = worldmodel.AttachmentCode;
		this._forceInserted = worldmodel.WorldmodelType == FirearmWorldmodelType.Presentation;
		this.UpdateAllMags();
	}

	private void UpdateAllMags()
	{
		bool magInserted = this._forceInserted || MagazineModule.GetMagazineInserted(this._lastId.SerialNumber);
		Magazine[] magazines = this._magazines;
		for (int i = 0; i < magazines.Length; i++)
		{
			magazines[i].UpdateState(this, magInserted);
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		MagazineModule.OnDataReceived += delegate(ushort serial)
		{
			if (FirearmWorldmodel.Instances.TryGetValue(serial, out var value) && value.TryGetExtension<WorldmodelMagazineExtension>(out var extension))
			{
				extension.UpdateAllMags();
			}
		};
	}
}
