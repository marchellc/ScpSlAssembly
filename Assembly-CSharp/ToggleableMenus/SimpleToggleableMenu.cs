using UnityEngine;

namespace ToggleableMenus;

public class SimpleToggleableMenu : ToggleableMenuBase
{
	[SerializeField]
	private GameObject _targetRoot;

	public override bool CanToggle => true;

	protected override void OnToggled()
	{
		_targetRoot.SetActive(IsEnabled);
	}
}
