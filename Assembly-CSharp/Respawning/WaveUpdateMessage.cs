using System;
using Mirror;
using Respawning.Waves;
using Respawning.Waves.Generic;
using Utils.Networking;

namespace Respawning;

public struct WaveUpdateMessage : NetworkMessage
{
	public readonly float? SpawnIntervalSeconds;

	public readonly float? TimePassed;

	public readonly float? PauseDuration;

	public readonly int? RespawnTokens;

	private readonly UpdateMessageFlags _flags;

	private readonly int _index;

	public SpawnableWaveBase Wave { get; private set; }

	public bool IsTrigger => WaveUpdateMessage.HasFlagFast(this._flags, UpdateMessageFlags.Trigger);

	public bool IsSpawn => WaveUpdateMessage.HasFlagFast(this._flags, UpdateMessageFlags.Spawn);

	public static void ServerSendUpdate(SpawnableWaveBase wave, UpdateMessageFlags flags)
	{
		if (NetworkServer.active)
		{
			new WaveUpdateMessage(wave, flags).SendToAuthenticated();
		}
	}

	public WaveUpdateMessage(NetworkReader reader)
	{
		this._index = reader.ReadInt();
		this._flags = (UpdateMessageFlags)reader.ReadByte();
		if (!WaveManager.Waves.TryGet(this._index, out var element))
		{
			throw new ArgumentOutOfRangeException($"Failed to get spawnable wave of index: {this._index}.");
		}
		this.Wave = element;
		this.RespawnTokens = null;
		this.SpawnIntervalSeconds = null;
		this.TimePassed = null;
		this.PauseDuration = null;
		if (WaveUpdateMessage.HasFlagFast(this._flags, UpdateMessageFlags.Tokens) && this.Wave is ILimitedWave)
		{
			this.RespawnTokens = reader.ReadInt();
		}
		if (this.Wave is TimeBasedWave)
		{
			if (WaveUpdateMessage.HasFlagFast(this._flags, UpdateMessageFlags.Timer))
			{
				this.SpawnIntervalSeconds = reader.ReadFloat();
				this.TimePassed = reader.ReadFloat();
			}
			if (WaveUpdateMessage.HasFlagFast(this._flags, UpdateMessageFlags.Pause))
			{
				this.PauseDuration = reader.ReadFloat();
			}
		}
	}

	private WaveUpdateMessage(SpawnableWaveBase wave, UpdateMessageFlags flags)
	{
		this.Wave = wave;
		this._flags = flags;
		this._index = WaveManager.Waves.IndexOf(wave);
		this.RespawnTokens = ((wave is ILimitedWave limitedWave) ? new int?(limitedWave.RespawnTokens) : ((int?)null));
		if (this.Wave is TimeBasedWave timeBasedWave)
		{
			this.SpawnIntervalSeconds = timeBasedWave.Timer.SpawnIntervalSeconds;
			this.TimePassed = timeBasedWave.Timer.TimePassed;
			this.PauseDuration = timeBasedWave.Timer.PauseTimeLeft;
		}
		else
		{
			this.SpawnIntervalSeconds = null;
			this.TimePassed = null;
			this.PauseDuration = null;
		}
	}

	public void Write(NetworkWriter writer)
	{
		writer.WriteInt(this._index);
		writer.WriteByte((byte)this._flags);
		if (WaveUpdateMessage.HasFlagFast(this._flags, UpdateMessageFlags.Tokens) && this.Wave is ILimitedWave limitedWave)
		{
			writer.WriteInt(limitedWave.RespawnTokens);
		}
		if (this.Wave is TimeBasedWave timeBasedWave)
		{
			if (WaveUpdateMessage.HasFlagFast(this._flags, UpdateMessageFlags.Timer))
			{
				writer.WriteFloat(timeBasedWave.Timer.SpawnIntervalSeconds);
				writer.WriteFloat(timeBasedWave.Timer.TimePassed);
			}
			if (WaveUpdateMessage.HasFlagFast(this._flags, UpdateMessageFlags.Pause))
			{
				writer.WriteFloat(timeBasedWave.Timer.PauseTimeLeft);
			}
		}
	}

	private static bool HasFlagFast(UpdateMessageFlags flags, UpdateMessageFlags flag)
	{
		return (flags & flag) == flag;
	}
}
