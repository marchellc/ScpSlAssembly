using AudioPooling;
using InventorySystem.Items.Thirdperson;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;

namespace InventorySystem.Items.ToggleableLights.Flashlight;

public class FlashlightThirdpersonItem : IdleThirdpersonItem
{
	public const float MaxAudioDistance = 3.2f;

	[SerializeField]
	private Light _lightSource;

	[SerializeField]
	private Renderer[] _targetRenderers;

	[SerializeField]
	private Material _onMat;

	[SerializeField]
	private Material _offMat;

	private static FlashlightItem Template => FlashlightItem.Template;

	internal override void Initialize(InventorySubcontroller subctrl, ItemIdentifier id)
	{
		base.Initialize(subctrl, id);
		FlashlightNetworkHandler.OnStatusReceived += ProcesReceivedStatus;
		if (FlashlightNetworkHandler.ReceivedStatuses.TryGetValue(id.SerialNumber, out var value))
		{
			this.SetState(value);
		}
	}

	private void OnDestroy()
	{
		FlashlightNetworkHandler.OnStatusReceived -= ProcesReceivedStatus;
	}

	private void ProcesReceivedStatus(FlashlightNetworkHandler.FlashlightMessage msg)
	{
		if (msg.Serial == base.ItemId.SerialNumber)
		{
			this.SetState(msg.NewState);
		}
	}

	private void SetState(bool newState)
	{
		if (this._lightSource.enabled != newState)
		{
			this._lightSource.enabled = newState;
			Renderer[] targetRenderers = this._targetRenderers;
			for (int i = 0; i < targetRenderers.Length; i++)
			{
				targetRenderers[i].sharedMaterial = (newState ? this._onMat : this._offMat);
			}
			AudioSourcePoolManager.PlayOnTransform(newState ? FlashlightThirdpersonItem.Template.OnClip : FlashlightThirdpersonItem.Template.OffClip, base.transform, 3.2f);
		}
	}
}
