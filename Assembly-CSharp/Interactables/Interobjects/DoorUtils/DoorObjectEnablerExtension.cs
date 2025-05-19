using UnityEngine;

namespace Interactables.Interobjects.DoorUtils;

public class DoorObjectEnablerExtension : DoorVariantExtension
{
	public GameObject[] Objects;

	public bool EnableOnOpen = true;

	public bool EnableOnClose = true;

	public bool EnableOnMoving = true;

	public bool EnableOnPry = true;

	private bool _previousState;

	private void Start()
	{
		GameObject[] objects = Objects;
		for (int i = 0; i < objects.Length; i++)
		{
			objects[i].SetActive(value: false);
		}
	}

	private void Update()
	{
		bool targetState = TargetDoor.TargetState;
		bool isMoving = TargetDoor.IsMoving;
		bool flag = TargetDoor is PryableDoor pryableDoor && pryableDoor.IsBeingPried;
		bool flag2 = (EnableOnPry || !flag) && ((EnableOnOpen && targetState) || (EnableOnClose && !targetState) || (EnableOnMoving && isMoving) || (EnableOnPry && flag));
		if (flag2 != _previousState)
		{
			_previousState = flag2;
			GameObject[] objects = Objects;
			for (int i = 0; i < objects.Length; i++)
			{
				objects[i].SetActive(flag2);
			}
		}
	}
}
