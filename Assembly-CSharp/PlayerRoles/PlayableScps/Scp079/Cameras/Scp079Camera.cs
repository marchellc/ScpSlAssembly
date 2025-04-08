using System;
using System.Collections.Generic;
using MapGeneration;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp079.Cameras
{
	public class Scp079Camera : Scp079InteractableBase, IAdvancedCameraController, ICameraController
	{
		public static event Action<Scp079Camera> OnInstanceCreated;

		public static event Action<Scp079Camera> OnInstanceRemoved;

		public static event Action<Scp079Camera> OnAnyCameraStateChanged;

		public bool IsActive
		{
			get
			{
				return this._isActive;
			}
			set
			{
				if (value == this._isActive)
				{
					return;
				}
				this._isActive = value;
				Renderer[] targetRenderers = this._targetRenderers;
				for (int i = 0; i < targetRenderers.Length; i++)
				{
					targetRenderers[i].sharedMaterial = (this._isActive ? this._onlineMat : this._offlineMat);
				}
				Action<Scp079Camera> onAnyCameraStateChanged = Scp079Camera.OnAnyCameraStateChanged;
				if (onAnyCameraStateChanged == null)
				{
					return;
				}
				onAnyCameraStateChanged(this);
			}
		}

		public bool IsUsedByLocalPlayer
		{
			get
			{
				return Scp079Role.LocalInstanceActive && Scp079Role.LocalInstance.CurrentCamera == this;
			}
		}

		public Vector3 CameraPosition { get; private set; }

		public float VerticalRotation { get; private set; }

		public float HorizontalRotation { get; private set; }

		public float RollRotation { get; private set; }

		protected override void Awake()
		{
			base.Awake();
			this.VerticalAxis.Awake(this);
			this.HorizontalAxis.Awake(this);
			this.ZoomAxis.Awake(this);
		}

		private void Start()
		{
			Action<Scp079Camera> onInstanceCreated = Scp079Camera.OnInstanceCreated;
			if (onInstanceCreated == null)
			{
				return;
			}
			onInstanceCreated(this);
		}

		internal void WriteAxes(NetworkWriter writer)
		{
			writer.WriteUShort(this.VerticalAxis.Value16BitCompression);
			writer.WriteUShort(this.HorizontalAxis.Value16BitCompression);
			writer.WriteByte(this.ZoomAxis.Value8BitCompression);
		}

		internal void ApplyAxes(NetworkReader reader)
		{
			if (this.IsUsedByLocalPlayer)
			{
				return;
			}
			this.VerticalAxis.Value16BitCompression = reader.ReadUShort();
			this.HorizontalAxis.Value16BitCompression = reader.ReadUShort();
			this.ZoomAxis.Value8BitCompression = reader.ReadByte();
		}

		protected virtual void Update()
		{
			this.VerticalAxis.Update(this);
			this.HorizontalAxis.Update(this);
			this.ZoomAxis.Update(this);
			if (!this.IsActive)
			{
				return;
			}
			if (Scp079Role.ActiveInstances.All((Scp079Role x) => x.CurrentCamera != this, true))
			{
				this.IsActive = false;
				return;
			}
			Vector3 eulerAngles = this.CameraAnchor.rotation.eulerAngles;
			this.VerticalRotation = eulerAngles.x;
			this.HorizontalRotation = eulerAngles.y;
			this.RollRotation = eulerAngles.z;
			this.CameraPosition = this.CameraAnchor.position;
		}

		public static bool TryGetClosestCamera(Vector3 pos, Func<Scp079Camera, bool> validator, out Scp079Camera closest)
		{
			closest = null;
			float num = float.MaxValue;
			bool flag = false;
			foreach (Scp079InteractableBase scp079InteractableBase in Scp079InteractableBase.AllInstances)
			{
				Scp079Camera scp079Camera = scp079InteractableBase as Scp079Camera;
				if (scp079Camera != null && (validator == null || validator(scp079Camera)))
				{
					float sqrMagnitude = (scp079Camera.Position - pos).sqrMagnitude;
					if (num >= sqrMagnitude)
					{
						flag = true;
						closest = scp079Camera;
						num = sqrMagnitude;
					}
				}
			}
			return flag;
		}

		public static bool TryGetMainCamera(RoomIdentifier room, out Scp079Camera main)
		{
			foreach (Scp079InteractableBase scp079InteractableBase in Scp079InteractableBase.AllInstances)
			{
				Scp079Camera scp079Camera = scp079InteractableBase as Scp079Camera;
				if (scp079Camera != null && scp079Camera.IsMain && !(scp079Camera.Room != room))
				{
					main = scp079Camera;
					return true;
				}
			}
			main = null;
			return false;
		}

		public static List<Scp079Camera> GetRoomCameras(RoomIdentifier room)
		{
			List<Scp079Camera> list = new List<Scp079Camera>();
			foreach (Scp079InteractableBase scp079InteractableBase in Scp079InteractableBase.AllInstances)
			{
				Scp079Camera scp079Camera = scp079InteractableBase as Scp079Camera;
				if (scp079Camera != null && !(scp079Camera.Room != room))
				{
					list.Add(scp079Camera);
				}
			}
			return list;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			Action<Scp079Camera> onInstanceRemoved = Scp079Camera.OnInstanceRemoved;
			if (onInstanceRemoved == null)
			{
				return;
			}
			onInstanceRemoved(this);
		}

		public bool IsMain;

		public string Label;

		public CameraRotationAxis VerticalAxis;

		public CameraRotationAxis HorizontalAxis;

		public CameraZoomAxis ZoomAxis;

		[FormerlySerializedAs("_cameraAnchor")]
		[SerializeField]
		public Transform CameraAnchor;

		[SerializeField]
		private Renderer[] _targetRenderers;

		[SerializeField]
		private Material _offlineMat;

		[SerializeField]
		private Material _onlineMat;

		private bool _isActive;
	}
}
