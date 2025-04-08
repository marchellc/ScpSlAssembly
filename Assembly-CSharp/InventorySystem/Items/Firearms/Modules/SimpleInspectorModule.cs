using System;
using System.Collections.Generic;
using AudioPooling;
using InventorySystem.GUI;
using InventorySystem.Items.Firearms.Modules.Misc;
using Mirror;
using PlayerRoles;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public class SimpleInspectorModule : ModuleBase, ITriggerPressPreventerModule, ISwayModifierModule, IInspectorModule
	{
		private bool ValidateStart
		{
			get
			{
				foreach (ModuleBase moduleBase in base.Firearm.Modules)
				{
					IInspectPreventerModule inspectPreventerModule = moduleBase as IInspectPreventerModule;
					if (inspectPreventerModule != null)
					{
						if (!inspectPreventerModule.InspectionAllowed)
						{
							return false;
						}
					}
					else
					{
						IBusyIndicatorModule busyIndicatorModule = moduleBase as IBusyIndicatorModule;
						if (busyIndicatorModule != null && busyIndicatorModule.IsBusy)
						{
							return false;
						}
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
				foreach (int num in this._inspectLayer.Layers)
				{
					if (base.Firearm.AnimGetCurStateInfo(num).tagHash == FirearmAnimatorHashes.Idle)
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

		public bool DisplayInspecting
		{
			get
			{
				return this._isInspecting;
			}
		}

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
					base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.StartInspect, true);
				}
			}
			else
			{
				this.ReleaseTriggerLock();
			}
			base.Firearm.AnimSetBool(FirearmAnimatorHashes.Inspect, val, false);
			if (NetworkServer.active)
			{
				this.SendRpc(delegate(NetworkWriter x)
				{
					x.WriteBool(val);
				}, true);
			}
		}

		private void UpdateSwayScale()
		{
			float num;
			float num2;
			float num3;
			if (this._isInspecting)
			{
				num = this._walkSwayScale;
				num2 = this._jumpSwayScale;
				num3 = this._bobbingScale;
			}
			else
			{
				num = 1f;
				num2 = 1f;
				num3 = 1f;
			}
			float num4 = 5f * Time.deltaTime;
			this.WalkSwayScale = Mathf.Lerp(this.WalkSwayScale, num, num4);
			this.JumpSwayScale = Mathf.Lerp(this.JumpSwayScale, num2, num4);
			this.BobbingSwayScale = Mathf.Lerp(this.BobbingSwayScale, num3, num4);
		}

		private void InterceptNewSound(ItemIdentifier id, PlayerRoleBase role, PooledAudioSource src)
		{
			if (!this._isInspecting)
			{
				return;
			}
			if (base.Firearm.ItemSerial != id.SerialNumber)
			{
				return;
			}
			if (base.Firearm.ItemTypeId != id.TypeId)
			{
				return;
			}
			if (!this._clipsToStopOnInterrupt.Contains(src.Source.clip))
			{
				return;
			}
			this._capturedClips.Add(new AudioPoolSession(src));
		}

		private void StopInspectSounds()
		{
			bool flag = false;
			foreach (AudioPoolSession audioPoolSession in this._capturedClips)
			{
				if (audioPoolSession.SameSession && audioPoolSession.Source.volume > 0f)
				{
					flag = true;
					audioPoolSession.Source.volume -= Time.deltaTime * 10f;
				}
			}
			if (!flag)
			{
				this._capturedClips.Clear();
			}
		}

		private void OnDestroy()
		{
			if (!this._eventListenerSet)
			{
				return;
			}
			this._eventListenerSet = false;
			AudioModule.OnSoundPlayed -= this.InterceptNewSound;
		}

		internal override void EquipUpdate()
		{
			base.EquipUpdate();
			this.UpdateSwayScale();
			if (this._isInspecting)
			{
				if (!this.ValidateUpdate)
				{
					this.SetInspecting(false);
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
			if (!base.IsLocalPlayer || this._isInspecting)
			{
				return;
			}
			if (this._idleElapsed < 0.15f)
			{
				return;
			}
			if (!InventoryGuiController.ItemsSafeForInteraction)
			{
				return;
			}
			if (!base.GetActionDown(ActionName.InspectItem))
			{
				return;
			}
			if (base.ItemUsageBlocked)
			{
				return;
			}
			this.SendCmd(null);
			this.SetInspecting(true);
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
			this.SetInspecting(false);
		}

		internal override void OnClientReady()
		{
			base.OnClientReady();
			SimpleInspectorModule.SpectatorInspectingFirearms.Clear();
		}

		internal override void SpectatorInit()
		{
			base.SpectatorInit();
			if (!SimpleInspectorModule.SpectatorInspectingFirearms.Contains(base.Firearm.ItemSerial))
			{
				return;
			}
			this._isInspecting = true;
			base.Firearm.AnimSetBool(FirearmAnimatorHashes.Inspect, true, false);
		}

		protected override void OnInit()
		{
			base.OnInit();
			if (!this._eventListenerSet)
			{
				AudioModule.OnSoundPlayed += this.InterceptNewSound;
				this._eventListenerSet = true;
			}
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			if (!this.ValidateStart)
			{
				return;
			}
			this.SetInspecting(true);
		}

		public override void ClientProcessRpcInstance(NetworkReader reader)
		{
			base.ClientProcessRpcInstance(reader);
			if (!base.Firearm.IsSpectator || this._isInspecting == reader.ReadBool())
			{
				return;
			}
			this.SetInspecting(!this._isInspecting);
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			if (reader.ReadBool())
			{
				SimpleInspectorModule.SpectatorInspectingFirearms.Add(serial);
				return;
			}
			SimpleInspectorModule.SpectatorInspectingFirearms.Remove(serial);
		}

		[ExposedFirearmEvent]
		public void BlockTrigger()
		{
			if (!base.IsLocalPlayer)
			{
				return;
			}
			IActionModule actionModule;
			if (base.Firearm.TryGetModule(out actionModule, true) && actionModule.IsLoaded)
			{
				return;
			}
			this.ClientBlockTrigger = true;
		}

		[ExposedFirearmEvent]
		public void ReleaseTriggerLock()
		{
			this.ClientBlockTrigger = false;
		}

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
	}
}
