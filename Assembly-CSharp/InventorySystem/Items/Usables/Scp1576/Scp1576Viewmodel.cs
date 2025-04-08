using System;
using System.Collections.Generic;
using System.Diagnostics;
using AudioPooling;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace InventorySystem.Items.Usables.Scp1576
{
	public class Scp1576Viewmodel : UsableItemViewmodel
	{
		private static float UseTime
		{
			get
			{
				if (!Scp1576Viewmodel._useTimeCacheSet)
				{
					Scp1576Viewmodel._cachedUseTime = (InventoryItemLoader.AvailableItems[ItemType.SCP1576] as UsableItem).UseTime;
					Scp1576Viewmodel._useTimeCacheSet = true;
				}
				return Scp1576Viewmodel._cachedUseTime;
			}
		}

		protected override void LateUpdate()
		{
			base.LateUpdate();
			float num;
			if (!Scp1576Viewmodel.PrevWeights.TryGetValue(base.ItemId.SerialNumber, out num))
			{
				num = 0f;
			}
			Stopwatch stopwatch;
			if (!Scp1576Viewmodel.TimersBySerial.TryGetValue(base.ItemId.SerialNumber, out stopwatch) || !stopwatch.IsRunning)
			{
				if (this._particles.isPlaying)
				{
					this._particles.Stop();
				}
				if (this._audioLoop.volume > 0f)
				{
					AudioSourcePoolManager.Play2DWithParent(this._endRecordClip, base.transform, 1f, MixerChannel.DefaultSfx, 1f);
				}
				this._audioLoop.volume = 0f;
				this._playbackSource.enabled = false;
				Scp1576Item.LocallyUsed = false;
				this.AnimatorSetLayerWeight(this._posLayer, num);
				return;
			}
			float num2 = (float)stopwatch.Elapsed.TotalSeconds;
			if (num2 > Scp1576Viewmodel.UseTime)
			{
				if (this._wasCranking)
				{
					this._particles.Play();
					AudioSourcePoolManager.Play2DWithParent(this._startRecording, base.transform, 1f, MixerChannel.DefaultSfx, 1f);
					this._wasCranking = false;
					this._playbackSource.enabled |= base.IsLocal;
					Scp1576Item.LocallyUsed |= base.IsLocal;
				}
				float num3 = num2 - Scp1576Viewmodel.UseTime;
				this._audioLoop.volume = num3;
				num = num3 / 30f;
				this._beltMaterial.mainTextureOffset += this._beltSpeed * Time.deltaTime;
			}
			else if (num2 > 1.1f)
			{
				this._wasCranking = true;
				float num4 = num;
				num -= Time.deltaTime * 0.4f;
				if (num4 > 0f && num <= 0f)
				{
					AudioSourcePoolManager.Play2DWithParent(this._rewindClip, base.transform, 1f, MixerChannel.DefaultSfx, 1f);
				}
			}
			num = Mathf.Clamp01(num);
			Scp1576Viewmodel.PrevWeights[base.ItemId.SerialNumber] = num;
			this.AnimatorSetLayerWeight(this._posLayer, num);
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			UsableItemsController.OnClientStatusReceived += Scp1576Viewmodel.OnClientStatusReceived;
			Scp1576Pickup.OnHornPositionUpdated += delegate(ushort serial, float pos)
			{
				Scp1576Viewmodel.PrevWeights[serial] = pos;
			};
		}

		private static void OnClientStatusReceived(StatusMessage msg)
		{
			StatusMessage.StatusType status = msg.Status;
			if (status != StatusMessage.StatusType.Start)
			{
				if (status != StatusMessage.StatusType.Cancel)
				{
					return;
				}
				Stopwatch stopwatch;
				if (Scp1576Viewmodel.TimersBySerial.TryGetValue(msg.ItemSerial, out stopwatch))
				{
					stopwatch.Reset();
				}
			}
			else if (ReferenceHub.AllHubs.Any((ReferenceHub x) => x.inventory.CurItem.SerialNumber == msg.ItemSerial && x.inventory.CurItem.TypeId == ItemType.SCP1576))
			{
				Scp1576Viewmodel.TimersBySerial.GetOrAdd(msg.ItemSerial, () => new Stopwatch()).Restart();
				return;
			}
		}

		private const ItemType Scp1576Type = ItemType.SCP1576;

		private static readonly Dictionary<ushort, float> PrevWeights = new Dictionary<ushort, float>();

		private static readonly Dictionary<ushort, Stopwatch> TimersBySerial = new Dictionary<ushort, Stopwatch>();

		private static float _cachedUseTime;

		private static bool _useTimeCacheSet;

		private bool _wasCranking;

		[SerializeField]
		private int _posLayer;

		[SerializeField]
		private Material _beltMaterial;

		[SerializeField]
		private Vector2 _beltSpeed;

		[SerializeField]
		private ParticleSystem _particles;

		[SerializeField]
		private AudioSource _audioLoop;

		[SerializeField]
		private AudioClip _endRecordClip;

		[SerializeField]
		private AudioClip _rewindClip;

		[SerializeField]
		private AudioClip _startRecording;

		[SerializeField]
		private Scp1576Source _playbackSource;
	}
}
