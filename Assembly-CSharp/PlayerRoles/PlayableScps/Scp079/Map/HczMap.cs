using System;
using System.Collections.Generic;
using MapGeneration;
using MapGeneration.Distributors;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using UnityEngine;
using UnityEngine.UI;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp079.Map
{
	public class HczMap : ProceduralZoneMap
	{
		protected override void PlaceRooms()
		{
			base.PlaceRooms();
			RoomIdentifier roomIdentifier;
			if (!RoomIdentifier.AllRoomIdentifiers.TryGetFirst((RoomIdentifier x) => x.Name == RoomName.HczCheckpointToEntranceZone && x.Zone == FacilityZone.HeavyContainment, out roomIdentifier))
			{
				return;
			}
			ProceduralZoneMap.RoomNode roomNode;
			if (!this.NodesByRoom.TryGetValue(roomIdentifier, out roomNode))
			{
				return;
			}
			float z = roomNode.Transform.localEulerAngles.z;
			this.Rotate(this.AllNodes, z);
			this.Rotate(this._entranceMap.AllNodes, z);
		}

		public override void UpdateOpened(Scp079Camera curCam)
		{
			base.UpdateOpened(curCam);
			float num = Mathf.Sin(Time.timeSinceLevelLoad * 3.1415927f);
			foreach (Scp079Generator scp079Generator in Scp079Recontainer.AllGenerators)
			{
				if (scp079Generator.Activating)
				{
					RoomIdentifier roomIdentifier = RoomUtils.RoomAtPosition(scp079Generator.transform.position);
					ProceduralZoneMap.RoomNode roomNode;
					if (this.NodesByRoom.TryGetValue(roomIdentifier, out roomNode))
					{
						Image icon = roomNode.Icon;
						icon.color = Color.Lerp(icon.color, HczMap.GeneratorColor, Mathf.Abs(num));
					}
				}
			}
		}

		private void Rotate(List<ProceduralZoneMap.RoomNode> nodesToRotate, float angleDeg)
		{
			Vector3 vector = Vector3.forward * (180f - angleDeg);
			foreach (ProceduralZoneMap.RoomNode roomNode in nodesToRotate)
			{
				RectTransform transform = roomNode.Transform;
				transform.localPosition = Quaternion.Euler(vector) * transform.localPosition;
				transform.Rotate(vector, Space.Self);
			}
		}

		private static readonly Color GeneratorColor = new Color(1f, 0.1f, 0f, 0.15f);

		private const RoomName RotateRoom = RoomName.HczCheckpointToEntranceZone;

		private const float AngleOffset = 180f;

		[SerializeField]
		private ProceduralZoneMap _entranceMap;
	}
}
