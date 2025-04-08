using System;
using System.Diagnostics;
using AudioPooling;
using RelativePositioning;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Misc
{
	public class BulletWhizzHandler : MonoBehaviour
	{
		public AudioClip[] Clips { get; private set; }

		public float SourceRange { get; private set; } = 10f;

		public float MidpointTime { get; private set; } = 0.07f;

		public float TravelDistance { get; private set; } = 6f;

		public bool IsPlaying
		{
			get
			{
				if (!this._wasPlaying)
				{
					return false;
				}
				if (this.Source.isPlaying)
				{
					return true;
				}
				this._wasPlaying = false;
				return false;
			}
		}

		private AudioSource Source
		{
			get
			{
				if (this._src != null)
				{
					return this._src;
				}
				this._src = AudioSourcePoolManager.CreateNewSource().Source;
				AudioSourcePoolManager.ApplyStandardSettings(this._src, null, FalloffType.Exponential, MixerChannel.NoDucking, 1f, this.SourceRange);
				this._srcTr = this._src.transform;
				return this._src;
			}
		}

		public void Play(Vector3 dir, Vector3 midpoint)
		{
			this._midpoint = new RelativePosition(midpoint);
			this._dir = dir;
			this._sw.Restart();
			this.Source.PlayOneShot(this.Clips.RandomItem<AudioClip>());
			this._wasPlaying = true;
			this.Update();
		}

		private void Update()
		{
			if (!this.IsPlaying)
			{
				return;
			}
			Vector3 position = this._midpoint.Position;
			Vector3 vector = position - 0.5f * this.TravelDistance * this._dir;
			Vector3 vector2 = position + 0.5f * this.TravelDistance * this._dir;
			this._srcTr.position = Vector3.LerpUnclamped(vector, vector2, (float)this._sw.Elapsed.TotalSeconds / this.MidpointTime);
		}

		private AudioSource _src;

		private RelativePosition _midpoint;

		private Vector3 _dir;

		private Transform _srcTr;

		private bool _wasPlaying;

		private readonly Stopwatch _sw = new Stopwatch();
	}
}
