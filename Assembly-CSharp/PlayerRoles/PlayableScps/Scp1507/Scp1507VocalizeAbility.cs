using System;
using AudioPooling;
using CustomPlayerEffects;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Subroutines;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp1507;

public class Scp1507VocalizeAbility : KeySubroutine<Scp1507Role>
{
	private const float ConcussionDuration = 2f;

	private const float ConcussionMaxDistanceSqr = 9f;

	private const float BaseCooldown = 5f;

	private const float HearableRange = 45f;

	private const float TrackingRangeSqr = 250f;

	public readonly AbilityCooldown Cooldown = new AbilityCooldown();

	public Action OnVocalized;

	[SerializeField]
	private AudioClip[] _alphaSounds;

	[SerializeField]
	private AudioClip[] _regularSounds;

	protected override ActionName TargetKey => ActionName.ToggleFlashlight;

	public static event Action<ReferenceHub> OnServerVocalize;

	public void ServerScream()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		Vector3 position = base.CastRole.FpcModule.Position;
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (HitboxIdentity.IsEnemy(base.Owner, allHub) && allHub.roleManager.CurrentRole is IFpcRole target && !(target.SqrDistanceTo(position) > 9f))
			{
				allHub.playerEffectsController.EnableEffect<Concussed>(2f, addDuration: true);
			}
		}
		Scp1507VocalizeAbility.OnServerVocalize?.Invoke(base.Owner);
		base.ServerSendRpc(toAll: true);
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (this.Cooldown.IsReady)
		{
			this.Cooldown.Trigger(5.0);
			this.ServerScream();
		}
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		this.Cooldown.WriteCooldown(writer);
		writer.WriteRelativePosition(new RelativePosition(base.CastRole.FpcModule.Position));
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		this.Cooldown.ReadCooldown(reader);
		AudioClip sound = ((base.Role.RoleTypeId == RoleTypeId.AlphaFlamingo) ? this._alphaSounds : this._regularSounds).RandomItem();
		RelativePosition relativePosition = reader.ReadRelativePosition();
		if ((relativePosition.Position - base.CastRole.FpcModule.Position).sqrMagnitude <= 250f)
		{
			AudioSourcePoolManager.PlayOnTransform(sound, base.transform, 45f);
		}
		else
		{
			AudioSourcePoolManager.PlayAtPosition(sound, relativePosition, 45f);
		}
		this.OnVocalized?.Invoke();
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this.Cooldown.Clear();
	}

	protected override void OnKeyUp()
	{
		base.OnKeyUp();
		if (this.Cooldown.IsReady)
		{
			base.ClientSendCmd();
		}
	}
}
