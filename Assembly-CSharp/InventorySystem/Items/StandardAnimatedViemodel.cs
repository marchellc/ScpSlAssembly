using CameraShaking;
using InventorySystem.Items.SwayControllers;
using UnityEngine;

namespace InventorySystem.Items;

public class StandardAnimatedViemodel : AnimatedViewmodelBase
{
	[SerializeField]
	protected Transform HandsPivot;

	[SerializeField]
	private Transform _trackerCamera;

	[SerializeField]
	private float _trackerForceScale = 1f;

	[SerializeField]
	private Vector3 _trackerOffset;

	[SerializeField]
	private float _fov = 50f;

	private IItemSwayController _swayController;

	public override IItemSwayController SwayController => this._swayController;

	public override float ViewmodelCameraFOV => this._fov;

	internal override void OnEquipped()
	{
		base.OnEquipped();
		if (this._swayController == null)
		{
			this._swayController = this.GetNewSwayController();
		}
		CameraShakeController.AddEffect(new TrackerShake(this._trackerCamera, Quaternion.Euler(this._trackerOffset), this._trackerForceScale));
	}

	public override void InitSpectator(ReferenceHub ply, ItemIdentifier id, bool wasEquipped)
	{
		base.InitSpectator(ply, id, wasEquipped);
		if (this._swayController == null)
		{
			this._swayController = this.GetNewSwayController();
		}
	}

	protected virtual IItemSwayController GetNewSwayController()
	{
		return new GoopSway(new GoopSway.GoopSwaySettings(this.HandsPivot, 0.65f, 0.0035f, 0.04f, 7f, 6.5f, 0.03f, 1.6f, invertSway: false), base.Hub);
	}
}
