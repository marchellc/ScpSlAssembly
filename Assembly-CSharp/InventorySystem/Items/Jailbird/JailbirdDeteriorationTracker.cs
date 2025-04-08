using System;
using System.Collections.Generic;
using InventorySystem.Items.Autosync;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Jailbird
{
	[Serializable]
	public class JailbirdDeteriorationTracker
	{
		public static float Scp914CoarseDamage { get; private set; }

		public static int Scp914CoarseCharges { get; private set; }

		public JailbirdWearState WearState
		{
			get
			{
				JailbirdWearState jailbirdWearState;
				if (!JailbirdDeteriorationTracker.ReceivedStates.TryGetValue(this._jailbird.ItemSerial, out jailbirdWearState))
				{
					return JailbirdWearState.Healthy;
				}
				return jailbirdWearState;
			}
		}

		public bool IsBroken
		{
			get
			{
				return this.WearState == JailbirdWearState.Broken;
			}
		}

		public void Setup(JailbirdItem item, JailbirdHitreg hitreg)
		{
			this._jailbird = item;
			this._hitreg = hitreg;
			if (JailbirdDeteriorationTracker._914ValuesSet)
			{
				return;
			}
			for (int i = 0; i < this._damageToWearState.length; i++)
			{
				Keyframe keyframe = this._damageToWearState.keys[i];
				if (this.FloatToState(keyframe.value) == JailbirdWearState.AlmostBroken)
				{
					JailbirdDeteriorationTracker.Scp914CoarseDamage = keyframe.time;
					break;
				}
			}
			for (int j = 0; j < this._chargesToWearState.length; j++)
			{
				Keyframe keyframe2 = this._chargesToWearState.keys[j];
				if (this.FloatToState(keyframe2.value) == JailbirdWearState.AlmostBroken)
				{
					JailbirdDeteriorationTracker.Scp914CoarseCharges = Mathf.RoundToInt(keyframe2.time);
					break;
				}
			}
			JailbirdDeteriorationTracker._914ValuesSet = true;
		}

		private JailbirdWearState FloatToState(float stateFloat)
		{
			return (JailbirdWearState)Mathf.Clamp((int)stateFloat, 0, 5);
		}

		private JailbirdWearState StateForTotalDamage(float totalDamage)
		{
			return this.FloatToState(this._damageToWearState.Evaluate(totalDamage));
		}

		private JailbirdWearState StateForCharges(int numOfCharges)
		{
			return this.FloatToState(this._chargesToWearState.Evaluate((float)numOfCharges));
		}

		public void RecheckUsage()
		{
			JailbirdWearState jailbirdWearState = this.StateForTotalDamage(this._hitreg.TotalMeleeDamageDealt);
			JailbirdWearState jailbirdWearState2 = this.StateForCharges(this._jailbird.TotalChargesPerformed);
			JailbirdWearState jailbirdWearState3 = ((jailbirdWearState > jailbirdWearState2) ? jailbirdWearState : jailbirdWearState2);
			JailbirdDeteriorationTracker.ReceivedStates[this._jailbird.ItemSerial] = jailbirdWearState3;
			NetworkWriter networkWriter;
			using (new AutosyncRpc(this._jailbird.ItemId, out networkWriter))
			{
				networkWriter.WriteByte(0);
				networkWriter.WriteByte((byte)jailbirdWearState3);
			}
			if (jailbirdWearState3 != JailbirdWearState.Broken)
			{
				return;
			}
			NetworkWriter networkWriter2;
			using (new AutosyncRpc(this._jailbird.ItemId, out networkWriter2))
			{
				networkWriter2.WriteByte(1);
			}
		}

		public static void ReadUsage(ushort serial, NetworkReader reader)
		{
			JailbirdDeteriorationTracker.ReceivedStates[serial] = (JailbirdWearState)reader.ReadByte();
			JailbirdDeteriorationTracker._anyReceived = true;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				if (!JailbirdDeteriorationTracker._anyReceived)
				{
					return;
				}
				JailbirdDeteriorationTracker._anyReceived = false;
				JailbirdDeteriorationTracker.ReceivedStates.Clear();
			};
		}

		public static Dictionary<ushort, JailbirdWearState> ReceivedStates = new Dictionary<ushort, JailbirdWearState>();

		private static bool _anyReceived;

		private static bool _914ValuesSet;

		private JailbirdItem _jailbird;

		private JailbirdHitreg _hitreg;

		[SerializeField]
		private AnimationCurve _damageToWearState;

		[SerializeField]
		private AnimationCurve _chargesToWearState;
	}
}
