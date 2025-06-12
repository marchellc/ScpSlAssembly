using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Map;

public class Scp079DetectedPlayerIndicator : MonoBehaviour
{
	private ReferenceHub _trackedPlayer;

	private Transform _rotTr;

	private PlayerRoleBase _role;

	private RectTransform _rt;

	private IZoneMap[] _maps;

	private float _mainTimer;

	[SerializeField]
	private CanvasGroup _mainRoot;

	[SerializeField]
	private CanvasGroup _lostRoot;

	[SerializeField]
	private UiCircle _rippleCircle;

	[SerializeField]
	private AnimationCurve _mainFadeAnim;

	[SerializeField]
	private AnimationCurve _lostFadeAnim;

	[SerializeField]
	private AnimationCurve _rippleRadius;

	[SerializeField]
	private AnimationCurve _rippleWidth;

	[SerializeField]
	private int _mainRepeats;

	[SerializeField]
	private float _deleteTime;

	public void Setup(ReferenceHub ply, IZoneMap[] maps, RectTransform rotationTransform)
	{
		this._trackedPlayer = ply;
		this._rotTr = rotationTransform;
		this._role = ply.roleManager.CurrentRole;
		this._rt = base.GetComponent<RectTransform>();
		this._maps = maps;
	}

	private void Update()
	{
		if (!this.UpdateMain())
		{
			this.UpdateLost();
		}
	}

	private bool UpdateMain()
	{
		if (this._mainTimer >= (float)this._mainRepeats)
		{
			return false;
		}
		this._mainTimer = Mathf.Min(this._mainTimer + Time.deltaTime, this._mainRepeats);
		if (this._trackedPlayer == null || this._role != this._trackedPlayer.roleManager.CurrentRole)
		{
			return false;
		}
		float time = this._mainTimer - (float)(int)this._mainTimer;
		this._mainRoot.alpha = this._mainFadeAnim.Evaluate(time);
		this._rippleCircle.Width = this._rippleWidth.Evaluate(time);
		this._rippleCircle.Radius = this._rippleRadius.Evaluate(time);
		this._maps.ForEach(delegate(IZoneMap x)
		{
			x.TrySetPlayerIndicator(this._trackedPlayer, this._rt, exact: true);
		});
		this._rt.rotation = this._rotTr.rotation;
		return true;
	}

	private void UpdateLost()
	{
		this._mainRoot.alpha = 0f;
		this._lostRoot.alpha = this._lostFadeAnim.Evaluate(this._deleteTime);
		this._deleteTime -= Time.deltaTime;
		if (!(this._deleteTime > 0f))
		{
			Object.Destroy(base.gameObject);
		}
	}
}
