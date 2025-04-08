using System;
using GameObjectPools;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.Scp106Events;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using PlayerRoles.Spectating;
using ProgressiveCulling;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp106
{
	public class Scp106HuntersAtlasAbility : Scp106VigorAbilityBase, IPoolResettable
	{
		private static int DetectionMask
		{
			get
			{
				if (!Scp106HuntersAtlasAbility._maskSet)
				{
					Scp106HuntersAtlasAbility._mask = LayerMask.GetMask(new string[] { "Default" });
					Scp106HuntersAtlasAbility._maskSet = true;
				}
				return Scp106HuntersAtlasAbility._mask;
			}
		}

		protected override ActionName TargetKey
		{
			get
			{
				return ActionName.Inventory;
			}
		}

		public override bool ServerWantsSubmerged
		{
			get
			{
				return this._syncSubmerged;
			}
		}

		public override float SubmergeTime
		{
			get
			{
				return 2f;
			}
		}

		public override float EmergeTime
		{
			get
			{
				return 1f;
			}
		}

		protected override bool KeyPressable
		{
			get
			{
				return base.Owner.isLocalPlayer;
			}
		}

		private void UpdateAny()
		{
			if (!base.Owner.isLocalPlayer && !base.Owner.IsLocallySpectated())
			{
				return;
			}
			float submergeProgress = base.CastRole.Sinkhole.SubmergeProgress;
			this._lastDissolveAmount = (this._syncSubmerged ? Mathf.InverseLerp(0.5f, 1f, submergeProgress) : 0f);
			Scp106Hud.SetDissolveAnimation(this._lastDissolveAmount);
		}

		private void UpdateClientside()
		{
			Scp106Minimap singleton = Scp106Minimap.Singleton;
			if (singleton == null)
			{
				return;
			}
			if (base.CastRole.Sinkhole.SubmergeProgress != 0f || !this.IsKeyHeld || !base.CastRole.FpcModule.IsGrounded)
			{
				singleton.IsVisible = false;
				return;
			}
			if (!base.CastRole.Sinkhole.ReadonlyCooldown.IsReady)
			{
				singleton.IsVisible = false;
				Scp106Hud.PlayFlash(false);
				return;
			}
			if (base.VigorAmount < 0.25f)
			{
				singleton.IsVisible = false;
				Scp106Hud.PlayFlash(true);
				return;
			}
			singleton.IsVisible = true;
			if (!Scp106MinimapElement.AnyHighlighted)
			{
				return;
			}
			if (!Input.GetKey(NewInput.GetKey(ActionName.Shoot, KeyCode.None)))
			{
				return;
			}
			this._syncPos = singleton.LastWorldPos;
			this._syncRoom = Scp106MinimapElement.LastHighlighted.Room;
			base.ClientSendCmd();
		}

		private void UpdateServerside()
		{
			if (!this._syncSubmerged || base.CastRole.Sinkhole.SubmergeProgress < 1f)
			{
				return;
			}
			Vector3 safePosition = this.GetSafePosition();
			Vector3 position = base.CastRole.FpcModule.Position;
			float num = (safePosition - position).MagnitudeIgnoreY();
			base.VigorAmount -= Mathf.Min(this._estimatedCost, num * 0.019f);
			base.CastRole.FpcModule.ServerOverridePosition(safePosition);
			this._syncSubmerged = false;
			Scp106Events.OnUsedHunterAtlas(new Scp106UsedHunterAtlasEventArgs(base.Owner, position));
			base.ServerSendRpc(true);
		}

		private Vector3 GetSafePosition()
		{
			Vector3 vector = base.CastRole.FpcModule.Position;
			float num = float.MaxValue;
			foreach (Pose pose in SafeLocationFinder.GetLocations(new Predicate<RoomCullingConnection>(this.ValidateDestinationConnection), new Predicate<DoorVariant>(this.ValidateDestinationDoor)))
			{
				if (Mathf.Abs(pose.position.y - this._syncPos.y) <= 50f)
				{
					Vector3 vector2 = this.ClosestDoorPosition(pose.position);
					float num2 = (vector2 - this._syncPos).SqrMagnitudeIgnoreY();
					if (num2 <= num)
					{
						num = num2;
						vector = vector2;
					}
				}
			}
			return vector;
		}

		private bool ValidateDestinationDoor(DoorVariant dv)
		{
			IScp106PassableDoor scp106PassableDoor = dv as IScp106PassableDoor;
			return scp106PassableDoor != null && scp106PassableDoor.IsScp106Passable && dv.Rooms.Contains(this._syncRoom);
		}

		private bool ValidateDestinationConnection(RoomCullingConnection connection)
		{
			RoomCullingConnection.RoomLink link = connection.Link;
			return link.Valid && (this._syncRoom == link.RoomA || this._syncRoom == link.RoomB);
		}

		private Vector3 ClosestDoorPosition(Vector3 doorPos)
		{
			Vector3 vector = this._syncPos - doorPos;
			Vector3 vector2 = new Vector3(vector.x, 0f, vector.z);
			float num = vector2.magnitude;
			if (num > 0f)
			{
				vector2 /= num;
			}
			float radius = base.CastRole.FpcModule.CharController.radius;
			float height = base.CastRole.FpcModule.CharController.height;
			Vector3 vector3 = doorPos + Vector3.up * (0.2f + radius);
			Color color = ((Scp106HuntersAtlasAbility.DebugDuration > 0f) ? global::UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.4f, 0.8f) : Color.clear);
			Vector3 vector4;
			while (!this.TrySphereCast(color, vector3, vector2, radius, height, num, out vector4))
			{
				num = Mathf.Min(15f, num - radius);
				if (num < radius)
				{
					return doorPos + Vector3.up * height;
				}
			}
			return vector4;
		}

		private bool TrySphereCast(Color debugColor, Vector3 origin, Vector3 dir, float radius, float height, float maxDis, out Vector3 pos)
		{
			Debug.DrawRay(origin, dir, debugColor, Scp106HuntersAtlasAbility.DebugDuration);
			RaycastHit raycastHit;
			if (Physics.SphereCast(origin, radius, dir, out raycastHit, maxDis + radius, Scp106HuntersAtlasAbility.DetectionMask))
			{
				raycastHit.point += 1.1f * radius * raycastHit.normal;
			}
			else
			{
				raycastHit.point = origin + dir * maxDis;
			}
			pos = raycastHit.point;
			if (Scp106HuntersAtlasAbility.DebugDuration > 0f)
			{
				this.DebugHitPoint(raycastHit.point, debugColor);
			}
			RaycastHit raycastHit2;
			if (!Physics.Raycast(pos + Vector3.up * 0.2f, Vector3.down, out raycastHit2, 2f, Scp106HuntersAtlasAbility.DetectionMask))
			{
				return false;
			}
			int num = Physics.OverlapSphereNonAlloc(raycastHit2.point, radius * 2f, Scp106HuntersAtlasAbility.DetectionsNonAlloc);
			for (int i = 0; i < num; i++)
			{
				TeslaGate teslaGate;
				if (Scp106HuntersAtlasAbility.DetectionsNonAlloc[i].TryGetComponent<TeslaGate>(out teslaGate))
				{
					return false;
				}
			}
			pos = raycastHit2.point + Vector3.up * (0.2f + radius);
			if (Physics.CheckCapsule(pos, pos + Vector3.up * (height - radius - 0.2f), radius, Scp106HuntersAtlasAbility.DetectionMask))
			{
				return false;
			}
			pos = raycastHit2.point + Vector3.up * height;
			return true;
		}

		private void DebugHitPoint(Vector3 point, Color debugColor)
		{
			foreach (Vector3 vector in new Vector3[]
			{
				Vector3.up,
				Vector3.down,
				Vector3.left,
				Vector3.right,
				Vector3.forward,
				Vector3.back
			})
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
			if (base.CastRole.Sinkhole.SubmergeProgress > 0f)
			{
				return;
			}
			if (!base.CastRole.Sinkhole.ReadonlyCooldown.IsReady)
			{
				return;
			}
			Vector3 position = reader.ReadRelativePosition().Position;
			this._syncRoom = RoomUtils.RoomAtPosition(position);
			Vector3 vector = new Vector3((float)reader.ReadShort(), 0f, (float)reader.ReadShort());
			this._syncPos = position + vector / 50f;
			if (this._syncRoom == null)
			{
				return;
			}
			Vector3 position2 = base.CastRole.FpcModule.Position;
			if (Mathf.Abs(position2.y - this._syncPos.y) > 400f)
			{
				return;
			}
			float num = (position2 - this._syncPos).MagnitudeIgnoreY() * 0.019f;
			if (num > base.VigorAmount)
			{
				return;
			}
			Scp106UsingHunterAtlasEventArgs scp106UsingHunterAtlasEventArgs = new Scp106UsingHunterAtlasEventArgs(base.Owner, this._syncPos);
			Scp106Events.OnUsingHunterAtlas(scp106UsingHunterAtlasEventArgs);
			if (!scp106UsingHunterAtlasEventArgs.IsAllowed)
			{
				return;
			}
			this._syncPos = scp106UsingHunterAtlasEventArgs.DestinationPosition;
			this._estimatedCost = num;
			this._syncSubmerged = true;
			base.ServerSendRpc(true);
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

		public const float CostPerMeter = 0.019f;

		private const ActionName SelectKey = ActionName.Shoot;

		private const int SyncAccuracy = 50;

		private const float HeightOffset = 0.2f;

		private const float NormalMultiplier = 1.1f;

		private const float GroundDetectorHeight = 2f;

		private const float DissolvePercent = 0.5f;

		private const float MaxRetakeRange = 15f;

		private const float HeightTolerance = 400f;

		private const float DoorHeightTolerance = 50f;

		private const int OverlapSphereMaxDetections = 8;

		private const float MinVigor = 0.25f;

		private Vector3 _syncPos;

		private RoomIdentifier _syncRoom;

		private bool _syncSubmerged;

		private float _lastDissolveAmount;

		private float _estimatedCost;

		private static readonly Collider[] DetectionsNonAlloc = new Collider[8];

		private static readonly float DebugDuration = 0f;

		private static bool _maskSet;

		private static int _mask;
	}
}
