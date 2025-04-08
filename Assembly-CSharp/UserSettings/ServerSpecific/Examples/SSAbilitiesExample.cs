using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using InventorySystem;
using InventorySystem.Items;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using UnityEngine;

namespace UserSettings.ServerSpecific.Examples
{
	public class SSAbilitiesExample : SSExampleImplementationBase
	{
		public override string Name
		{
			get
			{
				return "Abilities Extension";
			}
		}

		public override void Activate()
		{
			SSAbilitiesExample._activeSpeedBoosts = new HashSet<ReferenceHub>();
			ServerSpecificSettingsSync.DefinedSettings = new ServerSpecificSettingBase[]
			{
				new SSGroupHeader("Abilities", false, null),
				new SSKeybindSetting(new int?(2), "Heal Ally", KeyCode.H, true, string.Format("Press this key while holding a medkit to instantly heal a stationary ally for {0} HP.", 50f)),
				new SSKeybindSetting(new int?(0), "Speed Boost (Human-only)", KeyCode.Y, true, "Increase your speed by draining your health."),
				new SSTwoButtonsSetting(new int?(1), "Speed Boost - Activation Mode", "Hold", "Toggle", false, null)
			};
			ServerSpecificSettingsSync.SendToAll();
			ServerSpecificSettingsSync.ServerOnSettingValueReceived += this.ProcessUserInput;
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(this.OnPlayerDisconnected));
			PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
			StaticUnityMethods.OnUpdate += this.OnUpdate;
		}

		public override void Deactivate()
		{
			ServerSpecificSettingsSync.ServerOnSettingValueReceived -= this.ProcessUserInput;
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Remove(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(this.OnPlayerDisconnected));
			PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
			StaticUnityMethods.OnUpdate -= this.OnUpdate;
		}

		private void ProcessUserInput(ReferenceHub sender, ServerSpecificSettingBase setting)
		{
			switch (setting.SettingId)
			{
			case 0:
			{
				SSKeybindSetting sskeybindSetting = setting as SSKeybindSetting;
				if (sskeybindSetting != null)
				{
					if (!ServerSpecificSettingsSync.GetSettingOfUser<SSTwoButtonsSetting>(sender, 1).SyncIsB)
					{
						this.SetSpeedBoost(sender, sskeybindSetting.SyncIsPressed);
						return;
					}
					if (sskeybindSetting.SyncIsPressed)
					{
						this.SetSpeedBoost(sender, !SSAbilitiesExample._activeSpeedBoosts.Contains(sender));
						return;
					}
				}
				break;
			}
			case 1:
				this.SetSpeedBoost(sender, false);
				break;
			case 2:
			{
				SSKeybindSetting sskeybindSetting2 = setting as SSKeybindSetting;
				if (sskeybindSetting2 != null && sskeybindSetting2.SyncIsPressed)
				{
					this.TryHealAlly(sender);
					return;
				}
				break;
			}
			default:
				return;
			}
		}

		private void TryHealAlly(ReferenceHub sender)
		{
			ItemIdentifier curItem = sender.inventory.CurItem;
			if (curItem.TypeId != ItemType.Medkit)
			{
				return;
			}
			Vector3 vector = sender.PlayerCameraReference.position;
			Vector3 forward = sender.PlayerCameraReference.forward;
			RaycastHit raycastHit;
			while (Physics.Raycast(vector, forward, out raycastHit, 3.5f))
			{
				HitboxIdentity hitboxIdentity;
				if (!raycastHit.collider.TryGetComponent<HitboxIdentity>(out hitboxIdentity))
				{
					return;
				}
				if (HitboxIdentity.IsEnemy(hitboxIdentity.TargetHub, sender))
				{
					return;
				}
				if (!(hitboxIdentity.TargetHub == sender))
				{
					ReferenceHub targetHub = hitboxIdentity.TargetHub;
					targetHub.playerStats.GetModule<HealthStat>().ServerHeal(50f);
					sender.inventory.ServerRemoveItem(curItem.SerialNumber, null);
					return;
				}
				vector += forward * 0.08f;
			}
		}

		private void SetSpeedBoost(ReferenceHub hub, bool state)
		{
			MovementBoost effect = hub.playerEffectsController.GetEffect<MovementBoost>();
			if (state && hub.IsHuman())
			{
				effect.ServerSetState(60, 0f, false);
				SSAbilitiesExample._activeSpeedBoosts.Add(hub);
				return;
			}
			effect.ServerDisable();
			SSAbilitiesExample._activeSpeedBoosts.Remove(hub);
		}

		private void OnPlayerDisconnected(ReferenceHub hub)
		{
			SSAbilitiesExample._activeSpeedBoosts.Remove(hub);
		}

		private void OnRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			this.SetSpeedBoost(userHub, false);
		}

		private void OnUpdate()
		{
			if (!StaticUnityMethods.IsPlaying)
			{
				return;
			}
			foreach (ReferenceHub referenceHub in SSAbilitiesExample._activeSpeedBoosts)
			{
				if (!Mathf.Approximately(referenceHub.GetVelocity().SqrMagnitudeIgnoreY(), 0f))
				{
					referenceHub.playerStats.DealDamage(new UniversalDamageHandler(Time.deltaTime * 5f, DeathTranslations.Scp207, null));
				}
			}
		}

		private const float HealAllyHp = 50f;

		private const float HealAllyRange = 3.5f;

		private const byte BoostIntensity = 60;

		private const float BoostHealthDrain = 5f;

		private static HashSet<ReferenceHub> _activeSpeedBoosts;

		private enum ExampleId
		{
			SpeedBoostKey,
			SpeedBoostToggle,
			HealAlly
		}
	}
}
