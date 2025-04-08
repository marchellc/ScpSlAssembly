using System;
using System.Collections.Generic;
using UnityEngine;
using VoiceChat.Networking;

namespace VoiceChat.Playbacks
{
	public class SpatializedRadioPlaybackBase : VoiceChatPlaybackBase
	{
		public AudioSource NoiseSource { get; private set; }

		public Vector3 LastPosition { get; private set; }

		public bool Culled { get; private set; }

		public override int MaxSamples
		{
			get
			{
				int num = 0;
				for (int i = 0; i < 8; i++)
				{
					num = Mathf.Max(num, this.Buffers[i].Length);
				}
				return num;
			}
		}

		protected override void Awake()
		{
			base.Awake();
			this._t = base.transform;
			this.Buffers = new PlaybackBuffer[8];
			for (int i = 0; i < 8; i++)
			{
				this.Buffers[i] = new PlaybackBuffer(24000, false);
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			SpatializedRadioPlaybackBase.AllInstances.Add(this);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			SpatializedRadioPlaybackBase.AllInstances.Remove(this);
		}

		protected override void Update()
		{
			base.Update();
			bool flag = false;
			for (int i = 0; i < 8; i++)
			{
				if (this.Buffers[i].Length != 0)
				{
					flag = true;
				}
			}
			this.NoiseSource.mute = !flag;
			this.LastPosition = this._t.position;
		}

		protected override float ReadSample()
		{
			float num = 0f;
			for (int i = 0; i < 8; i++)
			{
				num += this.Buffers[i].Read();
			}
			return Mathf.Clamp(num, -1f, 1f);
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			StaticUnityMethods.OnLateUpdate += delegate
			{
				if (!MainCameraController.InstanceActive)
				{
					return;
				}
				int num = 0;
				Vector3 position = MainCameraController.CurrentCamera.position;
				foreach (SpatializedRadioPlaybackBase spatializedRadioPlaybackBase in SpatializedRadioPlaybackBase.AllInstances)
				{
					spatializedRadioPlaybackBase.Culled = true;
					float sqrMagnitude = (spatializedRadioPlaybackBase.LastPosition - position).sqrMagnitude;
					float maxDistance = spatializedRadioPlaybackBase.Source.maxDistance;
					if (sqrMagnitude <= maxDistance * maxDistance)
					{
						if (num < 4)
						{
							SpatializedRadioPlaybackBase.AudibleRadiosDis[num] = sqrMagnitude;
							SpatializedRadioPlaybackBase.AudibleRadioInst[num] = spatializedRadioPlaybackBase;
							num++;
						}
						else
						{
							int num2 = -1;
							for (int i = 0; i < 4; i++)
							{
								float num3 = SpatializedRadioPlaybackBase.AudibleRadiosDis[i];
								if (num3 >= sqrMagnitude)
								{
									num2 = i;
								}
							}
							if (num2 != -1)
							{
								SpatializedRadioPlaybackBase.AudibleRadiosDis[num2] = sqrMagnitude;
								SpatializedRadioPlaybackBase.AudibleRadioInst[num2] = spatializedRadioPlaybackBase;
							}
						}
					}
				}
				for (int j = 0; j < num; j++)
				{
					SpatializedRadioPlaybackBase.AudibleRadioInst[j].Culled = false;
				}
			};
		}

		private Transform _t;

		private const int MaxAudibleRadios = 4;

		private static readonly SpatializedRadioPlaybackBase[] AudibleRadioInst = new SpatializedRadioPlaybackBase[4];

		private static readonly float[] AudibleRadiosDis = new float[4];

		public const int MaxSignals = 8;

		public PlaybackBuffer[] Buffers;

		public int RangeId;

		public uint IgnoredNetId;

		public static readonly HashSet<SpatializedRadioPlaybackBase> AllInstances = new HashSet<SpatializedRadioPlaybackBase>();
	}
}
