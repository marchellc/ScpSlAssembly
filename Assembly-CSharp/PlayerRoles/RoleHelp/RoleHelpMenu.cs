using System;
using CursorManagement;
using ToggleableMenus;
using UnityEngine;

namespace PlayerRoles.RoleHelp
{
	public class RoleHelpMenu : ToggleableMenuBase
	{
		public override bool CanToggle
		{
			get
			{
				return this._instanceSet;
			}
		}

		public override CursorOverrideMode CursorOverride
		{
			get
			{
				return CursorOverrideMode.NoOverride;
			}
		}

		protected override void OnToggled()
		{
			this._fading = true;
			this._root.SetActive(true);
		}

		protected override void Awake()
		{
			base.Awake();
			PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
		}

		private void Update()
		{
			if (!this._fading)
			{
				return;
			}
			if (!this.UpdateAlpha(this.IsEnabled ? this._fadeSpeed : (-this._fadeSpeed)))
			{
				return;
			}
			this._fading = false;
			this._root.SetActive(this.IsEnabled);
		}

		private bool UpdateAlpha(float speed)
		{
			float num = this._fader.alpha;
			num = Mathf.Clamp01(num + Time.deltaTime * speed);
			bool flag = num == 1f;
			bool flag2 = num == 0f;
			this._fader.alpha = num;
			if (speed <= 0f)
			{
				return flag2;
			}
			return flag;
		}

		private void OnRoleChanged(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			if (!hub.isLocalPlayer)
			{
				return;
			}
			this.IsEnabled = false;
			if (this._instanceSet)
			{
				global::UnityEngine.Object.Destroy(this._instance);
				this._instanceSet = false;
			}
			GameObject roleHelpInfo = newRole.RoleHelpInfo;
			if (roleHelpInfo == null)
			{
				return;
			}
			this._instance = global::UnityEngine.Object.Instantiate<GameObject>(roleHelpInfo, this._parent);
			this._instanceSet = true;
		}

		private bool _instanceSet;

		private GameObject _instance;

		private bool _fading;

		[SerializeField]
		private Transform _parent;

		[SerializeField]
		private GameObject _root;

		[SerializeField]
		private CanvasGroup _fader;

		[SerializeField]
		private float _fadeSpeed;
	}
}
