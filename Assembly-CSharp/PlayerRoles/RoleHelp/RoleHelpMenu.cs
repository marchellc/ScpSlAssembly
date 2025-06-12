using CursorManagement;
using ToggleableMenus;
using UnityEngine;

namespace PlayerRoles.RoleHelp;

public class RoleHelpMenu : ToggleableMenuBase
{
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

	public override bool CanToggle => this._instanceSet;

	public override CursorOverrideMode CursorOverride => CursorOverrideMode.NoOverride;

	protected override void OnToggled()
	{
		this._fading = true;
		this._root.SetActive(value: true);
	}

	protected override void Awake()
	{
		base.Awake();
		PlayerRoleManager.OnRoleChanged += OnRoleChanged;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		PlayerRoleManager.OnRoleChanged -= OnRoleChanged;
	}

	private void Update()
	{
		if (this._fading && this.UpdateAlpha(this.IsEnabled ? this._fadeSpeed : (0f - this._fadeSpeed)))
		{
			this._fading = false;
			this._root.SetActive(this.IsEnabled);
		}
	}

	private bool UpdateAlpha(float speed)
	{
		float alpha = this._fader.alpha;
		alpha = Mathf.Clamp01(alpha + Time.deltaTime * speed);
		bool result = alpha == 1f;
		bool result2 = alpha == 0f;
		this._fader.alpha = alpha;
		if (!(speed > 0f))
		{
			return result2;
		}
		return result;
	}

	private void OnRoleChanged(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
	{
		if (hub.isLocalPlayer)
		{
			this.IsEnabled = false;
			if (this._instanceSet)
			{
				Object.Destroy(this._instance);
				this._instanceSet = false;
			}
			GameObject roleHelpInfo = newRole.RoleHelpInfo;
			if (!(roleHelpInfo == null))
			{
				this._instance = Object.Instantiate(roleHelpInfo, this._parent);
				this._instanceSet = true;
			}
		}
	}
}
