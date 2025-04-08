using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.Scp173Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Subroutines;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp173
{
	public class Scp173AudioPlayer : SubroutineBase
	{
		protected override void Awake()
		{
			base.Awake();
			if (Scp173AudioPlayer._soundsDictionarized)
			{
				return;
			}
			foreach (Scp173AudioPlayer.Scp173Sound scp173Sound in this._sounds)
			{
				Scp173AudioPlayer.Sounds.Add((byte)scp173Sound.Id, scp173Sound);
			}
			Scp173AudioPlayer._soundsDictionarized = true;
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

		public void ServerSendSound(Scp173AudioPlayer.Scp173SoundId soundId)
		{
			ReferenceHub referenceHub;
			bool flag = base.Role.TryGetOwner(out referenceHub);
			if (flag)
			{
				Scp173PlayingSoundEventArgs scp173PlayingSoundEventArgs = new Scp173PlayingSoundEventArgs(referenceHub, soundId);
				Scp173Events.OnPlayingSound(scp173PlayingSoundEventArgs);
				if (!scp173PlayingSoundEventArgs.IsAllowed)
				{
					return;
				}
				soundId = scp173PlayingSoundEventArgs.SoundId;
			}
			this._soundToSend = (byte)soundId;
			this._lastPos = (base.Role as Scp173Role).FpcModule.Position;
			Scp173AudioPlayer.Scp173Sound scp173Sound;
			if (!Scp173AudioPlayer.Sounds.TryGetValue(this._soundToSend, out scp173Sound))
			{
				return;
			}
			float disSqr = scp173Sound.MaxDistance * scp173Sound.MaxDistance;
			base.ServerSendRpc(delegate(ReferenceHub x)
			{
				IFpcRole fpcRole = x.roleManager.CurrentRole as IFpcRole;
				return fpcRole == null || (fpcRole.FpcModule.Position - this._lastPos).sqrMagnitude <= disSqr;
			});
			if (flag)
			{
				Scp173Events.OnPlayedSound(new Scp173PlayedSoundEventArgs(referenceHub, soundId));
			}
		}

		[SerializeField]
		private Scp173AudioPlayer.Scp173Sound[] _sounds;

		private byte _soundToSend;

		private Vector3 _lastPos;

		private static bool _soundsDictionarized = false;

		private static readonly Dictionary<byte, Scp173AudioPlayer.Scp173Sound> Sounds = new Dictionary<byte, Scp173AudioPlayer.Scp173Sound>();

		[Serializable]
		private class Scp173Sound
		{
			public Scp173AudioPlayer.Scp173SoundId Id;

			public float MaxDistance;
		}

		public enum Scp173SoundId : byte
		{
			Hit,
			Teleport,
			Snap
		}
	}
}
