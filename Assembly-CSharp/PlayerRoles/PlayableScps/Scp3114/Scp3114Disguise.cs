using System;
using System.Collections.Generic;
using PlayerRoles.PlayableScps.Scp049;
using PlayerRoles.Ragdolls;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114;

public class Scp3114Disguise : RagdollAbilityBase<Scp3114Role>
{
	public readonly AbilityCooldown Cooldown = new AbilityCooldown();

	private readonly Dictionary<BasicRagdoll, byte> _prevUnitIds = new Dictionary<BasicRagdoll, byte>();

	[SerializeField]
	private AudioSource _equipSkinSound;

	protected override float RangeSqr => 3.5f;

	protected override float Duration => 5f;

	public event Action<Scp3114HudTranslation> OnClientError;

	public override void SpawnObject()
	{
		base.SpawnObject();
		RagdollManager.ServerOnRagdollCreated += ServerOnRagdollCreated;
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this.Cooldown.Clear();
		RagdollManager.ServerOnRagdollCreated -= ServerOnRagdollCreated;
	}

	protected override void OnKeyDown()
	{
		base.OnKeyDown();
		if (this.Cooldown.IsReady)
		{
			base.ClientTryStart();
		}
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
			curIdentity.UnitNameId = (byte)(this._prevUnitIds.TryGetValue(base.CurRagdoll, out var value) ? value : 0);
			curIdentity.Status = Scp3114Identity.DisguiseStatus.Equipping;
		}
		else if (curIdentity.Status == Scp3114Identity.DisguiseStatus.Equipping)
		{
			this._equipSkinSound.Stop();
			curIdentity.Status = Scp3114Identity.DisguiseStatus.None;
			this.Cooldown.Trigger(this.Duration);
		}
	}

	protected override void ServerComplete()
	{
		base.CastRole.Disguised = true;
		if (!(base.CurRagdoll == null) && base.CurRagdoll is DynamicRagdoll ragdoll)
		{
			Scp3114RagdollToBonesConverter.ServerConvertNew(base.CastRole, ragdoll);
		}
	}

	protected override byte ServerValidateBegin(BasicRagdoll ragdoll)
	{
		Scp3114HudTranslation error;
		return (byte)((!this.AnyValidateBegin(ragdoll, out error)) ? error : Scp3114HudTranslation.IdentityStolenWarning);
	}

	protected override bool ClientValidateBegin(BasicRagdoll raycastedRagdoll)
	{
		if (this.AnyValidateBegin(raycastedRagdoll, out var error))
		{
			return base.ClientValidateBegin(raycastedRagdoll);
		}
		this.OnClientError(error);
		return false;
	}

	private bool AnyValidateBegin(BasicRagdoll rg, out Scp3114HudTranslation error)
	{
		if (!rg.Info.RoleType.IsHuman())
		{
			error = Scp3114HudTranslation.RagdollErrorNotHuman;
			return false;
		}
		if (rg.Info.Handler is Scp3114DamageHandler { Subtype: Scp3114DamageHandler.HandlerType.SkinSteal })
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
		if (owner.roleManager.CurrentRole is HumanRole humanRole)
		{
			this._prevUnitIds[ragdoll] = humanRole.UnitNameId;
		}
	}
}
