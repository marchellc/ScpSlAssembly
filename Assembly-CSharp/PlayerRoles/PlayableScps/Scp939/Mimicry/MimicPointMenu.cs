using System;
using TMPro;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry
{
	public class MimicPointMenu : MimicryMenuBase
	{
		private void Update()
		{
			bool active = this._mimicController.Active;
			this._onRoot.SetActive(!active);
			this._offRoot.SetActive(active);
			if (!active)
			{
				return;
			}
			int num = Mathf.RoundToInt(this._mimicController.Distance);
			int num2 = Mathf.RoundToInt(this._mimicController.MaxDistance);
			this._distanceText.text = string.Format("{0}m / {1}m", Mathf.Min(num, num2), num2);
		}

		protected override void Setup(Scp939Role role)
		{
			base.Setup(role);
			role.SubroutineModule.TryGetSubroutine<MimicPointController>(out this._mimicController);
		}

		public void RequestToggle()
		{
			this._mimicController.ClientToggle();
			if (!MimicryMenuController.SingletonSet)
			{
				return;
			}
			MimicryMenuController.Singleton.IsEnabled = false;
		}

		[SerializeField]
		private GameObject _onRoot;

		[SerializeField]
		private GameObject _offRoot;

		[SerializeField]
		private TMP_Text _distanceText;

		private MimicPointController _mimicController;

		private const string DistanceTextFormat = "{0}m / {1}m";
	}
}
