using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MapGeneration;
using Mirror;
using UnityEngine;
using Utils.NonAllocLINQ;

public class RoomLightController : NetworkBehaviour
{
	public const float HeightRadius = 50f;

	private float _flickerDuration;

	private RoomIdentifier _cachedRoom;

	private bool _roomCacheSet;

	[SyncVar(hook = "LightsEnabledHook")]
	public bool LightsEnabled;

	[SyncVar(hook = "OverrideColorHook")]
	public Color OverrideColor;

	public static readonly List<RoomLightController> Instances = new List<RoomLightController>();

	public RoomIdentifier Room
	{
		get
		{
			if (!_roomCacheSet)
			{
				if (!base.transform.TryGetComponentInParent<RoomIdentifier>(out _cachedRoom))
				{
					throw new NullReferenceException("Null room for Light Controller: " + base.transform.GetHierarchyPath());
				}
				_roomCacheSet = true;
			}
			return _cachedRoom;
		}
	}

	public bool NetworkLightsEnabled
	{
		get
		{
			return LightsEnabled;
		}
		[param: In]
		set
		{
			GeneratedSyncVarSetter(value, ref LightsEnabled, 1uL, LightsEnabledHook);
		}
	}

	public Color NetworkOverrideColor
	{
		get
		{
			return OverrideColor;
		}
		[param: In]
		set
		{
			GeneratedSyncVarSetter(value, ref OverrideColor, 2uL, OverrideColorHook);
		}
	}

	public static event Action<RoomLightController> OnAdded;

	public static event Action<RoomLightController> OnRemoved;

	public event Action<bool> OnLightsSet;

	private void Start()
	{
		if (NetworkServer.active)
		{
			NetworkLightsEnabled = true;
		}
		else
		{
			SetLights(LightsEnabled);
		}
		Instances.Add(this);
		RoomLightController.OnAdded?.Invoke(this);
		Room.LightControllers.AddIfNotContains(this);
	}

	private void OnDestroy()
	{
		Instances.Remove(this);
		RoomLightController.OnRemoved?.Invoke(this);
	}

	private void Update()
	{
		if (NetworkServer.active && !(_flickerDuration <= 0f))
		{
			_flickerDuration -= Time.deltaTime;
			if (!(_flickerDuration > 0f))
			{
				SetLights(state: true);
			}
		}
	}

	private void SetLights(bool state)
	{
		if (NetworkServer.active)
		{
			NetworkLightsEnabled = state;
		}
		this.OnLightsSet?.Invoke(state);
	}

	private void LightsEnabledHook(bool oldValue, bool newValue)
	{
	}

	private void OverrideColorHook(Color oldValue, Color newValue)
	{
	}

	[Server]
	public void ServerFlickerLights(float dur)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoomLightController::ServerFlickerLights(System.Single)' called when server was not active");
		}
		else if (dur <= 0f)
		{
			_flickerDuration = 0f;
			SetLights(state: true);
		}
		else
		{
			_flickerDuration = dur;
			SetLights(state: false);
		}
	}

	public static bool IsInDarkenedRoom(Vector3 positionToCheck)
	{
		if (positionToCheck.TryGetRoom(out var room))
		{
			return IsInDarkenedRoom(room, positionToCheck);
		}
		return false;
	}

	public static bool IsInDarkenedRoom(RoomIdentifier rid, Vector3 positionToCheck)
	{
		foreach (RoomLightController lightController in rid.LightControllers)
		{
			float num = Mathf.Abs(lightController.transform.position.y - positionToCheck.y);
			if (!lightController.LightsEnabled && !(num > 50f))
			{
				return true;
			}
		}
		return false;
	}

	public override bool Weaved()
	{
		return true;
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteBool(LightsEnabled);
			writer.WriteColor(OverrideColor);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteBool(LightsEnabled);
		}
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteColor(OverrideColor);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			GeneratedSyncVarDeserialize(ref LightsEnabled, LightsEnabledHook, reader.ReadBool());
			GeneratedSyncVarDeserialize(ref OverrideColor, OverrideColorHook, reader.ReadColor());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			GeneratedSyncVarDeserialize(ref LightsEnabled, LightsEnabledHook, reader.ReadBool());
		}
		if ((num & 2L) != 0L)
		{
			GeneratedSyncVarDeserialize(ref OverrideColor, OverrideColorHook, reader.ReadColor());
		}
	}
}
