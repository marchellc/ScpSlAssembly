using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PlayerRoles.RoleAssign;

public class ScpPreferenceSlider : Slider
{
	private static ScpPreferenceSlider _highlighted;

	[SerializeField]
	private RoleTypeId _role = RoleTypeId.None;

	public static bool AnyHighlighted { get; private set; }

	private int SavedPreference
	{
		get
		{
			return ScpSpawnPreferences.GetPreference(this._role);
		}
		set
		{
			ScpSpawnPreferences.SavePreference(this._role, value);
		}
	}

	private void OnValueChanged(float x)
	{
		this.SavedPreference = Mathf.RoundToInt(x);
	}

	private void Deselect()
	{
		if (!(ScpPreferenceSlider._highlighted != this))
		{
			ScpPreferenceSlider.AnyHighlighted = false;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		base.onValueChanged.AddListener(OnValueChanged);
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		this.SetValueWithoutNotify(this.SavedPreference);
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		this.Deselect();
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		base.OnPointerDown(eventData);
		ScpPreferenceSlider.AnyHighlighted = true;
		ScpPreferenceSlider._highlighted = this;
	}

	public override void OnPointerUp(PointerEventData eventData)
	{
		base.OnPointerUp(eventData);
		this.Deselect();
	}

	public void SetRole(RoleTypeId rt)
	{
		this._role = rt;
		this.SetValueWithoutNotify(this.SavedPreference);
	}
}
