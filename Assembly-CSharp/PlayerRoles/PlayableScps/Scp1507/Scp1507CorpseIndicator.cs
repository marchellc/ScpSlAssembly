using System;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.PlayableScps.Scp1507
{
	public class Scp1507CorpseIndicator : MonoBehaviour
	{
		private void Update()
		{
			this._fillAmount = Mathf.MoveTowards(this._fillAmount, this.Ragdoll.RevivalProgress, Time.deltaTime * 0.5f);
			this._imageFill.fillAmount = this._fillAmount;
		}

		[SerializeField]
		private Image _imageFill;

		private float _fillAmount;

		public Scp1507Ragdoll Ragdoll;

		private const float FillSpeed = 0.5f;
	}
}
