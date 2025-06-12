using System.Collections.Generic;
using System.Diagnostics;
using AudioPooling;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace InventorySystem.Items.Usables.Scp1576;

public class Scp1576Viewmodel : UsableItemViewmodel
{
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
		if (!Scp1576Viewmodel.PrevWeights.TryGetValue(base.ItemId.SerialNumber, out var value))
		{
			value = 0f;
		}
		if (!Scp1576Viewmodel.TimersBySerial.TryGetValue(base.ItemId.SerialNumber, out var value2) || !value2.IsRunning)
		{
			if (this._particles.isPlaying)
			{
				this._particles.Stop();
			}
			if (this._audioLoop.volume > 0f)
			{
				AudioSourcePoolManager.Play2DWithParent(this._endRecordClip, base.transform);
			}
			this._audioLoop.volume = 0f;
			this._playbackSource.enabled = false;
			Scp1576Item.LocallyUsed = false;
			this.AnimatorSetLayerWeight(this._posLayer, value);
			return;
		}
		float num = (float)value2.Elapsed.TotalSeconds;
		if (num > Scp1576Viewmodel.UseTime)
		{
			if (this._wasCranking)
			{
				this._particles.Play();
				AudioSourcePoolManager.Play2DWithParent(this._startRecording, base.transform);
				this._wasCranking = false;
				this._playbackSource.enabled |= base.IsLocal;
				Scp1576Item.LocallyUsed |= base.IsLocal;
			}
			float num2 = num - Scp1576Viewmodel.UseTime;
			this._audioLoop.volume = num2;
			value = num2 / 30f;
			this._beltMaterial.mainTextureOffset += this._beltSpeed * Time.deltaTime;
		}
		else if (num > 1.1f)
		{
			this._wasCranking = true;
			float num3 = value;
			value -= Time.deltaTime * 0.4f;
			if (num3 > 0f && value <= 0f)
			{
				AudioSourcePoolManager.Play2DWithParent(this._rewindClip, base.transform);
			}
		}
		value = Mathf.Clamp01(value);
		Scp1576Viewmodel.PrevWeights[base.ItemId.SerialNumber] = value;
		this.AnimatorSetLayerWeight(this._posLayer, value);
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		UsableItemsController.OnClientStatusReceived += OnClientStatusReceived;
		Scp1576Pickup.OnHornPositionUpdated += delegate(ushort serial, float pos)
		{
			Scp1576Viewmodel.PrevWeights[serial] = pos;
		};
	}

	private static void OnClientStatusReceived(StatusMessage msg)
	{
		switch (msg.Status)
		{
		case StatusMessage.StatusType.Start:
			if (ReferenceHub.AllHubs.Any((ReferenceHub x) => x.inventory.CurItem.SerialNumber == msg.ItemSerial && x.inventory.CurItem.TypeId == ItemType.SCP1576))
			{
				Scp1576Viewmodel.TimersBySerial.GetOrAdd(msg.ItemSerial, () => new Stopwatch()).Restart();
			}
			break;
		case StatusMessage.StatusType.Cancel:
		{
			if (Scp1576Viewmodel.TimersBySerial.TryGetValue(msg.ItemSerial, out var value))
			{
				value.Reset();
			}
			break;
		}
		}
	}
}
