using System;
using System.Collections.Generic;
using MapGeneration;
using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114
{
	public class Scp3114VoiceLines : StandardSubroutine<Scp3114Role>
	{
		protected override void Awake()
		{
			base.Awake();
			this._voiceLines.ForEach(delegate(Scp3114VoiceLines.VoiceLinesDefinition x)
			{
				x.Init();
			});
			base.CastRole.CurIdentity.OnStatusChanged += this.OnStatusChanged;
			Scp3114Slap scp3114Slap;
			base.GetSubroutine<Scp3114Slap>(out scp3114Slap);
			scp3114Slap.ServerOnHit += delegate
			{
				this.ServerPlayConditionally(Scp3114VoiceLines.VoiceLinesName.Slap);
			};
			scp3114Slap.ServerOnKill += delegate
			{
				this.ServerPlayConditionally(Scp3114VoiceLines.VoiceLinesName.KillSlap);
			};
			Scp3114Strangle scp3114Strangle;
			base.GetSubroutine<Scp3114Strangle>(out scp3114Strangle);
			scp3114Strangle.ServerOnBegin += delegate
			{
				this.ServerPlayConditionally(Scp3114VoiceLines.VoiceLinesName.StartStrangle);
			};
			scp3114Strangle.ServerOnKill += delegate
			{
				this.ServerPlayConditionally(Scp3114VoiceLines.VoiceLinesName.KillStrangle);
			};
		}

		private void OnStatusChanged()
		{
			switch (base.CastRole.CurIdentity.Status)
			{
			case Scp3114Identity.DisguiseStatus.None:
				if (this._hasDisguise)
				{
					this._hasDisguise = false;
					this.ServerPlayConditionally(Scp3114VoiceLines.VoiceLinesName.Reveal);
				}
				return;
			case Scp3114Identity.DisguiseStatus.Equipping:
				this.ServerPlayConditionally(Scp3114VoiceLines.VoiceLinesName.EquipStart);
				return;
			case Scp3114Identity.DisguiseStatus.Active:
				this._hasDisguise = true;
				this._idleRemaining = this._idleCycleTime;
				return;
			default:
				return;
			}
		}

		private void Update()
		{
			if (!this._randomized)
			{
				this.RandomizeWhenReady();
			}
			if (!NetworkServer.active || this._hasDisguise)
			{
				return;
			}
			this._idleRemaining -= Time.deltaTime;
			if (this._idleRemaining > 0f)
			{
				return;
			}
			this._idleRemaining = this._idleCycleTime;
			this.ServerPlayConditionally(Scp3114VoiceLines.VoiceLinesName.RandomIdle);
		}

		private void RandomizeWhenReady()
		{
			if (!SeedSynchronizer.MapGenerated)
			{
				return;
			}
			int seed = SeedSynchronizer.Seed + (int)base.Owner.netId;
			this._voiceLines.ForEach(delegate(Scp3114VoiceLines.VoiceLinesDefinition x)
			{
				x.Randomize(seed);
			});
			this._randomized = true;
		}

		private void ServerPlayConditionally(Scp3114VoiceLines.VoiceLinesName lineToPlay)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			Scp3114VoiceLines.VoiceLinesDefinition voiceLinesDefinition = null;
			float num = float.PositiveInfinity;
			foreach (Scp3114VoiceLines.VoiceLinesDefinition voiceLinesDefinition2 in this._voiceLines)
			{
				num = Mathf.Min(num, voiceLinesDefinition2.LastUseElapsedSeconds);
				if (voiceLinesDefinition2.Label == lineToPlay)
				{
					voiceLinesDefinition = voiceLinesDefinition2;
				}
			}
			if (voiceLinesDefinition == null)
			{
				return;
			}
			if (voiceLinesDefinition.MinIdleTime > num)
			{
				return;
			}
			ushort num2;
			if (!voiceLinesDefinition.TryDrawNext(out num2))
			{
				return;
			}
			this._syncName = (byte)lineToPlay;
			this._syncId = num2;
			base.ServerSendRpc(true);
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteByte(this._syncName);
			writer.WriteUShort(this._syncId);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			this._syncName = reader.ReadByte();
			this._syncId = reader.ReadUShort();
			foreach (Scp3114VoiceLines.VoiceLinesDefinition voiceLinesDefinition in this._voiceLines)
			{
				if ((byte)voiceLinesDefinition.Label == this._syncName)
				{
					if (this._source.isPlaying)
					{
						this._source.Stop();
					}
					this._source.PlayOneShot(voiceLinesDefinition.GetClip((int)this._syncId));
				}
			}
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			this._randomized = false;
			if (!NetworkServer.active)
			{
				return;
			}
			this._hasDisguise = false;
			this._idleRemaining = this._idleCycleTime;
		}

		[SerializeField]
		private Scp3114VoiceLines.VoiceLinesDefinition[] _voiceLines;

		[SerializeField]
		private AudioSource _source;

		[SerializeField]
		private float _idleCycleTime;

		private float _idleRemaining;

		private byte _syncName;

		private ushort _syncId;

		private bool _hasDisguise;

		private bool _randomized;

		[Serializable]
		private class VoiceLinesDefinition
		{
			public float LastUseElapsedSeconds
			{
				get
				{
					return (float)(NetworkTime.time - this._lastUse - (double)this.MaxDuration);
				}
			}

			public bool TryDrawNext(out ushort clipId)
			{
				if (global::UnityEngine.Random.value > this.Chance)
				{
					clipId = 0;
					return false;
				}
				this._lastUse = NetworkTime.time;
				this._lastIndex++;
				clipId = (ushort)this._lastIndex;
				return true;
			}

			public AudioClip GetClip(int index)
			{
				return this.RandomClips[this._order[index % this._order.Count]];
			}

			public void Init()
			{
				this._lastUse = 0.0;
				if (this._order == null)
				{
					this._order = new List<int>(this.RandomClips.Length);
				}
				this._order.Add(0);
			}

			public void Randomize(int seed)
			{
				this._order.Clear();
				for (int i = 0; i < this.RandomClips.Length; i++)
				{
					this._order.Add(i);
				}
				this._order.ShuffleList(new global::System.Random(seed));
			}

			public Scp3114VoiceLines.VoiceLinesName Label;

			public AudioClip[] RandomClips;

			public float MinIdleTime;

			public float MaxDuration;

			[Range(0f, 1f)]
			public float Chance;

			private double _lastUse;

			private List<int> _order;

			private int _lastIndex;
		}

		private enum VoiceLinesName
		{
			KillSlap,
			KillStrangle,
			Slap,
			RandomIdle,
			Reveal,
			EquipStart,
			StartStrangle
		}
	}
}
