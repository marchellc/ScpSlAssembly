using System;
using CursorManagement;
using Interactables.Interobjects;
using MapGeneration;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp106
{
	public class Scp106Minimap : MonoBehaviour, ICursorOverride
	{
		private float CurTime
		{
			get
			{
				return Time.timeSinceLevelLoad;
			}
		}

		private bool InElevator
		{
			get
			{
				return this.CurTime - this._lastElevatorRide < 0.2f;
			}
		}

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

		public bool LockMovement
		{
			get
			{
				return false;
			}
		}

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
				Scp106MinimapElement scp106MinimapElement = global::UnityEngine.Object.Instantiate<Scp106MinimapElement>(this._template, this._offsetTransform);
				scp106MinimapElement.Rt.localScale = Vector3.one;
				Scp106Minimap._pool[i] = scp106MinimapElement;
			}
			this._setUp = true;
		}

		private void OnElevatorMoved(Bounds elevatorBounds, ElevatorChamber chamber, Vector3 deltaPos, Quaternion deltaRot)
		{
			if (!elevatorBounds.Contains(MainCameraController.CurrentCamera.position))
			{
				return;
			}
			this._lastElevatorRide = this.CurTime;
		}

		private void Awake()
		{
			CursorManager.Register(this);
			ElevatorChamber.OnElevatorMoved += this.OnElevatorMoved;
			Scp106Minimap.Singleton = this;
			this._tr = base.transform;
		}

		private void OnDestroy()
		{
			CursorManager.Unregister(this);
			ElevatorChamber.OnElevatorMoved -= this.OnElevatorMoved;
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
				this._cursor.gameObject.SetActive(true);
			}
			else
			{
				this._cursor.gameObject.SetActive(false);
			}
			this.Scale = Mathf.MoveTowards(this.Scale, (float)(this.IsVisible ? 1 : 0), 19f * Time.deltaTime);
			if (this.Scale <= 0f)
			{
				return;
			}
			Transform currentCamera = MainCameraController.CurrentCamera;
			Vector3 position = currentCamera.position;
			int usedRooms = this._usedRooms;
			this._usedRooms = 0;
			if (this.InElevator)
			{
				this._errorSurface.SetActive(false);
				this._errorElevator.SetActive(true);
			}
			else if (position.y > 800f)
			{
				this._errorSurface.SetActive(true);
				this._errorElevator.SetActive(false);
			}
			else
			{
				this._errorSurface.SetActive(false);
				this._errorElevator.SetActive(false);
				this.SetupRooms(currentCamera, position);
				this.RefreshRange();
			}
			for (int i = this._usedRooms; i < usedRooms; i++)
			{
				Scp106Minimap._pool[i].Img.enabled = false;
			}
			Vector3 vector = this._template.Rt.InverseTransformPoint(Input.mousePosition) / this._gridScale;
			this.LastWorldPos = new Vector3(-vector.x, position.y, -vector.y);
		}

		private void RefreshRange()
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return;
			}
			VigorStat vigorStat;
			if (!referenceHub.playerStats.TryGetModule<VigorStat>(out vigorStat))
			{
				return;
			}
			float num = vigorStat.NormalizedValue / 0.019f;
			this._rangeTransform.localScale = Vector3.one / (this._radiusScale * num);
		}

		private void SetupRooms(Transform camTr, Vector3 camPos)
		{
			foreach (RoomIdentifier roomIdentifier in RoomIdentifier.AllRoomIdentifiers)
			{
				Transform transform = roomIdentifier.transform;
				Vector3 position = transform.position;
				if (roomIdentifier.Zone != FacilityZone.None && Mathf.Abs(position.y - camPos.y) <= 350f)
				{
					Scp106MinimapElement scp106MinimapElement = Scp106Minimap._pool[this._usedRooms];
					scp106MinimapElement.Room = roomIdentifier;
					scp106MinimapElement.Img.sprite = roomIdentifier.Icon;
					scp106MinimapElement.Img.enabled = true;
					scp106MinimapElement.Rt.localEulerAngles = Vector3.back * transform.eulerAngles.y;
					scp106MinimapElement.Rt.localPosition = new Vector3(position.x, position.z, 0f) * this._gridScale;
					this._usedRooms++;
				}
			}
			Vector3 vector = camPos * this._gridScale;
			this._offsetTransform.localPosition = -new Vector3(vector.x, vector.z, 0f);
			this._rotorTransform.localEulerAngles = Vector3.forward * camTr.eulerAngles.y;
		}

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

		private const float SurfaceHeightThreshold = 800f;

		private const float HeightRange = 350f;

		private const float ElevatorCooldown = 0.2f;

		private const float FadeSpeed = 19f;
	}
}
