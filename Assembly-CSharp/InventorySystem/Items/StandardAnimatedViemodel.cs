using System;
using CameraShaking;
using InventorySystem.Items.SwayControllers;
using UnityEngine;

namespace InventorySystem.Items
{
	public class StandardAnimatedViemodel : AnimatedViewmodelBase
	{
		public override IItemSwayController SwayController
		{
			get
			{
				return this._swayController;
			}
		}

		public override float ViewmodelCameraFOV
		{
			get
			{
				return this._fov;
			}
		}

		public override void InitAny()
		{
			base.InitAny();
			this._swayController = this.GetNewSwayController();
		}

		internal override void OnEquipped()
		{
			base.OnEquipped();
			CameraShakeController.AddEffect(new TrackerShake(this._trackerCamera, Quaternion.Euler(this._trackerOffset), this._trackerForceScale));
		}

		protected virtual IItemSwayController GetNewSwayController()
		{
			return new GoopSway(new GoopSway.GoopSwaySettings(this.HandsPivot, 0.65f, 0.0035f, 0.04f, 7f, 6.5f, 0.03f, 1.6f, false), base.Hub);
		}

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
	}
}
