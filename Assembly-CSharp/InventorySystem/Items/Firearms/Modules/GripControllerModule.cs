using System;
using System.Collections.Generic;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules.Misc;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public class GripControllerModule : ModuleBase
	{
		private bool SkippingForward
		{
			get
			{
				EventManagerModule eventManagerModule;
				return base.Firearm.TryGetModule(out eventManagerModule, true) && eventManagerModule.SkippingForward;
			}
		}

		[ExposedFirearmEvent]
		public void EnableGripLayer(float transitionDurationFrames)
		{
			this._activeSafeguard = null;
			this.ModifyGripSpeed(transitionDurationFrames, true);
			this._safeguardEvent = null;
			if (base.IsServer)
			{
				this.SendRpc(delegate(NetworkWriter x)
				{
					x.WriteBool(true);
				}, true);
			}
		}

		[ExposedFirearmEvent]
		public void DisableGripLayer(float transitionDurationFrames)
		{
			this.ModifyGripSpeed(transitionDurationFrames, false);
			if (base.IsServer)
			{
				this.SendRpc(delegate(NetworkWriter x)
				{
					x.WriteBool(false);
				}, true);
			}
		}

		[ExposedFirearmEvent]
		public void SetSafeguards(float transitionDurationFrames)
		{
			this._safeguardEvent = FirearmEvent.CurrentlyInvokedEvent;
			FirearmEvent safeguardEvent = this._safeguardEvent;
			this._activeSafeguard = ((safeguardEvent != null) ? new int?(safeguardEvent.NameHash) : null);
			this._safeguardDuration = transitionDurationFrames;
		}

		internal override void SpectatorPostprocessSkip()
		{
			base.SpectatorPostprocessSkip();
			this.ForceWeight((float)(GripControllerModule.SyncEnabledGrips.Contains(base.ItemSerial) ? 1 : 0));
		}

		internal override void EquipUpdate()
		{
			base.EquipUpdate();
			if (!base.HasViewmodel)
			{
				return;
			}
			this.UpdateSafeguard();
			this.UpdateWeight();
		}

		protected override void OnInit()
		{
			base.OnInit();
			this._gripAttachmentLink.InitCache(base.Firearm);
		}

		internal override void OnHolstered()
		{
			base.OnHolstered();
			this._lastWeight = 0f;
			this._adjustSpeed = 0f;
			this._activeSafeguard = null;
			if (base.IsServer)
			{
				this.SendRpc(delegate(NetworkWriter x)
				{
					x.WriteBool(false);
				}, true);
			}
		}

		internal override void OnClientReady()
		{
			base.OnClientReady();
			GripControllerModule.SyncEnabledGrips.Clear();
		}

		internal override void ServerOnPlayerConnected(ReferenceHub hub, bool firstModule)
		{
			base.ServerOnPlayerConnected(hub, firstModule);
			if (!firstModule)
			{
				return;
			}
			using (HashSet<ushort>.Enumerator enumerator = GripControllerModule.SyncEnabledGrips.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ushort serial = enumerator.Current;
					this.SendRpc(hub, delegate(NetworkWriter x)
					{
						x.WriteUShort(serial);
					});
				}
			}
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			byte b = 2;
			while (reader.Remaining >= (int)b)
			{
				GripControllerModule.SyncEnabledGrips.Add(reader.ReadUShort());
			}
			if (reader.Remaining > 0)
			{
				if (reader.ReadBool())
				{
					GripControllerModule.SyncEnabledGrips.Add(serial);
					return;
				}
				GripControllerModule.SyncEnabledGrips.Remove(serial);
			}
		}

		private void ModifyGripSpeed(float transitionDurationFrames, bool targetEnable)
		{
			FirearmEvent firearmEvent = FirearmEvent.CurrentlyInvokedEvent;
			if (firearmEvent == null)
			{
				if (!targetEnable || this._safeguardEvent == null)
				{
					return;
				}
				firearmEvent = this._safeguardEvent;
			}
			float totalSpeedMultiplier = firearmEvent.LastInvocation.TotalSpeedMultiplier;
			float num = transitionDurationFrames / firearmEvent.Clip.frameRate;
			if (totalSpeedMultiplier <= 0f || num <= 0f || this.SkippingForward)
			{
				this.ForceWeight((float)(targetEnable ? 1 : 0));
				return;
			}
			this._adjustSpeed = 1f / num;
			if (!targetEnable)
			{
				this._adjustSpeed *= -1f;
			}
		}

		private void ForceWeight(float weight)
		{
			this._adjustSpeed = 0f;
			this._lastWeight = weight;
			this.UpdateWeight();
		}

		private void UpdateWeight()
		{
			this._lastWeight = Mathf.Clamp01(this._lastWeight + this._adjustSpeed * Time.deltaTime);
			if (!base.Firearm.HasViewmodel)
			{
				return;
			}
			float num = (this._gripAttachmentLink.Instance.IsEnabled ? this._lastWeight : 0f);
			AnimatedFirearmViewmodel clientViewmodelInstance = base.Firearm.ClientViewmodelInstance;
			this._layers.SetWeight(new Action<int, float>(clientViewmodelInstance.AnimatorSetLayerWeight), num);
			base.Firearm.AnimSetFloat(FirearmAnimatorHashes.GripWeight, num, true);
		}

		private void UpdateSafeguard()
		{
			if (this._activeSafeguard == null)
			{
				return;
			}
			EventManagerModule eventManagerModule;
			if (!base.Firearm.TryGetModule(out eventManagerModule, true))
			{
				return;
			}
			AnimatedFirearmViewmodel clientViewmodelInstance = base.Firearm.ClientViewmodelInstance;
			int value = this._activeSafeguard.Value;
			foreach (int num in eventManagerModule.AffectedLayers.Layers)
			{
				if (clientViewmodelInstance.AnimatorStateInfo(num).shortNameHash == value)
				{
					return;
				}
			}
			this.EnableGripLayer(this._safeguardDuration);
		}

		private static readonly HashSet<ushort> SyncEnabledGrips = new HashSet<ushort>();

		[SerializeField]
		private AttachmentLink _gripAttachmentLink;

		[SerializeField]
		private AnimatorLayerMask _layers;

		private float _lastWeight;

		private float _adjustSpeed;

		private int? _activeSafeguard;

		private FirearmEvent _safeguardEvent;

		private float _safeguardDuration;
	}
}
