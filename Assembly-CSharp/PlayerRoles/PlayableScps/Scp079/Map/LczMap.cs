using System;
using MapGeneration;
using TMPro;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Map
{
	public class LczMap : ProceduralZoneMap
	{
		protected override void PlaceRooms()
		{
			base.PlaceRooms();
		}

		protected override void PostProcessRooms()
		{
			base.PostProcessRooms();
			Vector2 vector = this._hczMap.RectBounds.center - base.RectBounds.center;
			vector += Vector2.up * (this._spacing.y + this._hczMap.RectBounds.extents.y + base.RectBounds.extents.y);
			vector += Vector2.right * (base.RectBounds.extents.x + this._spacing.x - this._hczMap.RectBounds.extents.x);
			foreach (ProceduralZoneMap.RoomNode roomNode in this.AllNodes)
			{
				this.ProcessName(roomNode);
				roomNode.Transform.anchoredPosition += vector;
			}
			this.ZoneLabel.rectTransform.anchoredPosition += vector;
			base.RectBounds = new Bounds(base.RectBounds.center + vector, base.RectBounds.size);
		}

		private void ProcessName(ProceduralZoneMap.RoomNode node)
		{
			if (node.Room.Name != RoomName.Unnamed)
			{
				return;
			}
			SpawnableRoom spawnableRoom;
			if (!node.Room.TryGetComponent<SpawnableRoom>(out spawnableRoom))
			{
				return;
			}
			TextMeshProUGUI componentInChildren = node.Icon.GetComponentInChildren<TextMeshProUGUI>();
			componentInChildren.text = string.Format(componentInChildren.text, (1 + spawnableRoom.DuplicateId).ToString("00"));
			componentInChildren.alpha = 0.2f;
			componentInChildren.fontSize *= 0.7f;
		}

		private const float UnnamedAlpha = 0.2f;

		private const float UnnamedSize = 0.7f;

		private const int RoomIdOffset = 1;

		[SerializeField]
		private ProceduralZoneMap _hczMap;

		[SerializeField]
		private ProceduralZoneMap _ezMap;

		[SerializeField]
		private Vector2 _spacing;
	}
}
