using System;
using UnityEngine;

public class WeaponShootAnimation : MonoBehaviour
{
	private void LateUpdate()
	{
		if ((double)this.curPosition > 0.03)
		{
			this.curPosition = Mathf.Lerp(this.curPosition, 0f, Time.deltaTime * this.backSpeed * this.curPosition);
		}
		else
		{
			this.curPosition -= Time.deltaTime * 0.1f;
		}
		if (this.curPosition < 0f)
		{
			this.curPosition = 0f;
		}
		this.yOverride = Mathf.Lerp(0f, this.yOverride, this.curPosition);
		this.curY = Mathf.Lerp(this.curY, this.yOverride, Time.deltaTime * this.backY_Speed * this.curPosition);
		base.transform.localPosition = Vector3.Lerp(Vector3.zero, this.maxRecoilPos, this.curPosition);
		base.transform.localRotation = Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(this.maxRecoilRot + Vector3.up * this.curY), this.curPosition);
	}

	public void Recoil(float f)
	{
		this.curPosition = Mathf.Clamp01(this.curPosition + f);
		this.yOverride = global::UnityEngine.Random.Range(-10f, 10f) * f;
	}

	public float curPosition;

	public Vector3 maxRecoilPos;

	public Vector3 maxRecoilRot;

	public float backSpeed;

	public float backY_Speed;

	private float yOverride;

	private float curY;
}
