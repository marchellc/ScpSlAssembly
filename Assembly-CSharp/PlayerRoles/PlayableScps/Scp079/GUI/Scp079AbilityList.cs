using System;
using System.Collections.Generic;
using PlayerRoles.Subroutines;
using TMPro;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.GUI
{
	public class Scp079AbilityList : Scp079GuiElementBase
	{
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
				if (value == this._cachedAlpha)
				{
					return;
				}
				this._failMessageText.alpha = value;
				this._cachedAlpha = value;
			}
		}

		private static float CurrentTime
		{
			get
			{
				return Time.timeSinceLevelLoad;
			}
		}

		public static Scp079AbilityList Singleton { get; private set; }

		public IScp079FailMessageProvider TrackedFailMessage
		{
			get
			{
				return this._trackedMessage;
			}
			set
			{
				bool flag;
				if (value != null)
				{
					global::UnityEngine.Object @object = value as global::UnityEngine.Object;
					flag = @object != null && @object == null;
				}
				else
				{
					flag = true;
				}
				bool flag2 = flag;
				if (!flag2)
				{
					value.OnFailMessageAssigned();
					if (string.IsNullOrEmpty(value.FailMessage))
					{
						return;
					}
				}
				this._trackedMessage = value;
				this._failTextReady = false;
				if (flag2)
				{
					return;
				}
				this._fadeoutBeginTime = Scp079AbilityList.CurrentTime + 4f;
				this._fadeoutEndTime = this._fadeoutBeginTime + 1.8f;
				base.PlaySound(this._popupSound, 1f);
			}
		}

		private void Awake()
		{
			Scp079AbilityList.Singleton = this;
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
				return;
			}
			float num = 1f - Mathf.InverseLerp(this._fadeoutBeginTime, this._fadeoutEndTime, Scp079AbilityList.CurrentTime);
			this.FailMessageAlpha = Mathf.MoveTowards(this.FailMessageAlpha, num, Time.deltaTime * 5.5f);
			this._failMessageText.text = this._trackedMessage.FailMessage;
		}

		private void UpdateList()
		{
			this.UpdateGroup(true);
			this.UpdateGroup(false);
		}

		private void UpdateGroup(bool isLeft)
		{
			List<Scp079KeyAbilityGui> list = (isLeft ? this._leftGroupInstances : this._mainGroupInstances);
			int num = 0;
			int num2 = -1;
			foreach (SubroutineBase subroutineBase in base.Role.SubroutineModule.AllSubroutines)
			{
				Scp079LostSignalHandler scp079LostSignalHandler = subroutineBase as Scp079LostSignalHandler;
				if (scp079LostSignalHandler != null && scp079LostSignalHandler.Lost)
				{
					num = 0;
					break;
				}
				Scp079KeyAbilityBase scp079KeyAbilityBase = subroutineBase as Scp079KeyAbilityBase;
				if (scp079KeyAbilityBase != null && scp079KeyAbilityBase.UseLeftMenu == isLeft && scp079KeyAbilityBase.IsVisible)
				{
					bool flag;
					if (scp079KeyAbilityBase.CategoryId != num2)
					{
						flag = num2 != -1;
						num2 = scp079KeyAbilityBase.CategoryId;
					}
					else
					{
						flag = false;
					}
					list[num++].Setup(scp079KeyAbilityBase.IsReady, scp079KeyAbilityBase.AbilityName, scp079KeyAbilityBase.ActivationKey, flag);
					if (num >= list.Count)
					{
						Scp079KeyAbilityGui scp079KeyAbilityGui = list[0];
						list.Add(global::UnityEngine.Object.Instantiate<Scp079KeyAbilityGui>(scp079KeyAbilityGui, scp079KeyAbilityGui.transform.parent));
					}
				}
			}
			if (!Scp079Role.LocalInstanceActive)
			{
				num = 0;
			}
			for (int j = num; j < list.Count; j++)
			{
				list[j].gameObject.SetActive(false);
			}
		}

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

		private const float TransitionSpeed = 5.5f;

		private const float FadeoutDuration = 1.8f;

		private const float SustainDuration = 4f;
	}
}
