using System;
using System.Runtime.InteropServices;
using Mirror;
using UnityEngine;
using VoiceChat.Playbacks;

namespace AdminToys;

public class SpeakerToy : AdminToyBase
{
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

	public override string CommandName => "Speaker";

	public byte NetworkControllerId
	{
		get
		{
			return this.ControllerId;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.ControllerId, 32uL, OnControllerIdChanged);
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
			base.GeneratedSyncVarSetter(value, ref this.IsSpatial, 64uL, OnIsSpatialChanged);
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
			base.GeneratedSyncVarSetter(value, ref this.Volume, 128uL, OnVolumeChanged);
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
			base.GeneratedSyncVarSetter(value, ref this.MinDistance, 256uL, OnMinDistanceChanged);
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
			base.GeneratedSyncVarSetter(value, ref this.MaxDistance, 512uL, OnMaxDistanceChanged);
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

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			NetworkWriterExtensions.WriteByte(writer, this.ControllerId);
			writer.WriteBool(this.IsSpatial);
			writer.WriteFloat(this.Volume);
			writer.WriteFloat(this.MinDistance);
			writer.WriteFloat(this.MaxDistance);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 0x20L) != 0L)
		{
			NetworkWriterExtensions.WriteByte(writer, this.ControllerId);
		}
		if ((base.syncVarDirtyBits & 0x40L) != 0L)
		{
			writer.WriteBool(this.IsSpatial);
		}
		if ((base.syncVarDirtyBits & 0x80L) != 0L)
		{
			writer.WriteFloat(this.Volume);
		}
		if ((base.syncVarDirtyBits & 0x100L) != 0L)
		{
			writer.WriteFloat(this.MinDistance);
		}
		if ((base.syncVarDirtyBits & 0x200L) != 0L)
		{
			writer.WriteFloat(this.MaxDistance);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.ControllerId, OnControllerIdChanged, NetworkReaderExtensions.ReadByte(reader));
			base.GeneratedSyncVarDeserialize(ref this.IsSpatial, OnIsSpatialChanged, reader.ReadBool());
			base.GeneratedSyncVarDeserialize(ref this.Volume, OnVolumeChanged, reader.ReadFloat());
			base.GeneratedSyncVarDeserialize(ref this.MinDistance, OnMinDistanceChanged, reader.ReadFloat());
			base.GeneratedSyncVarDeserialize(ref this.MaxDistance, OnMaxDistanceChanged, reader.ReadFloat());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 0x20L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.ControllerId, OnControllerIdChanged, NetworkReaderExtensions.ReadByte(reader));
		}
		if ((num & 0x40L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.IsSpatial, OnIsSpatialChanged, reader.ReadBool());
		}
		if ((num & 0x80L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.Volume, OnVolumeChanged, reader.ReadFloat());
		}
		if ((num & 0x100L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.MinDistance, OnMinDistanceChanged, reader.ReadFloat());
		}
		if ((num & 0x200L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.MaxDistance, OnMaxDistanceChanged, reader.ReadFloat());
		}
	}
}
