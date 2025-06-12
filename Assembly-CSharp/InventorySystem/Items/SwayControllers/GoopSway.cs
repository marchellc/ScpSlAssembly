using System;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using UnityEngine;

namespace InventorySystem.Items.SwayControllers;

public class GoopSway : IItemSwayController
{
	[Serializable]
	public struct GoopSwaySettings
	{
		public Transform TargetTransform;

		public float SwayIntensity;

		public float TranslationIntensity;

		public float ZAxisIntensity;

		public float SwaySmoothness;

		public float TranslationSmoothness;

		public float BobIntensity;

		public float CentrifugalIntensity;

		public int Invert;

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
			this.Invert = ((!invertSway) ? 1 : (-1));
		}
	}

	private const float MaximumReasonableMouseSpeed = 15f;

	protected readonly ReferenceHub Owner;

	private readonly GoopSwaySettings _settings;

	private readonly Vector3 _positionOffset;

	private readonly Transform _ownerTransform;

	private readonly Transform _camTransform;

	private float _prevRotX;

	private float _prevRotY;

	private float CurRotX => this._ownerTransform.localEulerAngles.y;

	private float CurRotY => this._camTransform.localEulerAngles.x;

	private AnimatedCharacterModel CharModel
	{
		get
		{
			if (!(this.Owner.roleManager.CurrentRole is IFpcRole fpcRole))
			{
				return null;
			}
			return fpcRole.FpcModule.CharacterModelInstance as AnimatedCharacterModel;
		}
	}

	protected virtual GoopSwaySettings Settings => this._settings;

	protected virtual float OverallBobMultiplier => 12f;

	protected virtual float OverallSwayMultiplier => 0.013f;

	public GoopSway(GoopSwaySettings settings, ReferenceHub owner)
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
		if (NetworkClient.active)
		{
			this.CameraSway(this.Settings.TargetTransform);
			this.Transition(this.Settings.TargetTransform);
		}
	}

	private void CameraSway(Transform tr)
	{
		this.GetInput(out var x, out var y);
		GoopSwaySettings settings = this.Settings;
		Quaternion quaternion = Quaternion.AngleAxis(settings.SwayIntensity * x, -Vector3.up * settings.Invert);
		Quaternion quaternion2 = Quaternion.AngleAxis(settings.SwayIntensity * y, Vector3.right * settings.Invert);
		Quaternion quaternion3 = Quaternion.AngleAxis(settings.ZAxisIntensity * x, -Vector3.forward * settings.Invert);
		Quaternion b = quaternion * quaternion2 * quaternion3;
		tr.localRotation = Quaternion.Slerp(tr.localRotation, b, settings.SwaySmoothness * Time.deltaTime);
	}

	private void GetInput(out float x, out float y)
	{
		float num = 15f;
		float value = Vector3.Dot(this._ownerTransform.forward, this._camTransform.forward);
		float num2 = this.OverallSwayMultiplier / Time.deltaTime;
		x = Mathf.Clamp(num2 * Mathf.DeltaAngle(this._prevRotX, this.CurRotX), 0f - num, num);
		y = Mathf.Clamp(num2 * Mathf.DeltaAngle(this.CurRotY, this._prevRotY) * Mathf.Clamp01(value), 0f - num, num);
		this._prevRotX = this.CurRotX;
		this._prevRotY = this.CurRotY;
	}

	private void Transition(Transform tr)
	{
		GoopSwaySettings settings = this.Settings;
		Vector3 vector = this._ownerTransform.InverseTransformDirection(this.Owner.GetVelocity());
		float x = vector.x;
		float num = Mathf.Abs(vector.z);
		Vector3 b = new Vector3(settings.TranslationIntensity * (0f - x), settings.TranslationIntensity * (0f - num), 0f) + this._positionOffset;
		if (settings.CentrifugalIntensity != 0f)
		{
			b += Vector3.forward * (Quaternion.Angle(Quaternion.identity, tr.localRotation) / 360f) * settings.CentrifugalIntensity;
		}
		AnimatedCharacterModel charModel = this.CharModel;
		Vector3 vector2 = ((charModel != null) ? (settings.BobIntensity * this.OverallBobMultiplier * charModel.HeadBobPosition) : Vector3.zero);
		tr.localPosition = Vector3.Slerp(tr.localPosition - vector2, b, Time.deltaTime * settings.TranslationSmoothness) + vector2;
	}
}
