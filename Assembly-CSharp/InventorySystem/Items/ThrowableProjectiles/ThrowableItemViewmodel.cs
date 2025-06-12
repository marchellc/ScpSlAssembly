using System;
using CustomPlayerEffects;
using UnityEngine;

namespace InventorySystem.Items.ThrowableProjectiles;

public class ThrowableItemViewmodel : StandardAnimatedViemodel
{
	private static readonly int AimHash = Animator.StringToHash("Aim");

	private static readonly int ThrowWeakHash = Animator.StringToHash("ThrowWeak");

	private static readonly int ThrowFullHash = Animator.StringToHash("ThrowFull");

	private static readonly int GrenadeModifier = Animator.StringToHash("SpeedModifier");

	public override void InitLocal(ItemBase ib)
	{
		base.InitLocal(ib);
		((ib as ThrowableItem) ?? throw new InvalidOperationException(string.Format("Item {0} is not a valid target for {1}", ib.ItemTypeId, "ThrowableItemViewmodel"))).OnRequestSent += ProcessAnim;
	}

	public override void InitSpectator(ReferenceHub ply, ItemIdentifier id, bool wasEquipped)
	{
		base.InitSpectator(ply, id, wasEquipped);
		ThrowableNetworkHandler.OnAudioMessageReceived += OnMsgReceived;
		this.OnEquipped();
		if (!wasEquipped)
		{
			return;
		}
		if (base.TryGetComponent<AudioSource>(out var component))
		{
			component.Stop();
		}
		if (!ThrowableNetworkHandler.ReceivedRequests.TryGetValue(id.SerialNumber, out var value))
		{
			this.AnimatorForceUpdate(base.SkipEquipTime);
			return;
		}
		float deltaTime = Mathf.Min(Time.timeSinceLevelLoad - value.Time, base.SkipEquipTime);
		if (value.Request == ThrowableNetworkHandler.RequestType.CancelThrow)
		{
			this.ProcessAnim(ThrowableNetworkHandler.RequestType.BeginThrow);
			this.AnimatorForceUpdate(base.SkipEquipTime, fastMode: false);
		}
		this.ProcessAnim(value.Request);
		this.AnimatorForceUpdate(deltaTime, fastMode: false);
	}

	internal override void OnEquipped()
	{
		base.OnEquipped();
		this.AnimatorSetFloat(ThrowableItemViewmodel.GrenadeModifier, base.ItemId.TypeId.GetSpeedMultiplier(base.Hub));
	}

	private void OnDestroy()
	{
		if (base.IsSpectator)
		{
			ThrowableNetworkHandler.OnAudioMessageReceived -= OnMsgReceived;
		}
		else
		{
			(base.ParentItem as ThrowableItem).OnRequestSent -= ProcessAnim;
		}
	}

	private void ProcessAnim(ThrowableNetworkHandler.RequestType request)
	{
		switch (request)
		{
		case ThrowableNetworkHandler.RequestType.BeginThrow:
			this.AnimatorSetBool(ThrowableItemViewmodel.AimHash, val: true);
			break;
		case ThrowableNetworkHandler.RequestType.CancelThrow:
			this.AnimatorSetBool(ThrowableItemViewmodel.AimHash, val: false);
			break;
		case ThrowableNetworkHandler.RequestType.ConfirmThrowFullForce:
			this.AnimatorSetBool(ThrowableItemViewmodel.AimHash, val: true);
			this.AnimatorSetTrigger(ThrowableItemViewmodel.ThrowFullHash);
			break;
		case ThrowableNetworkHandler.RequestType.ConfirmThrowWeak:
			this.AnimatorSetBool(ThrowableItemViewmodel.AimHash, val: true);
			this.AnimatorSetTrigger(ThrowableItemViewmodel.ThrowWeakHash);
			break;
		}
	}

	private void OnMsgReceived(ThrowableNetworkHandler.ThrowableItemAudioMessage msg)
	{
		if (msg.Serial == base.ItemId.SerialNumber)
		{
			this.ProcessAnim(msg.Request);
		}
	}
}
