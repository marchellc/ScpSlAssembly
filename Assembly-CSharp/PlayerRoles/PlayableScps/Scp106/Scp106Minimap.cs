using CursorManagement;
using Interactables.Interobjects;
using MapGeneration;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp106;

public class Scp106Minimap : MonoBehaviour, ICursorOverride
{
	[SerializeField]
	private Transform _rotorTransform;

	[SerializeField]
	private Transform _offsetTransform;

	[SerializeField]
	private Transform _rangeTransform;

	[SerializeField]
	private Scp106MinimapElement _template;

	[SerializeField]
	private float _gridScale;

	[SerializeField]
	private float _radiusScale;

	[SerializeField]
	private GameObject _errorSurface;

	[SerializeField]
	private GameObject _errorElevator;

	[SerializeField]
	private RectTransform _cursor;

	private bool _setUp;

	private float _lastElevatorRide;

	private int _poolSize;

	private int _usedRooms;

	private float _scaleCache;

	private Transform _tr;

	private static Scp106MinimapElement[] _pool;

	private const float HeightRange = 100f;

	private const float ElevatorCooldown = 0.2f;

	private const float FadeSpeed = 19f;

	private float CurTime => Time.timeSinceLevelLoad;

	private bool InElevator => this.CurTime - this._lastElevatorRide < 0.2f;

	private float Scale
	{
		get
		{
			return this._scaleCache;
		}
		set
		{
			this._scaleCache = Mathf.Clamp01(value);
			this._tr.localScale = Vector3.one * this._scaleCache;
		}
	}

	public CursorOverrideMode CursorOverride
	{
		get
		{
			if (!this.IsVisible)
			{
				return CursorOverrideMode.NoOverride;
			}
			return CursorOverrideMode.Free;
		}
	}

	public bool LockMovement => false;

	public bool IsVisible { get; set; }

	public Vector3 LastWorldPos { get; set; }

	public static Scp106Minimap Singleton { get; private set; }

	private void Setup()
	{
		this._poolSize = RoomIdentifier.AllRoomIdentifiers.Count;
		if (Scp106Minimap._pool == null || this._poolSize > Scp106Minimap._pool.Length)
		{
			Scp106Minimap._pool = new Scp106MinimapElement[this._poolSize];
		}
		for (int i = 0; i < this._poolSize; i++)
		{
			Scp106MinimapElement scp106MinimapElement = Object.Instantiate(this._template, this._offsetTransform);
			scp106MinimapElement.Rt.localScale = Vector3.one;
			Scp106Minimap._pool[i] = scp106MinimapElement;
		}
		this._setUp = true;
	}

	private void OnElevatorMoved(Bounds elevatorBounds, ElevatorChamber chamber, Vector3 deltaPos, Quaternion deltaRot)
	{
		if (elevatorBounds.Contains(MainCameraController.CurrentCamera.position))
		{
			this._lastElevatorRide = this.CurTime;
		}
	}

	private void Awake()
	{
		CursorManager.Register(this);
		ElevatorChamber.OnElevatorMoved += OnElevatorMoved;
		Scp106Minimap.Singleton = this;
		this._tr = base.transform;
	}

	private void OnDestroy()
	{
		CursorManager.Unregister(this);
		ElevatorChamber.OnElevatorMoved -= OnElevatorMoved;
	}

	private void Update()
	{
		if (!SeedSynchronizer.MapGenerated)
		{
			return;
		}
		if (!this._setUp)
		{
			this.Setup();
		}
		if (Scp106MinimapElement.AnyHighlighted)
		{
			RectTransform rt = Scp106MinimapElement.LastHighlighted.Rt;
			this._cursor.SetParent(rt);
			this._cursor.position = Input.mousePosition;
			this._cursor.gameObject.SetActive(value: true);
		}
		else
		{
			this._cursor.gameObject.SetActive(value: false);
		}
		this.Scale = Mathf.MoveTowards(this.Scale, this.IsVisible ? 1 : 0, 19f * Time.deltaTime);
		if (!(this.Scale <= 0f))
		{
			Transform currentCamera = MainCameraController.CurrentCamera;
			Vector3 position = currentCamera.position;
			int usedRooms = this._usedRooms;
			this._usedRooms = 0;
			if (this.InElevator)
			{
				this._errorSurface.SetActive(value: false);
				this._errorElevator.SetActive(value: true);
			}
			else if (position.GetZone() == FacilityZone.Surface)
			{
				this._errorSurface.SetActive(value: true);
				this._errorElevator.SetActive(value: false);
			}
			else
			{
				this._errorSurface.SetActive(value: false);
				this._errorElevator.SetActive(value: false);
				this.SetupRooms(currentCamera, position);
				this.RefreshRange();
			}
			for (int i = this._usedRooms; i < usedRooms; i++)
			{
				Scp106Minimap._pool[i].Img.enabled = false;
			}
			Vector3 vector = this._template.Rt.InverseTransformPoint(Input.mousePosition) / this._gridScale;
			this.LastWorldPos = new Vector3(0f - vector.x, position.y, 0f - vector.y);
		}
	}

	private void RefreshRange()
	{
		if (ReferenceHub.TryGetLocalHub(out var hub) && hub.playerStats.TryGetModule<VigorStat>(out var module))
		{
			float num = module.NormalizedValue / 0.019f;
			this._rangeTransform.localScale = Vector3.one / (this._radiusScale * num);
		}
	}

	private void SetupRooms(Transform camTr, Vector3 camPos)
	{
		foreach (RoomIdentifier allRoomIdentifier in RoomIdentifier.AllRoomIdentifiers)
		{
			Transform transform = allRoomIdentifier.transform;
			Vector3 position = transform.position;
			if (allRoomIdentifier.Zone != FacilityZone.None && !(Mathf.Abs(position.y - camPos.y) > 100f))
			{
				Scp106MinimapElement obj = Scp106Minimap._pool[this._usedRooms];
				obj.Room = allRoomIdentifier;
				obj.Img.sprite = allRoomIdentifier.Icon;
				obj.Img.enabled = true;
				obj.Rt.localEulerAngles = Vector3.back * transform.eulerAngles.y;
				obj.Rt.localPosition = new Vector3(position.x, position.z, 0f) * this._gridScale;
				this._usedRooms++;
			}
		}
		Vector3 vector = camPos * this._gridScale;
		this._offsetTransform.localPosition = -new Vector3(vector.x, vector.z, 0f);
		this._rotorTransform.localEulerAngles = Vector3.forward * camTr.eulerAngles.y;
	}
}
