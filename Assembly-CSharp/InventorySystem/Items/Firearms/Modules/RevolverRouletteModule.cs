using System;
using InventorySystem.Items.Firearms.Modules.Misc;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public class RevolverRouletteModule : ModuleBase, IBusyIndicatorModule, IAdsPreventerModule
	{
		public bool IsBusy
		{
			get
			{
				return this._busy || this._requestTimer.Busy;
			}
		}

		public bool AdsAllowed
		{
			get
			{
				return !this._busy;
			}
		}

		protected override void OnInit()
		{
			base.OnInit();
			if (!base.Firearm.TryGetModules(out this._doubleActionModule, out this._cylinderModule))
			{
				throw new InvalidOperationException(string.Concat(new string[]
				{
					"The ",
					base.Firearm.name,
					" is missing one or more essential modules (required by ",
					base.name,
					")."
				}));
			}
		}

		internal override void OnHolstered()
		{
			base.OnHolstered();
			this._busy = false;
		}

		internal override void EquipUpdate()
		{
			base.EquipUpdate();
			if (!base.IsLocalPlayer)
			{
				return;
			}
			if (base.PrimaryActionBlocked)
			{
				return;
			}
			if (!base.GetAction(ActionName.WeaponAlt) || base.Firearm.AnyModuleBusy(null))
			{
				this._keyHoldTime = 0f;
				return;
			}
			this._keyHoldTime += Time.deltaTime;
			if (this._keyHoldTime > 1f)
			{
				this._requestTimer.Trigger();
				this.SendCmd(null);
			}
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			if (!base.IsLocalPlayer && base.Firearm.AnyModuleBusy(null))
			{
				return;
			}
			this.SendRpc(null, true);
		}

		public override void ClientProcessRpcInstance(NetworkReader reader)
		{
			base.ClientProcessRpcInstance(reader);
			this._busy = true;
			if (this._doubleActionModule.Cocked)
			{
				this._doubleActionModule.TriggerDecocking(new int?(FirearmAnimatorHashes.Roulette));
				return;
			}
			base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.Roulette, false);
		}

		[ExposedFirearmEvent]
		public void ServerRandomize()
		{
			if (!base.IsServer)
			{
				return;
			}
			int ammoMax = this._cylinderModule.AmmoMax;
			int num = global::UnityEngine.Random.Range(0, ammoMax);
			this._cylinderModule.RotateCylinder(num);
		}

		[ExposedFirearmEvent]
		public void EndSpin()
		{
			this._busy = false;
		}

		private readonly ClientRequestTimer _requestTimer = new ClientRequestTimer();

		private CylinderAmmoModule _cylinderModule;

		private DoubleActionModule _doubleActionModule;

		private float _keyHoldTime;

		private bool _busy;
	}
}
