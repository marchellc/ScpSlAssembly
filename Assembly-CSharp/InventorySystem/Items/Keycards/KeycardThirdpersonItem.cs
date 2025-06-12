using System.Collections.Generic;
using InventorySystem.Items.Thirdperson;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;

namespace InventorySystem.Items.Keycards;

public class KeycardThirdpersonItem : IdleThirdpersonItem
{
	[SerializeField]
	private Transform _gfxSpawnPoint;

	[SerializeField]
	private AnimationClip _useClip;

	private bool _eventSet;

	private KeycardGfx _lastInstance;

	private readonly Dictionary<KeycardGfx, KeycardGfx> _prevInstances = new Dictionary<KeycardGfx, KeycardGfx>();

	internal override void Initialize(InventorySubcontroller subcontroller, ItemIdentifier id)
	{
		base.Initialize(subcontroller, id);
		if (!this._eventSet)
		{
			KeycardItem.OnKeycardUsed += OnKeycardUsed;
			this._eventSet = true;
		}
		base.SetAnim(AnimState3p.Override1, this._useClip);
		if (base.ItemId.TryGetTemplate<KeycardItem>(out var item))
		{
			this.SpawnKeycard(item.KeycardGfx);
		}
	}

	private void SpawnKeycard(KeycardGfx template)
	{
		if (this._lastInstance != null)
		{
			this._lastInstance.gameObject.SetActive(value: false);
		}
		if (this._prevInstances.TryGetValue(template, out var value))
		{
			value.gameObject.SetActive(value: true);
			KeycardDetailSynchronizer.TryReapplyDetails(value);
			this._lastInstance = value;
		}
		else
		{
			KeycardGfx keycardGfx = Object.Instantiate(template, this._gfxSpawnPoint);
			keycardGfx.transform.ResetTransform();
			this._lastInstance = keycardGfx;
		}
	}

	private void OnKeycardUsed(ushort serial, bool success)
	{
		if (base.ItemId.SerialNumber == serial)
		{
			base.OverrideBlend = 1f;
			base.ReplayOverrideBlend(soft: true);
		}
	}

	private void OnDestroy()
	{
		if (this._eventSet)
		{
			KeycardItem.OnKeycardUsed -= OnKeycardUsed;
		}
	}
}
