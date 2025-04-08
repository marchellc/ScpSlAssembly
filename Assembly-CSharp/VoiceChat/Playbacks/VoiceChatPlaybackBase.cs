using System;
using UnityEngine;

namespace VoiceChat.Playbacks
{
	[RequireComponent(typeof(AudioSource))]
	public abstract class VoiceChatPlaybackBase : MonoBehaviour
	{
		public AudioSource Source { get; private set; }

		public float Loudness { get; private set; }

		public abstract int MaxSamples { get; }

		private AudioClip Flatline
		{
			get
			{
				int num = 48000;
				return AudioClip.Create(string.Empty, num, this.Channels, num, true, delegate(float[] buf)
				{
					int num2 = buf.Length;
					if (VoiceChatPlaybackBase._flatlinePcmLen < num2)
					{
						VoiceChatPlaybackBase._flatlinePcm = new float[num2];
						for (int i = 0; i < num2; i++)
						{
							VoiceChatPlaybackBase._flatlinePcm[i] = 1f;
						}
						VoiceChatPlaybackBase._flatlinePcmLen = num2;
					}
					Array.Copy(VoiceChatPlaybackBase._flatlinePcm, buf, num2);
				});
			}
		}

		private void OnAudioFilterRead(float[] data, int channels)
		{
			int num = this.MaxSamples * channels;
			int num2 = data.Length;
			if (num <= 0)
			{
				Array.Clear(data, 0, num2);
				return;
			}
			int i = 0;
			bool flag = num < num2;
			int num3 = (flag ? num : num2);
			while (i < num3)
			{
				float num4 = this.ReadSample();
				for (int j = 0; j < channels; j++)
				{
					data[i] *= num4 * this.VolumeScale;
					i++;
				}
				this._collectedLoudness += Mathf.Abs(num4);
			}
			if (flag)
			{
				Array.Clear(data, num, num2 - num);
			}
			this._collectedSamples += num2;
		}

		protected virtual void OnDisable()
		{
			this.Source.Stop();
		}

		protected virtual void OnEnable()
		{
			this.Source.Play();
		}

		protected virtual void Awake()
		{
			this.Source = base.GetComponent<AudioSource>();
			this.Source.clip = this.Flatline;
			this.Source.loop = true;
			this.Source.bypassReverbZones = true;
		}

		protected virtual void Update()
		{
			if (this._collectedSamples >= 1200)
			{
				float num = this._collectedLoudness * 5f;
				this._targetLoudness = Mathf.Sqrt(num / (float)this._collectedSamples);
				this._collectedLoudness = 0f;
				this._collectedSamples = 0;
			}
			this.Loudness = Mathf.Lerp(this.Loudness, this._targetLoudness, Time.deltaTime * 40f);
		}

		protected abstract float ReadSample();

		public float VolumeScale = 1f;

		public int Channels = 1;

		private float _collectedLoudness;

		private int _collectedSamples;

		private float _targetLoudness;

		private const int LoudnessCollectorThreshold = 1200;

		private const float LoudnessCollectorMultiplier = 5f;

		private const float LoudnessLerpSpeed = 40f;

		private static int _flatlinePcmLen = 0;

		private static float[] _flatlinePcm = new float[0];
	}
}
