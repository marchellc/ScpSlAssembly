using UnityEngine;

namespace InventorySystem.Items.Jailbird;

public class JailbirdMaterialController : MonoBehaviour
{
	private ushort _serial;

	[SerializeField]
	private Material _almostDepletedMat;

	[SerializeField]
	private Material _normalMat;

	[SerializeField]
	private Renderer _emissionRend;

	private void Update()
	{
		JailbirdWearState value;
		bool flag = JailbirdDeteriorationTracker.ReceivedStates.TryGetValue(_serial, out value) && value >= JailbirdWearState.AlmostBroken;
		_emissionRend.sharedMaterial = (flag ? _almostDepletedMat : _normalMat);
	}

	public void SetSerial(ushort serial)
	{
		_serial = serial;
	}
}
