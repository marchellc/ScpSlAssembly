using System;
using Mirror;
using Respawning.Waves;
using Respawning.Waves.Generic;
using Utils.Networking;

namespace Respawning
{
	public struct WaveUpdateMessage : NetworkMessage
	{
		public SpawnableWaveBase Wave { readonly get; private set; }

		public bool IsTrigger
		{
			get
			{
				return WaveUpdateMessage.HasFlagFast(this._flags, UpdateMessageFlags.Trigger);
			}
		}

		public static void ServerSendUpdate(SpawnableWaveBase wave, UpdateMessageFlags flags)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			new WaveUpdateMessage(wave, flags).SendToAuthenticated(0);
		}

		public WaveUpdateMessage(NetworkReader reader)
		{
			this._index = reader.ReadInt();
			this._flags = (UpdateMessageFlags)reader.ReadByte();
			SpawnableWaveBase spawnableWaveBase;
			if (!WaveManager.Waves.TryGet(this._index, out spawnableWaveBase))
			{
				throw new ArgumentOutOfRangeException(string.Format("Failed to get spawnable wave of index: {0}.", this._index));
			}
			this.Wave = spawnableWaveBase;
			this.RespawnTokens = null;
			this.SpawnIntervalSeconds = null;
			this.TimePassed = null;
			this.PauseDuration = null;
			if (WaveUpdateMessage.HasFlagFast(this._flags, UpdateMessageFlags.Tokens) && this.Wave is ILimitedWave)
			{
				this.RespawnTokens = new int?(reader.ReadInt());
			}
			if (!(this.Wave is TimeBasedWave))
			{
				return;
			}
			if (WaveUpdateMessage.HasFlagFast(this._flags, UpdateMessageFlags.Timer))
			{
				this.SpawnIntervalSeconds = new float?(reader.ReadFloat());
				this.TimePassed = new float?(reader.ReadFloat());
			}
			if (WaveUpdateMessage.HasFlagFast(this._flags, UpdateMessageFlags.Pause))
			{
				this.PauseDuration = new float?(reader.ReadFloat());
			}
		}

		private WaveUpdateMessage(SpawnableWaveBase wave, UpdateMessageFlags flags)
		{
			this.Wave = wave;
			this._flags = flags;
			this._index = WaveManager.Waves.IndexOf(wave);
			ILimitedWave limitedWave = wave as ILimitedWave;
			this.RespawnTokens = ((limitedWave != null) ? new int?(limitedWave.RespawnTokens) : null);
			TimeBasedWave timeBasedWave = this.Wave as TimeBasedWave;
			if (timeBasedWave != null)
			{
				this.SpawnIntervalSeconds = new float?(timeBasedWave.Timer.SpawnIntervalSeconds);
				this.TimePassed = new float?(timeBasedWave.Timer.TimePassed);
				this.PauseDuration = new float?(timeBasedWave.Timer.PauseTimeLeft);
				return;
			}
			this.SpawnIntervalSeconds = null;
			this.TimePassed = null;
			this.PauseDuration = null;
		}

		public void Write(NetworkWriter writer)
		{
			writer.WriteInt(this._index);
			writer.WriteByte((byte)this._flags);
			if (WaveUpdateMessage.HasFlagFast(this._flags, UpdateMessageFlags.Tokens))
			{
				ILimitedWave limitedWave = this.Wave as ILimitedWave;
				if (limitedWave != null)
				{
					writer.WriteInt(limitedWave.RespawnTokens);
				}
			}
			TimeBasedWave timeBasedWave = this.Wave as TimeBasedWave;
			if (timeBasedWave != null)
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

		public readonly float? SpawnIntervalSeconds;

		public readonly float? TimePassed;

		public readonly float? PauseDuration;

		public readonly int? RespawnTokens;

		private readonly UpdateMessageFlags _flags;

		private readonly int _index;
	}
}
