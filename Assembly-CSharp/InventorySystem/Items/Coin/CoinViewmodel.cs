using System;
using System.Diagnostics;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Coin
{
	public class CoinViewmodel : StandardAnimatedViemodel
	{
		public override void InitAny()
		{
			base.InitAny();
			Coin.OnFlipped += this.ProcessCoinflip;
		}

		public override void InitSpectator(ReferenceHub ply, ItemIdentifier id, bool wasEquipped)
		{
			base.InitSpectator(ply, id, wasEquipped);
			if (!wasEquipped)
			{
				return;
			}
			base.GetComponent<AudioSource>().Stop();
			bool flag;
			if (this.TryGetMessage(id.SerialNumber, out flag))
			{
				this.ProcessCoinflip(id.SerialNumber, flag);
				this.AnimatorForceUpdate(base.SkipEquipTime, false);
				return;
			}
			this.AnimatorForceUpdate(base.SkipEquipTime, true);
		}

		protected override void LateUpdate()
		{
			base.LateUpdate();
			float num = this._positionOverrideOverTime.Evaluate((float)this._animStopwatch.Elapsed.TotalSeconds);
			if (num <= 0f)
			{
				return;
			}
			this._coinTr.position = Vector3.Lerp(this._coinTr.position, this._coinOverrideTr.position, num);
		}

		private bool TryGetMessage(ushort serial, out bool isTails)
		{
			double num;
			bool flag = Coin.FlipTimes.TryGetValue(serial, out num);
			if (num < 0.0)
			{
				isTails = true;
				num += NetworkTime.time;
			}
			else
			{
				isTails = false;
				num = NetworkTime.time - num;
			}
			return flag && num < 3.9000000953674316;
		}

		private void ProcessCoinflip(ushort serial, bool isTails)
		{
			if (serial != base.ItemId.SerialNumber)
			{
				return;
			}
			this.AnimatorSetBool(CoinViewmodel.TailsHash, isTails);
			this.AnimatorSetTrigger(CoinViewmodel.TriggerHash);
			this._animStopwatch.Restart();
		}

		private void OnDisable()
		{
			this._animStopwatch.Reset();
		}

		private void OnDestroy()
		{
			Coin.OnFlipped -= this.ProcessCoinflip;
		}

		private static readonly int TriggerHash = Animator.StringToHash("Flip");

		private static readonly int TailsHash = Animator.StringToHash("IsTails");

		private readonly Stopwatch _animStopwatch = new Stopwatch();

		private const float MessageVitality = 3.9f;

		[SerializeField]
		private AnimationCurve _positionOverrideOverTime;

		[SerializeField]
		private Transform _coinTr;

		[SerializeField]
		private Transform _coinOverrideTr;
	}
}
