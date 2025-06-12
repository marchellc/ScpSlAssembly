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

namespace PlayerRoles.PlayableScps.Scp079.Map;

public class ProceduralZoneMap : MonoBehaviour, IZoneMap
{
	public class RoomNode
	{
		public readonly Image Icon;

		public readonly List<RoomNode> SubNodes;

		public readonly RoomIdentifier Room;

		public readonly Scp079Camera CameraOverride;

		public readonly RectTransform Transform;

		public readonly TextMeshProUGUI Label;

		private readonly ProceduralZoneMap _map;

		private readonly Vector2 _halfBounds;

		private readonly RoomNode _parentNode;

		public bool Highlighted
		{
			get
			{
				if (this.SubNodes != null && this.SubNodes.Any((RoomNode x) => x.Highlighted))
				{
					return false;
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
				if (Mathf.Abs(vector4.x) < this._halfBounds.x)
				{
					return Mathf.Abs(vector4.y) < this._halfBounds.y;
				}
				return false;
			}
		}

		public RoomNode(RoomIdentifier room, ProceduralZoneMap map)
		{
			this.Room = room;
			this.Icon = UnityEngine.Object.Instantiate(map._iconTemplate, map._iconTemplate.rectTransform.parent);
			this.Label = this.Icon.GetComponentInChildren<TextMeshProUGUI>();
			this.Transform = this.Icon.rectTransform;
			this._halfBounds = this.Transform.sizeDelta / 2f;
			this._map = map;
			if (map.TryGetIcon(room, out var result))
			{
				this.Label.text = result.Name;
				map._spawnedTexts.Add(this.Label);
				map._transformsToUpright.Add(this.Label.rectTransform);
				this.Transform.localPosition = result.TextOffset;
				this.Transform.localEulerAngles = Vector3.back * room.transform.eulerAngles.y;
			}
			Vector3 position = room.transform.position;
			this.Transform.localPosition = new Vector3(position.x, position.z, 0f);
			this.Icon.sprite = room.Icon;
			this.SubNodes = this.GenerateElevatorFastTravelSubnodes();
		}

		private RoomNode(RoomNode parent, ProceduralZoneMap map, Scp079Camera targetCam, Sprite icon, int index)
		{
			this.Icon = UnityEngine.Object.Instantiate(map._subNodeTemplate, parent.Transform);
			this.Transform = this.Icon.rectTransform;
			this.Icon.sprite = icon;
			this.CameraOverride = targetCam;
			this.SubNodes = null;
			this._halfBounds = this.Transform.sizeDelta / 2f;
			this._parentNode = parent;
			this._map = map;
			if (map.TryGetIcon(parent.Room, out var result))
			{
				map._transformsToUpright.Add(this.Transform);
				this.Transform.localPosition = result.SubNodeFirstOffset + result.SubNodeNextOffset * index;
			}
		}

		private List<RoomNode> GenerateElevatorFastTravelSubnodes()
		{
			List<ElevatorDoor> list = ListPool<ElevatorDoor>.Shared.Rent();
			ElevatorGroup[] values = EnumUtils<ElevatorGroup>.Values;
			for (int i = 0; i < values.Length; i++)
			{
				foreach (ElevatorDoor item in ElevatorDoor.GetDoorsForGroup(values[i]))
				{
					if (item.Rooms.Contains(this.Room))
					{
						list.Add(item);
					}
				}
			}
			if (list.Count < 2 || !Scp079Camera.TryGetMainCamera(this.Room, out var main))
			{
				ListPool<ElevatorDoor>.Shared.Return(list);
				return null;
			}
			List<RoomNode> list2 = null;
			foreach (ElevatorDoor item2 in list)
			{
				if (!Scp079Camera.TryGetClosestCamera(item2.transform.position, null, out var closest))
				{
					continue;
				}
				float num = closest.Position.y - main.Position.y;
				if (!(Mathf.Abs(num) < 7f))
				{
					if (list2 == null)
					{
						list2 = new List<RoomNode>();
					}
					Sprite icon = ((num < 0f) ? this._map._iconElevatorDown : this._map._iconElevatorUp);
					list2.Add(new RoomNode(this, this._map, closest, icon, list2.Count));
				}
			}
			ListPool<ElevatorDoor>.Shared.Return(list);
			return list2;
		}
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

	public static readonly Dictionary<FacilityZone, Scp079HudTranslation> ZoneTranslations = new Dictionary<FacilityZone, Scp079HudTranslation>
	{
		[FacilityZone.LightContainment] = Scp079HudTranslation.LightContZone,
		[FacilityZone.HeavyContainment] = Scp079HudTranslation.HeavyContZone,
		[FacilityZone.Entrance] = Scp079HudTranslation.EntranceZone,
		[FacilityZone.Surface] = Scp079HudTranslation.SurfaceZone
	};

	public static readonly Color CurrentZoneColor = new Color(1f, 1f, 1f, 0.055f);

	public readonly Dictionary<RoomIdentifier, RoomNode> NodesByRoom = new Dictionary<RoomIdentifier, RoomNode>();

	public readonly List<RoomNode> AllNodes = new List<RoomNode>();

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
	private IconDefinition[] _roomIcons;

	[SerializeField]
	private float _positionScale;

	[SerializeField]
	private RectTransform _rectParent;

	private Scp079InteractableBase _highlightedCamera;

	private readonly HashSet<TextMeshProUGUI> _spawnedTexts = new HashSet<TextMeshProUGUI>();

	private readonly List<RectTransform> _transformsToUpright = new List<RectTransform>();

	private readonly Queue<ProceduralZoneMap> _queuedPostProcessing = new Queue<ProceduralZoneMap>();

	public static Color OtherZoneColor => Color.Lerp(new Color(0.5f, 0.5f, 0.5f, 0.042f), ProceduralZoneMap.CurrentZoneColor, Scp079ScannerGui.AnimInterpolant);

	public static Color CurrentRoomColor => Color.Lerp(new Color(0f, 1f, 0.4f, 0.15f), ProceduralZoneMap.CurrentZoneColor, Scp079ScannerGui.AnimInterpolant);

	public static Color HighlightedColor => Color.Lerp(new Color(1f, 1f, 1f, 0.27f), ProceduralZoneMap.CurrentZoneColor, Scp079ScannerGui.AnimInterpolant);

	public bool Ready { get; private set; }

	public Bounds RectBounds { get; protected set; }

	[field: SerializeField]
	public FacilityZone Zone { get; protected set; }

	private void Update()
	{
		if (this._queuedPostProcessing.TryDequeue(out var result))
		{
			result.PostProcessRooms();
			result.Ready = true;
		}
	}

	private bool TryGetIcon(RoomIdentifier room, out IconDefinition result)
	{
		IconDefinition[] roomIcons = this._roomIcons;
		for (int i = 0; i < roomIcons.Length; i++)
		{
			IconDefinition iconDefinition = roomIcons[i];
			if (iconDefinition.Room == room.Name && iconDefinition.Shape == room.Shape)
			{
				result = iconDefinition;
				return true;
			}
		}
		result = default(IconDefinition);
		return false;
	}

	private bool TryGetCamOfRoom(Scp079Camera curCam, RoomIdentifier room, out Scp079Camera result)
	{
		if (Scp079Camera.TryGetMainCamera(room, out result))
		{
			return result != curCam;
		}
		return false;
	}

	private void UpdateNode(Scp079Camera curCam, RoomNode node)
	{
		bool flag = curCam.Room.Zone == this.Zone;
		Scp079Camera result = node.CameraOverride;
		node.SubNodes?.ForEach(delegate(RoomNode subNode)
		{
			this.UpdateNode(curCam, subNode);
		});
		if (node.Highlighted && (result != null || this.TryGetCamOfRoom(curCam, node.Room, out result)))
		{
			if (result != curCam)
			{
				this._highlightedCamera = result;
			}
			node.Icon.color = ProceduralZoneMap.HighlightedColor;
		}
		else
		{
			node.Icon.color = (flag ? ProceduralZoneMap.CurrentZoneColor : ProceduralZoneMap.OtherZoneColor);
		}
	}

	protected virtual void PlaceRooms()
	{
		foreach (RoomIdentifier allRoomIdentifier in RoomIdentifier.AllRoomIdentifiers)
		{
			if (allRoomIdentifier.Zone == this.Zone)
			{
				RoomNode roomNode = new RoomNode(allRoomIdentifier, this);
				this.NodesByRoom.Add(allRoomIdentifier, roomNode);
				this.AllNodes.Add(roomNode);
			}
		}
		foreach (RoomNode value in this.NodesByRoom.Values)
		{
			value.Transform.localPosition *= this._positionScale;
		}
	}

	protected virtual void PostProcessRooms()
	{
		foreach (TextMeshProUGUI spawnedText in this._spawnedTexts)
		{
			if (!string.IsNullOrEmpty(spawnedText.text))
			{
				spawnedText.GetComponentInChildren<LayoutGroup>(includeInactive: true).transform.localPosition = Vector3.down * spawnedText.rectTransform.sizeDelta.y / 2f;
			}
		}
		foreach (RectTransform item in this._transformsToUpright)
		{
			item.rotation = this._rectParent.rotation;
		}
		UnityEngine.Object.Destroy(this._iconTemplate.gameObject);
		UnityEngine.Object.Destroy(this._subNodeTemplate.gameObject);
		Bounds rectBounds = default(Bounds);
		bool flag = true;
		foreach (RoomNode allNode in this.AllNodes)
		{
			RectTransform rectTransform = allNode.Transform;
			Bounds bounds = new Bounds(rectTransform.anchoredPosition, rectTransform.sizeDelta);
			if (flag)
			{
				rectBounds = bounds;
			}
			else
			{
				rectBounds.Encapsulate(bounds);
			}
			flag = false;
		}
		this.RectBounds = rectBounds;
		RectTransform rectTransform2 = this.ZoneLabel.rectTransform;
		rectTransform2.anchoredPosition = (Vector2)rectBounds.center + Vector2.up * (rectBounds.extents.y + rectTransform2.sizeDelta.y);
		this.ZoneLabel.text = Translations.Get(ProceduralZoneMap.ZoneTranslations[this.Zone]);
	}

	public void Generate()
	{
		this.PlaceRooms();
		this._queuedPostProcessing.Enqueue(this);
	}

	public virtual void UpdateOpened(Scp079Camera curCam)
	{
		this._highlightedCamera = null;
		foreach (RoomNode allNode in this.AllNodes)
		{
			this.UpdateNode(curCam, allNode);
		}
		if (this.NodesByRoom.TryGetValue(curCam.Room, out var value))
		{
			value.Icon.color = ProceduralZoneMap.CurrentRoomColor;
			value.SubNodes?.ForEach(delegate(RoomNode x)
			{
				x.Icon.color = ProceduralZoneMap.CurrentRoomColor;
			});
		}
	}

	public bool TryGetCamera(out Scp079Camera target)
	{
		target = this._highlightedCamera as Scp079Camera;
		return target != null;
	}

	public bool TryGetCenterTransform(Scp079Camera curCam, out Vector3 center)
	{
		if (this.NodesByRoom.TryGetValue(curCam.Room, out var value))
		{
			center = -value.Transform.anchoredPosition;
			return true;
		}
		center = Vector3.zero;
		return false;
	}

	public bool TrySetPlayerIndicator(ReferenceHub ply, RectTransform indicator, bool exact)
	{
		if (!(ply.roleManager.CurrentRole is IFpcRole fpcRole))
		{
			return false;
		}
		Vector3 position = fpcRole.FpcModule.Position;
		if (!position.TryGetRoom(out var room))
		{
			return false;
		}
		if (!this.NodesByRoom.TryGetValue(room, out var value))
		{
			return false;
		}
		if (exact)
		{
			RectTransform rectTransform = value.Transform;
			Transform transform = room.transform;
			Vector3 vector = transform.InverseTransformPoint(position);
			Vector2 vector2 = new Vector3(vector.x, vector.z) * this._positionScale;
			IconDefinition result;
			Bounds bounds = (this.TryGetIcon(room, out result) ? result.IndicatorLimits : new Bounds(Vector3.zero, rectTransform.sizeDelta));
			indicator.SetParent(rectTransform);
			vector2.x = Mathf.Clamp(vector2.x, bounds.min.x, bounds.max.x);
			vector2.y = Mathf.Clamp(vector2.y, bounds.min.y, bounds.max.y);
			indicator.localPosition = vector2;
			indicator.localScale = Vector3.one;
			indicator.rotation = this._rectParent.rotation;
			indicator.Rotate(Vector3.back * (fpcRole.FpcModule.MouseLook.CurrentHorizontal - transform.eulerAngles.y - rectTransform.localEulerAngles.z), Space.Self);
		}
		else
		{
			LayoutGroup componentInChildren = value.Icon.GetComponentInChildren<LayoutGroup>();
			indicator.SetParent(componentInChildren.transform);
			indicator.localScale = Vector3.one;
			indicator.localPosition = Vector3.one;
			indicator.localEulerAngles = Vector3.zero;
		}
		return true;
	}
}
