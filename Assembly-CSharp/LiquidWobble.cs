using System;
using UnityEngine;

public class LiquidWobble : MonoBehaviour
{
	[SerializeField]
	private float _maxWobble = 0.03f;

	[SerializeField]
	private float _wobbleSpeed = 1f;

	[SerializeField]
	private float _recovery = 1f;

	private Renderer _renderer;

	private Vector3 _lastPosition;

	private Vector3 _velocity;

	private Vector3 _lastRotation;

	private Vector3 _angularVelocity;

	private float _wobbleAmountX;

	private float _wobbleAmountZ;

	private float _wobbleAmountToAddX;

	private float _wobbleAmountToAddZ;

	private float _pulse;

	private float _time = 0.5f;

	private void Start()
	{
		this._renderer = base.GetComponent<Renderer>();
	}

	private void Update()
	{
		this._time += Time.deltaTime;
		this._wobbleAmountToAddX = Mathf.Lerp(this._wobbleAmountToAddX, 0f, Time.deltaTime * this._recovery);
		this._wobbleAmountToAddZ = Mathf.Lerp(this._wobbleAmountToAddZ, 0f, Time.deltaTime * this._recovery);
		this._pulse = MathF.PI * 2f * this._wobbleSpeed;
		this._wobbleAmountX = this._wobbleAmountToAddX * Mathf.Sin(this._pulse * this._time);
		this._wobbleAmountZ = this._wobbleAmountToAddZ * Mathf.Sin(this._pulse * this._time);
		this._renderer.material.SetFloat("_WobbleZ", this._wobbleAmountX);
		this._renderer.material.SetFloat("_WobbleX", this._wobbleAmountZ);
		this._velocity = (this._lastPosition - base.transform.position) / Time.deltaTime;
		this._angularVelocity = base.transform.rotation.eulerAngles - this._lastRotation;
		this._wobbleAmountToAddX += Mathf.Clamp((this._velocity.x + this._angularVelocity.z * 0.2f) * this._maxWobble, 0f - this._maxWobble, this._maxWobble);
		this._wobbleAmountToAddZ += Mathf.Clamp((this._velocity.z + this._angularVelocity.x * 0.2f) * this._maxWobble, 0f - this._maxWobble, this._maxWobble);
		this._lastPosition = base.transform.position;
		this._lastRotation = base.transform.rotation.eulerAngles;
	}
}
