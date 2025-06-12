using UnityEngine;

namespace MapGeneration.Clutter;

public class ClutterBlocker : MonoBehaviour, IClutterBlocker
{
	[SerializeField]
	private Bounds _blockingBounds;

	[SerializeField]
	private Transform _targetTransform;

	public Bounds BlockingBounds
	{
		get
		{
			this._targetTransform.GetPositionAndRotation(out var position, out var rotation);
			Vector3 vector = rotation * this._blockingBounds.center;
			Vector3 size = (rotation * this._blockingBounds.size).Abs();
			return new Bounds(position + vector, size);
		}
	}

	private void Awake()
	{
		IClutterBlocker.Instances.Add(this);
	}

	private void OnDestroy()
	{
		IClutterBlocker.Instances.Remove(this);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(this.BlockingBounds.center, this.BlockingBounds.size);
	}

	private void OnValidate()
	{
		if (!(this._targetTransform != null))
		{
			this._targetTransform = base.transform;
		}
	}
}
