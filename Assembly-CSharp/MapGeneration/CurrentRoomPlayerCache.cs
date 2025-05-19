using Mirror;
using PlayerRoles;
using RoundRestarting;
using UnityEngine;

namespace MapGeneration;

public class CurrentRoomPlayerCache : MonoBehaviour
{
	[SerializeField]
	private PlayerRoleManager _roleManager;

	private bool _hasAnyValidRoom;

	private bool _lastValid;

	private RoomIdentifier _lastDetected;

	private int _lastFrame;

	private Transform _tr;

	private void Awake()
	{
		_tr = base.transform;
	}

	private void Update()
	{
		if (NetworkServer.active || _roleManager.isLocalPlayer)
		{
			ValidateCache();
		}
	}

	private void ValidateCache()
	{
		if (_lastFrame != Time.frameCount && !RoundRestart.IsRoundRestarting)
		{
			if (((_roleManager.CurrentRole is ICameraController cameraController) ? cameraController.CameraPosition : _tr.position).TryGetRoom(out var room))
			{
				_lastDetected = room;
				_lastValid = true;
				_hasAnyValidRoom = true;
			}
			else
			{
				_lastValid = false;
			}
			_lastFrame = Time.frameCount;
		}
	}

	public bool TryGetCurrent(out RoomIdentifier rid)
	{
		ValidateCache();
		rid = _lastDetected;
		return _lastValid;
	}

	public bool TryGetLastValid(out RoomIdentifier rid)
	{
		ValidateCache();
		rid = _lastDetected;
		return _hasAnyValidRoom;
	}
}
