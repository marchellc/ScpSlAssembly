using System;
using System.Runtime.InteropServices;
using Mirror;
using UnityEngine;
using VoiceChat.Playbacks;

namespace AdminToys
{
	public class SpeakerToy : AdminToyBase
	{
		public override string CommandName
		{
			get
			{
				return "Speaker";
			}
		}

		public override void OnSpawned(ReferenceHub admin, ArraySegment<string> arguments)
		{
			base.OnSpawned(admin, arguments);
			base.transform.position = admin.PlayerCameraReference.position;
			base.transform.localScale = Vector3.one;
			this.Playback.ControllerId = this.ControllerId;
			this.Playback.Source.spatialBlend = (this.IsSpatial ? 1f : 0f);
			this.Playback.Source.volume = Math.Clamp(this.Volume, 0f, 1f);
			this.Playback.Source.minDistance = this.MinDistance;
			this.Playback.Source.maxDistance = this.MaxDistance;
		}

		private void OnControllerIdChanged(byte _, byte id)
		{
			this.Playback.ControllerId = id;
		}

		private void OnIsSpatialChanged(bool _, bool isSpatial)
		{
			this.Playback.Source.spatialBlend = (isSpatial ? 1f : 0f);
		}

		private void OnVolumeChanged(float _, float volume)
		{
			this.Playback.Source.volume = Math.Clamp(volume, 0f, 1f);
		}

		private void OnMinDistanceChanged(float _, float distance)
		{
			this.Playback.Source.minDistance = distance;
		}

		private void OnMaxDistanceChanged(float _, float distance)
		{
			this.Playback.Source.maxDistance = distance;
		}

		public override bool Weaved()
		{
			return true;
		}

		public byte NetworkControllerId
		{
			get
			{
				return this.ControllerId;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<byte>(value, ref this.ControllerId, 32UL, new Action<byte, byte>(this.OnControllerIdChanged));
			}
		}

		public bool NetworkIsSpatial
		{
			get
			{
				return this.IsSpatial;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<bool>(value, ref this.IsSpatial, 64UL, new Action<bool, bool>(this.OnIsSpatialChanged));
			}
		}

		public float NetworkVolume
		{
			get
			{
				return this.Volume;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<float>(value, ref this.Volume, 128UL, new Action<float, float>(this.OnVolumeChanged));
			}
		}

		public float NetworkMinDistance
		{
			get
			{
				return this.MinDistance;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<float>(value, ref this.MinDistance, 256UL, new Action<float, float>(this.OnMinDistanceChanged));
			}
		}

		public float NetworkMaxDistance
		{
			get
			{
				return this.MaxDistance;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<float>(value, ref this.MaxDistance, 512UL, new Action<float, float>(this.OnMaxDistanceChanged));
			}
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				writer.WriteByte(this.ControllerId);
				writer.WriteBool(this.IsSpatial);
				writer.WriteFloat(this.Volume);
				writer.WriteFloat(this.MinDistance);
				writer.WriteFloat(this.MaxDistance);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 32UL) != 0UL)
			{
				writer.WriteByte(this.ControllerId);
			}
			if ((base.syncVarDirtyBits & 64UL) != 0UL)
			{
				writer.WriteBool(this.IsSpatial);
			}
			if ((base.syncVarDirtyBits & 128UL) != 0UL)
			{
				writer.WriteFloat(this.Volume);
			}
			if ((base.syncVarDirtyBits & 256UL) != 0UL)
			{
				writer.WriteFloat(this.MinDistance);
			}
			if ((base.syncVarDirtyBits & 512UL) != 0UL)
			{
				writer.WriteFloat(this.MaxDistance);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<byte>(ref this.ControllerId, new Action<byte, byte>(this.OnControllerIdChanged), reader.ReadByte());
				base.GeneratedSyncVarDeserialize<bool>(ref this.IsSpatial, new Action<bool, bool>(this.OnIsSpatialChanged), reader.ReadBool());
				base.GeneratedSyncVarDeserialize<float>(ref this.Volume, new Action<float, float>(this.OnVolumeChanged), reader.ReadFloat());
				base.GeneratedSyncVarDeserialize<float>(ref this.MinDistance, new Action<float, float>(this.OnMinDistanceChanged), reader.ReadFloat());
				base.GeneratedSyncVarDeserialize<float>(ref this.MaxDistance, new Action<float, float>(this.OnMaxDistanceChanged), reader.ReadFloat());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 32L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<byte>(ref this.ControllerId, new Action<byte, byte>(this.OnControllerIdChanged), reader.ReadByte());
			}
			if ((num & 64L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<bool>(ref this.IsSpatial, new Action<bool, bool>(this.OnIsSpatialChanged), reader.ReadBool());
			}
			if ((num & 128L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<float>(ref this.Volume, new Action<float, float>(this.OnVolumeChanged), reader.ReadFloat());
			}
			if ((num & 256L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<float>(ref this.MinDistance, new Action<float, float>(this.OnMinDistanceChanged), reader.ReadFloat());
			}
			if ((num & 512L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<float>(ref this.MaxDistance, new Action<float, float>(this.OnMaxDistanceChanged), reader.ReadFloat());
			}
		}

		public SpeakerToyPlaybackBase Playback;

		[SyncVar(hook = "OnControllerIdChanged")]
		public byte ControllerId;

		[SyncVar(hook = "OnIsSpatialChanged")]
		public bool IsSpatial = true;

		[SyncVar(hook = "OnVolumeChanged")]
		public float Volume = 1f;

		[SyncVar(hook = "OnMinDistanceChanged")]
		public float MinDistance = 1f;

		[SyncVar(hook = "OnMaxDistanceChanged")]
		public float MaxDistance = 15f;
	}
}
