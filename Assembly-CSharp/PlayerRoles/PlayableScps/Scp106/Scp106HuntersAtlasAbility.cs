using GameObjectPools;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.Scp106Events;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Spectating;
using ProgressiveCulling;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp106;

public class Scp106HuntersAtlasAbility : Scp106VigorAbilityBase, IPoolResettable
{
	public const float CostPerMeter = 0.019f;

	private const ActionName SelectKey = ActionName.Shoot;

	private const int SyncAccuracy = 50;

	private const float HeightOffset = 0.2f;

	private const float NormalMultiplier = 1.1f;

	private const float GroundDetectorHeight = 2f;

	private const float DissolvePercent = 0.5f;

	private const float MaxRetakeRange = 15f;

	private const float HeightTolerance = 100f;

	private const float DoorHeightTolerance = 40f;

	private const int PhysicsMaxDetections = 8;

	private const float MinVigor = 0.25f;

	private Vector3 _syncPos;

	private RoomIdentifier _syncRoom;

	private bool _syncSubmerged;

	private float _lastDissolveAmount;

	private float _estimatedCost;

	private static LayerMask? _absoluteCollisionMask;

	private static readonly float DebugDuration = 0f;

	private static readonly Collider[] DetectionsNonAlloc = new Collider[8];

	private static readonly RaycastHit[] HitsNonAlloc = new RaycastHit[8];

	protected override ActionName TargetKey => ActionName.Inventory;

	public override bool ServerWantsSubmerged => this._syncSubmerged;

	public override float SubmergeTime => 2f;

	public override float EmergeTime => 1f;

	protected override bool KeyPressable => base.Owner.isLocalPlayer;

	private void UpdateAny()
	{
		if (base.Owner.isLocalPlayer || base.Owner.IsLocallySpectated())
		{
			float submergeProgress = base.CastRole.Sinkhole.SubmergeProgress;
			this._lastDissolveAmount = (this._syncSubmerged ? Mathf.InverseLerp(0.5f, 1f, submergeProgress) : 0f);
			Scp106Hud.SetDissolveAnimation(this._lastDissolveAmount);
		}
	}

	private void UpdateClientside()
	{
		Scp106Minimap singleton = Scp106Minimap.Singleton;
		if (singleton == null)
		{
			return;
		}
		if (base.CastRole.Sinkhole.SubmergeProgress != 0f || !this.IsKeyHeld || !base.CastRole.FpcModule.IsGrounded || !base.Owner.TryGetCurrentRoom(out var _))
		{
			singleton.IsVisible = false;
			return;
		}
		if (!base.CastRole.Sinkhole.ReadonlyCooldown.IsReady)
		{
			singleton.IsVisible = false;
			Scp106Hud.PlayFlash(vigor: false);
			return;
		}
		if (base.VigorAmount < 0.25f)
		{
			singleton.IsVisible = false;
			Scp106Hud.PlayFlash(vigor: true);
			return;
		}
		singleton.IsVisible = true;
		if (Scp106MinimapElement.AnyHighlighted && Input.GetKey(NewInput.GetKey(ActionName.Shoot)))
		{
			this._syncPos = singleton.LastWorldPos;
			this._syncRoom = Scp106MinimapElement.LastHighlighted.Room;
			base.ClientSendCmd();
		}
	}

	private void UpdateServerside()
	{
		if (this._syncSubmerged && !(base.CastRole.Sinkhole.SubmergeProgress < 1f))
		{
			Vector3 safePosition = this.GetSafePosition();
			Vector3 position = base.CastRole.FpcModule.Position;
			float num = (safePosition - position).MagnitudeIgnoreY();
			base.VigorAmount -= Mathf.Min(this._estimatedCost, num * 0.019f);
			base.CastRole.FpcModule.ServerOverridePosition(safePosition);
			this._syncSubmerged = false;
			Scp106Events.OnUsedHunterAtlas(new Scp106UsedHunterAtlasEventArgs(base.Owner, position));
			base.ServerSendRpc(toAll: true);
		}
	}

	private Vector3 GetSafePosition()
	{
		Vector3 result = base.CastRole.FpcModule.Position;
		float num = float.MaxValue;
		foreach (Pose location in SafeLocationFinder.GetLocations(ValidateDestinationConnection, ValidateDestinationDoor))
		{
			if (!(Mathf.Abs(location.position.y - this._syncPos.y) > 40f))
			{
				Vector3 vector = this.ClosestDoorPosition(location.position);
				float num2 = (vector - this._syncPos).SqrMagnitudeIgnoreY();
				if (!(num2 > num))
				{
					num = num2;
					result = vector;
				}
			}
		}
		return result;
	}

	private bool ValidateDestinationDoor(DoorVariant dv)
	{
		if (!(dv is IScp106PassableDoor { IsScp106Passable: not false }))
		{
			return false;
		}
		if (!dv.Rooms.Contains(this._syncRoom))
		{
			return false;
		}
		return true;
	}

	private bool ValidateDestinationConnection(RoomCullingConnection connection)
	{
		RoomCullingConnection.RoomLink link = connection.Link;
		if (link.Valid)
		{
			if (!(this._syncRoom == link.RoomA))
			{
				return this._syncRoom == link.RoomB;
			}
			return true;
		}
		return false;
	}

	private Vector3 ClosestDoorPosition(Vector3 doorPos)
	{
		Vector3 vector = this._syncPos - doorPos;
		Vector3 dir = new Vector3(vector.x, 0f, vector.z);
		float num = dir.magnitude;
		if (num > 0f)
		{
			dir /= num;
		}
		float radius = base.CastRole.FpcModule.CharController.radius;
		float height = base.CastRole.FpcModule.CharController.height;
		Vector3 origin = doorPos + Vector3.up * (0.2f + radius);
		Color debugColor = ((Scp106HuntersAtlasAbility.DebugDuration > 0f) ? Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.4f, 0.8f) : Color.clear);
		do
		{
			if (this.TrySphereCast(debugColor, origin, dir, radius, height, num, out var pos))
			{
				return pos;
			}
			num = Mathf.Min(15f, num - radius);
		}
		while (!(num < radius));
		return doorPos + Vector3.up * height;
	}

	private bool TrySphereCast(Color debugColor, Vector3 origin, Vector3 dir, float radius, float height, float maxDis, out Vector3 pos)
	{
		LayerMask valueOrDefault = Scp106HuntersAtlasAbility._absoluteCollisionMask.GetValueOrDefault();
		if (!Scp106HuntersAtlasAbility._absoluteCollisionMask.HasValue)
		{
			valueOrDefault = (int)FpcStateProcessor.Mask & ~Scp106MovementModule.PassableDetectionMask;
			Scp106HuntersAtlasAbility._absoluteCollisionMask = valueOrDefault;
		}
		LayerMask value = Scp106HuntersAtlasAbility._absoluteCollisionMask.Value;
		Debug.DrawRay(origin, dir, debugColor, Scp106HuntersAtlasAbility.DebugDuration);
		if (Physics.SphereCast(origin, radius, dir, out var hitInfo, maxDis + radius, value))
		{
			hitInfo.point += 1.1f * radius * hitInfo.normal;
		}
		else
		{
			hitInfo.point = origin + dir * maxDis;
		}
		pos = hitInfo.point;
		if (Scp106HuntersAtlasAbility.DebugDuration > 0f)
		{
			this.DebugHitPoint(hitInfo.point, debugColor);
		}
		if (!Physics.Raycast(pos + Vector3.up * 0.2f, Vector3.down, out var hitInfo2, 2f, value))
		{
			return false;
		}
		int num = Physics.OverlapSphereNonAlloc(hitInfo2.point, radius * 2f, Scp106HuntersAtlasAbility.DetectionsNonAlloc);
		for (int i = 0; i < num; i++)
		{
			if (Scp106HuntersAtlasAbility.DetectionsNonAlloc[i].TryGetComponent<TeslaGate>(out var _))
			{
				return false;
			}
		}
		pos = hitInfo2.point + Vector3.up * (0.2f + radius);
		if (Physics.CheckCapsule(pos, pos + Vector3.up * (height - radius - 0.2f), radius, value))
		{
			return false;
		}
		if (!this.TryClearPath(pos, dir, maxDis))
		{
			return false;
		}
		pos = hitInfo2.point + Vector3.up * height;
		return true;
	}

	private bool TryClearPath(Vector3 start, Vector3 dir, float dis)
	{
		int num = Physics.RaycastNonAlloc(new Ray(start, dir), Scp106HuntersAtlasAbility.HitsNonAlloc, dis, Scp106MovementModule.PassableDetectionMask);
		for (int i = 0; i < num; i++)
		{
			Scp106MovementModule.GetSlowdownFromCollider(Scp106HuntersAtlasAbility.HitsNonAlloc[i].collider, out var isPassable);
			if (!isPassable)
			{
				return false;
			}
		}
		return true;
	}

	private void DebugHitPoint(Vector3 point, Color debugColor)
	{
		Vector3[] array = new Vector3[6]
		{
			Vector3.up,
			Vector3.down,
			Vector3.left,
			Vector3.right,
			Vector3.forward,
			Vector3.back
		};
		foreach (Vector3 vector in array)
		{
			Debug.DrawLine(point, point + vector * 0.1f, debugColor, Scp106HuntersAtlasAbility.DebugDuration);
		}
	}

	protected override void Update()
	{
		base.Update();
		this.UpdateAny();
		if (base.Owner.isLocalPlayer)
		{
			this.UpdateClientside();
		}
		if (NetworkServer.active)
		{
			this.UpdateServerside();
		}
	}

	public override void ClientWriteCmd(NetworkWriter writer)
	{
		base.ClientWriteCmd(writer);
		Vector3 position = this._syncRoom.transform.position;
		writer.WriteRelativePosition(new RelativePosition(position));
		Vector3Int vector3Int = Vector3Int.RoundToInt((this._syncPos - position) * 50f);
		writer.WriteShort((short)vector3Int.x);
		writer.WriteShort((short)vector3Int.z);
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (base.CastRole.Sinkhole.SubmergeProgress > 0f || !base.CastRole.Sinkhole.ReadonlyCooldown.IsReady)
		{
			return;
		}
		Vector3 position = reader.ReadRelativePosition().Position;
		if (!position.TryGetRoom(out this._syncRoom))
		{
			return;
		}
		Vector3 vector = new Vector3(reader.ReadShort(), 0f, reader.ReadShort());
		this._syncPos = position + vector / 50f;
		Vector3 position2 = base.CastRole.FpcModule.Position;
		if (Mathf.Abs(position2.y - this._syncPos.y) > 100f)
		{
			return;
		}
		float num = (position2 - this._syncPos).MagnitudeIgnoreY() * 0.019f;
		if (!(num > base.VigorAmount))
		{
			Scp106UsingHunterAtlasEventArgs e = new Scp106UsingHunterAtlasEventArgs(base.Owner, this._syncPos);
			Scp106Events.OnUsingHunterAtlas(e);
			if (e.IsAllowed)
			{
				this._syncPos = e.DestinationPosition;
				this._estimatedCost = num;
				this._syncSubmerged = true;
				base.ServerSendRpc(toAll: true);
			}
		}
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteBool(this._syncSubmerged);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		this._syncSubmerged = reader.ReadBool();
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this._syncSubmerged = false;
		if (this._lastDissolveAmount > 0f)
		{
			this._lastDissolveAmount = 0f;
			Scp106Hud.SetDissolveAnimation(0f);
		}
	}
}
