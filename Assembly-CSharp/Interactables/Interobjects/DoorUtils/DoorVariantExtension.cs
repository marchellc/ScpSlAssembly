using UnityEngine;

namespace Interactables.Interobjects.DoorUtils;

public abstract class DoorVariantExtension : MonoBehaviour
{
	public DoorVariant TargetDoor;

	private void OnValidate()
	{
		this.TargetDoor = base.GetComponent<DoorVariant>();
	}

	private void Awake()
	{
		if (!this.TargetDoor)
		{
			this.TargetDoor = base.GetComponent<DoorVariant>();
		}
	}
}
