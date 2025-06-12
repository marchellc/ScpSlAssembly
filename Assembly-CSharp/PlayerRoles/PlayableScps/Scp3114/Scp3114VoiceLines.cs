using System;
using System.Collections.Generic;
using MapGeneration;
using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;
using UserSettings;
using UserSettings.AudioSettings;

namespace PlayerRoles.PlayableScps.Scp3114;

public class Scp3114VoiceLines : StandardSubroutine<Scp3114Role>
{
	[Serializable]
	private class VoiceLinesDefinition
	{
		public VoiceLinesName Label;

		public AudioClip[] RandomClips;

		public float MinIdleTime;

		public float MaxDuration;

		[Range(0f, 1f)]
		public float Chance;

		private double _lastUse;

		private List<int> _order;

		private int _lastIndex;

		public float LastUseElapsedSeconds => (float)(NetworkTime.time - this._lastUse - (double)this.MaxDuration);

		public bool TryDrawNext(out ushort clipId)
		{
			if (UnityEngine.Random.value > this.Chance)
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
			this._order.ShuffleList(new System.Random(seed));
		}
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

	private static readonly CachedUserSetting<float> VolumeSetting = new CachedUserSetting<float>(MixerAudioSettings.VolumeSliderSetting.Scp3114Voice);

	[SerializeField]
	private VoiceLinesDefinition[] _voiceLines;

	[SerializeField]
	private AudioSource _source;

	[SerializeField]
	private float _idleCycleTime;

	private float _idleRemaining;

	private byte _syncName;

	private ushort _syncId;

	private bool _hasDisguise;

	private bool _randomized;

	protected override void Awake()
	{
		base.Awake();
		this._voiceLines.ForEach(delegate(VoiceLinesDefinition x)
		{
			x.Init();
		});
		base.CastRole.CurIdentity.OnStatusChanged += OnStatusChanged;
		base.GetSubroutine<Scp3114Slap>(out var sr);
		sr.ServerOnHit += delegate
		{
			this.ServerPlayConditionally(VoiceLinesName.Slap);
		};
		sr.ServerOnKill += delegate
		{
			this.ServerPlayConditionally(VoiceLinesName.KillSlap);
		};
		base.GetSubroutine<Scp3114Strangle>(out var sr2);
		sr2.ServerOnBegin += delegate
		{
			this.ServerPlayConditionally(VoiceLinesName.StartStrangle);
		};
		sr2.ServerOnKill += delegate
		{
			this.ServerPlayConditionally(VoiceLinesName.KillStrangle);
		};
	}

	private void OnStatusChanged()
	{
		switch (base.CastRole.CurIdentity.Status)
		{
		case Scp3114Identity.DisguiseStatus.Active:
			this._hasDisguise = true;
			this._idleRemaining = this._idleCycleTime;
			break;
		case Scp3114Identity.DisguiseStatus.Equipping:
			this.ServerPlayConditionally(VoiceLinesName.EquipStart);
			break;
		case Scp3114Identity.DisguiseStatus.None:
			if (this._hasDisguise)
			{
				this._hasDisguise = false;
				this.ServerPlayConditionally(VoiceLinesName.Reveal);
			}
			break;
		}
	}

	private void Update()
	{
		if (!this._randomized)
		{
			this.RandomizeWhenReady();
		}
		if (NetworkServer.active && !this._hasDisguise)
		{
			this._idleRemaining -= Time.deltaTime;
			if (!(this._idleRemaining > 0f))
			{
				this._idleRemaining = this._idleCycleTime;
				this.ServerPlayConditionally(VoiceLinesName.RandomIdle);
			}
		}
	}

	private void RandomizeWhenReady()
	{
		if (SeedSynchronizer.MapGenerated)
		{
			int seed = SeedSynchronizer.Seed + (int)base.Owner.netId;
			this._voiceLines.ForEach(delegate(VoiceLinesDefinition x)
			{
				x.Randomize(seed);
			});
			this._randomized = true;
		}
	}

	private void ServerPlayConditionally(VoiceLinesName lineToPlay)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		VoiceLinesDefinition voiceLinesDefinition = null;
		float num = float.PositiveInfinity;
		VoiceLinesDefinition[] voiceLines = this._voiceLines;
		foreach (VoiceLinesDefinition voiceLinesDefinition2 in voiceLines)
		{
			num = Mathf.Min(num, voiceLinesDefinition2.LastUseElapsedSeconds);
			if (voiceLinesDefinition2.Label == lineToPlay)
			{
				voiceLinesDefinition = voiceLinesDefinition2;
			}
		}
		if (voiceLinesDefinition != null && !(voiceLinesDefinition.MinIdleTime > num) && voiceLinesDefinition.TryDrawNext(out var clipId))
		{
			this._syncName = (byte)lineToPlay;
			this._syncId = clipId;
			base.ServerSendRpc(toAll: true);
		}
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
		VoiceLinesDefinition[] voiceLines = this._voiceLines;
		foreach (VoiceLinesDefinition voiceLinesDefinition in voiceLines)
		{
			if ((byte)voiceLinesDefinition.Label == this._syncName)
			{
				if (this._source.isPlaying)
				{
					this._source.Stop();
				}
				this._source.PlayOneShot(voiceLinesDefinition.GetClip(this._syncId), Scp3114VoiceLines.VolumeSetting.Value);
				break;
			}
		}
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		this._randomized = false;
		if (NetworkServer.active)
		{
			this._hasDisguise = false;
			this._idleRemaining = this._idleCycleTime;
		}
	}
}
