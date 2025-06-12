using Mirror;
using UnityEngine;

namespace Interactables.Interobjects.DoorUtils;

public class DoorRandomInitialStateExtension : DoorVariantExtension
{
	[Range(0f, 1f)]
	public float OpenChance = 0.5f;

	private void Start()
	{
		if (NetworkServer.active)
		{
			base.TargetDoor.NetworkTargetState = Random.value < this.OpenChance;
		}
	}
}
