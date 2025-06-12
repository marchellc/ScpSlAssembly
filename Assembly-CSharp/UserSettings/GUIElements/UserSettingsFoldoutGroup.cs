using UnityEngine;
using UnityEngine.UI;

namespace UserSettings.GUIElements;

public class UserSettingsFoldoutGroup : MonoBehaviour
{
	private static readonly Quaternion CollapsedRot = Quaternion.Euler(0f, 0f, 90f);

	private static readonly Quaternion ExtendedRot = Quaternion.Euler(0f, 0f, 180f);

	private const float FullAnimTime = 0.15f;

	private const float ExtendTime = 0.08f;

	[Tooltip("Fader of the collapsable group. Note the height of this grooup will be added to the start height when the group is extended.")]
	[SerializeField]
	private CanvasGroup _fadeGroup;

	[Tooltip("Deactivates the game object of the group is folded")]
	[SerializeField]
	private bool _disableWhenInvisible;

	[Tooltip("Will be rotated downwards when the group is extended, and will point to the left when the group is extended.")]
	[SerializeField]
	private RectTransform _arrow;

	[SerializeField]
	private Toggle _extendToggle;

	[SerializeField]
	private bool _collapseOthers;

	[SerializeField]
	private bool _collapseOnDisable;

	private Vector2 _startSize;

	private RectTransform _parentRt;

	private RectTransform _fadeGroupRt;

	private float _animStatus;

	private bool _initialized;

	private static UserSettingsFoldoutGroup _lastFoldedOut;

	public float ExtendRate => Mathf.Clamp01(this._animStatus / 0.15f);

	private void Awake()
	{
		this.Initialize();
	}

	private void OnDisable()
	{
		if (this._collapseOnDisable)
		{
			this.FoldInstantly();
		}
	}

	private void Initialize()
	{
		if (!this._initialized)
		{
			this._parentRt = base.GetComponent<RectTransform>();
			this._startSize = this._parentRt.sizeDelta;
			this._fadeGroupRt = this._fadeGroup.GetComponent<RectTransform>();
			this._initialized = true;
		}
	}

	private void LateUpdate()
	{
		bool isOn = this._extendToggle.isOn;
		if (isOn)
		{
			if (UserSettingsFoldoutGroup._lastFoldedOut != this && this._collapseOthers)
			{
				if (UserSettingsFoldoutGroup._lastFoldedOut != null)
				{
					UserSettingsFoldoutGroup._lastFoldedOut._extendToggle.isOn = false;
				}
				UserSettingsFoldoutGroup._lastFoldedOut = this;
			}
			if (this._animStatus < 0.15f)
			{
				this._animStatus += Time.deltaTime;
			}
			this.RefreshSize();
		}
		if (!isOn && this._animStatus > 0f)
		{
			this._animStatus -= Time.deltaTime;
			this.RefreshSize();
		}
	}

	public void FoldInstantly()
	{
		this._extendToggle.isOn = false;
		this._animStatus = 0f;
		this.RefreshSize();
	}

	public void ExtendInstantly()
	{
		this._extendToggle.isOn = true;
		this._animStatus = 0.15f;
		this.RefreshSize();
	}

	public void RefreshSize()
	{
		this.Initialize();
		float num = Mathf.Clamp01(this._animStatus / 0.08f);
		this._parentRt.sizeDelta = this._startSize + this._fadeGroupRt.sizeDelta.y * num * Vector2.up;
		this._arrow.localRotation = Quaternion.Lerp(UserSettingsFoldoutGroup.CollapsedRot, UserSettingsFoldoutGroup.ExtendedRot, num);
		float num2 = Mathf.InverseLerp(0.08f, 0.15f, this._animStatus);
		this._fadeGroup.gameObject.SetActive(this._animStatus > 0f || !this._disableWhenInvisible);
		this._fadeGroup.blocksRaycasts = num2 >= 1f;
		this._fadeGroup.alpha = num2;
	}
}
