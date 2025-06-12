using UnityEngine;

namespace ToggleableMenus;

public class SimpleToggleableMenu : ToggleableMenuBase
{
	[SerializeField]
	private GameObject _targetRoot;

	public override bool CanToggle => true;

	protected override void OnToggled()
	{
		this._targetRoot.SetActive(this.IsEnabled);
	}
}
