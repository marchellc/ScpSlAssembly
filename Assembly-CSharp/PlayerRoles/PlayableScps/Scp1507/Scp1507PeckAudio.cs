using System;
using AudioPooling;
using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp1507
{
	public class Scp1507PeckAudio : StandardSubroutine<Scp1507Role>
	{
		protected override void Awake()
		{
			base.Awake();
			Scp1507AttackAbility scp1507AttackAbility;
			base.GetSubroutine<Scp1507AttackAbility>(out scp1507AttackAbility);
			scp1507AttackAbility.ServerOnDoorAttacked += delegate
			{
				this._attackedDoor = true;
				base.ServerSendRpc(true);
			};
			scp1507AttackAbility.ServerOnMissed += delegate
			{
				this._attackedDoor = false;
				base.ServerSendRpc(true);
			};
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			writer.WriteBool(this._attackedDoor);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			AudioClip audioClip = (reader.ReadBool() ? this._doorHitClip : this._missClip);
			float num = 1f + global::UnityEngine.Random.Range(-this._audioPitchOffsetRandomization, this._audioPitchOffsetRandomization);
			AudioSourcePoolManager.PlayOnTransform(audioClip, base.transform, 13f, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, num);
		}

		[SerializeField]
		private AudioClip _missClip;

		[SerializeField]
		private AudioClip _doorHitClip;

		[SerializeField]
		private float _audioPitchOffsetRandomization;

		private const float SoundRange = 13f;

		private const float BasePitch = 1f;

		private bool _attackedDoor;
	}
}
