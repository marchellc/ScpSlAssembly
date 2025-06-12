using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace InventorySystem.Items.Jailbird;

public class JailbirdViewmodel : StandardAnimatedViemodel
{
	[Serializable]
	private struct InspectPreset
	{
		public JailbirdWearState State;

		public AudioClip Sound;

		public float Speed;

		public int VariantId;
	}

	private static readonly Dictionary<ushort, double> LastUpdates = new Dictionary<ushort, double>();

	private static readonly Dictionary<ushort, JailbirdMessageType> LastRpcs = new Dictionary<ushort, JailbirdMessageType>();

	private static readonly HashSet<ushort> BrokenJailbirds = new HashSet<ushort>();

	private static readonly int BrokenHash = Animator.StringToHash("Broken");

	private static readonly int LeftHandHash = Animator.StringToHash("LeftHand");

	private static readonly int ChargeLoadHash = Animator.StringToHash("ChargeLoad");

	private static readonly int ChargingHash = Animator.StringToHash("Charging");

	private static readonly int SkipPickupHash = Animator.StringToHash("AlreadyPickedUp");

	private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");

	private static readonly int InspectTriggerHash = Animator.StringToHash("Inspect");

	private static readonly int InspectSpeedHash = Animator.StringToHash("InspectSpeed");

	private static readonly int InspectVariantHash = Animator.StringToHash("InspectVariant");

	private static readonly int IdleTagHash = Animator.StringToHash("Idle");

	private static Dictionary<JailbirdWearState, InspectPreset> _presetsByWear;

	private static bool _alreadyPickedUp;

	private static bool _anyCollectionModified;

	private static bool _wasLeftHand;

	private const float FastModeThreshold = 1.5f;

	private const float InspectCooldown = 0.5f;

	[SerializeField]
	private JailbirdMaterialController _materialController;

	[SerializeField]
	private GameObject _particlesSmall;

	[SerializeField]
	private GameObject _particlesLarge;

	[SerializeField]
	private GameObject _particlesTrail;

	[SerializeField]
	private GameObject _particlesBroken;

	[SerializeField]
	private InspectPreset[] _inspectPresets;

	[SerializeField]
	private AudioClip _firstEquipSound;

	[SerializeField]
	private AudioClip _normalEquipSound;

	[SerializeField]
	private AudioClip _chargeLoadSound;

	[SerializeField]
	private AudioClip _chargingSound;

	[SerializeField]
	private AudioClip _swipeSoundLeft;

	[SerializeField]
	private AudioClip _swipeSoundRight;

	[SerializeField]
	private AudioClip _chargeHitSound;

	[SerializeField]
	private AudioClip _brokenSound;

	[SerializeField]
	private AudioSource _targetAudioSource;

	[SerializeField]
	private GameObject _inspectParticlesRoot;

	private double _nextInspect;

	private bool _wasCharging;

	public override void InitAny()
	{
		base.InitAny();
		JailbirdItem.OnRpcReceived += RpcReceived;
		this._materialController.SetSerial(base.ItemId.SerialNumber);
	}

	internal override void OnEquipped()
	{
		base.OnEquipped();
		this.AnimatorSetBool(JailbirdViewmodel.SkipPickupHash, JailbirdViewmodel._alreadyPickedUp);
		this.PlaySound(JailbirdViewmodel._alreadyPickedUp ? this._normalEquipSound : this._firstEquipSound, base.SkipEquipTime);
		JailbirdViewmodel._alreadyPickedUp = true;
	}

	public override void InitSpectator(ReferenceHub ply, ItemIdentifier id, bool wasEquipped)
	{
		base.InitSpectator(ply, id, wasEquipped);
		if (JailbirdViewmodel.BrokenJailbirds.Contains(id.SerialNumber))
		{
			this.SetBroken();
		}
		this.AnimatorForceUpdate(base.SkipEquipTime);
		if (JailbirdViewmodel.LastRpcs.TryGetValue(id.SerialNumber, out var value) && JailbirdViewmodel.LastUpdates.TryGetValue(id.SerialNumber, out var value2))
		{
			float num = (float)(NetworkTime.time - value2);
			this.ProcessRpc(value, num);
			if (num > 1.5f)
			{
				this.AnimatorForceUpdate(1.5f, fastMode: false);
				this.AnimatorForceUpdate(num - 1.5f);
			}
			else
			{
				this.AnimatorForceUpdate(num, fastMode: false);
			}
		}
	}

	public override void InitLocal(ItemBase ib)
	{
		base.InitLocal(ib);
		(base.ParentItem as JailbirdItem).OnCmdSent += OnCmdSent;
	}

	private void OnDestroy()
	{
		JailbirdItem.OnRpcReceived -= RpcReceived;
	}

	private void Update()
	{
		int tagHash = this.AnimatorStateInfo(0).tagHash;
		this._particlesSmall.SetActive(tagHash == JailbirdViewmodel.AttackTriggerHash);
		this._particlesTrail.SetActive(tagHash == JailbirdViewmodel.ChargingHash);
		this._particlesLarge.SetActive(tagHash == JailbirdViewmodel.ChargeLoadHash || tagHash == JailbirdViewmodel.ChargingHash);
		this._inspectParticlesRoot.SetActive(tagHash == JailbirdViewmodel.InspectTriggerHash);
	}

	private void RpcReceived(ushort serial, JailbirdMessageType rpc)
	{
		if (serial == base.ItemId.SerialNumber)
		{
			this.ProcessRpc(rpc, 0f);
		}
	}

	private void ProcessRpc(JailbirdMessageType rpc, float delay)
	{
		bool flag = rpc == JailbirdMessageType.ChargeStarted;
		bool val = rpc == JailbirdMessageType.ChargeLoadTriggered;
		this.AnimatorSetBool(JailbirdViewmodel.ChargingHash, flag);
		this.AnimatorSetBool(JailbirdViewmodel.ChargeLoadHash, val);
		if (this._wasCharging && !flag)
		{
			this.PlaySound(this._chargeHitSound, delay);
		}
		switch (rpc)
		{
		case JailbirdMessageType.AttackTriggered:
			if (base.IsSpectator)
			{
				this.PlayAttackAnim(delay);
			}
			break;
		case JailbirdMessageType.ChargeLoadTriggered:
			this.PlaySound(this._chargeLoadSound, delay);
			break;
		case JailbirdMessageType.ChargeStarted:
			this.PlaySound(this._chargingSound, delay);
			break;
		case JailbirdMessageType.ChargeFailed:
			this.PlaySound(null, delay);
			break;
		case JailbirdMessageType.Broken:
			this.SetBroken();
			this.PlaySound(this._brokenSound, delay, stopPrev: false);
			break;
		case JailbirdMessageType.Inspect:
		{
			if (this.AnimatorStateInfo(0).tagHash != JailbirdViewmodel.IdleTagHash || this._nextInspect > NetworkTime.time)
			{
				break;
			}
			JailbirdDeteriorationTracker.ReceivedStates.TryGetValue(base.ItemId.SerialNumber, out var value);
			if (JailbirdViewmodel._presetsByWear == null)
			{
				JailbirdViewmodel._presetsByWear = new Dictionary<JailbirdWearState, InspectPreset>();
				JailbirdViewmodel._presetsByWear.FromArray(this._inspectPresets, (InspectPreset x) => x.State);
			}
			if (JailbirdViewmodel._presetsByWear.TryGetValue(value, out var value2))
			{
				this.PlaySound(value2.Sound, delay);
				this.AnimatorSetTrigger(JailbirdViewmodel.InspectTriggerHash);
				this.AnimatorSetFloat(JailbirdViewmodel.InspectSpeedHash, value2.Speed);
				this.AnimatorSetFloat(JailbirdViewmodel.InspectVariantHash, value2.VariantId);
				this._nextInspect = NetworkTime.time + 0.5;
			}
			break;
		}
		}
		this._wasCharging = flag;
	}

	private void PlaySound(AudioClip clip, float delay, bool stopPrev = true, float pitchRandom = 0f)
	{
		int num = 0;
		if (stopPrev)
		{
			this._targetAudioSource.Stop();
		}
		if (delay > 0f)
		{
			num = Mathf.RoundToInt(delay * (float)AudioSettings.outputSampleRate);
			if (num > clip.samples)
			{
				return;
			}
		}
		this._targetAudioSource.PlayOneShot(clip);
		this._targetAudioSource.pitch = 1f + UnityEngine.Random.Range(0f - pitchRandom, pitchRandom);
		if (num != 0)
		{
			this._targetAudioSource.timeSamples = num;
		}
	}

	private void SetBroken()
	{
		this.AnimatorSetBool(JailbirdViewmodel.BrokenHash, val: true);
		this._particlesBroken.SetActive(value: true);
	}

	private void OnCmdSent(JailbirdMessageType cmd)
	{
		if (cmd == JailbirdMessageType.AttackTriggered)
		{
			this.PlayAttackAnim(0f);
		}
	}

	private void PlayAttackAnim(float delay)
	{
		JailbirdViewmodel._wasLeftHand = !JailbirdViewmodel._wasLeftHand;
		this.AnimatorSetBool(JailbirdViewmodel.LeftHandHash, JailbirdViewmodel._wasLeftHand);
		this.AnimatorSetTrigger(JailbirdViewmodel.AttackTriggerHash);
		this.PlaySound(JailbirdViewmodel._wasLeftHand ? this._swipeSoundLeft : this._swipeSoundRight, delay, stopPrev: true, 0.1f);
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			JailbirdViewmodel._alreadyPickedUp = false;
			if (JailbirdViewmodel._anyCollectionModified)
			{
				JailbirdViewmodel.LastRpcs.Clear();
				JailbirdViewmodel.LastUpdates.Clear();
				JailbirdViewmodel.BrokenJailbirds.Clear();
				JailbirdViewmodel._anyCollectionModified = false;
			}
		};
		JailbirdItem.OnRpcReceived += delegate(ushort serial, JailbirdMessageType rpc)
		{
			JailbirdViewmodel._anyCollectionModified = true;
			JailbirdViewmodel.LastRpcs[serial] = rpc;
			JailbirdViewmodel.LastUpdates[serial] = NetworkTime.time;
			if (rpc == JailbirdMessageType.Broken)
			{
				JailbirdViewmodel.BrokenJailbirds.Add(serial);
			}
		};
	}
}
