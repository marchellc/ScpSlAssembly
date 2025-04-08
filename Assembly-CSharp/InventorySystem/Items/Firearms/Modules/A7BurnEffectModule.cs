using System;
using CustomPlayerEffects;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public class A7BurnEffectModule : ModuleBase
	{
		protected override void OnInit()
		{
			base.OnInit();
			IHitregModule hitregModule;
			if (!base.Firearm.TryGetModule(out hitregModule, true))
			{
				return;
			}
			hitregModule.ServerOnFired += this.OnFired;
		}

		private void OnFired()
		{
			ReferenceHub owner = base.Firearm.Owner;
			Vector3 position = owner.transform.position;
			Vector3 forward = owner.PlayerCameraReference.forward;
			Vector3 vector = position + forward * this._forwardOffset;
			float num = this._radius * this._radius;
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
				Burned burned;
				if (fpcRole != null && HitboxIdentity.IsDamageable(owner, referenceHub) && (vector - fpcRole.FpcModule.Position).sqrMagnitude <= num && referenceHub.playerEffectsController.TryGetEffect<Burned>(out burned))
				{
					float num2 = Mathf.Min((float)this._perShotDuration, (float)this._maxDuration - burned.TimeLeft);
					if (num2 > 0f)
					{
						burned.IsEnabled = true;
						burned.ServerChangeDuration(num2, true);
					}
				}
			}
		}

		[SerializeField]
		private int _maxDuration;

		[SerializeField]
		private int _perShotDuration;

		[SerializeField]
		private float _forwardOffset;

		[SerializeField]
		private float _radius;
	}
}
