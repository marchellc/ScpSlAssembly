using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Subroutines;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp049.Zombies;

public class ZombieAudioPlayer : SubroutineBase
{
	[Serializable]
	private class Scp0492Sound
	{
		public Scp0492SoundId Id;

		public float MaxDistance;
	}

	public enum Scp0492SoundId : byte
	{
		Growl,
		AngryGrowl,
		Attack
	}

	private const float GrowlMaxCooldown = 7.5f;

	private const float GrowlMinCooldown = 11.25f;

	private static bool _soundsSerialized = false;

	private static readonly Dictionary<byte, Scp0492Sound> Sounds = new Dictionary<byte, Scp0492Sound>();

	public readonly AbilityCooldown GrowlTimer = new AbilityCooldown();

	[SerializeField]
	private Scp0492Sound[] _sounds;

	private ZombieBloodlustAbility _visionTracker;

	private byte _soundToSend;

	private Vector3 _lastPos;

	public void ServerGrowl()
	{
		this.GrowlTimer.Trigger(UnityEngine.Random.Range(11.25f, 7.5f));
		this.ServerSendSound(this._visionTracker.LookingAtTarget ? Scp0492SoundId.AngryGrowl : Scp0492SoundId.Growl);
	}

	private void Update()
	{
		if (NetworkServer.active && this.GrowlTimer.IsReady)
		{
			this.ServerGrowl();
		}
	}

	protected override void Awake()
	{
		base.Awake();
		(base.Role as ZombieRole).SubroutineModule.TryGetSubroutine<ZombieBloodlustAbility>(out this._visionTracker);
		if (!ZombieAudioPlayer._soundsSerialized)
		{
			Scp0492Sound[] sounds = this._sounds;
			foreach (Scp0492Sound scp0492Sound in sounds)
			{
				ZombieAudioPlayer.Sounds.Add((byte)scp0492Sound.Id, scp0492Sound);
			}
			ZombieAudioPlayer._soundsSerialized = true;
		}
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteByte(this._soundToSend);
		writer.WriteRelativePosition(new RelativePosition(this._lastPos));
	}

	public void ServerSendSound(Scp0492SoundId soundId)
	{
		this._soundToSend = (byte)soundId;
		this._lastPos = (base.Role as ZombieRole).FpcModule.Position;
		if (ZombieAudioPlayer.Sounds.TryGetValue(this._soundToSend, out var value))
		{
			float disSqr = value.MaxDistance * value.MaxDistance;
			base.ServerSendRpc((ReferenceHub x) => !(x.roleManager.CurrentRole is IFpcRole fpcRole) || (fpcRole.FpcModule.Position - this._lastPos).sqrMagnitude <= disSqr);
		}
	}
}
