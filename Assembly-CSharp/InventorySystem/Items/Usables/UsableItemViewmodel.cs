using CustomPlayerEffects;
using UnityEngine;

namespace InventorySystem.Items.Usables;

public class UsableItemViewmodel : StandardAnimatedViemodel
{
	private static readonly int UseAnimHash = Animator.StringToHash("IsUsing");

	private static readonly int SpeedModifierHash = Animator.StringToHash("SpeedModifier");

	[SerializeField]
	private AudioSource _equipSoundSource;

	private float _originalPitch = 1f;

	public override void InitAny()
	{
		base.InitAny();
		if (this._equipSoundSource != null)
		{
			this._originalPitch = this._equipSoundSource.pitch;
		}
	}

	public override void InitSpectator(ReferenceHub ply, ItemIdentifier id, bool wasEquipped)
	{
		base.InitSpectator(ply, id, wasEquipped);
		this.OnEquipped();
		UsableItemsController.OnClientStatusReceived += HandleMessage;
		if (wasEquipped)
		{
			if (this._equipSoundSource != null)
			{
				this._equipSoundSource.Stop();
			}
			if (UsableItemsController.StartTimes.TryGetValue(id.SerialNumber, out var value))
			{
				this.AnimatorSetBool(UsableItemViewmodel.UseAnimHash, val: true);
				this.AnimatorForceUpdate(Time.timeSinceLevelLoad - value, fastMode: false);
			}
			else
			{
				this.AnimatorForceUpdate(base.SkipEquipTime);
			}
		}
	}

	public virtual void OnUsingCancelled()
	{
		this.AnimatorSetBool(UsableItemViewmodel.UseAnimHash, val: false);
	}

	public virtual void OnUsingStarted()
	{
		if (this._equipSoundSource != null)
		{
			this._equipSoundSource.Stop();
		}
		this.AnimatorSetBool(UsableItemViewmodel.UseAnimHash, val: true);
	}

	internal override void OnEquipped()
	{
		base.OnEquipped();
		float speedMultiplier = base.ItemId.TypeId.GetSpeedMultiplier(base.Hub);
		this.AnimatorSetFloat(UsableItemViewmodel.SpeedModifierHash, speedMultiplier);
		if (this._equipSoundSource != null)
		{
			this._equipSoundSource.pitch = this._originalPitch * speedMultiplier;
		}
	}

	private void HandleMessage(StatusMessage msg)
	{
		if (msg.ItemSerial == base.ItemId.SerialNumber)
		{
			this.AnimatorSetBool(UsableItemViewmodel.UseAnimHash, msg.Status == StatusMessage.StatusType.Start);
		}
	}

	protected virtual void OnDestroy()
	{
		if (base.IsSpectator)
		{
			UsableItemsController.OnClientStatusReceived -= HandleMessage;
		}
	}
}
