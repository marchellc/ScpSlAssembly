using TMPro;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry;

public class MimicPointMenu : MimicryMenuBase
{
	[SerializeField]
	private GameObject _onRoot;

	[SerializeField]
	private GameObject _offRoot;

	[SerializeField]
	private TMP_Text _distanceText;

	private MimicPointController _mimicController;

	private const string DistanceTextFormat = "{0}m / {1}m";

	private void Update()
	{
		bool active = this._mimicController.Active;
		this._onRoot.SetActive(!active);
		this._offRoot.SetActive(active);
		if (active)
		{
			int a = Mathf.RoundToInt(this._mimicController.Distance);
			int num = Mathf.RoundToInt(this._mimicController.MaxDistance);
			this._distanceText.text = $"{Mathf.Min(a, num)}m / {num}m";
		}
	}

	protected override void Setup(Scp939Role role)
	{
		base.Setup(role);
		role.SubroutineModule.TryGetSubroutine<MimicPointController>(out this._mimicController);
	}

	public void RequestToggle()
	{
		this._mimicController.ClientToggle();
		if (MimicryMenuController.SingletonSet)
		{
			MimicryMenuController.Singleton.IsEnabled = false;
		}
	}
}
