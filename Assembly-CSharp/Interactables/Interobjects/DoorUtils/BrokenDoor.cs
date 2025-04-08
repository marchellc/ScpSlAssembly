using System;
using UnityEngine;

namespace Interactables.Interobjects.DoorUtils
{
	public class BrokenDoor : MonoBehaviour
	{
		private void Start()
		{
			foreach (BrokenDoor.BrokenDoorPart brokenDoorPart in this._parts)
			{
				brokenDoorPart.Init();
			}
		}

		private void Update()
		{
			this._timeUntilFreeze -= Time.deltaTime;
			if (this._timeUntilFreeze < 0f)
			{
				foreach (BrokenDoor.BrokenDoorPart brokenDoorPart in this._parts)
				{
					brokenDoorPart.Freeze();
				}
				base.enabled = false;
			}
		}

		[SerializeField]
		private BrokenDoor.BrokenDoorPart[] _parts;

		[SerializeField]
		private float _timeUntilFreeze = 5f;

		[Serializable]
		public struct BrokenDoorPart
		{
			public void Init()
			{
				this._rigidbody.velocity = this._rigidbody.transform.TransformDirection(this._direction.normalized) * this._force;
				this._rigidbody.angularVelocity = new Vector3((global::UnityEngine.Random.value - 0.5f) * 2f, (global::UnityEngine.Random.value - 0.5f) * 2f, (global::UnityEngine.Random.value - 0.5f) * 2f) * this._randomTorque;
			}

			public void Freeze()
			{
				Collider[] components = this._rigidbody.GetComponents<Collider>();
				for (int i = 0; i < components.Length; i++)
				{
					components[i].enabled = false;
				}
				global::UnityEngine.Object.Destroy(this._rigidbody);
			}

			[SerializeField]
			private Rigidbody _rigidbody;

			[SerializeField]
			private float _force;

			[SerializeField]
			private float _randomTorque;

			[SerializeField]
			private Vector3 _direction;
		}
	}
}
