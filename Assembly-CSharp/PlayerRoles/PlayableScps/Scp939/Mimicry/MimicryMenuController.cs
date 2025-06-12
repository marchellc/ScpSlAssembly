using ToggleableMenus;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry;

public class MimicryMenuController : ToggleableMenuBase
{
	[SerializeField]
	private float _fadeSpeed;

	[SerializeField]
	private CanvasGroup _fader;

	[SerializeField]
	private CanvasGroup[] _inverseFaders;

	private bool _canvasCacheSet;

	private Canvas _cachedCanvas;

	public static MimicryMenuController Singleton { get; private set; }

	public static bool SingletonSet { get; private set; }

	public static bool FullyClosed
	{
		get
		{
			if (MimicryMenuController.SingletonSet)
			{
				return !MimicryMenuController.Singleton.gameObject.activeSelf;
			}
			return false;
		}
	}

	public override bool CanToggle
	{
		get
		{
			if (ReferenceHub.TryGetLocalHub(out var hub))
			{
				return hub.roleManager.CurrentRole is Scp939Role;
			}
			return false;
		}
	}

	public static float ScaleFactor
	{
		get
		{
			if (!MimicryMenuController.SingletonSet)
			{
				return 1f;
			}
			if (!MimicryMenuController.Singleton._canvasCacheSet)
			{
				MimicryMenuController.Singleton._cachedCanvas = MimicryMenuController.Singleton.GetComponentInParent<Canvas>();
				MimicryMenuController.Singleton._canvasCacheSet = true;
			}
			return MimicryMenuController.Singleton._cachedCanvas.scaleFactor;
		}
	}

	private void Update()
	{
		float num = (this.IsEnabled ? 1f : (-1f));
		float newAlpha = this._fader.alpha + num * Time.deltaTime * this._fadeSpeed;
		newAlpha = Mathf.Clamp01(newAlpha);
		this._fader.alpha = newAlpha;
		this._inverseFaders.ForEach(delegate(CanvasGroup x)
		{
			x.alpha = 1f - newAlpha;
		});
		if (newAlpha <= 0f)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	protected override void Awake()
	{
		base.Awake();
		MimicryMenuController.Singleton = this;
		MimicryMenuController.SingletonSet = true;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (!(MimicryMenuController.Singleton != this))
		{
			MimicryMenuController.Singleton = null;
			MimicryMenuController.SingletonSet = false;
		}
	}

	protected override void OnToggled()
	{
		if (this.IsEnabled)
		{
			base.gameObject.SetActive(value: true);
		}
	}
}
