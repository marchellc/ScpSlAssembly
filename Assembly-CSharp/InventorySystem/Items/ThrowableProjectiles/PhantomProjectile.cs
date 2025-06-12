using Mirror;
using UnityEngine;

namespace InventorySystem.Items.ThrowableProjectiles;

public class PhantomProjectile : MonoBehaviour
{
	public Rigidbody Rigidbody;

	[SerializeField]
	private float _minimalExistenceTime;

	[SerializeField]
	private float _transitionTime;

	[SerializeField]
	private Vector3 _startScale;

	[SerializeField]
	private Vector3 _targetScale;

	private const float AutoDestroyTime = 0.5f;

	private ushort _projectileSerial;

	private float _scaleFactor;

	private float _transitionFactor;

	private float _replaceTime;

	private bool _hasPickup;

	private ThrownProjectile _pickupToReplace;

	private float CurTime => Time.timeSinceLevelLoad;

	public void Init(ushort serial)
	{
		ThrownProjectile.OnProjectileSpawned += OnSpawned;
		this._projectileSerial = serial;
		base.gameObject.SetActive(value: false);
	}

	public void Activate(Transform cam, Vector3 relativePosition)
	{
		base.gameObject.SetActive(value: true);
		this._replaceTime = this.CurTime + this._minimalExistenceTime;
		this._scaleFactor = -1f;
		this._transitionFactor = -1f;
		Object.Destroy(base.gameObject, this._minimalExistenceTime + this._transitionTime + 0.5f);
		base.transform.SetParent(null);
		base.transform.position = cam.TransformPoint(relativePosition);
		base.transform.localScale = this._startScale;
	}

	public void Replace()
	{
		if (this._pickupToReplace != null)
		{
			if (this._transitionFactor < 1f && this._pickupToReplace.TryGetComponent<Rigidbody>(out var component))
			{
				this._transitionFactor = Mathf.Clamp01(this._transitionFactor + Time.deltaTime / this._transitionTime);
				this.Rigidbody.MovePosition(Vector3.Lerp(this.Rigidbody.position, component.position, this._transitionFactor));
				this.Rigidbody.linearVelocity = Vector3.Lerp(this.Rigidbody.linearVelocity, component.linearVelocity, this._transitionFactor);
				this.Rigidbody.rotation = Quaternion.Lerp(this.Rigidbody.rotation, component.rotation, this._transitionFactor);
				return;
			}
			this._pickupToReplace.ToggleRenderers(state: true);
		}
		Object.Destroy(base.gameObject);
	}

	private void OnDestroy()
	{
		ThrownProjectile.OnProjectileSpawned -= OnSpawned;
	}

	private void Update()
	{
		if (this._scaleFactor < 1f)
		{
			this._scaleFactor = Mathf.Clamp01(this._scaleFactor + Time.deltaTime / this._minimalExistenceTime);
			base.transform.localScale = Vector3.Lerp(this._startScale, this._targetScale, this._scaleFactor);
		}
		if (this._hasPickup && !(this.CurTime < this._replaceTime))
		{
			this.Replace();
		}
	}

	private void OnSpawned(ThrownProjectile projectile)
	{
		if (projectile.Info.Serial == this._projectileSerial)
		{
			this._hasPickup = true;
			this._pickupToReplace = projectile;
			if (!NetworkServer.active)
			{
				projectile.ToggleRenderers(state: false);
			}
			else
			{
				this.Replace();
			}
		}
	}
}
