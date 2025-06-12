using System.Collections.Generic;
using PlayerRoles.Subroutines;
using TMPro;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.GUI;

public class Scp079AbilityList : Scp079GuiElementBase
{
	[SerializeField]
	private List<Scp079KeyAbilityGui> _mainGroupInstances = new List<Scp079KeyAbilityGui>();

	[SerializeField]
	private List<Scp079KeyAbilityGui> _leftGroupInstances = new List<Scp079KeyAbilityGui>();

	[SerializeField]
	private TextMeshProUGUI _failMessageText;

	[SerializeField]
	private AudioClip _popupSound;

	private IScp079FailMessageProvider _trackedMessage;

	private float _cachedAlpha = -1f;

	private bool _failTextReady;

	private float _fadeoutBeginTime;

	private float _fadeoutEndTime;

	private static Scp079AbilityList _singleton;

	private const float TransitionSpeed = 5.5f;

	private const float FadeoutDuration = 1.8f;

	private const float SustainDuration = 4f;

	private float FailMessageAlpha
	{
		get
		{
			if (this._cachedAlpha < 0f)
			{
				this._cachedAlpha = this._failMessageText.alpha;
			}
			return this._cachedAlpha;
		}
		set
		{
			value = Mathf.Clamp01(value);
			if (value != this._cachedAlpha)
			{
				this._failMessageText.alpha = value;
				this._cachedAlpha = value;
			}
		}
	}

	private static float CurrentTime => Time.timeSinceLevelLoad;

	public IScp079FailMessageProvider TrackedFailMessage
	{
		get
		{
			return this._trackedMessage;
		}
		set
		{
			bool flag = value == null || (value is Object obj && obj == null);
			if (!flag)
			{
				value.OnFailMessageAssigned();
				if (string.IsNullOrEmpty(value.FailMessage))
				{
					return;
				}
			}
			this._trackedMessage = value;
			this._failTextReady = false;
			if (!flag)
			{
				this._fadeoutBeginTime = Scp079AbilityList.CurrentTime + 4f;
				this._fadeoutEndTime = this._fadeoutBeginTime + 1.8f;
				base.PlaySound(this._popupSound);
			}
		}
	}

	public static bool TryGetSingleton(out Scp079AbilityList singleton)
	{
		singleton = Scp079AbilityList._singleton;
		return singleton != null;
	}

	private void Awake()
	{
		Scp079AbilityList._singleton = this;
	}

	private void Update()
	{
		this.UpdateFailMessage();
		this.UpdateList();
	}

	private void UpdateFailMessage()
	{
		if (!this._failTextReady || this._trackedMessage == null || string.IsNullOrEmpty(this._trackedMessage.FailMessage))
		{
			this.FailMessageAlpha -= Time.deltaTime * 5.5f;
			if (this.FailMessageAlpha == 0f)
			{
				this._failTextReady = true;
			}
		}
		else
		{
			float target = 1f - Mathf.InverseLerp(this._fadeoutBeginTime, this._fadeoutEndTime, Scp079AbilityList.CurrentTime);
			this.FailMessageAlpha = Mathf.MoveTowards(this.FailMessageAlpha, target, Time.deltaTime * 5.5f);
			this._failMessageText.text = this._trackedMessage.FailMessage;
		}
	}

	private void UpdateList()
	{
		this.UpdateGroup(isLeft: true);
		this.UpdateGroup(isLeft: false);
	}

	private void UpdateGroup(bool isLeft)
	{
		List<Scp079KeyAbilityGui> list = (isLeft ? this._leftGroupInstances : this._mainGroupInstances);
		int num = 0;
		int num2 = -1;
		SubroutineBase[] allSubroutines = base.Role.SubroutineModule.AllSubroutines;
		foreach (SubroutineBase subroutineBase in allSubroutines)
		{
			if (subroutineBase is Scp079LostSignalHandler { Lost: not false })
			{
				num = 0;
				break;
			}
			if (subroutineBase is Scp079KeyAbilityBase scp079KeyAbilityBase && scp079KeyAbilityBase.UseLeftMenu == isLeft && scp079KeyAbilityBase.IsVisible)
			{
				bool createSpace;
				if (scp079KeyAbilityBase.CategoryId != num2)
				{
					createSpace = num2 != -1;
					num2 = scp079KeyAbilityBase.CategoryId;
				}
				else
				{
					createSpace = false;
				}
				list[num++].Setup(scp079KeyAbilityBase.IsReady, scp079KeyAbilityBase.AbilityName, scp079KeyAbilityBase.ActivationKey, createSpace);
				if (num >= list.Count)
				{
					Scp079KeyAbilityGui scp079KeyAbilityGui = list[0];
					list.Add(Object.Instantiate(scp079KeyAbilityGui, scp079KeyAbilityGui.transform.parent));
				}
			}
		}
		if (!Scp079Role.LocalInstanceActive)
		{
			num = 0;
		}
		for (int j = num; j < list.Count; j++)
		{
			list[j].gameObject.SetActive(value: false);
		}
	}
}
