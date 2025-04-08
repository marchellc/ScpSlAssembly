using System;
using System.Collections.Generic;
using Interactables.Interobjects;
using MapGeneration;
using UnityEngine;
using Utils.NonAllocLINQ;

public class ConnectorLight : MonoBehaviour
{
	private void Awake()
	{
		this._roomConnector = base.GetComponent<IRoomConnector>();
		if (this._roomConnector.RoomsAlreadyRegistered)
		{
			this.FindControllers();
			return;
		}
		this._roomConnector.OnRoomsRegistered += this.FindControllers;
	}

	private void FindControllers()
	{
		RoomIdentifier[] rooms = this._roomConnector.Rooms;
		foreach (RoomLightController roomLightController in RoomLightController.Instances)
		{
			if (rooms.Contains(roomLightController.Room))
			{
				this._lightControllers.Add(roomLightController);
				roomLightController.OnLightsSet += this.OnAnyUpdated;
			}
		}
	}

	private void OnAnyUpdated(bool _)
	{
		bool flag = this._lightControllers.Any((RoomLightController x) => x.LightsEnabled);
		foreach (GameObject gameObject in this._objectsToDisable)
		{
			if (!(gameObject == null))
			{
				gameObject.SetActive(flag);
			}
		}
	}

	[SerializeField]
	private GameObject[] _objectsToDisable;

	private IRoomConnector _roomConnector;

	private readonly List<RoomLightController> _lightControllers = new List<RoomLightController>();
}
