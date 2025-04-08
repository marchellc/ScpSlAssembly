using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry
{
	[RequireComponent(typeof(Button))]
	public class EnvMimicryStandardButton : MonoBehaviour
	{
		protected virtual bool IsAvailable
		{
			get
			{
				return true;
			}
		}

		protected virtual void Awake()
		{
			this._button = base.GetComponent<Button>();
			this._button.onClick.AddListener(new UnityAction(this.OnButtonPressed));
			this._buttonGameObject = this._button.gameObject;
			this._prevState = !this.IsAvailable;
			StaticUnityMethods.OnUpdate += this.AlwaysUpdate;
		}

		protected virtual void OnDestroy()
		{
			StaticUnityMethods.OnUpdate -= this.AlwaysUpdate;
		}

		protected virtual void AlwaysUpdate()
		{
			if (this._prevState == this.IsAvailable)
			{
				return;
			}
			bool flag = !this._prevState;
			this._buttonGameObject.SetActive(flag);
			this._prevState = flag;
		}

		protected virtual void OnButtonPressed()
		{
			EnvironmentalMimicry environmentalMimicry;
			if (!this.TryGetLocalSubroutine(out environmentalMimicry))
			{
				return;
			}
			environmentalMimicry.ClientSelect(this._randomSequences.RandomItem<EnvMimicrySequence>());
		}

		private bool TryGetLocalSubroutine(out EnvironmentalMimicry localSubroutine)
		{
			localSubroutine = this._cachedSubroutine;
			if (this._cacheSet)
			{
				return this._cachedSubroutine != null;
			}
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return false;
			}
			Scp939Role scp939Role = referenceHub.roleManager.CurrentRole as Scp939Role;
			if (scp939Role == null)
			{
				return false;
			}
			if (!scp939Role.SubroutineModule.TryGetSubroutine<EnvironmentalMimicry>(out localSubroutine))
			{
				return false;
			}
			this._cacheSet = true;
			this._cachedSubroutine = localSubroutine;
			return true;
		}

		[SerializeField]
		private EnvMimicrySequence[] _randomSequences;

		private bool _prevState;

		private Button _button;

		private GameObject _buttonGameObject;

		private bool _cacheSet;

		private EnvironmentalMimicry _cachedSubroutine;
	}
}
