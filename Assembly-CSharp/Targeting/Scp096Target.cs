using System;
using UnityEngine;

namespace Targeting
{
	public class Scp096Target : TargetComponent
	{
		public override bool IsTarget
		{
			get
			{
				return this._isTarget;
			}
			set
			{
				this._targetParticles.SetActive(value);
				this._isTarget = value;
			}
		}

		private void Start()
		{
			this.IsTarget = false;
		}

		[SerializeField]
		private GameObject _targetParticles;

		private bool _isTarget;
	}
}
