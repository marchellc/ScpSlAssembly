using System;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.ThrowableProjectiles
{
	public class PhantomProjectile : MonoBehaviour
	{
		private float CurTime
		{
			get
			{
				return Time.timeSinceLevelLoad;
			}
		}

		public void Init(ushort serial)
		{
			ThrownProjectile.OnProjectileSpawned += this.OnSpawned;
			this._projectileSerial = serial;
			base.gameObject.SetActive(false);
		}

		public void Activate(Transform cam, Vector3 relativePosition)
		{
			base.gameObject.SetActive(true);
			this._replaceTime = this.CurTime + this._minimalExistenceTime;
			this._scaleFactor = -1f;
			this._transitionFactor = -1f;
			global::UnityEngine.Object.Destroy(base.gameObject, this._minimalExistenceTime + this._transitionTime + 0.5f);
			base.transform.SetParent(null);
			base.transform.position = cam.TransformPoint(relativePosition);
			base.transform.localScale = this._startScale;
		}

		private void OnDestroy()
		{
			ThrownProjectile.OnProjectileSpawned -= this.OnSpawned;
		}

		private void Update()
		{
			if (this._scaleFactor < 1f)
			{
				this._scaleFactor = Mathf.Clamp01(this._scaleFactor + Time.deltaTime / this._minimalExistenceTime);
				base.transform.localScale = Vector3.Lerp(this._startScale, this._targetScale, this._scaleFactor);
			}
			if (!this._hasPickup || this.CurTime < this._replaceTime)
			{
				return;
			}
			this.Replace();
		}

		private void Replace()
		{
			if (this._pickupToReplace != null)
			{
				Rigidbody rigidbody;
				if (this._transitionFactor < 1f && this._pickupToReplace.TryGetComponent<Rigidbody>(out rigidbody))
				{
					this._transitionFactor = Mathf.Clamp01(this._transitionFactor + Time.deltaTime / this._transitionTime);
					this.Rigidbody.MovePosition(Vector3.Lerp(this.Rigidbody.position, rigidbody.position, this._transitionFactor));
					this.Rigidbody.velocity = Vector3.Lerp(this.Rigidbody.velocity, rigidbody.velocity, this._transitionFactor);
					this.Rigidbody.rotation = Quaternion.Lerp(this.Rigidbody.rotation, rigidbody.rotation, this._transitionFactor);
					return;
				}
				this._pickupToReplace.ToggleRenderers(true);
			}
			global::UnityEngine.Object.Destroy(base.gameObject);
		}

		private void OnSpawned(ThrownProjectile projectile)
		{
			if (projectile.Info.Serial != this._projectileSerial)
			{
				return;
			}
			this._hasPickup = true;
			this._pickupToReplace = projectile;
			if (!NetworkServer.active)
			{
				projectile.ToggleRenderers(false);
				return;
			}
			this.Replace();
		}

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
	}
}
