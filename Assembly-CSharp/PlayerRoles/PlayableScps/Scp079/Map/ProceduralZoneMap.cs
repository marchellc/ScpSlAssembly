using System;
using System.Collections.Generic;
using Interactables.Interobjects;
using MapGeneration;
using NorthwoodLib.Pools;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PlayerRoles.PlayableScps.Scp079.GUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp079.Map
{
	public class ProceduralZoneMap : MonoBehaviour, IZoneMap
	{
		public static Color OtherZoneColor
		{
			get
			{
				return Color.Lerp(new Color(0.5f, 0.5f, 0.5f, 0.042f), ProceduralZoneMap.CurrentZoneColor, Scp079ScannerGui.AnimInterpolant);
			}
		}

		public static Color CurrentRoomColor
		{
			get
			{
				return Color.Lerp(new Color(0f, 1f, 0.4f, 0.15f), ProceduralZoneMap.CurrentZoneColor, Scp079ScannerGui.AnimInterpolant);
			}
		}

		public static Color HighlightedColor
		{
			get
			{
				return Color.Lerp(new Color(1f, 1f, 1f, 0.27f), ProceduralZoneMap.CurrentZoneColor, Scp079ScannerGui.AnimInterpolant);
			}
		}

		public bool Ready { get; private set; }

		public Bounds RectBounds { get; protected set; }

		public FacilityZone Zone { get; protected set; }

		private void Update()
		{
			ProceduralZoneMap proceduralZoneMap;
			if (!this._queuedPostProcessing.TryDequeue(out proceduralZoneMap))
			{
				return;
			}
			proceduralZoneMap.PostProcessRooms();
			proceduralZoneMap.Ready = true;
		}

		private bool TryGetIcon(RoomIdentifier room, out ProceduralZoneMap.IconDefinition result)
		{
			foreach (ProceduralZoneMap.IconDefinition iconDefinition in this._roomIcons)
			{
				if (iconDefinition.Room == room.Name && iconDefinition.Shape == room.Shape)
				{
					result = iconDefinition;
					return true;
				}
			}
			result = default(ProceduralZoneMap.IconDefinition);
			return false;
		}

		private bool TryGetCamOfRoom(Scp079Camera curCam, RoomIdentifier room, out Scp079Camera result)
		{
			return Scp079Camera.TryGetMainCamera(room, out result) && result != curCam;
		}

		private void UpdateNode(Scp079Camera curCam, ProceduralZoneMap.RoomNode node)
		{
			bool flag = curCam.Room.Zone == this.Zone;
			Scp079Camera cameraOverride = node.CameraOverride;
			List<ProceduralZoneMap.RoomNode> subNodes = node.SubNodes;
			if (subNodes != null)
			{
				subNodes.ForEach(delegate(ProceduralZoneMap.RoomNode subNode)
				{
					this.UpdateNode(curCam, subNode);
				});
			}
			if (node.Highlighted && (cameraOverride != null || this.TryGetCamOfRoom(curCam, node.Room, out cameraOverride)))
			{
				if (cameraOverride != curCam)
				{
					this._highlightedCamera = cameraOverride;
				}
				node.Icon.color = ProceduralZoneMap.HighlightedColor;
				return;
			}
			node.Icon.color = (flag ? ProceduralZoneMap.CurrentZoneColor : ProceduralZoneMap.OtherZoneColor);
		}

		protected virtual void PlaceRooms()
		{
			foreach (RoomIdentifier roomIdentifier in RoomIdentifier.AllRoomIdentifiers)
			{
				if (roomIdentifier.Zone == this.Zone)
				{
					ProceduralZoneMap.RoomNode roomNode = new ProceduralZoneMap.RoomNode(roomIdentifier, this);
					this.NodesByRoom.Add(roomIdentifier, roomNode);
					this.AllNodes.Add(roomNode);
				}
			}
			foreach (ProceduralZoneMap.RoomNode roomNode2 in this.NodesByRoom.Values)
			{
				roomNode2.Transform.localPosition *= this._positionScale;
			}
		}

		protected virtual void PostProcessRooms()
		{
			foreach (TextMeshProUGUI textMeshProUGUI in this._spawnedTexts)
			{
				if (!string.IsNullOrEmpty(textMeshProUGUI.text))
				{
					textMeshProUGUI.GetComponentInChildren<LayoutGroup>(true).transform.localPosition = Vector3.down * textMeshProUGUI.rectTransform.sizeDelta.y / 2f;
				}
			}
			foreach (RectTransform rectTransform in this._transformsToUpright)
			{
				rectTransform.rotation = this._rectParent.rotation;
			}
			global::UnityEngine.Object.Destroy(this._iconTemplate.gameObject);
			global::UnityEngine.Object.Destroy(this._subNodeTemplate.gameObject);
			Bounds bounds = default(Bounds);
			bool flag = true;
			foreach (ProceduralZoneMap.RoomNode roomNode in this.AllNodes)
			{
				RectTransform transform = roomNode.Transform;
				Bounds bounds2 = new Bounds(transform.anchoredPosition, transform.sizeDelta);
				if (flag)
				{
					bounds = bounds2;
				}
				else
				{
					bounds.Encapsulate(bounds2);
				}
				flag = false;
			}
			this.RectBounds = bounds;
			RectTransform rectTransform2 = this.ZoneLabel.rectTransform;
			rectTransform2.anchoredPosition = bounds.center + Vector2.up * (bounds.extents.y + rectTransform2.sizeDelta.y);
			this.ZoneLabel.text = Translations.Get<Scp079HudTranslation>(ProceduralZoneMap.ZoneTranslations[this.Zone]);
		}

		public void Generate()
		{
			this.PlaceRooms();
			this._queuedPostProcessing.Enqueue(this);
		}

		public virtual void UpdateOpened(Scp079Camera curCam)
		{
			this._highlightedCamera = null;
			foreach (ProceduralZoneMap.RoomNode roomNode in this.AllNodes)
			{
				this.UpdateNode(curCam, roomNode);
			}
			ProceduralZoneMap.RoomNode roomNode2;
			if (!this.NodesByRoom.TryGetValue(curCam.Room, out roomNode2))
			{
				return;
			}
			roomNode2.Icon.color = ProceduralZoneMap.CurrentRoomColor;
			List<ProceduralZoneMap.RoomNode> subNodes = roomNode2.SubNodes;
			if (subNodes == null)
			{
				return;
			}
			subNodes.ForEach(delegate(ProceduralZoneMap.RoomNode x)
			{
				x.Icon.color = ProceduralZoneMap.CurrentRoomColor;
			});
		}

		public bool TryGetCamera(out Scp079Camera target)
		{
			target = this._highlightedCamera as Scp079Camera;
			return target != null;
		}

		public bool TryGetCenterTransform(Scp079Camera curCam, out Vector3 center)
		{
			ProceduralZoneMap.RoomNode roomNode;
			if (this.NodesByRoom.TryGetValue(curCam.Room, out roomNode))
			{
				center = -roomNode.Transform.anchoredPosition;
				return true;
			}
			center = Vector3.zero;
			return false;
		}

		public bool TrySetPlayerIndicator(ReferenceHub ply, RectTransform indicator, bool exact)
		{
			IFpcRole fpcRole = ply.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return false;
			}
			Vector3 position = fpcRole.FpcModule.Position;
			RoomIdentifier roomIdentifier = RoomUtils.RoomAtPositionRaycasts(position, true);
			ProceduralZoneMap.RoomNode roomNode;
			if (roomIdentifier == null || !this.NodesByRoom.TryGetValue(roomIdentifier, out roomNode))
			{
				return false;
			}
			if (exact)
			{
				RectTransform transform = roomNode.Transform;
				Transform transform2 = roomIdentifier.transform;
				Vector3 vector = transform2.InverseTransformPoint(position);
				Vector2 vector2 = new Vector3(vector.x, vector.z) * this._positionScale;
				ProceduralZoneMap.IconDefinition iconDefinition;
				Bounds bounds = (this.TryGetIcon(roomIdentifier, out iconDefinition) ? iconDefinition.IndicatorLimits : new Bounds(Vector3.zero, transform.sizeDelta));
				indicator.SetParent(transform);
				vector2.x = Mathf.Clamp(vector2.x, bounds.min.x, bounds.max.x);
				vector2.y = Mathf.Clamp(vector2.y, bounds.min.y, bounds.max.y);
				indicator.localPosition = vector2;
				indicator.localScale = Vector3.one;
				indicator.rotation = this._rectParent.rotation;
				indicator.Rotate(Vector3.back * (fpcRole.FpcModule.MouseLook.CurrentHorizontal - transform2.eulerAngles.y - transform.localEulerAngles.z), Space.Self);
			}
			else
			{
				LayoutGroup componentInChildren = roomNode.Icon.GetComponentInChildren<LayoutGroup>();
				indicator.SetParent(componentInChildren.transform);
				indicator.localScale = Vector3.one;
				indicator.localPosition = Vector3.one;
				indicator.localEulerAngles = Vector3.zero;
			}
			return true;
		}

		// Note: this type is marked as 'beforefieldinit'.
		static ProceduralZoneMap()
		{
			Dictionary<FacilityZone, Scp079HudTranslation> dictionary = new Dictionary<FacilityZone, Scp079HudTranslation>();
			dictionary[FacilityZone.LightContainment] = Scp079HudTranslation.LightContZone;
			dictionary[FacilityZone.HeavyContainment] = Scp079HudTranslation.HeavyContZone;
			dictionary[FacilityZone.Entrance] = Scp079HudTranslation.EntranceZone;
			dictionary[FacilityZone.Surface] = Scp079HudTranslation.SurfaceZone;
			ProceduralZoneMap.ZoneTranslations = dictionary;
			ProceduralZoneMap.CurrentZoneColor = new Color(1f, 1f, 1f, 0.055f);
		}

		public static readonly Dictionary<FacilityZone, Scp079HudTranslation> ZoneTranslations;

		public static readonly Color CurrentZoneColor;

		public readonly Dictionary<RoomIdentifier, ProceduralZoneMap.RoomNode> NodesByRoom = new Dictionary<RoomIdentifier, ProceduralZoneMap.RoomNode>();

		public readonly List<ProceduralZoneMap.RoomNode> AllNodes = new List<ProceduralZoneMap.RoomNode>();

		[SerializeField]
		protected TextMeshProUGUI ZoneLabel;

		[SerializeField]
		private Image _iconTemplate;

		[SerializeField]
		private Image _subNodeTemplate;

		[SerializeField]
		private Sprite _iconElevatorUp;

		[SerializeField]
		private Sprite _iconElevatorDown;

		[SerializeField]
		private ProceduralZoneMap.IconDefinition[] _roomIcons;

		[SerializeField]
		private float _positionScale;

		[SerializeField]
		private RectTransform _rectParent;

		private Scp079InteractableBase _highlightedCamera;

		private readonly HashSet<TextMeshProUGUI> _spawnedTexts = new HashSet<TextMeshProUGUI>();

		private readonly List<RectTransform> _transformsToUpright = new List<RectTransform>();

		private readonly Queue<ProceduralZoneMap> _queuedPostProcessing = new Queue<ProceduralZoneMap>();

		public class RoomNode
		{
			public bool Highlighted
			{
				get
				{
					if (this.SubNodes != null)
					{
						if (this.SubNodes.Any((ProceduralZoneMap.RoomNode x) => x.Highlighted))
						{
							return false;
						}
					}
					Vector2 anchoredPosition = this._map._rectParent.anchoredPosition;
					Vector2 vector;
					if (this._parentNode == null)
					{
						vector = this.Transform.anchoredPosition;
					}
					else
					{
						Vector2 vector2 = this.Transform.localPosition;
						float z = this._parentNode.Transform.localEulerAngles.z;
						Vector2 vector3 = vector2.RotateAroundZ(z);
						vector = this._parentNode.Transform.anchoredPosition + vector3;
					}
					Vector2 vector4 = anchoredPosition + vector;
					return Mathf.Abs(vector4.x) < this._halfBounds.x && Mathf.Abs(vector4.y) < this._halfBounds.y;
				}
			}

			public RoomNode(RoomIdentifier room, ProceduralZoneMap map)
			{
				this.Room = room;
				this.Icon = global::UnityEngine.Object.Instantiate<Image>(map._iconTemplate, map._iconTemplate.rectTransform.parent);
				this.Label = this.Icon.GetComponentInChildren<TextMeshProUGUI>();
				this.Transform = this.Icon.rectTransform;
				this._halfBounds = this.Transform.sizeDelta / 2f;
				this._map = map;
				ProceduralZoneMap.IconDefinition iconDefinition;
				if (map.TryGetIcon(room, out iconDefinition))
				{
					this.Label.text = iconDefinition.Name;
					map._spawnedTexts.Add(this.Label);
					map._transformsToUpright.Add(this.Label.rectTransform);
					this.Transform.localPosition = iconDefinition.TextOffset;
					this.Transform.localEulerAngles = Vector3.back * room.transform.eulerAngles.y;
				}
				Vector3 position = room.transform.position;
				this.Transform.localPosition = new Vector3(position.x, position.z, 0f);
				this.Icon.sprite = room.Icon;
				this.SubNodes = this.GenerateElevatorFastTravelSubnodes();
			}

			private RoomNode(ProceduralZoneMap.RoomNode parent, ProceduralZoneMap map, Scp079Camera targetCam, Sprite icon, int index)
			{
				this.Icon = global::UnityEngine.Object.Instantiate<Image>(map._subNodeTemplate, parent.Transform);
				this.Transform = this.Icon.rectTransform;
				this.Icon.sprite = icon;
				this.CameraOverride = targetCam;
				this.SubNodes = null;
				this._halfBounds = this.Transform.sizeDelta / 2f;
				this._parentNode = parent;
				this._map = map;
				ProceduralZoneMap.IconDefinition iconDefinition;
				if (!map.TryGetIcon(parent.Room, out iconDefinition))
				{
					return;
				}
				map._transformsToUpright.Add(this.Transform);
				this.Transform.localPosition = iconDefinition.SubNodeFirstOffset + iconDefinition.SubNodeNextOffset * (float)index;
			}

			private List<ProceduralZoneMap.RoomNode> GenerateElevatorFastTravelSubnodes()
			{
				List<ElevatorDoor> list = ListPool<ElevatorDoor>.Shared.Rent();
				ElevatorGroup[] values = EnumUtils<ElevatorGroup>.Values;
				for (int i = 0; i < values.Length; i++)
				{
					foreach (ElevatorDoor elevatorDoor in ElevatorDoor.GetDoorsForGroup(values[i]))
					{
						if (elevatorDoor.Rooms.Contains(this.Room))
						{
							list.Add(elevatorDoor);
						}
					}
				}
				Scp079Camera scp079Camera;
				if (list.Count < 2 || !Scp079Camera.TryGetMainCamera(this.Room, out scp079Camera))
				{
					ListPool<ElevatorDoor>.Shared.Return(list);
					return null;
				}
				List<ProceduralZoneMap.RoomNode> list2 = null;
				using (List<ElevatorDoor>.Enumerator enumerator = list.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						Scp079Camera scp079Camera2;
						if (Scp079Camera.TryGetClosestCamera(enumerator.Current.transform.position, null, out scp079Camera2))
						{
							float num = scp079Camera2.Position.y - scp079Camera.Position.y;
							if (Mathf.Abs(num) >= 7f)
							{
								if (list2 == null)
								{
									list2 = new List<ProceduralZoneMap.RoomNode>();
								}
								Sprite sprite = ((num < 0f) ? this._map._iconElevatorDown : this._map._iconElevatorUp);
								list2.Add(new ProceduralZoneMap.RoomNode(this, this._map, scp079Camera2, sprite, list2.Count));
							}
						}
					}
				}
				ListPool<ElevatorDoor>.Shared.Return(list);
				return list2;
			}

			public readonly Image Icon;

			public readonly List<ProceduralZoneMap.RoomNode> SubNodes;

			public readonly RoomIdentifier Room;

			public readonly Scp079Camera CameraOverride;

			public readonly RectTransform Transform;

			public readonly TextMeshProUGUI Label;

			private readonly ProceduralZoneMap _map;

			private readonly Vector2 _halfBounds;

			private readonly ProceduralZoneMap.RoomNode _parentNode;
		}

		[Serializable]
		private struct IconDefinition
		{
			public RoomName Room;

			public RoomShape Shape;

			public string Name;

			public Vector3 TextOffset;

			public Bounds IndicatorLimits;

			public Vector2 SubNodeFirstOffset;

			public Vector2 SubNodeNextOffset;
		}
	}
}
