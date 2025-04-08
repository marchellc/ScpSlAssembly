using System;
using MapGeneration.Distributors;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Ripples
{
	public class GeneratorRippleTrigger : RippleTriggerBase
	{
		public override void SpawnObject()
		{
			base.SpawnObject();
			Scp079Generator.OnCount += this.OnCount;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			Scp079Generator.OnCount -= this.OnCount;
		}

		private void OnCount(Scp079Generator generator)
		{
			if (!base.IsLocalOrSpectated)
			{
				return;
			}
			base.PlayInRangeSqr(generator.transform.position + this._offset, 100f, Color.red);
		}

		private readonly Vector3 _offset = Vector3.up * 1.5f;

		private const float RangeSqr = 100f;
	}
}
