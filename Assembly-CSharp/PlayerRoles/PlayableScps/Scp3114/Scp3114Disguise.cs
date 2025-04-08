using System;
using System.Collections.Generic;
using PlayerRoles.PlayableScps.Scp049;
using PlayerRoles.Ragdolls;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114
{
	public class Scp3114Disguise : RagdollAbilityBase<Scp3114Role>
	{
		protected override float RangeSqr
		{
			get
			{
				return 3.5f;
			}
		}

		protected override float Duration
		{
			get
			{
				return 5f;
			}
		}

		public event Action<Scp3114HudTranslation> OnClientError;

		public override void SpawnObject()
		{
			base.SpawnObject();
			RagdollManager.ServerOnRagdollCreated += this.ServerOnRagdollCreated;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this.Cooldown.Clear();
			RagdollManager.ServerOnRagdollCreated -= this.ServerOnRagdollCreated;
		}

		protected override void OnKeyDown()
		{
			base.OnKeyDown();
			if (!this.Cooldown.IsReady)
			{
				return;
			}
			base.ClientTryStart();
		}

		protected override void OnKeyUp()
		{
			base.OnKeyUp();
			base.ClientTryCancel();
		}

		protected override void OnProgressSet()
		{
			base.OnProgressSet();
			Scp3114Identity.StolenIdentity curIdentity = base.CastRole.CurIdentity;
			if (base.IsInProgress)
			{
				this._equipSkinSound.Play();
				curIdentity.Ragdoll = base.CurRagdoll;
				byte b;
				curIdentity.UnitNameId = (this._prevUnitIds.TryGetValue(base.CurRagdoll, out b) ? b : 0);
				curIdentity.Status = Scp3114Identity.DisguiseStatus.Equipping;
				return;
			}
			if (curIdentity.Status == Scp3114Identity.DisguiseStatus.Equipping)
			{
				this._equipSkinSound.Stop();
				curIdentity.Status = Scp3114Identity.DisguiseStatus.None;
				this.Cooldown.Trigger((double)this.Duration);
			}
		}

		protected override void ServerComplete()
		{
			base.CastRole.Disguised = true;
			if (!(base.CurRagdoll == null))
			{
				DynamicRagdoll dynamicRagdoll = base.CurRagdoll as DynamicRagdoll;
				if (dynamicRagdoll != null)
				{
					Scp3114RagdollToBonesConverter.ServerConvertNew(base.CastRole, dynamicRagdoll);
					return;
				}
			}
		}

		protected override byte ServerValidateBegin(BasicRagdoll ragdoll)
		{
			Scp3114HudTranslation scp3114HudTranslation;
			return (byte)((!this.AnyValidateBegin(ragdoll, out scp3114HudTranslation)) ? scp3114HudTranslation : Scp3114HudTranslation.IdentityStolenWarning);
		}

		protected override bool ClientValidateBegin(BasicRagdoll raycastedRagdoll)
		{
			Scp3114HudTranslation scp3114HudTranslation;
			if (this.AnyValidateBegin(raycastedRagdoll, out scp3114HudTranslation))
			{
				return base.ClientValidateBegin(raycastedRagdoll);
			}
			this.OnClientError(scp3114HudTranslation);
			return false;
		}

		private bool AnyValidateBegin(BasicRagdoll rg, out Scp3114HudTranslation error)
		{
			if (!rg.Info.RoleType.IsHuman())
			{
				error = Scp3114HudTranslation.RagdollErrorNotHuman;
				return false;
			}
			Scp3114DamageHandler scp3114DamageHandler = rg.Info.Handler as Scp3114DamageHandler;
			if (scp3114DamageHandler != null && scp3114DamageHandler.Subtype == Scp3114DamageHandler.HandlerType.SkinSteal)
			{
				error = Scp3114HudTranslation.RagdollErrorPreviouslyUsed;
				return false;
			}
			if (base.CastRole.Disguised)
			{
				error = Scp3114HudTranslation.RagdollErrorAlreadyDisguised;
				return false;
			}
			error = Scp3114HudTranslation.IdentityStolenWarning;
			return true;
		}

		private void ServerOnRagdollCreated(ReferenceHub owner, BasicRagdoll ragdoll)
		{
			HumanRole humanRole = owner.roleManager.CurrentRole as HumanRole;
			if (humanRole == null)
			{
				return;
			}
			this._prevUnitIds[ragdoll] = humanRole.UnitNameId;
		}

		public readonly AbilityCooldown Cooldown = new AbilityCooldown();

		private readonly Dictionary<BasicRagdoll, byte> _prevUnitIds = new Dictionary<BasicRagdoll, byte>();

		[SerializeField]
		private AudioSource _equipSkinSound;
	}
}
