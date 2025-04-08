using System;
using InventorySystem.Items.Firearms.Modules.Misc;
using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public class DisruptorModeSelector : ModuleBase, IRecoilScalingModule
	{
		public event Action OnAnimationRequested;

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

		[ExposedFirearmEvent]
		public void RequestAnimation()
		{
			Action onAnimationRequested = this.OnAnimationRequested;
			if (onAnimationRequested != null)
			{
				onAnimationRequested();
			}
			if (this.SingleShotSelected)
			{
				this._audioModule.PlayClientside(this._singleClip);
				return;
			}
			this._audioModule.PlayClientside(this._rapidClip);
		}

		protected override void OnInit()
		{
			base.OnInit();
			base.Firearm.TryGetModule(out this._audioModule, true);
		}

		internal override void EquipUpdate()
		{
			base.EquipUpdate();
			if (!base.IsLocalPlayer)
			{
				return;
			}
			if (!base.GetActionDown(ActionName.WeaponAlt))
			{
				return;
			}
			if (base.ItemUsageBlocked)
			{
				return;
			}
			if (!this._switchCooldown.IsReady)
			{
				return;
			}
			if (base.Firearm.AnyModuleBusy(null))
			{
				return;
			}
			this.SingleShotSelected = !this.SingleShotSelected;
			this._switchCooldown.Trigger(0.4000000059604645);
			this.RequestAnimation();
			this.SendCmd(delegate(NetworkWriter x)
			{
				x.WriteBool(this.SingleShotSelected);
			});
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteBool(reader.ReadBool());
			}, true);
		}

		public override void ClientProcessRpcInstance(NetworkReader reader)
		{
			base.ClientProcessRpcInstance(reader);
			if (base.IsLocalPlayer)
			{
				return;
			}
			if (!this._switchCooldown.TolerantIsReady)
			{
				return;
			}
			this.SingleShotSelected = reader.ReadBool();
			this._switchCooldown.Trigger(0.4000000059604645);
			this.RequestAnimation();
		}

		private const float SwitchCooldown = 0.4f;

		private readonly TolerantAbilityCooldown _switchCooldown = new TolerantAbilityCooldown(0.2f);

		private AudioModule _audioModule;

		[SerializeField]
		private AudioClip _singleClip;

		[SerializeField]
		private AudioClip _rapidClip;

		[SerializeField]
		private float _singleRecoilScale;
	}
}
