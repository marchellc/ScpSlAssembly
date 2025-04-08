using System;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Map
{
	public class Scp079DetectedPlayerIndicator : MonoBehaviour
	{
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
			if (this.UpdateMain())
			{
				return;
			}
			this.UpdateLost();
		}

		private bool UpdateMain()
		{
			if (this._mainTimer >= (float)this._mainRepeats)
			{
				return false;
			}
			this._mainTimer = Mathf.Min(this._mainTimer + Time.deltaTime, (float)this._mainRepeats);
			if (this._trackedPlayer == null || this._role != this._trackedPlayer.roleManager.CurrentRole)
			{
				return false;
			}
			float num = this._mainTimer - (float)((int)this._mainTimer);
			this._mainRoot.alpha = this._mainFadeAnim.Evaluate(num);
			this._rippleCircle.Width = this._rippleWidth.Evaluate(num);
			this._rippleCircle.Radius = this._rippleRadius.Evaluate(num);
			this._maps.ForEach(delegate(IZoneMap x)
			{
				x.TrySetPlayerIndicator(this._trackedPlayer, this._rt, true);
			});
			this._rt.rotation = this._rotTr.rotation;
			return true;
		}

		private void UpdateLost()
		{
			this._mainRoot.alpha = 0f;
			this._lostRoot.alpha = this._lostFadeAnim.Evaluate(this._deleteTime);
			this._deleteTime -= Time.deltaTime;
			if (this._deleteTime > 0f)
			{
				return;
			}
			global::UnityEngine.Object.Destroy(base.gameObject);
		}

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
	}
}
