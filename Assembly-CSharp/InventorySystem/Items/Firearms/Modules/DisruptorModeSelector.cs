using System;
using InventorySystem.Items.Firearms.Modules.Misc;
using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public class DisruptorModeSelector : ModuleBase, IRecoilScalingModule
{
	private const float SwitchCooldown = 0.4f;

	private readonly TolerantAbilityCooldown _switchCooldown = new TolerantAbilityCooldown();

	private AudioModule _audioModule;

	[SerializeField]
	private AudioClip _singleClip;

	[SerializeField]
	private AudioClip _rapidClip;

	[SerializeField]
	private float _singleRecoilScale;

	public bool SingleShotSelected { get; private set; }

	public float RecoilMultiplier
	{
		get
		{
			if (!this.SingleShotSelected)
			{
				return 1f;
			}
			return this._singleRecoilScale;
		}
	}

	public event Action OnAnimationRequested;

	[ExposedFirearmEvent]
	public void RequestAnimation()
	{
		this.OnAnimationRequested?.Invoke();
		if (this.SingleShotSelected)
		{
			this._audioModule.PlayClientside(this._singleClip);
		}
		else
		{
			this._audioModule.PlayClientside(this._rapidClip);
		}
	}

	protected override void OnInit()
	{
		base.OnInit();
		base.Firearm.TryGetModule<AudioModule>(out this._audioModule);
	}

	internal override void EquipUpdate()
	{
		base.EquipUpdate();
		if (base.IsControllable && base.GetActionDown(ActionName.WeaponAlt) && !base.ItemUsageBlocked && this._switchCooldown.IsReady && !base.Firearm.AnyModuleBusy())
		{
			this.SingleShotSelected = !this.SingleShotSelected;
			this._switchCooldown.Trigger(0.4000000059604645);
			this.RequestAnimation();
			this.SendCmd(delegate(NetworkWriter x)
			{
				x.WriteBool(this.SingleShotSelected);
			});
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		this.SendRpc(delegate(NetworkWriter x)
		{
			x.WriteBool(reader.ReadBool());
		});
	}

	public override void ClientProcessRpcInstance(NetworkReader reader)
	{
		base.ClientProcessRpcInstance(reader);
		if (!base.IsLocalPlayer && this._switchCooldown.TolerantIsReady)
		{
			this.SingleShotSelected = reader.ReadBool();
			this._switchCooldown.Trigger(0.4000000059604645);
			this.RequestAnimation();
		}
	}
}
