using AudioPooling;
using InventorySystem.Items.Pickups;
using MapGeneration.Distributors;
using Mirror;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.Firearms.Modules.Scp127;

public class Scp127GreetingsVoiceTrigger : Scp127VoiceTriggerBase
{
	[SerializeField]
	private Scp127VoiceLineCollection _firstTimeEquipLines;

	[SerializeField]
	private Scp127VoiceLineCollection _lockerEquipLines;

	[SerializeField]
	private Scp127VoiceLineCollection _reEquipLines;

	[SerializeField]
	private Scp127VoiceLineCollection _holsterLines;

	[SerializeField]
	private Scp127VoiceLineCollection _droppedLines;

	[SerializeField]
	private float _uniquePickupLineTimeout;

	private int _remainingFramesHolster;

	private float _rangeMultiplier;

	private bool _pickedUpFromLocker;

	private bool _newFriendship;

	private double _pickupTimestamp;

	private const float OwnerDroppedRangeScale = 3f;

	protected override float DefaultAudioRange => this._rangeMultiplier * base.DefaultAudioRange;

	internal override void ServerOnPickedUp(ItemPickupBase ipb)
	{
		base.ServerOnPickedUp(ipb);
		if (ipb.transform.TryGetComponentInParent<Locker>(out var _))
		{
			this._pickupTimestamp = NetworkTime.time;
			this._pickedUpFromLocker = true;
			base.Item.OwnerInventory.ServerSelectItem(base.ItemSerial);
		}
	}

	public override AudioPoolSession? OnVoiceLineRequested(ushort serial, AudioClip clip, NetworkReader extraData)
	{
		ReferenceHub hub;
		bool flag = extraData.Remaining > 0 && extraData.TryReadReferenceHub(out hub) && hub.IsPOV;
		this._rangeMultiplier = (flag ? 3f : 1f);
		return base.OnVoiceLineRequested(serial, clip, extraData);
	}

	public override void OnFriendshipCreated()
	{
		base.OnFriendshipCreated();
		this._newFriendship = true;
	}

	internal override void OnEquipped()
	{
		base.OnEquipped();
		if (!base.IsServer)
		{
			return;
		}
		if (this._newFriendship)
		{
			double num = NetworkTime.time - this._pickupTimestamp;
			if (this._pickedUpFromLocker && num < (double)this._uniquePickupLineTimeout)
			{
				this._pickedUpFromLocker = false;
				base.ServerPlayVoiceLineFromCollection(this._lockerEquipLines);
			}
			else
			{
				base.ServerPlayVoiceLineFromCollection(this._firstTimeEquipLines);
			}
			this._newFriendship = false;
		}
		else
		{
			base.ServerPlayVoiceLineFromCollection(this._reEquipLines, null, VoiceLinePriority.Low);
		}
	}

	internal override void OnHolstered()
	{
		base.OnHolstered();
		if (base.IsServer)
		{
			this._remainingFramesHolster = 3;
		}
	}

	internal override void OnRemoved(ItemPickupBase pickup)
	{
		base.OnRemoved(pickup);
		if (base.IsServer && !(pickup == null))
		{
			base.ServerPlayVoiceLineFromCollection(this._droppedLines, WriteOwner);
		}
	}

	internal override void AlwaysUpdate()
	{
		base.AlwaysUpdate();
		if (!base.IsServer || this._remainingFramesHolster == 0)
		{
			return;
		}
		if (base.Firearm.IsEquipped)
		{
			this._remainingFramesHolster = 0;
			return;
		}
		this._remainingFramesHolster--;
		if (this._remainingFramesHolster <= 0)
		{
			base.ServerPlayVoiceLineFromCollection(this._holsterLines, null, VoiceLinePriority.Low);
		}
	}

	private void WriteOwner(NetworkWriter writer)
	{
		writer.WriteReferenceHub(base.Item.Owner);
	}
}
