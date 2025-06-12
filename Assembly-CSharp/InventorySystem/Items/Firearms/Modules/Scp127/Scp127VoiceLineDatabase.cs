using System;
using System.Collections.Generic;
using AudioPooling;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Scp127;

[CreateAssetMenu(fileName = "SCP-127 Voice Line Database", menuName = "ScriptableObject/Firearms/SCP-127 Voice Line Database")]
public class Scp127VoiceLineDatabase : ScriptableObject
{
	[Serializable]
	public class VoiceEntry
	{
		private const float SilenceThreshold = 0.01f;

		private const int SilenceFrameSize = 4800;

		public Scp127VoiceLinesTranslation Translation;

		public AudioClip Clip;

		public float[] SegmentTimestamps;

		public float LineDuration
		{
			get
			{
				if (!this.ClipDurationValid)
				{
					return this.Clip.length;
				}
				return this.SegmentTimestamps[^1];
			}
			private set
			{
				if (!this.ClipDurationValid)
				{
					this.SegmentTimestamps = new float[1];
				}
				this.SegmentTimestamps[^1] = value;
			}
		}

		private bool ClipDurationValid
		{
			get
			{
				if (this.SegmentTimestamps != null)
				{
					return this.SegmentTimestamps.Length != 0;
				}
				return false;
			}
		}

		public void RecalculateClipDuration()
		{
			float[] array = new float[this.Clip.samples * this.Clip.channels];
			this.Clip.GetData(array, 0);
			for (int num = array.Length - 1; num >= 0; num -= 4800)
			{
				float num2 = 0f;
				for (int i = 0; i <= 4800; i++)
				{
					int num3 = num - i;
					if (num3 < 0)
					{
						break;
					}
					num2 += Mathf.Abs(array[num3]);
				}
				if (num2 > 48f)
				{
					this.LineDuration = this.Clip.length * ((float)num / (float)array.Length);
					return;
				}
			}
			this.LineDuration = this.Clip.length;
		}
	}

	private readonly Dictionary<AudioClip, VoiceEntry> _entriesByClip = new Dictionary<AudioClip, VoiceEntry>();

	private readonly Dictionary<Scp127VoiceLinesTranslation, AudioClip> _clipsByTranslation = new Dictionary<Scp127VoiceLinesTranslation, AudioClip>();

	private bool _indexingSet;

	public VoiceEntry[] Entries;

	public bool TryGetClip(Scp127VoiceLinesTranslation translation, out AudioClip clip)
	{
		this.SetIndexing(force: false);
		return this._clipsByTranslation.TryGetValue(translation, out clip);
	}

	public bool TryGetEntry(AudioClip voiceLine, out VoiceEntry entry)
	{
		this.SetIndexing(force: false);
		return this._entriesByClip.TryGetValue(voiceLine, out entry);
	}

	public bool IsStillPlaying(AudioPoolSession session)
	{
		if (!session.SameSession)
		{
			return false;
		}
		AudioSource source = session.Source;
		if (!this._entriesByClip.TryGetValue(source.clip, out var value))
		{
			return true;
		}
		float time = source.time;
		float lineDuration = value.LineDuration;
		return time < lineDuration;
	}

	public void SetIndexing(bool force)
	{
		if (!this._indexingSet || force)
		{
			VoiceEntry[] entries = this.Entries;
			foreach (VoiceEntry voiceEntry in entries)
			{
				this._entriesByClip[voiceEntry.Clip] = voiceEntry;
				this._clipsByTranslation[voiceEntry.Translation] = voiceEntry.Clip;
			}
			this._indexingSet = true;
		}
	}
}
