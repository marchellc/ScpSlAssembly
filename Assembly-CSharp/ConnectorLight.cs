using System.Collections.Generic;
using Interactables.Interobjects;
using MapGeneration;
using UnityEngine;
using Utils.NonAllocLINQ;

public class ConnectorLight : MonoBehaviour
{
	[SerializeField]
	private GameObject[] _objectsToDisable;

	private IRoomConnector _roomConnector;

	private readonly List<RoomLightController> _lightControllers = new List<RoomLightController>();

	private void Awake()
	{
		this._roomConnector = base.GetComponent<IRoomConnector>();
		if (this._roomConnector.RoomsAlreadyRegistered)
		{
			this.FindControllers();
		}
		else
		{
			this._roomConnector.OnRoomsRegistered += FindControllers;
		}
	}

	private void FindControllers()
	{
		RoomIdentifier[] rooms = this._roomConnector.Rooms;
		foreach (RoomLightController instance in RoomLightController.Instances)
		{
			if (rooms.Contains(instance.Room))
			{
				this._lightControllers.Add(instance);
				instance.OnLightsSet += OnAnyUpdated;
			}
		}
	}

	private void OnAnyUpdated(bool _)
	{
		bool active = this._lightControllers.Any((RoomLightController x) => x.LightsEnabled);
		GameObject[] objectsToDisable = this._objectsToDisable;
		foreach (GameObject gameObject in objectsToDisable)
		{
			if (!(gameObject == null))
			{
				gameObject.SetActive(active);
			}
		}
	}
}
