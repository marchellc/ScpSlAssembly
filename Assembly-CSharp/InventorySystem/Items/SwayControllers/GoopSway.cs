using System;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using UnityEngine;

namespace InventorySystem.Items.SwayControllers
{
	public class GoopSway : IItemSwayController
	{
		private float CurRotX
		{
			get
			{
				return this._ownerTransform.localEulerAngles.y;
			}
		}

		private float CurRotY
		{
			get
			{
				return this._camTransform.localEulerAngles.x;
			}
		}

		private AnimatedCharacterModel CharModel
		{
			get
			{
				IFpcRole fpcRole = this.Owner.roleManager.CurrentRole as IFpcRole;
				if (fpcRole == null)
				{
					return null;
				}
				return fpcRole.FpcModule.CharacterModelInstance as AnimatedCharacterModel;
			}
		}

		protected virtual GoopSway.GoopSwaySettings Settings
		{
			get
			{
				return this._settings;
			}
		}

		protected virtual float OverallBobMultiplier
		{
			get
			{
				return 12f;
			}
		}

		protected virtual float OverallSwayMultiplier
		{
			get
			{
				return 0.013f;
			}
		}

		public GoopSway(GoopSway.GoopSwaySettings settings, ReferenceHub owner)
		{
			this._settings = settings;
			this._positionOffset = settings.TargetTransform.localPosition;
			this.Owner = owner;
			this._ownerTransform = this.Owner.transform;
			this._camTransform = owner.PlayerCameraReference;
			this._prevRotX = this.CurRotX;
			this._prevRotY = this.CurRotY;
		}

		public virtual void UpdateSway()
		{
			if (!NetworkClient.active)
			{
				return;
			}
			this.CameraSway(this.Settings.TargetTransform);
			this.Transition(this.Settings.TargetTransform);
		}

		private void CameraSway(Transform tr)
		{
			float num;
			float num2;
			this.GetInput(out num, out num2);
			GoopSway.GoopSwaySettings settings = this.Settings;
			Quaternion quaternion = Quaternion.AngleAxis(settings.SwayIntensity * num, -Vector3.up * (float)settings.Invert);
			Quaternion quaternion2 = Quaternion.AngleAxis(settings.SwayIntensity * num2, Vector3.right * (float)settings.Invert);
			Quaternion quaternion3 = Quaternion.AngleAxis(settings.ZAxisIntensity * num, -Vector3.forward * (float)settings.Invert);
			Quaternion quaternion4 = quaternion * quaternion2 * quaternion3;
			tr.localRotation = Quaternion.Slerp(tr.localRotation, quaternion4, settings.SwaySmoothness * Time.deltaTime);
		}

		private void GetInput(out float x, out float y)
		{
			float num = 15f;
			float num2 = Vector3.Dot(this._ownerTransform.forward, this._camTransform.forward);
			float num3 = this.OverallSwayMultiplier / Time.deltaTime;
			x = Mathf.Clamp(num3 * Mathf.DeltaAngle(this._prevRotX, this.CurRotX), -num, num);
			y = Mathf.Clamp(num3 * Mathf.DeltaAngle(this.CurRotY, this._prevRotY) * Mathf.Clamp01(num2), -num, num);
			this._prevRotX = this.CurRotX;
			this._prevRotY = this.CurRotY;
		}

		private void Transition(Transform tr)
		{
			GoopSway.GoopSwaySettings settings = this.Settings;
			Vector3 vector = this._ownerTransform.InverseTransformDirection(this.Owner.GetVelocity());
			float x = vector.x;
			float num = Mathf.Abs(vector.z);
			Vector3 vector2 = new Vector3(settings.TranslationIntensity * -x, settings.TranslationIntensity * -num, 0f) + this._positionOffset;
			if (settings.CentrifugalIntensity != 0f)
			{
				vector2 += Vector3.forward * (Quaternion.Angle(Quaternion.identity, tr.localRotation) / 360f) * settings.CentrifugalIntensity;
			}
			AnimatedCharacterModel charModel = this.CharModel;
			Vector3 vector3 = ((charModel != null) ? (settings.BobIntensity * this.OverallBobMultiplier * charModel.HeadBobPosition) : Vector3.zero);
			tr.localPosition = Vector3.Slerp(tr.localPosition - vector3, vector2, Time.deltaTime * settings.TranslationSmoothness) + vector3;
		}

		private const float MaximumReasonableMouseSpeed = 15f;

		protected readonly ReferenceHub Owner;

		private readonly GoopSway.GoopSwaySettings _settings;

		private readonly Vector3 _positionOffset;

		private readonly Transform _ownerTransform;

		private readonly Transform _camTransform;

		private float _prevRotX;

		private float _prevRotY;

		[Serializable]
		public struct GoopSwaySettings
		{
			public GoopSwaySettings(Transform targetTransform, float swayIntensity, float translationIntensity, float zAxisIntensity, float swaySmoothness, float translationSmoothness, float bobIntensity, float centrifugalIntensity, bool invertSway)
			{
				this.TargetTransform = targetTransform;
				this.SwayIntensity = swayIntensity;
				this.TranslationIntensity = translationIntensity;
				this.ZAxisIntensity = zAxisIntensity;
				this.SwaySmoothness = swaySmoothness;
				this.TranslationSmoothness = translationSmoothness;
				this.BobIntensity = bobIntensity;
				this.CentrifugalIntensity = centrifugalIntensity;
				this.Invert = (invertSway ? (-1) : 1);
			}

			public Transform TargetTransform;

			public float SwayIntensity;

			public float TranslationIntensity;

			public float ZAxisIntensity;

			public float SwaySmoothness;

			public float TranslationSmoothness;

			public float BobIntensity;

			public float CentrifugalIntensity;

			public int Invert;
		}
	}
}
