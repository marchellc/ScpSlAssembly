using System;
using CustomPlayerEffects;
using UnityEngine;

namespace InventorySystem.Items.ThrowableProjectiles
{
	public class ThrowableItemViewmodel : StandardAnimatedViemodel
	{
		public override void InitLocal(ItemBase ib)
		{
			base.InitLocal(ib);
			ThrowableItem throwableItem = ib as ThrowableItem;
			if (throwableItem == null)
			{
				throw new InvalidOperationException(string.Format("Item {0} is not a valid target for {1}", ib.ItemTypeId, "ThrowableItemViewmodel"));
			}
			throwableItem.OnRequestSent += this.ProcessAnim;
		}

		public override void InitSpectator(ReferenceHub ply, ItemIdentifier id, bool wasEquipped)
		{
			base.InitSpectator(ply, id, wasEquipped);
			ThrowableNetworkHandler.OnAudioMessageReceived += this.OnMsgReceived;
			this.OnEquipped();
			if (!wasEquipped)
			{
				return;
			}
			AudioSource audioSource;
			if (base.TryGetComponent<AudioSource>(out audioSource))
			{
				audioSource.Stop();
			}
			ThrowableNetworkHandler.ThrowableItemAudioMessage throwableItemAudioMessage;
			if (!ThrowableNetworkHandler.ReceivedRequests.TryGetValue(id.SerialNumber, out throwableItemAudioMessage))
			{
				this.AnimatorForceUpdate(base.SkipEquipTime, true);
				return;
			}
			float num = Mathf.Min(Time.timeSinceLevelLoad - throwableItemAudioMessage.Time, base.SkipEquipTime);
			if (throwableItemAudioMessage.Request == ThrowableNetworkHandler.RequestType.CancelThrow)
			{
				this.ProcessAnim(ThrowableNetworkHandler.RequestType.BeginThrow);
				this.AnimatorForceUpdate(base.SkipEquipTime, false);
			}
			this.ProcessAnim(throwableItemAudioMessage.Request);
			this.AnimatorForceUpdate(num, false);
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
				ThrowableNetworkHandler.OnAudioMessageReceived -= this.OnMsgReceived;
				return;
			}
			(base.ParentItem as ThrowableItem).OnRequestSent -= this.ProcessAnim;
		}

		private void ProcessAnim(ThrowableNetworkHandler.RequestType request)
		{
			switch (request)
			{
			case ThrowableNetworkHandler.RequestType.BeginThrow:
				this.AnimatorSetBool(ThrowableItemViewmodel.AimHash, true);
				return;
			case ThrowableNetworkHandler.RequestType.ConfirmThrowWeak:
				this.AnimatorSetBool(ThrowableItemViewmodel.AimHash, true);
				this.AnimatorSetTrigger(ThrowableItemViewmodel.ThrowWeakHash);
				return;
			case ThrowableNetworkHandler.RequestType.ConfirmThrowFullForce:
				this.AnimatorSetBool(ThrowableItemViewmodel.AimHash, true);
				this.AnimatorSetTrigger(ThrowableItemViewmodel.ThrowFullHash);
				return;
			case ThrowableNetworkHandler.RequestType.CancelThrow:
				this.AnimatorSetBool(ThrowableItemViewmodel.AimHash, false);
				return;
			default:
				return;
			}
		}

		private void OnMsgReceived(ThrowableNetworkHandler.ThrowableItemAudioMessage msg)
		{
			if (msg.Serial != base.ItemId.SerialNumber)
			{
				return;
			}
			this.ProcessAnim(msg.Request);
		}

		private static readonly int AimHash = Animator.StringToHash("Aim");

		private static readonly int ThrowWeakHash = Animator.StringToHash("ThrowWeak");

		private static readonly int ThrowFullHash = Animator.StringToHash("ThrowFull");

		private static readonly int GrenadeModifier = Animator.StringToHash("SpeedModifier");
	}
}
