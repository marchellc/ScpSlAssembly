using System;
using System.Collections.Generic;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace MapGeneration;

public class RoomIdentifier : MonoBehaviour
{
	public static readonly HashSet<RoomIdentifier> AllRoomIdentifiers = new HashSet<RoomIdentifier>();

	public static readonly Dictionary<Vector3Int, RoomIdentifier> RoomsByCoords = new Dictionary<Vector3Int, RoomIdentifier>();

	public static readonly Vector3 GridScale = new Vector3(15f, 100f, 15f);

	public readonly HashSet<RoomIdentifier> ConnectedRooms = new HashSet<RoomIdentifier>();

	public readonly List<RoomLightController> LightControllers = new List<RoomLightController>();

	public RoomShape Shape;

	public RoomName Name;

	public FacilityZone Zone;

	public Sprite Icon;

	public Vector3Int MainCoords { get; private set; }

	public Bounds WorldspaceBounds { get; private set; }

	public static event Action<RoomIdentifier> OnAdded;

	public static event Action<RoomIdentifier> OnRemoved;

	private void Awake()
	{
		SeedSynchronizer.OnGenerationStage += OnMapgenPhase;
		if (SeedSynchronizer.MapGenerated)
		{
			this.RegisterCoords();
		}
		RoomIdentifier.AllRoomIdentifiers.Add(this);
		RoomIdentifier.OnAdded?.Invoke(this);
	}

	private void OnDestroy()
	{
		SeedSynchronizer.OnGenerationStage -= OnMapgenPhase;
		RoomIdentifier.AllRoomIdentifiers.Remove(this);
		RoomIdentifier.RoomsByCoords.Remove(this.MainCoords);
		RoomIdentifier.OnRemoved?.Invoke(this);
		this.LightControllers.Clear();
	}

	private void OnMapgenPhase(MapGenerationPhase phase)
	{
		if (phase == MapGenerationPhase.RoomCoordsRegistrations)
		{
			this.RegisterCoords();
		}
	}

	private void RegisterCoords()
	{
		this.MainCoords = RoomUtils.PositionToCoords(base.transform.position);
		RoomIdentifier.RoomsByCoords[this.MainCoords] = this;
		this.RecalculateWorldspaceBounds();
	}

	private void RecalculateWorldspaceBounds()
	{
		Bounds worldspaceBounds = new Bounds(base.transform.position, Vector3.zero);
		MeshRenderer[] componentsInChildren = base.GetComponentsInChildren<MeshRenderer>();
		foreach (MeshRenderer meshRenderer in componentsInChildren)
		{
			meshRenderer.ResetBounds();
			worldspaceBounds.Encapsulate(meshRenderer.bounds);
		}
		this.WorldspaceBounds = worldspaceBounds;
	}

	public RoomLightController GetClosestLightController(ReferenceHub hub)
	{
		if (hub == null || !(hub.roleManager.CurrentRole is IFpcRole fpcRole))
		{
			return null;
		}
		if (this.LightControllers.Count == 0)
		{
			return null;
		}
		if (this.LightControllers.Count == 1)
		{
			return this.LightControllers[0];
		}
		Vector3 position = fpcRole.FpcModule.Position;
		float num = float.MaxValue;
		RoomLightController result = null;
		foreach (RoomLightController lightController in this.LightControllers)
		{
			float sqrMagnitude = (lightController.transform.position - position).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				result = lightController;
			}
		}
		return result;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.white;
		Gizmos.DrawWireCube(this.WorldspaceBounds.center, this.WorldspaceBounds.size);
	}
}
