using System;
using System.Diagnostics;
using GameObjectPools;
using PlayerRoles.PlayableScps.Scp079.Overcons;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Cameras
{
	public class Scp079ForwardCameraSelector : Scp079DirectionalCameraSelector, IPoolResettable
	{
		public static Scp079Camera HighlightedCamera { get; private set; }

		private void OnCameraChanged()
		{
			if (!this._switchRequested)
			{
				return;
			}
			this._switchRequested = false;
			if (base.CurrentCamSync.CurrentCamera != this._requestedCamera)
			{
				return;
			}
			if (this._requestTimer.Elapsed.TotalSeconds > 4.0)
			{
				return;
			}
			CameraRotationAxis horizontalAxis = this._requestedCamera.HorizontalAxis;
			float num = (horizontalAxis.TargetValue + this._requestedRotation - horizontalAxis.Pivot.eulerAngles.y) % 360f;
			float minValue = horizontalAxis.MinValue;
			float maxValue = horizontalAxis.MaxValue;
			if (num > maxValue || num < minValue)
			{
				float num2 = Mathf.Abs(Mathf.DeltaAngle(num, minValue));
				float num3 = Mathf.Abs(Mathf.DeltaAngle(num, maxValue));
				num = ((num2 < num3) ? minValue : maxValue);
			}
			horizontalAxis.TargetValue = num;
		}

		protected override bool TryGetCamera(out Scp079Camera targetCamera)
		{
			bool flag = false;
			float num = this._minDot;
			targetCamera = null;
			Scp079Camera currentCamera = base.CurrentCamSync.CurrentCamera;
			Vector3 position = MainCameraController.CurrentCamera.position;
			Vector3 forward = MainCameraController.CurrentCamera.forward;
			float num2 = this._maxDistance * this._maxDistance;
			foreach (CameraOvercon cameraOvercon in CameraOverconRenderer.VisibleOvercons)
			{
				if (!(cameraOvercon == currentCamera))
				{
					Vector3 vector = cameraOvercon.Position - position;
					float sqrMagnitude = vector.sqrMagnitude;
					if (sqrMagnitude >= 0.2f && sqrMagnitude <= num2)
					{
						float num3 = Vector3.Dot(forward, vector / Mathf.Sqrt(sqrMagnitude));
						if (num3 >= num && (!cameraOvercon.IsElevator || num3 >= this._elevatorSwitchDot.Evaluate(sqrMagnitude)))
						{
							flag = true;
							num = num3;
							targetCamera = cameraOvercon.Target;
						}
					}
				}
			}
			if (flag || base.TryGetCamera(out targetCamera))
			{
				Scp079ForwardCameraSelector.HighlightedCamera = targetCamera;
				return true;
			}
			Scp079ForwardCameraSelector.HighlightedCamera = null;
			return false;
		}

		protected override void Trigger()
		{
			if (this.TryGetCamera(out this._requestedCamera) && this._requestedCamera.Room != base.CurrentCamSync.CurrentCamera.Room)
			{
				Transform transform = this._requestedCamera.Room.transform;
				Transform transform2 = base.CurrentCamSync.CurrentCamera.Room.transform;
				Vector3 vector = transform.position - transform2.position;
				this._requestedRotation = Vector3.SignedAngle(Vector3.forward, vector, Vector3.up);
				this._switchRequested = true;
				this._requestTimer.Restart();
			}
			base.Trigger();
		}

		protected override void Start()
		{
			base.Start();
			base.CurrentCamSync.OnCameraChanged += this.OnCameraChanged;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this._switchRequested = false;
		}

		[SerializeField]
		private float _maxDistance;

		[SerializeField]
		private float _minDot;

		[SerializeField]
		private AnimationCurve _elevatorSwitchDot;

		private bool _switchRequested;

		private Scp079Camera _requestedCamera;

		private float _requestedRotation;

		private readonly Stopwatch _requestTimer = new Stopwatch();

		private const float MinDisSqr = 0.2f;

		private const float RequestTimeout = 4f;
	}
}
