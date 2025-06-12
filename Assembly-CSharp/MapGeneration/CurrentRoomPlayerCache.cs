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
		this._tr = base.transform;
	}

	private void Update()
	{
		if (NetworkServer.active || this._roleManager.isLocalPlayer)
		{
			this.ValidateCache();
		}
	}

	private void ValidateCache()
	{
		if (this._lastFrame != Time.frameCount && !RoundRestart.IsRoundRestarting)
		{
			if (((this._roleManager.CurrentRole is ICameraController cameraController) ? cameraController.CameraPosition : this._tr.position).TryGetRoom(out var room))
			{
				this._lastDetected = room;
				this._lastValid = true;
				this._hasAnyValidRoom = true;
			}
			else
			{
				this._lastValid = false;
			}
			this._lastFrame = Time.frameCount;
		}
	}

	public bool TryGetCurrent(out RoomIdentifier rid)
	{
		this.ValidateCache();
		rid = this._lastDetected;
		return this._lastValid;
	}

	public bool TryGetLastValid(out RoomIdentifier rid)
	{
		this.ValidateCache();
		rid = this._lastDetected;
		return this._hasAnyValidRoom;
	}
}
