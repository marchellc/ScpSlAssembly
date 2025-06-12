using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.Scp173Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Subroutines;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp173;

public class Scp173AudioPlayer : SubroutineBase
{
	[Serializable]
	private class Scp173Sound
	{
		public Scp173SoundId Id;

		public float MaxDistance;
	}

	public enum Scp173SoundId : byte
	{
		Hit,
		Teleport,
		Snap
	}

	[SerializeField]
	private Scp173Sound[] _sounds;

	private byte _soundToSend;

	private Vector3 _lastPos;

	private static bool _soundsDictionarized = false;

	private static readonly Dictionary<byte, Scp173Sound> Sounds = new Dictionary<byte, Scp173Sound>();

	protected override void Awake()
	{
		base.Awake();
		if (!Scp173AudioPlayer._soundsDictionarized)
		{
			Scp173Sound[] sounds = this._sounds;
			foreach (Scp173Sound scp173Sound in sounds)
			{
				Scp173AudioPlayer.Sounds.Add((byte)scp173Sound.Id, scp173Sound);
			}
			Scp173AudioPlayer._soundsDictionarized = true;
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

	public void ServerSendSound(Scp173SoundId soundId)
	{
		ReferenceHub hub;
		bool flag = base.Role.TryGetOwner(out hub);
		if (flag)
		{
			Scp173PlayingSoundEventArgs e = new Scp173PlayingSoundEventArgs(hub, soundId);
			Scp173Events.OnPlayingSound(e);
			if (!e.IsAllowed)
			{
				return;
			}
			soundId = e.SoundId;
		}
		this._soundToSend = (byte)soundId;
		this._lastPos = (base.Role as Scp173Role).FpcModule.Position;
		if (Scp173AudioPlayer.Sounds.TryGetValue(this._soundToSend, out var value))
		{
			float disSqr = value.MaxDistance * value.MaxDistance;
			base.ServerSendRpc((ReferenceHub x) => !(x.roleManager.CurrentRole is IFpcRole fpcRole) || (fpcRole.FpcModule.Position - this._lastPos).sqrMagnitude <= disSqr);
			if (flag)
			{
				Scp173Events.OnPlayedSound(new Scp173PlayedSoundEventArgs(hub, soundId));
			}
		}
	}
}
