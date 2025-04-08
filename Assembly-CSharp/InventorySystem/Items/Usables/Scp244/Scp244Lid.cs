using System;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp244
{
	public class Scp244Lid : MonoBehaviour
	{
		private void Update()
		{
			if (this._pickup.State != Scp244State.Active)
			{
				return;
			}
			if (Vector3.Dot(base.transform.up, Vector3.up) > this._upDot)
			{
				base.transform.localPosition += this._offset;
			}
			else
			{
				this._pressureSound.enabled = true;
			}
			base.enabled = false;
			base.gameObject.AddComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
		}

		[SerializeField]
		private Scp244DeployablePickup _pickup;

		[SerializeField]
		private Vector3 _offset;

		[SerializeField]
		private float _upDot;

		[SerializeField]
		private AudioSource _pressureSound;
	}
}
