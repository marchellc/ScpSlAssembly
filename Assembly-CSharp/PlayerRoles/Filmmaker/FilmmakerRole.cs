using System;
using CameraShaking;
using GameObjectPools;
using UnityEngine;

namespace PlayerRoles.Filmmaker
{
	public class FilmmakerRole : PlayerRoleBase, IShakeEffect, IAdvancedCameraController, ICameraController, IPoolResettable, ICustomNameRole
	{
		public override RoleTypeId RoleTypeId
		{
			get
			{
				return RoleTypeId.Filmmaker;
			}
		}

		public override Team Team
		{
			get
			{
				return Team.Dead;
			}
		}

		public override Color RoleColor
		{
			get
			{
				return new Color(0.05f, 0.05f, 0.05f, 1f);
			}
		}

		public Vector3 CameraPosition { get; set; }

		public Quaternion CameraRotation
		{
			get
			{
				return Quaternion.Euler(this.VerticalRotation, this.HorizontalRotation, this.RollRotation);
			}
			set
			{
				Vector3 eulerAngles = value.eulerAngles;
				this.VerticalRotation = eulerAngles.x;
				this.HorizontalRotation = eulerAngles.y;
				this.RollRotation = eulerAngles.z;
			}
		}

		public float VerticalRotation { get; set; }

		public float HorizontalRotation { get; set; }

		public float RollRotation { get; set; }

		public static float ZoomScale { get; set; }

		public string CustomRoleName
		{
			get
			{
				return "Film Maker";
			}
		}

		private void OnDestroy()
		{
			this.ResetObject();
		}

		internal override void Init(ReferenceHub hub, RoleChangeReason spawnReason, RoleSpawnFlags spawnFlags)
		{
			base.Init(hub, spawnReason, spawnFlags);
			if (!hub.isLocalPlayer)
			{
				return;
			}
			Transform currentCamera = MainCameraController.CurrentCamera;
			this.CameraPosition = currentCamera.position;
			this.CameraRotation = currentCamera.rotation;
			FilmmakerRole.ZoomScale = 1f;
			CameraShakeController.AddEffect(this);
			this._toolsInstance = global::UnityEngine.Object.Instantiate<GameObject>(this._filmmakerTools);
		}

		public bool GetEffect(ReferenceHub ply, out ShakeEffectValues shakeValues)
		{
			float num = 1f / FilmmakerRole.ZoomScale;
			shakeValues = new ShakeEffectValues(null, null, null, num, 0f, 0f);
			return base.IsLocalPlayer;
		}

		public void ResetObject()
		{
			if (this._toolsInstance == null)
			{
				return;
			}
			global::UnityEngine.Object.Destroy(this._toolsInstance);
		}

		[SerializeField]
		private GameObject _filmmakerTools;

		private GameObject _toolsInstance;
	}
}
