using System;
using UnityEngine;

namespace InventorySystem.Items.Jailbird
{
	public class JailbirdMaterialController : MonoBehaviour
	{
		private void Update()
		{
			JailbirdWearState jailbirdWearState;
			bool flag = JailbirdDeteriorationTracker.ReceivedStates.TryGetValue(this._serial, out jailbirdWearState) && jailbirdWearState >= JailbirdWearState.AlmostBroken;
			this._emissionRend.sharedMaterial = (flag ? this._almostDepletedMat : this._normalMat);
		}

		public void SetSerial(ushort serial)
		{
			this._serial = serial;
		}

		private ushort _serial;

		[SerializeField]
		private Material _almostDepletedMat;

		[SerializeField]
		private Material _normalMat;

		[SerializeField]
		private Renderer _emissionRend;
	}
}
