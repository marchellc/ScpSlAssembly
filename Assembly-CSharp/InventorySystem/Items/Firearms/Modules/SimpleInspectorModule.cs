using System.Collections.Generic;
using AudioPooling;
using InventorySystem.Items.Firearms.Modules.Misc;
using Mirror;
using PlayerRoles;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public class SimpleInspectorModule : ModuleBase, ITriggerPressPreventerModule, ISwayModifierModule, IInspectorModule
{
	private static readonly HashSet<ushort> SpectatorInspectingFirearms = new HashSet<ushort>();

	private const float MinimalIdleTime = 0.15f;

	private const float MaxTransitionTime = 0.365f;

	private const float SwayAdjustSpeed = 5f;

	private const float ClipStoppingSpeed = 10f;

	private float _idleElapsed;

	private bool _isInspecting;

	private bool _eventListenerSet;

	private readonly List<AudioPoolSession> _capturedClips = new List<AudioPoolSession>();

	[Tooltip("Layer that contains the inspect animation. It must contain an idle state with 'Idle' tag, to which the animation returns when ends.")]
	[SerializeField]
	private AnimatorLayerMask _inspectLayer;

	[Tooltip("Scale of hip-fire bobbing when inspecting.")]
	[SerializeField]
	private float _bobbingScale = 1f;

	[Tooltip("Scale of runnin sway when inspecting")]
	[SerializeField]
	private float _walkSwayScale = 1f;

	[Tooltip("Scale of runnin sway when inspecting")]
	[SerializeField]
	private float _jumpSwayScale = 1f;

	[Tooltip("List of clips produced by audio manager that will be automatically stopped when the inspection is interrupted.")]
	[SerializeField]
	private AudioClip[] _clipsToStopOnInterrupt;

	private bool ValidateStart
	{
		get
		{
			ModuleBase[] modules = base.Firearm.Modules;
			foreach (ModuleBase moduleBase in modules)
			{
				if (moduleBase is IInspectPreventerModule inspectPreventerModule)
				{
					if (!inspectPreventerModule.InspectionAllowed)
					{
						return false;
					}
				}
				else if (moduleBase is IBusyIndicatorModule { IsBusy: not false })
				{
					return false;
				}
			}
			return true;
		}
	}

	private bool ValidateUpdate
	{
		get
		{
			if (!this.ValidateStart)
			{
				return false;
			}
			if (this._idleElapsed < 0.365f)
			{
				return true;
			}
			int[] layers = this._inspectLayer.Layers;
			foreach (int layer in layers)
			{
				if (base.Firearm.AnimGetCurStateInfo(layer).tagHash == FirearmAnimatorHashes.Idle)
				{
					return false;
				}
			}
			return true;
		}
	}

	public bool ClientBlockTrigger { get; private set; }

	public float WalkSwayScale { get; private set; }

	public float JumpSwayScale { get; private set; }

	public float BobbingSwayScale { get; private set; }

	public bool DisplayInspecting => this._isInspecting;

	private void SetInspecting(bool val)
	{
		bool isInspecting = this._isInspecting;
		this._isInspecting = val;
		this._idleElapsed = 0f;
		if (this._isInspecting)
		{
			if (!isInspecting)
			{
				this._capturedClips.Clear();
				base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.StartInspect, checkIfExists: true);
			}
		}
		else
		{
			this.ReleaseTriggerLock();
		}
		base.Firearm.AnimSetBool(FirearmAnimatorHashes.Inspect, val);
		if (NetworkServer.active)
		{
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteBool(val);
			});
		}
	}

	private void UpdateSwayScale()
	{
		float b;
		float b2;
		float b3;
		if (this._isInspecting)
		{
			b = this._walkSwayScale;
			b2 = this._jumpSwayScale;
			b3 = this._bobbingScale;
		}
		else
		{
			b = 1f;
			b2 = 1f;
			b3 = 1f;
		}
		float t = 5f * Time.deltaTime;
		this.WalkSwayScale = Mathf.Lerp(this.WalkSwayScale, b, t);
		this.JumpSwayScale = Mathf.Lerp(this.JumpSwayScale, b2, t);
		this.BobbingSwayScale = Mathf.Lerp(this.BobbingSwayScale, b3, t);
	}

	private void InterceptNewSound(ItemIdentifier id, PlayerRoleBase role, PooledAudioSource src)
	{
		if (this._isInspecting && base.Firearm.ItemSerial == id.SerialNumber && base.Firearm.ItemTypeId == id.TypeId && this._clipsToStopOnInterrupt.Contains(src.Source.clip))
		{
			this._capturedClips.Add(new AudioPoolSession(src));
		}
	}

	private void StopInspectSounds()
	{
		bool flag = false;
		foreach (AudioPoolSession capturedClip in this._capturedClips)
		{
			if (capturedClip.SameSession && !(capturedClip.Source.volume <= 0f))
			{
				flag = true;
				capturedClip.Source.volume -= Time.deltaTime * 10f;
			}
		}
		if (!flag)
		{
			this._capturedClips.Clear();
		}
	}

	private void OnDestroy()
	{
		if (this._eventListenerSet)
		{
			this._eventListenerSet = false;
			AudioModule.OnSoundPlayed -= InterceptNewSound;
		}
	}

	internal override void EquipUpdate()
	{
		base.EquipUpdate();
		this.UpdateSwayScale();
		if (this._isInspecting)
		{
			if (!this.ValidateUpdate)
			{
				this.SetInspecting(val: false);
			}
		}
		else if (this._capturedClips.Count > 0)
		{
			this.StopInspectSounds();
		}
		if (this.ValidateStart)
		{
			this._idleElapsed += Time.deltaTime;
		}
		else
		{
			this._idleElapsed = 0f;
		}
		if (base.IsControllable && !this._isInspecting && !(this._idleElapsed < 0.15f) && base.GetActionDown(ActionName.InspectItem) && !base.ItemUsageBlocked)
		{
			this.SendCmd();
			this.SetInspecting(val: true);
		}
	}

	internal override void OnEquipped()
	{
		base.OnEquipped();
		this._idleElapsed = 0f;
		this.ReleaseTriggerLock();
	}

	internal override void OnHolstered()
	{
		base.OnHolstered();
		this.SetInspecting(val: false);
	}

	internal override void OnClientReady()
	{
		base.OnClientReady();
		SimpleInspectorModule.SpectatorInspectingFirearms.Clear();
	}

	internal override void SpectatorInit()
	{
		base.SpectatorInit();
		if (SimpleInspectorModule.SpectatorInspectingFirearms.Contains(base.Firearm.ItemSerial))
		{
			this._isInspecting = true;
			base.Firearm.AnimSetBool(FirearmAnimatorHashes.Inspect, b: true);
		}
	}

	protected override void OnInit()
	{
		base.OnInit();
		if (!this._eventListenerSet)
		{
			AudioModule.OnSoundPlayed += InterceptNewSound;
			this._eventListenerSet = true;
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (this.ValidateStart)
		{
			this.SetInspecting(val: true);
		}
	}

	public override void ClientProcessRpcInstance(NetworkReader reader)
	{
		base.ClientProcessRpcInstance(reader);
		if (base.Firearm.IsSpectator && this._isInspecting != reader.ReadBool())
		{
			this.SetInspecting(!this._isInspecting);
		}
	}

	public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessRpcTemplate(reader, serial);
		if (reader.ReadBool())
		{
			SimpleInspectorModule.SpectatorInspectingFirearms.Add(serial);
		}
		else
		{
			SimpleInspectorModule.SpectatorInspectingFirearms.Remove(serial);
		}
	}

	[ExposedFirearmEvent]
	public void BlockTrigger()
	{
		if (base.IsLocalPlayer && (!base.Firearm.TryGetModule<IActionModule>(out var module) || !module.IsLoaded))
		{
			this.ClientBlockTrigger = true;
		}
	}

	[ExposedFirearmEvent]
	public void ReleaseTriggerLock()
	{
		this.ClientBlockTrigger = false;
	}
}
