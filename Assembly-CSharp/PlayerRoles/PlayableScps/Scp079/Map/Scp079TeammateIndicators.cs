using System;
using System.Collections.Generic;
using PlayerRoles.PlayableScps.Scp079.GUI;
using PlayerRoles.Subroutines;
using TMPro;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp079.Map
{
	public class Scp079TeammateIndicators : Scp079GuiElementBase
	{
		private Scp079TeammateIndicators.IndicatorType CurType
		{
			get
			{
				int accessTierLevel = this._tierManager.AccessTierLevel;
				if (accessTierLevel <= 2)
				{
					return Scp079TeammateIndicators.IndicatorType.Low;
				}
				if (accessTierLevel == 3)
				{
					return Scp079TeammateIndicators.IndicatorType.Medium;
				}
				return Scp079TeammateIndicators.IndicatorType.High;
			}
		}

		internal override void Init(Scp079Role role, ReferenceHub owner)
		{
			base.Init(role, owner);
			role.SubroutineModule.TryGetSubroutine<Scp079TierManager>(out this._tierManager);
			Scp079TierManager tierManager = this._tierManager;
			tierManager.OnLevelledUp = (Action)Delegate.Combine(tierManager.OnLevelledUp, new Action(this.Rebuild));
			PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(this.OnPlayerRemoved));
			this._maps = base.GetComponentsInChildren<IZoneMap>(true);
			this.Rebuild();
		}

		private void OnDestroy()
		{
			Scp079TierManager tierManager = this._tierManager;
			tierManager.OnLevelledUp = (Action)Delegate.Remove(tierManager.OnLevelledUp, new Action(this.Rebuild));
			PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Remove(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(this.OnPlayerRemoved));
		}

		private void OnRoleChanged(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			this.RefreshPlayerIndicator(hub);
		}

		private void OnPlayerRemoved(ReferenceHub hub)
		{
			this.RemovePlayerIndicator(hub);
		}

		private void RemovePlayerIndicator(ReferenceHub hub)
		{
			RectTransform rectTransform;
			if (this._instances.Remove(hub, out rectTransform))
			{
				global::UnityEngine.Object.Destroy(rectTransform.gameObject);
			}
		}

		private void RefreshPlayerIndicator(ReferenceHub hub)
		{
			this.RemovePlayerIndicator(hub);
			this.SetupPlayer(hub);
		}

		private void SetupPlayer(ReferenceHub hub)
		{
			if (!hub.IsSCP(false))
			{
				return;
			}
			RectTransform rectTransform;
			switch (this.CurType)
			{
			case Scp079TeammateIndicators.IndicatorType.Low:
				rectTransform = global::UnityEngine.Object.Instantiate<RectTransform>(this._templateLow);
				break;
			case Scp079TeammateIndicators.IndicatorType.Medium:
				rectTransform = global::UnityEngine.Object.Instantiate<RectTransform>(this._templateMid);
				break;
			case Scp079TeammateIndicators.IndicatorType.High:
			{
				rectTransform = global::UnityEngine.Object.Instantiate<RectTransform>(this._templateHigh);
				string text = hub.roleManager.CurrentRole.RoleTypeId.ToString();
				if (text.Length > 3)
				{
					rectTransform.GetComponentInChildren<TextMeshProUGUI>().text = text.Substring(3);
				}
				break;
			}
			default:
				return;
			}
			rectTransform.localScale = Vector3.one;
			this._instances[hub] = rectTransform;
		}

		private void Rebuild()
		{
			this._instances.ForEachValue(delegate(RectTransform x)
			{
				global::UnityEngine.Object.Destroy(x.gameObject);
			});
			this._instances.Clear();
			ReferenceHub.AllHubs.ForEach(new Action<ReferenceHub>(this.SetupPlayer));
		}

		private void Update()
		{
			if (!Scp079ToggleMenuAbilityBase<Scp079MapToggler>.IsOpen)
			{
				return;
			}
			Scp079TeammateIndicators.IndicatorType curType = this.CurType;
			if (curType == Scp079TeammateIndicators.IndicatorType.Low)
			{
				if (!this._nonExactCooldown.IsReady)
				{
					return;
				}
				this._nonExactCooldown.Trigger(1.5);
			}
			foreach (KeyValuePair<ReferenceHub, RectTransform> keyValuePair in this._instances)
			{
				bool flag = false;
				IZoneMap[] maps = this._maps;
				for (int i = 0; i < maps.Length; i++)
				{
					if (maps[i].TrySetPlayerIndicator(keyValuePair.Key, keyValuePair.Value, curType > Scp079TeammateIndicators.IndicatorType.Low))
					{
						flag = true;
					}
				}
				keyValuePair.Value.gameObject.SetActive(flag);
				if (flag && curType == Scp079TeammateIndicators.IndicatorType.High)
				{
					keyValuePair.Value.GetComponentInChildren<TextMeshProUGUI>().rectTransform.rotation = this._templateHigh.rotation;
				}
			}
		}

		private const float NonExactCooldown = 1.5f;

		private Scp079TierManager _tierManager;

		private IZoneMap[] _maps;

		private readonly AbilityCooldown _nonExactCooldown = new AbilityCooldown();

		private readonly Dictionary<ReferenceHub, RectTransform> _instances = new Dictionary<ReferenceHub, RectTransform>();

		[SerializeField]
		private RectTransform _templateLow;

		[SerializeField]
		private RectTransform _templateMid;

		[SerializeField]
		private RectTransform _templateHigh;

		private enum IndicatorType
		{
			Low,
			Medium,
			High
		}
	}
}
