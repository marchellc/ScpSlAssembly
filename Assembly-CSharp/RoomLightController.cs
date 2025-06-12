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
			if (!this._roomCacheSet)
			{
				if (!base.transform.TryGetComponentInParent<RoomIdentifier>(out this._cachedRoom))
				{
					throw new NullReferenceException("Null room for Light Controller: " + base.transform.GetHierarchyPath());
				}
				this._roomCacheSet = true;
			}
			return this._cachedRoom;
		}
	}

	public bool NetworkLightsEnabled
	{
		get
		{
			return this.LightsEnabled;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.LightsEnabled, 1uL, LightsEnabledHook);
		}
	}

	public Color NetworkOverrideColor
	{
		get
		{
			return this.OverrideColor;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.OverrideColor, 2uL, OverrideColorHook);
		}
	}

	public static event Action<RoomLightController> OnAdded;

	public static event Action<RoomLightController> OnRemoved;

	public event Action<bool> OnLightsSet;

	private void Start()
	{
		if (NetworkServer.active)
		{
			this.NetworkLightsEnabled = true;
		}
		else
		{
			this.SetLights(this.LightsEnabled);
		}
		RoomLightController.Instances.Add(this);
		RoomLightController.OnAdded?.Invoke(this);
		this.Room.LightControllers.AddIfNotContains(this);
	}

	private void OnDestroy()
	{
		RoomLightController.Instances.Remove(this);
		RoomLightController.OnRemoved?.Invoke(this);
	}

	private void Update()
	{
		if (NetworkServer.active && !(this._flickerDuration <= 0f))
		{
			this._flickerDuration -= Time.deltaTime;
			if (!(this._flickerDuration > 0f))
			{
				this.SetLights(state: true);
			}
		}
	}

	private void SetLights(bool state)
	{
		if (NetworkServer.active)
		{
			this.NetworkLightsEnabled = state;
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
			this._flickerDuration = 0f;
			this.SetLights(state: true);
		}
		else
		{
			this._flickerDuration = dur;
			this.SetLights(state: false);
		}
	}

	public static bool IsInDarkenedRoom(Vector3 positionToCheck)
	{
		if (positionToCheck.TryGetRoom(out var room))
		{
			return RoomLightController.IsInDarkenedRoom(room, positionToCheck);
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
			writer.WriteBool(this.LightsEnabled);
			writer.WriteColor(this.OverrideColor);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteBool(this.LightsEnabled);
		}
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteColor(this.OverrideColor);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.LightsEnabled, LightsEnabledHook, reader.ReadBool());
			base.GeneratedSyncVarDeserialize(ref this.OverrideColor, OverrideColorHook, reader.ReadColor());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.LightsEnabled, LightsEnabledHook, reader.ReadBool());
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.OverrideColor, OverrideColorHook, reader.ReadColor());
		}
	}
}
