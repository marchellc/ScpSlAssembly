using UnityEngine;

namespace Interactables.Interobjects.DoorButtons;

public class KeycardScannerNfcIcon : MonoBehaviour
{
	[SerializeField]
	private Material _deniedMat;

	[SerializeField]
	private Material _errorMat;

	[SerializeField]
	private Material _grantedMat;

	[SerializeField]
	private Material _regularMat;

	[SerializeField]
	private MeshRenderer _renderer;

	private float _remainingUntilRegular;

	private void Awake()
	{
		base.enabled = false;
	}

	private void Update()
	{
		this._remainingUntilRegular -= Time.deltaTime;
		if (!(this._remainingUntilRegular > 0f))
		{
			this.SetRegular();
		}
	}

	public void SetMaterial(Material mat, float? time)
	{
		this._renderer.sharedMaterial = mat;
		if (time.HasValue)
		{
			base.enabled = true;
			this._remainingUntilRegular = time.Value;
		}
		else
		{
			base.enabled = false;
		}
	}

	public void SetRegular()
	{
		this.SetMaterial(this._regularMat, null);
	}

	public void SetGranted()
	{
		this.SetMaterial(this._grantedMat, null);
	}

	public void SetTemporaryGranted(float timeSeconds)
	{
		this.SetMaterial(this._grantedMat, timeSeconds);
	}

	public void SetDenied()
	{
		this.SetMaterial(this._deniedMat, null);
	}

	public void SetTemporaryDenied(float timeSeconds)
	{
		this.SetMaterial(this._deniedMat, timeSeconds);
	}

	public void SetError()
	{
		this.SetMaterial(this._errorMat, null);
	}
}
