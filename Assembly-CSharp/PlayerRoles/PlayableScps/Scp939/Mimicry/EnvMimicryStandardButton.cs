using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry;

[RequireComponent(typeof(Button))]
public class EnvMimicryStandardButton : MonoBehaviour
{
	[SerializeField]
	private EnvMimicrySequence[] _randomSequences;

	private bool _prevState;

	private Button _button;

	private GameObject _buttonGameObject;

	private bool _cacheSet;

	private EnvironmentalMimicry _cachedSubroutine;

	protected virtual bool IsAvailable => true;

	protected virtual void Awake()
	{
		this._button = base.GetComponent<Button>();
		this._button.onClick.AddListener(OnButtonPressed);
		this._buttonGameObject = this._button.gameObject;
		this._prevState = !this.IsAvailable;
		StaticUnityMethods.OnUpdate += AlwaysUpdate;
	}

	protected virtual void OnDestroy()
	{
		StaticUnityMethods.OnUpdate -= AlwaysUpdate;
	}

	protected virtual void AlwaysUpdate()
	{
		if (this._prevState != this.IsAvailable)
		{
			bool flag = !this._prevState;
			this._buttonGameObject.SetActive(flag);
			this._prevState = flag;
		}
	}

	protected virtual void OnButtonPressed()
	{
		if (this.TryGetLocalSubroutine(out var localSubroutine))
		{
			localSubroutine.ClientSelect(this._randomSequences.RandomItem());
		}
	}

	private bool TryGetLocalSubroutine(out EnvironmentalMimicry localSubroutine)
	{
		localSubroutine = this._cachedSubroutine;
		if (this._cacheSet)
		{
			return this._cachedSubroutine != null;
		}
		if (!ReferenceHub.TryGetLocalHub(out var hub))
		{
			return false;
		}
		if (!(hub.roleManager.CurrentRole is Scp939Role scp939Role))
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
}
