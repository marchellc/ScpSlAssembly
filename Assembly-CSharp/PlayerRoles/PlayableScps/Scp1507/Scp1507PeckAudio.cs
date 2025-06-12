using AudioPooling;
using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp1507;

public class Scp1507PeckAudio : StandardSubroutine<Scp1507Role>
{
	[SerializeField]
	private AudioClip _missClip;

	[SerializeField]
	private AudioClip _doorHitClip;

	[SerializeField]
	private float _audioPitchOffsetRandomization;

	private const float SoundRange = 13f;

	private const float BasePitch = 1f;

	private bool _attackedDoor;

	protected override void Awake()
	{
		base.Awake();
		base.GetSubroutine<Scp1507AttackAbility>(out var sr);
		sr.ServerOnDoorAttacked += delegate
		{
			this._attackedDoor = true;
			base.ServerSendRpc(toAll: true);
		};
		sr.ServerOnMissed += delegate
		{
			this._attackedDoor = false;
			base.ServerSendRpc(toAll: true);
		};
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		writer.WriteBool(this._attackedDoor);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		AudioSourcePoolManager.PlayOnTransform(reader.ReadBool() ? this._doorHitClip : this._missClip, pitchScale: 1f + Random.Range(0f - this._audioPitchOffsetRandomization, this._audioPitchOffsetRandomization), trackedTransform: base.transform, maxDistance: 13f);
	}
}
