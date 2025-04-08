using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MapGeneration;
using Mirror;
using UnityEngine;

public class RoomLightController : NetworkBehaviour
{
	public static event Action<RoomLightController> OnAdded;

	public static event Action<RoomLightController> OnRemoved;

	public RoomIdentifier Room
	{
		get
		{
			if (!this._roomCacheSet)
			{
				if (!base.transform.TryGetComponentInParent(out this._cachedRoom))
				{
					throw new NullReferenceException("Null room for Light Controller: " + base.transform.GetHierarchyPath());
				}
				this._roomCacheSet = true;
			}
			return this._cachedRoom;
		}
	}

	public event Action<bool> OnLightsSet;

	public static event Action<RoomLightController> OnInstanceAdded;

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
		Action<RoomLightController> onInstanceAdded = RoomLightController.OnInstanceAdded;
		if (onInstanceAdded != null)
		{
			onInstanceAdded(this);
		}
		Action<RoomLightController> onAdded = RoomLightController.OnAdded;
		if (onAdded == null)
		{
			return;
		}
		onAdded(this);
	}

	private void OnDestroy()
	{
		RoomLightController.Instances.Remove(this);
		Action<RoomLightController> onRemoved = RoomLightController.OnRemoved;
		if (onRemoved == null)
		{
			return;
		}
		onRemoved(this);
	}

	private void Update()
	{
		if (!NetworkServer.active || this._flickerDuration <= 0f)
		{
			return;
		}
		this._flickerDuration -= Time.deltaTime;
		if (this._flickerDuration > 0f)
		{
			return;
		}
		this.SetLights(true);
	}

	private void SetLights(bool state)
	{
		if (NetworkServer.active)
		{
			this.NetworkLightsEnabled = state;
		}
		Action<bool> onLightsSet = this.OnLightsSet;
		if (onLightsSet == null)
		{
			return;
		}
		onLightsSet(state);
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
			return;
		}
		if (dur <= 0f)
		{
			this._flickerDuration = 0f;
			this.SetLights(true);
			return;
		}
		this._flickerDuration = dur;
		this.SetLights(false);
	}

	public static bool IsInDarkenedRoom(Vector3 positionToCheck)
	{
		RoomIdentifier roomIdentifier = RoomUtils.RoomAtPosition(positionToCheck);
		if (roomIdentifier == null)
		{
			roomIdentifier = RoomUtils.RoomAtPositionRaycasts(positionToCheck, true);
		}
		if (roomIdentifier == null)
		{
			return false;
		}
		foreach (RoomLightController roomLightController in roomIdentifier.LightControllers)
		{
			float num = Mathf.Abs(roomLightController.transform.position.y - positionToCheck.y);
			if (!roomLightController.LightsEnabled && num <= 100f)
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

	public bool NetworkLightsEnabled
	{
		get
		{
			return this.LightsEnabled;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<bool>(value, ref this.LightsEnabled, 1UL, new Action<bool, bool>(this.LightsEnabledHook));
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
			base.GeneratedSyncVarSetter<Color>(value, ref this.OverrideColor, 2UL, new Action<Color, Color>(this.OverrideColorHook));
		}
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
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteBool(this.LightsEnabled);
		}
		if ((base.syncVarDirtyBits & 2UL) != 0UL)
		{
			writer.WriteColor(this.OverrideColor);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize<bool>(ref this.LightsEnabled, new Action<bool, bool>(this.LightsEnabledHook), reader.ReadBool());
			base.GeneratedSyncVarDeserialize<Color>(ref this.OverrideColor, new Action<Color, Color>(this.OverrideColorHook), reader.ReadColor());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<bool>(ref this.LightsEnabled, new Action<bool, bool>(this.LightsEnabledHook), reader.ReadBool());
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<Color>(ref this.OverrideColor, new Action<Color, Color>(this.OverrideColorHook), reader.ReadColor());
		}
	}

	public const float HeightRadius = 100f;

	private float _flickerDuration;

	private RoomIdentifier _cachedRoom;

	private bool _roomCacheSet;

	[SyncVar(hook = "LightsEnabledHook")]
	public bool LightsEnabled;

	[SyncVar(hook = "OverrideColorHook")]
	public Color OverrideColor;

	public static readonly List<RoomLightController> Instances = new List<RoomLightController>();
}
