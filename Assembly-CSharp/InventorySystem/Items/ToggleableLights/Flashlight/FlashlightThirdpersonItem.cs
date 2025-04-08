using System;
using AudioPooling;
using InventorySystem.Items.Thirdperson;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;

namespace InventorySystem.Items.ToggleableLights.Flashlight
{
	public class FlashlightThirdpersonItem : IdleThirdpersonItem
	{
		private static FlashlightItem Template
		{
			get
			{
				return FlashlightItem.Template;
			}
		}

		internal override void Initialize(InventorySubcontroller subctrl, ItemIdentifier id)
		{
			base.Initialize(subctrl, id);
			FlashlightNetworkHandler.OnStatusReceived += this.ProcesReceivedStatus;
			bool flag;
			if (!FlashlightNetworkHandler.ReceivedStatuses.TryGetValue(id.SerialNumber, out flag))
			{
				return;
			}
			this.SetState(flag);
		}

		private void OnDestroy()
		{
			FlashlightNetworkHandler.OnStatusReceived -= this.ProcesReceivedStatus;
		}

		private void ProcesReceivedStatus(FlashlightNetworkHandler.FlashlightMessage msg)
		{
			if (msg.Serial != base.ItemId.SerialNumber)
			{
				return;
			}
			this.SetState(msg.NewState);
		}

		private void SetState(bool newState)
		{
			if (this._lightSource.enabled == newState)
			{
				return;
			}
			this._lightSource.enabled = newState;
			Renderer[] targetRenderers = this._targetRenderers;
			for (int i = 0; i < targetRenderers.Length; i++)
			{
				targetRenderers[i].sharedMaterial = (newState ? this._onMat : this._offMat);
			}
			AudioSourcePoolManager.PlayOnTransform(newState ? FlashlightThirdpersonItem.Template.OnClip : FlashlightThirdpersonItem.Template.OffClip, base.transform, 3.2f, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
		}

		public const float MaxAudioDistance = 3.2f;

		[SerializeField]
		private Light _lightSource;

		[SerializeField]
		private Renderer[] _targetRenderers;

		[SerializeField]
		private Material _onMat;

		[SerializeField]
		private Material _offMat;
	}
}
