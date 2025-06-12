using InventorySystem.Items.Firearms;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace InventorySystem.Crosshairs;

public class SingleBulletFirearmCrosshair : FirearmCrosshairBase
{
	[SerializeField]
	private RectTransform[] _elements;

	[SerializeField]
	private float _sizeRatio;

	[SerializeField]
	private float _width;

	[SerializeField]
	private float _lerpSpeed;

	[SerializeField]
	private AnimationCurve _sizeOverSpeed;

	private void SetupElements(float innerAngle, float speed, bool forceLerp)
	{
		float t = (forceLerp ? 1f : (Time.deltaTime * this._lerpSpeed));
		Vector2 b = this._sizeRatio * innerAngle * Vector2.left;
		Vector3 vector = new Vector3(this._sizeOverSpeed.Evaluate(speed), this._width);
		RectTransform[] elements = this._elements;
		foreach (RectTransform obj in elements)
		{
			obj.sizeDelta = Vector2.Lerp(obj.sizeDelta, vector, t);
			obj.anchoredPosition = Vector2.Lerp(obj.anchoredPosition, b, t);
		}
	}

	private void OnEnable()
	{
		this.SetupElements(0f, 20f, forceLerp: true);
	}

	protected override void UpdateCrosshair(Firearm firearm, float currentInaccuracy)
	{
		float speed = firearm.Owner.GetVelocity().MagnitudeIgnoreY();
		this.SetupElements(currentInaccuracy, speed, forceLerp: false);
	}
}
