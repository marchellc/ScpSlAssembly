using System;
using System.Text;
using NorthwoodLib.Pools;
using PlayerRoles.PlayableScps.Scp079.GUI;
using TMPro;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp079.Map
{
	public class Scp079TargetCounter : Scp079GuiElementBase
	{
		internal override void Init(Scp079Role role, ReferenceHub owner)
		{
			base.Init(role, owner);
			this._isDirty = true;
			PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
			role.SubroutineModule.TryGetSubroutine<Scp079TierManager>(out this._tier);
		}

		private void OnDestroy()
		{
			PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
		}

		private void Update()
		{
			if (!Scp079ToggleMenuAbilityBase<Scp079MapToggler>.IsOpen || !this._isDirty)
			{
				return;
			}
			int num = Mathf.Clamp(this._tier.AccessTierIndex, 0, this._countersForTier.Length - 1);
			this._counterTxt.text = this._countersForTier[num].Text;
		}

		private void OnRoleChanged(ReferenceHub hub, PlayerRoleBase oldRole, PlayerRoleBase newRole)
		{
			this._isDirty = true;
		}

		[SerializeField]
		private Scp079TargetCounter.CounterSet[] _countersForTier;

		[SerializeField]
		private TextMeshProUGUI _counterTxt;

		private bool _isDirty;

		private Scp079TierManager _tier;

		[Serializable]
		private struct CounterSet
		{
			public string Text
			{
				get
				{
					StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
					foreach (Scp079TargetCounter.TargetCounter targetCounter in this._allCounters)
					{
						stringBuilder.Append(targetCounter.Header);
						stringBuilder.Append(": ");
						stringBuilder.Append(ReferenceHub.AllHubs.Count(new Func<ReferenceHub, bool>(targetCounter.Check)));
						stringBuilder.Append("\n");
					}
					return StringBuilderPool.Shared.ToStringReturn(stringBuilder);
				}
			}

			[SerializeField]
			private Scp079TargetCounter.TargetCounter[] _allCounters;
		}

		[Serializable]
		private struct TargetCounter
		{
			public string Header
			{
				get
				{
					return TranslationReader.Get(this._translationKey, this._translationIndex, this._defaultValue);
				}
			}

			public bool Check(ReferenceHub hub)
			{
				return this._teams.Contains(hub.GetTeam());
			}

			[SerializeField]
			private string _defaultValue;

			[SerializeField]
			private string _translationKey;

			[SerializeField]
			private int _translationIndex;

			[SerializeField]
			private Team[] _teams;
		}
	}
}
