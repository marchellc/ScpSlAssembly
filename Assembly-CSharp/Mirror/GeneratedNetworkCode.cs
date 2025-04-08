using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Achievements;
using AdminToys;
using CentralAuth;
using CommandSystem.Commands.RemoteAdmin.Cleanup;
using CommandSystem.Commands.RemoteAdmin.Stripdown;
using CustomPlayerEffects;
using Hints;
using Interactables.Interobjects;
using InventorySystem.Disarming;
using InventorySystem.Items;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Ammo;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.BasicMessages;
using InventorySystem.Items.Firearms.Modules.Misc;
using InventorySystem.Items.Jailbird;
using InventorySystem.Items.Keycards;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Radio;
using InventorySystem.Items.ThrowableProjectiles;
using InventorySystem.Items.ToggleableLights;
using InventorySystem.Items.Usables;
using InventorySystem.Items.Usables.Scp1344;
using InventorySystem.Items.Usables.Scp1576;
using InventorySystem.Items.Usables.Scp244.Hypothermia;
using InventorySystem.Items.Usables.Scp330;
using InventorySystem.Searching;
using LightContainmentZoneDecontamination;
using MapGeneration;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.NetworkMessages;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.PlayableScps.Scp049.Zombies;
using PlayerRoles.PlayableScps.Scp106;
using PlayerRoles.Ragdolls;
using PlayerRoles.RoleAssign;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using PlayerRoles.Voice;
using PlayerStatsSystem;
using RelativePositioning;
using RemoteAdmin;
using Respawning;
using Respawning.NamingRules;
using Respawning.Waves;
using RoundRestarting;
using Scp914;
using Subtitles;
using UnityEngine;
using UserSettings.ServerSpecific;
using Utils;
using Utils.Networking;
using VoiceChat;
using VoiceChat.Networking;
using VoiceChat.Playbacks;

namespace Mirror
{
	[StructLayout(LayoutKind.Auto, CharSet = CharSet.Auto)]
	public static class GeneratedNetworkCode
	{
		public static TimeSnapshotMessage _Read_Mirror.TimeSnapshotMessage(NetworkReader reader)
		{
			return default(TimeSnapshotMessage);
		}

		public static void _Write_Mirror.TimeSnapshotMessage(NetworkWriter writer, TimeSnapshotMessage value)
		{
		}

		public static ReadyMessage _Read_Mirror.ReadyMessage(NetworkReader reader)
		{
			return default(ReadyMessage);
		}

		public static void _Write_Mirror.ReadyMessage(NetworkWriter writer, ReadyMessage value)
		{
		}

		public static NotReadyMessage _Read_Mirror.NotReadyMessage(NetworkReader reader)
		{
			return default(NotReadyMessage);
		}

		public static void _Write_Mirror.NotReadyMessage(NetworkWriter writer, NotReadyMessage value)
		{
		}

		public static AddPlayerMessage _Read_Mirror.AddPlayerMessage(NetworkReader reader)
		{
			return default(AddPlayerMessage);
		}

		public static void _Write_Mirror.AddPlayerMessage(NetworkWriter writer, AddPlayerMessage value)
		{
		}

		public static SceneMessage _Read_Mirror.SceneMessage(NetworkReader reader)
		{
			return new SceneMessage
			{
				sceneName = reader.ReadString(),
				sceneOperation = GeneratedNetworkCode._Read_Mirror.SceneOperation(reader),
				customHandling = reader.ReadBool()
			};
		}

		public static SceneOperation _Read_Mirror.SceneOperation(NetworkReader reader)
		{
			return (SceneOperation)reader.ReadByte();
		}

		public static void _Write_Mirror.SceneMessage(NetworkWriter writer, SceneMessage value)
		{
			writer.WriteString(value.sceneName);
			GeneratedNetworkCode._Write_Mirror.SceneOperation(writer, value.sceneOperation);
			writer.WriteBool(value.customHandling);
		}

		public static void _Write_Mirror.SceneOperation(NetworkWriter writer, SceneOperation value)
		{
			writer.WriteByte((byte)value);
		}

		public static CommandMessage _Read_Mirror.CommandMessage(NetworkReader reader)
		{
			return new CommandMessage
			{
				netId = reader.ReadUInt(),
				componentIndex = reader.ReadByte(),
				functionHash = reader.ReadUShort(),
				payload = reader.ReadArraySegmentAndSize()
			};
		}

		public static void _Write_Mirror.CommandMessage(NetworkWriter writer, CommandMessage value)
		{
			writer.WriteUInt(value.netId);
			writer.WriteByte(value.componentIndex);
			writer.WriteUShort(value.functionHash);
			writer.WriteArraySegmentAndSize(value.payload);
		}

		public static RpcMessage _Read_Mirror.RpcMessage(NetworkReader reader)
		{
			return new RpcMessage
			{
				netId = reader.ReadUInt(),
				componentIndex = reader.ReadByte(),
				functionHash = reader.ReadUShort(),
				payload = reader.ReadArraySegmentAndSize()
			};
		}

		public static void _Write_Mirror.RpcMessage(NetworkWriter writer, RpcMessage value)
		{
			writer.WriteUInt(value.netId);
			writer.WriteByte(value.componentIndex);
			writer.WriteUShort(value.functionHash);
			writer.WriteArraySegmentAndSize(value.payload);
		}

		public static SpawnMessage _Read_Mirror.SpawnMessage(NetworkReader reader)
		{
			return new SpawnMessage
			{
				netId = reader.ReadUInt(),
				isLocalPlayer = reader.ReadBool(),
				isOwner = reader.ReadBool(),
				sceneId = reader.ReadULong(),
				assetId = reader.ReadUInt(),
				position = reader.ReadVector3(),
				rotation = reader.ReadQuaternion(),
				scale = reader.ReadVector3(),
				payload = reader.ReadArraySegmentAndSize()
			};
		}

		public static void _Write_Mirror.SpawnMessage(NetworkWriter writer, SpawnMessage value)
		{
			writer.WriteUInt(value.netId);
			writer.WriteBool(value.isLocalPlayer);
			writer.WriteBool(value.isOwner);
			writer.WriteULong(value.sceneId);
			writer.WriteUInt(value.assetId);
			writer.WriteVector3(value.position);
			writer.WriteQuaternion(value.rotation);
			writer.WriteVector3(value.scale);
			writer.WriteArraySegmentAndSize(value.payload);
		}

		public static ChangeOwnerMessage _Read_Mirror.ChangeOwnerMessage(NetworkReader reader)
		{
			return new ChangeOwnerMessage
			{
				netId = reader.ReadUInt(),
				isOwner = reader.ReadBool(),
				isLocalPlayer = reader.ReadBool()
			};
		}

		public static void _Write_Mirror.ChangeOwnerMessage(NetworkWriter writer, ChangeOwnerMessage value)
		{
			writer.WriteUInt(value.netId);
			writer.WriteBool(value.isOwner);
			writer.WriteBool(value.isLocalPlayer);
		}

		public static ObjectSpawnStartedMessage _Read_Mirror.ObjectSpawnStartedMessage(NetworkReader reader)
		{
			return default(ObjectSpawnStartedMessage);
		}

		public static void _Write_Mirror.ObjectSpawnStartedMessage(NetworkWriter writer, ObjectSpawnStartedMessage value)
		{
		}

		public static ObjectSpawnFinishedMessage _Read_Mirror.ObjectSpawnFinishedMessage(NetworkReader reader)
		{
			return default(ObjectSpawnFinishedMessage);
		}

		public static void _Write_Mirror.ObjectSpawnFinishedMessage(NetworkWriter writer, ObjectSpawnFinishedMessage value)
		{
		}

		public static ObjectDestroyMessage _Read_Mirror.ObjectDestroyMessage(NetworkReader reader)
		{
			return new ObjectDestroyMessage
			{
				netId = reader.ReadUInt()
			};
		}

		public static void _Write_Mirror.ObjectDestroyMessage(NetworkWriter writer, ObjectDestroyMessage value)
		{
			writer.WriteUInt(value.netId);
		}

		public static ObjectHideMessage _Read_Mirror.ObjectHideMessage(NetworkReader reader)
		{
			return new ObjectHideMessage
			{
				netId = reader.ReadUInt()
			};
		}

		public static void _Write_Mirror.ObjectHideMessage(NetworkWriter writer, ObjectHideMessage value)
		{
			writer.WriteUInt(value.netId);
		}

		public static EntityStateMessage _Read_Mirror.EntityStateMessage(NetworkReader reader)
		{
			return new EntityStateMessage
			{
				netId = reader.ReadUInt(),
				payload = reader.ReadArraySegmentAndSize()
			};
		}

		public static void _Write_Mirror.EntityStateMessage(NetworkWriter writer, EntityStateMessage value)
		{
			writer.WriteUInt(value.netId);
			writer.WriteArraySegmentAndSize(value.payload);
		}

		public static NetworkPingMessage _Read_Mirror.NetworkPingMessage(NetworkReader reader)
		{
			return new NetworkPingMessage
			{
				localTime = reader.ReadDouble()
			};
		}

		public static void _Write_Mirror.NetworkPingMessage(NetworkWriter writer, NetworkPingMessage value)
		{
			writer.WriteDouble(value.localTime);
		}

		public static NetworkPongMessage _Read_Mirror.NetworkPongMessage(NetworkReader reader)
		{
			return new NetworkPongMessage
			{
				localTime = reader.ReadDouble()
			};
		}

		public static void _Write_Mirror.NetworkPongMessage(NetworkWriter writer, NetworkPongMessage value)
		{
			writer.WriteDouble(value.localTime);
		}

		public static Hitmarker.HitmarkerMessage _Read_Hitmarker/HitmarkerMessage(NetworkReader reader)
		{
			return new Hitmarker.HitmarkerMessage
			{
				Size = reader.ReadByte(),
				Audio = reader.ReadBool()
			};
		}

		public static void _Write_Hitmarker/HitmarkerMessage(NetworkWriter writer, Hitmarker.HitmarkerMessage value)
		{
			writer.WriteByte(value.Size);
			writer.WriteBool(value.Audio);
		}

		public static Escape.EscapeMessage _Read_Escape/EscapeMessage(NetworkReader reader)
		{
			return new Escape.EscapeMessage
			{
				ScenarioId = reader.ReadByte(),
				EscapeTime = reader.ReadUShort()
			};
		}

		public static void _Write_Escape/EscapeMessage(NetworkWriter writer, Escape.EscapeMessage value)
		{
			writer.WriteByte(value.ScenarioId);
			writer.WriteUShort(value.EscapeTime);
		}

		public static ServerShutdown.ServerShutdownMessage _Read_ServerShutdown/ServerShutdownMessage(NetworkReader reader)
		{
			return default(ServerShutdown.ServerShutdownMessage);
		}

		public static void _Write_ServerShutdown/ServerShutdownMessage(NetworkWriter writer, ServerShutdown.ServerShutdownMessage value)
		{
		}

		public static VoiceChatMuteIndicator.SyncMuteMessage _Read_VoiceChat.VoiceChatMuteIndicator/SyncMuteMessage(NetworkReader reader)
		{
			return new VoiceChatMuteIndicator.SyncMuteMessage
			{
				Flags = reader.ReadByte()
			};
		}

		public static void _Write_VoiceChat.VoiceChatMuteIndicator/SyncMuteMessage(NetworkWriter writer, VoiceChatMuteIndicator.SyncMuteMessage value)
		{
			writer.WriteByte(value.Flags);
		}

		public static VoiceChatPrivacySettings.VcPrivacyMessage _Read_VoiceChat.VoiceChatPrivacySettings/VcPrivacyMessage(NetworkReader reader)
		{
			return new VoiceChatPrivacySettings.VcPrivacyMessage
			{
				Flags = reader.ReadByte()
			};
		}

		public static void _Write_VoiceChat.VoiceChatPrivacySettings/VcPrivacyMessage(NetworkWriter writer, VoiceChatPrivacySettings.VcPrivacyMessage value)
		{
			writer.WriteByte(value.Flags);
		}

		public static PersonalRadioPlayback.TransmitterPositionMessage _Read_VoiceChat.Playbacks.PersonalRadioPlayback/TransmitterPositionMessage(NetworkReader reader)
		{
			return new PersonalRadioPlayback.TransmitterPositionMessage
			{
				Transmitter = reader.ReadRecyclablePlayerId(),
				WaypointId = reader.ReadByte()
			};
		}

		public static void _Write_VoiceChat.Playbacks.PersonalRadioPlayback/TransmitterPositionMessage(NetworkWriter writer, PersonalRadioPlayback.TransmitterPositionMessage value)
		{
			writer.WriteRecyclablePlayerId(value.Transmitter);
			writer.WriteByte(value.WaypointId);
		}

		public static LockerWaypoint.LockerWaypointAssignMessage _Read_RelativePositioning.LockerWaypoint/LockerWaypointAssignMessage(NetworkReader reader)
		{
			return new LockerWaypoint.LockerWaypointAssignMessage
			{
				LockerNetId = reader.ReadUInt(),
				Chamber = reader.ReadByte(),
				WaypointId = reader.ReadByte()
			};
		}

		public static void _Write_RelativePositioning.LockerWaypoint/LockerWaypointAssignMessage(NetworkWriter writer, LockerWaypoint.LockerWaypointAssignMessage value)
		{
			writer.WriteUInt(value.LockerNetId);
			writer.WriteByte(value.Chamber);
			writer.WriteByte(value.WaypointId);
		}

		public static VoiceChatReceivePrefs.GroupMuteFlagsMessage _Read_PlayerRoles.Voice.VoiceChatReceivePrefs/GroupMuteFlagsMessage(NetworkReader reader)
		{
			return new VoiceChatReceivePrefs.GroupMuteFlagsMessage
			{
				Flags = reader.ReadByte()
			};
		}

		public static void _Write_PlayerRoles.Voice.VoiceChatReceivePrefs/GroupMuteFlagsMessage(NetworkWriter writer, VoiceChatReceivePrefs.GroupMuteFlagsMessage value)
		{
			writer.WriteByte(value.Flags);
		}

		public static OverwatchVoiceChannelSelector.ChannelMuteFlagsMessage _Read_PlayerRoles.Spectating.OverwatchVoiceChannelSelector/ChannelMuteFlagsMessage(NetworkReader reader)
		{
			return new OverwatchVoiceChannelSelector.ChannelMuteFlagsMessage
			{
				SpatialAudio = reader.ReadBool(),
				EnabledChannels = reader.ReadUInt()
			};
		}

		public static void _Write_PlayerRoles.Spectating.OverwatchVoiceChannelSelector/ChannelMuteFlagsMessage(NetworkWriter writer, OverwatchVoiceChannelSelector.ChannelMuteFlagsMessage value)
		{
			writer.WriteBool(value.SpatialAudio);
			writer.WriteUInt(value.EnabledChannels);
		}

		public static SpectatorNetworking.SpectatedNetIdSyncMessage _Read_PlayerRoles.Spectating.SpectatorNetworking/SpectatedNetIdSyncMessage(NetworkReader reader)
		{
			return new SpectatorNetworking.SpectatedNetIdSyncMessage
			{
				NetId = reader.ReadUInt()
			};
		}

		public static void _Write_PlayerRoles.Spectating.SpectatorNetworking/SpectatedNetIdSyncMessage(NetworkWriter writer, SpectatorNetworking.SpectatedNetIdSyncMessage value)
		{
			writer.WriteUInt(value.NetId);
		}

		public static Scp106PocketItemManager.WarningMessage _Read_PlayerRoles.PlayableScps.Scp106.Scp106PocketItemManager/WarningMessage(NetworkReader reader)
		{
			return new Scp106PocketItemManager.WarningMessage
			{
				Position = reader.ReadRelativePosition()
			};
		}

		public static void _Write_PlayerRoles.PlayableScps.Scp106.Scp106PocketItemManager/WarningMessage(NetworkWriter writer, Scp106PocketItemManager.WarningMessage value)
		{
			writer.WriteRelativePosition(value.Position);
		}

		public static ZombieConfirmationBox.ScpReviveBlockMessage _Read_PlayerRoles.PlayableScps.Scp049.Zombies.ZombieConfirmationBox/ScpReviveBlockMessage(NetworkReader reader)
		{
			return default(ZombieConfirmationBox.ScpReviveBlockMessage);
		}

		public static void _Write_PlayerRoles.PlayableScps.Scp049.Zombies.ZombieConfirmationBox/ScpReviveBlockMessage(NetworkWriter writer, ZombieConfirmationBox.ScpReviveBlockMessage value)
		{
		}

		public static DynamicHumeShieldController.ShieldBreakMessage _Read_PlayerRoles.PlayableScps.HumeShield.DynamicHumeShieldController/ShieldBreakMessage(NetworkReader reader)
		{
			return new DynamicHumeShieldController.ShieldBreakMessage
			{
				Target = reader.ReadReferenceHub()
			};
		}

		public static void _Write_PlayerRoles.PlayableScps.HumeShield.DynamicHumeShieldController/ShieldBreakMessage(NetworkWriter writer, DynamicHumeShieldController.ShieldBreakMessage value)
		{
			writer.WriteReferenceHub(value.Target);
		}

		public static FpcRotationOverrideMessage _Read_PlayerRoles.FirstPersonControl.NetworkMessages.FpcRotationOverrideMessage(NetworkReader reader)
		{
			return new FpcRotationOverrideMessage
			{
				Rotation = reader.ReadVector2()
			};
		}

		public static void _Write_PlayerRoles.FirstPersonControl.NetworkMessages.FpcRotationOverrideMessage(NetworkWriter writer, FpcRotationOverrideMessage value)
		{
			writer.WriteVector2(value.Rotation);
		}

		public static FpcNoclipToggleMessage _Read_PlayerRoles.FirstPersonControl.NetworkMessages.FpcNoclipToggleMessage(NetworkReader reader)
		{
			return default(FpcNoclipToggleMessage);
		}

		public static void _Write_PlayerRoles.FirstPersonControl.NetworkMessages.FpcNoclipToggleMessage(NetworkWriter writer, FpcNoclipToggleMessage value)
		{
		}

		public static EmotionSync.EmotionSyncMessage _Read_PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.EmotionSync/EmotionSyncMessage(NetworkReader reader)
		{
			return new EmotionSync.EmotionSyncMessage
			{
				HubNetId = reader.ReadUInt(),
				Data = GeneratedNetworkCode._Read_PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.EmotionPresetType(reader)
			};
		}

		public static EmotionPresetType _Read_PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.EmotionPresetType(NetworkReader reader)
		{
			return (EmotionPresetType)reader.ReadByte();
		}

		public static void _Write_PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.EmotionSync/EmotionSyncMessage(NetworkWriter writer, EmotionSync.EmotionSyncMessage value)
		{
			writer.WriteUInt(value.HubNetId);
			GeneratedNetworkCode._Write_PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.EmotionPresetType(writer, value.Data);
		}

		public static void _Write_PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.EmotionPresetType(NetworkWriter writer, EmotionPresetType value)
		{
			writer.WriteByte((byte)value);
		}

		public static WearableSync.WearableSyncMessage _Read_PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.WearableSync/WearableSyncMessage(NetworkReader reader)
		{
			return new WearableSync.WearableSyncMessage
			{
				HubNetId = reader.ReadUInt(),
				Data = GeneratedNetworkCode._Read_PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.WearableElements(reader)
			};
		}

		public static WearableElements _Read_PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.WearableElements(NetworkReader reader)
		{
			return (WearableElements)reader.ReadByte();
		}

		public static void _Write_PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.WearableSync/WearableSyncMessage(NetworkWriter writer, WearableSync.WearableSyncMessage value)
		{
			writer.WriteUInt(value.HubNetId);
			GeneratedNetworkCode._Write_PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.WearableElements(writer, value.Data);
		}

		public static void _Write_PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.WearableElements(NetworkWriter writer, WearableElements value)
		{
			writer.WriteByte((byte)value);
		}

		public static ExplosionUtils.GrenadeExplosionMessage _Read_Utils.ExplosionUtils/GrenadeExplosionMessage(NetworkReader reader)
		{
			return new ExplosionUtils.GrenadeExplosionMessage
			{
				GrenadeType = reader.ReadByte(),
				Pos = reader.ReadRelativePosition()
			};
		}

		public static void _Write_Utils.ExplosionUtils/GrenadeExplosionMessage(NetworkWriter writer, ExplosionUtils.GrenadeExplosionMessage value)
		{
			writer.WriteByte(value.GrenadeType);
			writer.WriteRelativePosition(value.Pos);
		}

		public static SeedSynchronizer.SeedMessage _Read_MapGeneration.SeedSynchronizer/SeedMessage(NetworkReader reader)
		{
			return new SeedSynchronizer.SeedMessage
			{
				Value = reader.ReadInt()
			};
		}

		public static void _Write_MapGeneration.SeedSynchronizer/SeedMessage(NetworkWriter writer, SeedSynchronizer.SeedMessage value)
		{
			writer.WriteInt(value.Value);
		}

		public static global::CustomPlayerEffects.AntiScp207.BreakMessage _Read_CustomPlayerEffects.AntiScp207/BreakMessage(NetworkReader reader)
		{
			return new global::CustomPlayerEffects.AntiScp207.BreakMessage
			{
				SoundPos = reader.ReadVector3()
			};
		}

		public static void _Write_CustomPlayerEffects.AntiScp207/BreakMessage(NetworkWriter writer, global::CustomPlayerEffects.AntiScp207.BreakMessage value)
		{
			writer.WriteVector3(value.SoundPos);
		}

		public static InfluenceUpdateMessage _Read_Respawning.InfluenceUpdateMessage(NetworkReader reader)
		{
			return new InfluenceUpdateMessage
			{
				Faction = GeneratedNetworkCode._Read_PlayerRoles.Faction(reader),
				Influence = reader.ReadFloat()
			};
		}

		public static Faction _Read_PlayerRoles.Faction(NetworkReader reader)
		{
			return (Faction)reader.ReadByte();
		}

		public static void _Write_Respawning.InfluenceUpdateMessage(NetworkWriter writer, InfluenceUpdateMessage value)
		{
			GeneratedNetworkCode._Write_PlayerRoles.Faction(writer, value.Faction);
			writer.WriteFloat(value.Influence);
		}

		public static void _Write_PlayerRoles.Faction(NetworkWriter writer, Faction value)
		{
			writer.WriteByte((byte)value);
		}

		public static StripdownNetworking.StripdownResponse _Read_CommandSystem.Commands.RemoteAdmin.Stripdown.StripdownNetworking/StripdownResponse(NetworkReader reader)
		{
			return new StripdownNetworking.StripdownResponse
			{
				Lines = GeneratedNetworkCode._Read_System.String[](reader)
			};
		}

		public static string[] _Read_System.String[](NetworkReader reader)
		{
			return reader.ReadArray<string>();
		}

		public static void _Write_CommandSystem.Commands.RemoteAdmin.Stripdown.StripdownNetworking/StripdownResponse(NetworkWriter writer, StripdownNetworking.StripdownResponse value)
		{
			GeneratedNetworkCode._Write_System.String[](writer, value.Lines);
		}

		public static void _Write_System.String[](NetworkWriter writer, string[] value)
		{
			writer.WriteArray(value);
		}

		public static AchievementManager.AchievementMessage _Read_Achievements.AchievementManager/AchievementMessage(NetworkReader reader)
		{
			return new AchievementManager.AchievementMessage
			{
				AchievementId = reader.ReadByte()
			};
		}

		public static void _Write_Achievements.AchievementManager/AchievementMessage(NetworkWriter writer, AchievementManager.AchievementMessage value)
		{
			writer.WriteByte(value.AchievementId);
		}

		public static HumeShieldSubEffect.HumeBlockMsg _Read_InventorySystem.Items.Usables.Scp244.Hypothermia.HumeShieldSubEffect/HumeBlockMsg(NetworkReader reader)
		{
			return default(HumeShieldSubEffect.HumeBlockMsg);
		}

		public static void _Write_InventorySystem.Items.Usables.Scp244.Hypothermia.HumeShieldSubEffect/HumeBlockMsg(NetworkWriter writer, HumeShieldSubEffect.HumeBlockMsg value)
		{
		}

		public static Hypothermia.ForcedHypothermiaMessage _Read_InventorySystem.Items.Usables.Scp244.Hypothermia.Hypothermia/ForcedHypothermiaMessage(NetworkReader reader)
		{
			return new Hypothermia.ForcedHypothermiaMessage
			{
				IsForced = reader.ReadBool(),
				Exposure = reader.ReadFloat(),
				PlayerHub = reader.ReadReferenceHub()
			};
		}

		public static void _Write_InventorySystem.Items.Usables.Scp244.Hypothermia.Hypothermia/ForcedHypothermiaMessage(NetworkWriter writer, Hypothermia.ForcedHypothermiaMessage value)
		{
			writer.WriteBool(value.IsForced);
			writer.WriteFloat(value.Exposure);
			writer.WriteReferenceHub(value.PlayerHub);
		}

		public static Scp1576SpectatorWarningHandler.SpectatorWarningMessage _Read_InventorySystem.Items.Usables.Scp1576.Scp1576SpectatorWarningHandler/SpectatorWarningMessage(NetworkReader reader)
		{
			return new Scp1576SpectatorWarningHandler.SpectatorWarningMessage
			{
				IsStop = reader.ReadBool()
			};
		}

		public static void _Write_InventorySystem.Items.Usables.Scp1576.Scp1576SpectatorWarningHandler/SpectatorWarningMessage(NetworkWriter writer, Scp1576SpectatorWarningHandler.SpectatorWarningMessage value)
		{
			writer.WriteBool(value.IsStop);
		}

		public static Scp1344DetectionMessage _Read_InventorySystem.Items.Usables.Scp1344.Scp1344DetectionMessage(NetworkReader reader)
		{
			return new Scp1344DetectionMessage
			{
				DetectedNetId = reader.ReadUInt()
			};
		}

		public static void _Write_InventorySystem.Items.Usables.Scp1344.Scp1344DetectionMessage(NetworkWriter writer, Scp1344DetectionMessage value)
		{
			writer.WriteUInt(value.DetectedNetId);
		}

		public static Scp1344StatusMessage _Read_InventorySystem.Items.Usables.Scp1344.Scp1344StatusMessage(NetworkReader reader)
		{
			return new Scp1344StatusMessage
			{
				Serial = reader.ReadUShort(),
				NewState = GeneratedNetworkCode._Read_InventorySystem.Items.Usables.Scp1344.Scp1344Status(reader)
			};
		}

		public static Scp1344Status _Read_InventorySystem.Items.Usables.Scp1344.Scp1344Status(NetworkReader reader)
		{
			return (Scp1344Status)reader.ReadByte();
		}

		public static void _Write_InventorySystem.Items.Usables.Scp1344.Scp1344StatusMessage(NetworkWriter writer, Scp1344StatusMessage value)
		{
			writer.WriteUShort(value.Serial);
			GeneratedNetworkCode._Write_InventorySystem.Items.Usables.Scp1344.Scp1344Status(writer, value.NewState);
		}

		public static void _Write_InventorySystem.Items.Usables.Scp1344.Scp1344Status(NetworkWriter writer, Scp1344Status value)
		{
			writer.WriteByte((byte)value);
		}

		public static KeycardItem.UseMessage _Read_InventorySystem.Items.Keycards.KeycardItem/UseMessage(NetworkReader reader)
		{
			return new KeycardItem.UseMessage
			{
				ItemSerial = reader.ReadUShort()
			};
		}

		public static void _Write_InventorySystem.Items.Keycards.KeycardItem/UseMessage(NetworkWriter writer, KeycardItem.UseMessage value)
		{
			writer.WriteUShort(value.ItemSerial);
		}

		public static DamageIndicatorMessage _Read_InventorySystem.Items.Firearms.BasicMessages.DamageIndicatorMessage(NetworkReader reader)
		{
			return new DamageIndicatorMessage
			{
				ReceivedDamage = reader.ReadByte(),
				DamagePosition = reader.ReadRelativePosition()
			};
		}

		public static void _Write_InventorySystem.Items.Firearms.BasicMessages.DamageIndicatorMessage(NetworkWriter writer, DamageIndicatorMessage value)
		{
			writer.WriteByte(value.ReceivedDamage);
			writer.WriteRelativePosition(value.DamagePosition);
		}

		public static ServerConfigSynchronizer.PredefinedBanTemplate _Read_ServerConfigSynchronizer/PredefinedBanTemplate(NetworkReader reader)
		{
			return new ServerConfigSynchronizer.PredefinedBanTemplate
			{
				Duration = reader.ReadInt(),
				FormattedDuration = reader.ReadString(),
				Reason = reader.ReadString()
			};
		}

		public static void _Write_ServerConfigSynchronizer/PredefinedBanTemplate(NetworkWriter writer, ServerConfigSynchronizer.PredefinedBanTemplate value)
		{
			writer.WriteInt(value.Duration);
			writer.WriteString(value.FormattedDuration);
			writer.WriteString(value.Reason);
		}

		public static void _Write_Broadcast/BroadcastFlags(NetworkWriter writer, Broadcast.BroadcastFlags value)
		{
			writer.WriteByte((byte)value);
		}

		public static Broadcast.BroadcastFlags _Read_Broadcast/BroadcastFlags(NetworkReader reader)
		{
			return (Broadcast.BroadcastFlags)reader.ReadByte();
		}

		public static void _Write_UnityEngine.KeyCode(NetworkWriter writer, KeyCode value)
		{
			writer.WriteInt((int)value);
		}

		public static KeyCode _Read_UnityEngine.KeyCode(NetworkReader reader)
		{
			return (KeyCode)reader.ReadInt();
		}

		public static void _Write_PlayerInfoArea(NetworkWriter writer, PlayerInfoArea value)
		{
			writer.WriteInt((int)value);
		}

		public static PlayerInfoArea _Read_PlayerInfoArea(NetworkReader reader)
		{
			return (PlayerInfoArea)reader.ReadInt();
		}

		public static void _Write_PlayerInteract/AlphaPanelOperations(NetworkWriter writer, PlayerInteract.AlphaPanelOperations value)
		{
			writer.WriteByte((byte)value);
		}

		public static PlayerInteract.AlphaPanelOperations _Read_PlayerInteract/AlphaPanelOperations(NetworkReader reader)
		{
			return (PlayerInteract.AlphaPanelOperations)reader.ReadByte();
		}

		public static void _Write_RoundSummary/SumInfo_ClassList(NetworkWriter writer, RoundSummary.SumInfo_ClassList value)
		{
			writer.WriteInt(value.class_ds);
			writer.WriteInt(value.scientists);
			writer.WriteInt(value.chaos_insurgents);
			writer.WriteInt(value.mtf_and_guards);
			writer.WriteInt(value.scps_except_zombies);
			writer.WriteInt(value.zombies);
			writer.WriteInt(value.warhead_kills);
			writer.WriteInt(value.flamingos);
		}

		public static void _Write_RoundSummary/LeadingTeam(NetworkWriter writer, RoundSummary.LeadingTeam value)
		{
			writer.WriteByte((byte)value);
		}

		public static RoundSummary.SumInfo_ClassList _Read_RoundSummary/SumInfo_ClassList(NetworkReader reader)
		{
			return new RoundSummary.SumInfo_ClassList
			{
				class_ds = reader.ReadInt(),
				scientists = reader.ReadInt(),
				chaos_insurgents = reader.ReadInt(),
				mtf_and_guards = reader.ReadInt(),
				scps_except_zombies = reader.ReadInt(),
				zombies = reader.ReadInt(),
				warhead_kills = reader.ReadInt(),
				flamingos = reader.ReadInt()
			};
		}

		public static RoundSummary.LeadingTeam _Read_RoundSummary/LeadingTeam(NetworkReader reader)
		{
			return (RoundSummary.LeadingTeam)reader.ReadByte();
		}

		public static void _Write_ServerRoles/BadgePreferences(NetworkWriter writer, ServerRoles.BadgePreferences value)
		{
			writer.WriteInt((int)value);
		}

		public static void _Write_ServerRoles/BadgeVisibilityPreferences(NetworkWriter writer, ServerRoles.BadgeVisibilityPreferences value)
		{
			writer.WriteInt((int)value);
		}

		public static ServerRoles.BadgePreferences _Read_ServerRoles/BadgePreferences(NetworkReader reader)
		{
			return (ServerRoles.BadgePreferences)reader.ReadInt();
		}

		public static ServerRoles.BadgeVisibilityPreferences _Read_ServerRoles/BadgeVisibilityPreferences(NetworkReader reader)
		{
			return (ServerRoles.BadgeVisibilityPreferences)reader.ReadInt();
		}

		public static void _Write_RemoteAdmin.QueryProcessor/CommandData[](NetworkWriter writer, QueryProcessor.CommandData[] value)
		{
			writer.WriteArray(value);
		}

		public static void _Write_RemoteAdmin.QueryProcessor/CommandData(NetworkWriter writer, QueryProcessor.CommandData value)
		{
			writer.WriteString(value.Command);
			GeneratedNetworkCode._Write_System.String[](writer, value.Usage);
			writer.WriteString(value.Description);
			writer.WriteString(value.AliasOf);
			writer.WriteBool(value.Hidden);
		}

		public static QueryProcessor.CommandData[] _Read_RemoteAdmin.QueryProcessor/CommandData[](NetworkReader reader)
		{
			return reader.ReadArray<QueryProcessor.CommandData>();
		}

		public static QueryProcessor.CommandData _Read_RemoteAdmin.QueryProcessor/CommandData(NetworkReader reader)
		{
			return new QueryProcessor.CommandData
			{
				Command = reader.ReadString(),
				Usage = GeneratedNetworkCode._Read_System.String[](reader),
				Description = reader.ReadString(),
				AliasOf = reader.ReadString(),
				Hidden = reader.ReadBool()
			};
		}

		public static void _Write_LightContainmentZoneDecontamination.DecontaminationController/DecontaminationStatus(NetworkWriter writer, DecontaminationController.DecontaminationStatus value)
		{
			writer.WriteByte((byte)value);
		}

		public static DecontaminationController.DecontaminationStatus _Read_LightContainmentZoneDecontamination.DecontaminationController/DecontaminationStatus(NetworkReader reader)
		{
			return (DecontaminationController.DecontaminationStatus)reader.ReadByte();
		}

		public static void _Write_UnityEngine.LightShadows(NetworkWriter writer, LightShadows value)
		{
			writer.WriteInt((int)value);
		}

		public static void _Write_UnityEngine.LightType(NetworkWriter writer, LightType value)
		{
			writer.WriteInt((int)value);
		}

		public static void _Write_UnityEngine.LightShape(NetworkWriter writer, LightShape value)
		{
			writer.WriteInt((int)value);
		}

		public static LightShadows _Read_UnityEngine.LightShadows(NetworkReader reader)
		{
			return (LightShadows)reader.ReadInt();
		}

		public static LightType _Read_UnityEngine.LightType(NetworkReader reader)
		{
			return (LightType)reader.ReadInt();
		}

		public static LightShape _Read_UnityEngine.LightShape(NetworkReader reader)
		{
			return (LightShape)reader.ReadInt();
		}

		public static void _Write_UnityEngine.PrimitiveType(NetworkWriter writer, PrimitiveType value)
		{
			writer.WriteInt((int)value);
		}

		public static void _Write_AdminToys.PrimitiveFlags(NetworkWriter writer, PrimitiveFlags value)
		{
			writer.WriteByte((byte)value);
		}

		public static PrimitiveType _Read_UnityEngine.PrimitiveType(NetworkReader reader)
		{
			return (PrimitiveType)reader.ReadInt();
		}

		public static PrimitiveFlags _Read_AdminToys.PrimitiveFlags(NetworkReader reader)
		{
			return (PrimitiveFlags)reader.ReadByte();
		}

		public static void _Write_Scp914.Scp914KnobSetting(NetworkWriter writer, Scp914KnobSetting value)
		{
			writer.WriteByte((byte)value);
		}

		public static Scp914KnobSetting _Read_Scp914.Scp914KnobSetting(NetworkReader reader)
		{
			return (Scp914KnobSetting)reader.ReadByte();
		}

		public static void _Write_Interactables.Interobjects.ElevatorGroup(NetworkWriter writer, ElevatorGroup value)
		{
			writer.WriteInt((int)value);
		}

		public static ElevatorGroup _Read_Interactables.Interobjects.ElevatorGroup(NetworkReader reader)
		{
			return (ElevatorGroup)reader.ReadInt();
		}

		public static void _Write_InventorySystem.Items.ItemIdentifier[](NetworkWriter writer, ItemIdentifier[] value)
		{
			writer.WriteArray(value);
		}

		public static void _Write_InventorySystem.Items.ItemIdentifier(NetworkWriter writer, ItemIdentifier value)
		{
			GeneratedNetworkCode._Write_ItemType(writer, value.TypeId);
			writer.WriteUShort(value.SerialNumber);
		}

		public static void _Write_ItemType(NetworkWriter writer, ItemType value)
		{
			writer.WriteInt((int)value);
		}

		public static ItemIdentifier[] _Read_InventorySystem.Items.ItemIdentifier[](NetworkReader reader)
		{
			return reader.ReadArray<ItemIdentifier>();
		}

		public static ItemIdentifier _Read_InventorySystem.Items.ItemIdentifier(NetworkReader reader)
		{
			return new ItemIdentifier
			{
				TypeId = GeneratedNetworkCode._Read_ItemType(reader),
				SerialNumber = reader.ReadUShort()
			};
		}

		public static ItemType _Read_ItemType(NetworkReader reader)
		{
			return (ItemType)reader.ReadInt();
		}

		public static void _Write_System.UInt16[](NetworkWriter writer, ushort[] value)
		{
			writer.WriteArray(value);
		}

		public static ushort[] _Read_System.UInt16[](NetworkReader reader)
		{
			return reader.ReadArray<ushort>();
		}

		public static void _Write_InventorySystem.Items.Usables.Scp330.CandyKindID(NetworkWriter writer, CandyKindID value)
		{
			writer.WriteByte((byte)value);
		}

		public static CandyKindID _Read_InventorySystem.Items.Usables.Scp330.CandyKindID(NetworkReader reader)
		{
			return (CandyKindID)reader.ReadByte();
		}

		public static void _Write_InventorySystem.Items.Jailbird.JailbirdWearState(NetworkWriter writer, JailbirdWearState value)
		{
			writer.WriteInt((int)value);
		}

		public static JailbirdWearState _Read_InventorySystem.Items.Jailbird.JailbirdWearState(NetworkReader reader)
		{
			return (JailbirdWearState)reader.ReadInt();
		}

		public static void _Write_PlayerRoles.Team(NetworkWriter writer, Team value)
		{
			writer.WriteByte((byte)value);
		}

		public static Team _Read_PlayerRoles.Team(NetworkReader reader)
		{
			return (Team)reader.ReadByte();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void InitReadWriters()
		{
			Writer<byte>.write = new Action<NetworkWriter, byte>(NetworkWriterExtensions.WriteByte);
			Writer<byte?>.write = new Action<NetworkWriter, byte?>(NetworkWriterExtensions.WriteByteNullable);
			Writer<sbyte>.write = new Action<NetworkWriter, sbyte>(NetworkWriterExtensions.WriteSByte);
			Writer<sbyte?>.write = new Action<NetworkWriter, sbyte?>(NetworkWriterExtensions.WriteSByteNullable);
			Writer<char>.write = new Action<NetworkWriter, char>(NetworkWriterExtensions.WriteChar);
			Writer<char?>.write = new Action<NetworkWriter, char?>(NetworkWriterExtensions.WriteCharNullable);
			Writer<bool>.write = new Action<NetworkWriter, bool>(NetworkWriterExtensions.WriteBool);
			Writer<bool?>.write = new Action<NetworkWriter, bool?>(NullableBoolReaderWriter.WriteNullableBool);
			Writer<short>.write = new Action<NetworkWriter, short>(NetworkWriterExtensions.WriteShort);
			Writer<short?>.write = new Action<NetworkWriter, short?>(NetworkWriterExtensions.WriteShortNullable);
			Writer<ushort>.write = new Action<NetworkWriter, ushort>(NetworkWriterExtensions.WriteUShort);
			Writer<ushort?>.write = new Action<NetworkWriter, ushort?>(NetworkWriterExtensions.WriteUShortNullable);
			Writer<int>.write = new Action<NetworkWriter, int>(NetworkWriterExtensions.WriteInt);
			Writer<int?>.write = new Action<NetworkWriter, int?>(NetworkWriterExtensions.WriteIntNullable);
			Writer<uint>.write = new Action<NetworkWriter, uint>(NetworkWriterExtensions.WriteUInt);
			Writer<uint?>.write = new Action<NetworkWriter, uint?>(NetworkWriterExtensions.WriteUIntNullable);
			Writer<long>.write = new Action<NetworkWriter, long>(NetworkWriterExtensions.WriteLong);
			Writer<long?>.write = new Action<NetworkWriter, long?>(NetworkWriterExtensions.WriteLongNullable);
			Writer<ulong>.write = new Action<NetworkWriter, ulong>(NetworkWriterExtensions.WriteULong);
			Writer<ulong?>.write = new Action<NetworkWriter, ulong?>(NetworkWriterExtensions.WriteULongNullable);
			Writer<float>.write = new Action<NetworkWriter, float>(NetworkWriterExtensions.WriteFloat);
			Writer<float?>.write = new Action<NetworkWriter, float?>(NetworkWriterExtensions.WriteFloatNullable);
			Writer<double>.write = new Action<NetworkWriter, double>(NetworkWriterExtensions.WriteDouble);
			Writer<double?>.write = new Action<NetworkWriter, double?>(NetworkWriterExtensions.WriteDoubleNullable);
			Writer<decimal>.write = new Action<NetworkWriter, decimal>(NetworkWriterExtensions.WriteDecimal);
			Writer<decimal?>.write = new Action<NetworkWriter, decimal?>(NetworkWriterExtensions.WriteDecimalNullable);
			Writer<string>.write = new Action<NetworkWriter, string>(NetworkWriterExtensions.WriteString);
			Writer<byte[]>.write = new Action<NetworkWriter, byte[]>(NetworkWriterExtensions.WriteBytesAndSize);
			Writer<ArraySegment<byte>>.write = new Action<NetworkWriter, ArraySegment<byte>>(NetworkWriterExtensions.WriteArraySegmentAndSize);
			Writer<Vector2>.write = new Action<NetworkWriter, Vector2>(NetworkWriterExtensions.WriteVector2);
			Writer<Vector2?>.write = new Action<NetworkWriter, Vector2?>(NetworkWriterExtensions.WriteVector2Nullable);
			Writer<Vector3>.write = new Action<NetworkWriter, Vector3>(NetworkWriterExtensions.WriteVector3);
			Writer<Vector3?>.write = new Action<NetworkWriter, Vector3?>(NetworkWriterExtensions.WriteVector3Nullable);
			Writer<Vector4>.write = new Action<NetworkWriter, Vector4>(NetworkWriterExtensions.WriteVector4);
			Writer<Vector4?>.write = new Action<NetworkWriter, Vector4?>(NetworkWriterExtensions.WriteVector4Nullable);
			Writer<Vector2Int>.write = new Action<NetworkWriter, Vector2Int>(NetworkWriterExtensions.WriteVector2Int);
			Writer<Vector2Int?>.write = new Action<NetworkWriter, Vector2Int?>(NetworkWriterExtensions.WriteVector2IntNullable);
			Writer<Vector3Int>.write = new Action<NetworkWriter, Vector3Int>(NetworkWriterExtensions.WriteVector3Int);
			Writer<Vector3Int?>.write = new Action<NetworkWriter, Vector3Int?>(NetworkWriterExtensions.WriteVector3IntNullable);
			Writer<Color>.write = new Action<NetworkWriter, Color>(NetworkWriterExtensions.WriteColor);
			Writer<Color?>.write = new Action<NetworkWriter, Color?>(NetworkWriterExtensions.WriteColorNullable);
			Writer<Color32>.write = new Action<NetworkWriter, Color32>(NetworkWriterExtensions.WriteColor32);
			Writer<Color32?>.write = new Action<NetworkWriter, Color32?>(NetworkWriterExtensions.WriteColor32Nullable);
			Writer<Quaternion>.write = new Action<NetworkWriter, Quaternion>(NetworkWriterExtensions.WriteQuaternion);
			Writer<Quaternion?>.write = new Action<NetworkWriter, Quaternion?>(NetworkWriterExtensions.WriteQuaternionNullable);
			Writer<Rect>.write = new Action<NetworkWriter, Rect>(NetworkWriterExtensions.WriteRect);
			Writer<Rect?>.write = new Action<NetworkWriter, Rect?>(NetworkWriterExtensions.WriteRectNullable);
			Writer<Plane>.write = new Action<NetworkWriter, Plane>(NetworkWriterExtensions.WritePlane);
			Writer<Plane?>.write = new Action<NetworkWriter, Plane?>(NetworkWriterExtensions.WritePlaneNullable);
			Writer<Ray>.write = new Action<NetworkWriter, Ray>(NetworkWriterExtensions.WriteRay);
			Writer<Ray?>.write = new Action<NetworkWriter, Ray?>(NetworkWriterExtensions.WriteRayNullable);
			Writer<Matrix4x4>.write = new Action<NetworkWriter, Matrix4x4>(NetworkWriterExtensions.WriteMatrix4x4);
			Writer<Matrix4x4?>.write = new Action<NetworkWriter, Matrix4x4?>(NetworkWriterExtensions.WriteMatrix4x4Nullable);
			Writer<Guid>.write = new Action<NetworkWriter, Guid>(NetworkWriterExtensions.WriteGuid);
			Writer<Guid?>.write = new Action<NetworkWriter, Guid?>(NetworkWriterExtensions.WriteGuidNullable);
			Writer<NetworkIdentity>.write = new Action<NetworkWriter, NetworkIdentity>(NetworkWriterExtensions.WriteNetworkIdentity);
			Writer<NetworkBehaviour>.write = new Action<NetworkWriter, NetworkBehaviour>(NetworkWriterExtensions.WriteNetworkBehaviour);
			Writer<Transform>.write = new Action<NetworkWriter, Transform>(NetworkWriterExtensions.WriteTransform);
			Writer<GameObject>.write = new Action<NetworkWriter, GameObject>(NetworkWriterExtensions.WriteGameObject);
			Writer<Uri>.write = new Action<NetworkWriter, Uri>(NetworkWriterExtensions.WriteUri);
			Writer<Texture2D>.write = new Action<NetworkWriter, Texture2D>(NetworkWriterExtensions.WriteTexture2D);
			Writer<Sprite>.write = new Action<NetworkWriter, Sprite>(NetworkWriterExtensions.WriteSprite);
			Writer<DateTime>.write = new Action<NetworkWriter, DateTime>(NetworkWriterExtensions.WriteDateTime);
			Writer<DateTime?>.write = new Action<NetworkWriter, DateTime?>(NetworkWriterExtensions.WriteDateTimeNullable);
			Writer<TimeSnapshotMessage>.write = new Action<NetworkWriter, TimeSnapshotMessage>(GeneratedNetworkCode._Write_Mirror.TimeSnapshotMessage);
			Writer<ReadyMessage>.write = new Action<NetworkWriter, ReadyMessage>(GeneratedNetworkCode._Write_Mirror.ReadyMessage);
			Writer<NotReadyMessage>.write = new Action<NetworkWriter, NotReadyMessage>(GeneratedNetworkCode._Write_Mirror.NotReadyMessage);
			Writer<AddPlayerMessage>.write = new Action<NetworkWriter, AddPlayerMessage>(GeneratedNetworkCode._Write_Mirror.AddPlayerMessage);
			Writer<SceneMessage>.write = new Action<NetworkWriter, SceneMessage>(GeneratedNetworkCode._Write_Mirror.SceneMessage);
			Writer<SceneOperation>.write = new Action<NetworkWriter, SceneOperation>(GeneratedNetworkCode._Write_Mirror.SceneOperation);
			Writer<CommandMessage>.write = new Action<NetworkWriter, CommandMessage>(GeneratedNetworkCode._Write_Mirror.CommandMessage);
			Writer<RpcMessage>.write = new Action<NetworkWriter, RpcMessage>(GeneratedNetworkCode._Write_Mirror.RpcMessage);
			Writer<SpawnMessage>.write = new Action<NetworkWriter, SpawnMessage>(GeneratedNetworkCode._Write_Mirror.SpawnMessage);
			Writer<ChangeOwnerMessage>.write = new Action<NetworkWriter, ChangeOwnerMessage>(GeneratedNetworkCode._Write_Mirror.ChangeOwnerMessage);
			Writer<ObjectSpawnStartedMessage>.write = new Action<NetworkWriter, ObjectSpawnStartedMessage>(GeneratedNetworkCode._Write_Mirror.ObjectSpawnStartedMessage);
			Writer<ObjectSpawnFinishedMessage>.write = new Action<NetworkWriter, ObjectSpawnFinishedMessage>(GeneratedNetworkCode._Write_Mirror.ObjectSpawnFinishedMessage);
			Writer<ObjectDestroyMessage>.write = new Action<NetworkWriter, ObjectDestroyMessage>(GeneratedNetworkCode._Write_Mirror.ObjectDestroyMessage);
			Writer<ObjectHideMessage>.write = new Action<NetworkWriter, ObjectHideMessage>(GeneratedNetworkCode._Write_Mirror.ObjectHideMessage);
			Writer<EntityStateMessage>.write = new Action<NetworkWriter, EntityStateMessage>(GeneratedNetworkCode._Write_Mirror.EntityStateMessage);
			Writer<NetworkPingMessage>.write = new Action<NetworkWriter, NetworkPingMessage>(GeneratedNetworkCode._Write_Mirror.NetworkPingMessage);
			Writer<NetworkPongMessage>.write = new Action<NetworkWriter, NetworkPongMessage>(GeneratedNetworkCode._Write_Mirror.NetworkPongMessage);
			Writer<AlphaWarheadSyncInfo>.write = new Action<NetworkWriter, AlphaWarheadSyncInfo>(AlphaWarheadSyncInfoSerializer.WriteAlphaWarheadSyncInfo);
			Writer<RecyclablePlayerId>.write = new Action<NetworkWriter, RecyclablePlayerId>(RecyclablePlayerIdReaderWriter.WriteRecyclablePlayerId);
			Writer<ServerConfigSynchronizer.AmmoLimit>.write = new Action<NetworkWriter, ServerConfigSynchronizer.AmmoLimit>(AmmoLimitSerializer.WriteAmmoLimit);
			Writer<TeslaHitMsg>.write = new Action<NetworkWriter, TeslaHitMsg>(TeslaHitMsgSerializers.Serialize);
			Writer<EncryptedChannelManager.EncryptedMessageOutside>.write = new Action<NetworkWriter, EncryptedChannelManager.EncryptedMessageOutside>(EncryptedChannelFunctions.SerializeEncryptedMessageOutside);
			Writer<Offset>.write = new Action<NetworkWriter, Offset>(OffsetSerializer.WriteOffset);
			Writer<LowPrecisionQuaternion>.write = new Action<NetworkWriter, LowPrecisionQuaternion>(LowPrecisionQuaternionSerializer.WriteLowPrecisionQuaternion);
			Writer<bool[]>.write = new Action<NetworkWriter, bool[]>(Misc.WriteBoolArray);
			Writer<SSSEntriesPack>.write = new Action<NetworkWriter, SSSEntriesPack>(SSSNetworkMessageFunctions.SerializeSSSEntriesPack);
			Writer<SSSClientResponse>.write = new Action<NetworkWriter, SSSClientResponse>(SSSNetworkMessageFunctions.SerializeSSSClientResponse);
			Writer<SSSUserStatusReport>.write = new Action<NetworkWriter, SSSUserStatusReport>(SSSNetworkMessageFunctions.SerializeSSSVersionSelfReport);
			Writer<SSSUpdateMessage>.write = new Action<NetworkWriter, SSSUpdateMessage>(SSSNetworkMessageFunctions.SerializeSSSUpdateMessage);
			Writer<AudioMessage>.write = new Action<NetworkWriter, AudioMessage>(AudioMessageReadersWriters.SerializeVoiceMessage);
			Writer<VoiceMessage>.write = new Action<NetworkWriter, VoiceMessage>(VoiceMessageReadersWriters.SerializeVoiceMessage);
			Writer<SubtitleMessage>.write = new Action<NetworkWriter, SubtitleMessage>(SubtitleMessageExtensions.Serialize);
			Writer<RoundRestartMessage>.write = new Action<NetworkWriter, RoundRestartMessage>(RoundRestartMessageReaderWriter.WriteRoundRestartMessage);
			Writer<RelativePosition>.write = new Action<NetworkWriter, RelativePosition>(RelativePositionSerialization.WriteRelativePosition);
			Writer<DamageHandlerBase>.write = new Action<NetworkWriter, DamageHandlerBase>(DamageHandlerReaderWriter.WriteDamageHandler);
			Writer<SyncedStatMessages.StatMessage>.write = new Action<NetworkWriter, SyncedStatMessages.StatMessage>(SyncedStatMessages.Serialize);
			Writer<RoleTypeId>.write = new Action<NetworkWriter, RoleTypeId>(PlayerRoleEnumsReadersWriters.WriteRoleType);
			Writer<RoleSyncInfo>.write = new Action<NetworkWriter, RoleSyncInfo>(PlayerRolesNetUtils.WriteRoleSyncInfo);
			Writer<RoleSyncInfoPack>.write = new Action<NetworkWriter, RoleSyncInfoPack>(PlayerRolesNetUtils.WriteRoleSyncInfoPack);
			Writer<SubroutineMessage>.write = new Action<NetworkWriter, SubroutineMessage>(SubroutineMessageReaderWriter.WriteSubroutineMessage);
			Writer<SpectatorSpawnReason>.write = new Action<NetworkWriter, SpectatorSpawnReason>(SpectatorSpawnReasonReaderWriter.WriteSpawnReason);
			Writer<ScpSpawnPreferences.SpawnPreferences>.write = new Action<NetworkWriter, ScpSpawnPreferences.SpawnPreferences>(ScpSpawnPreferences.WriteSpawnPreferences);
			Writer<RagdollData>.write = new Action<NetworkWriter, RagdollData>(RagdollDataReaderWriter.WriteRagdollData);
			Writer<SyncedGravityMessages.GravityMessage>.write = new Action<NetworkWriter, SyncedGravityMessages.GravityMessage>(SyncedGravityMessages.Serialize);
			Writer<FpcFromClientMessage>.write = new Action<NetworkWriter, FpcFromClientMessage>(FpcMessagesReadersWriters.WriteFpcFromClientMessage);
			Writer<FpcPositionMessage>.write = new Action<NetworkWriter, FpcPositionMessage>(FpcMessagesReadersWriters.WriteFpcPositionMessage);
			Writer<FpcPositionOverrideMessage>.write = new Action<NetworkWriter, FpcPositionOverrideMessage>(FpcMessagesReadersWriters.WriteFpcRotationOverrideMessage);
			Writer<FpcFallDamageMessage>.write = new Action<NetworkWriter, FpcFallDamageMessage>(FpcMessagesReadersWriters.WriteFpcFallDamageMessage);
			Writer<SubcontrollerRpcHandler.SubcontrollerRpcMessage>.write = new Action<NetworkWriter, SubcontrollerRpcHandler.SubcontrollerRpcMessage>(SubcontrollerRpcHandler.WriteSubcontrollerRpcMessage);
			Writer<AnimationCurve>.write = new Action<NetworkWriter, AnimationCurve>(AnimationCurveReaderWriter.WriteAnimationCurve);
			Writer<IReadOnlyCollection<HintEffect>>.write = new Action<NetworkWriter, IReadOnlyCollection<HintEffect>>(HintEffectArrayReaderWriter.WriteHintEffectArray);
			Writer<HintEffect>.write = new Action<NetworkWriter, HintEffect>(HintEffectReaderWriter.WriteHintEffect);
			Writer<IReadOnlyCollection<HintParameter>>.write = new Action<NetworkWriter, IReadOnlyCollection<HintParameter>>(HintParameterArrayReaderWriter.WriteHintParameterArray);
			Writer<HintParameter>.write = new Action<NetworkWriter, HintParameter>(HintParameterReaderWriter.WriteHintParameter);
			Writer<Hint>.write = new Action<NetworkWriter, Hint>(HintReaderWriter.WriteHint);
			Writer<ReferenceHub>.write = new Action<NetworkWriter, ReferenceHub>(ReferenceHubReaderWriter.WriteReferenceHub);
			Writer<AlphaCurveHintEffect>.write = new Action<NetworkWriter, AlphaCurveHintEffect>(AlphaCurveHintEffectFunctions.Serialize);
			Writer<AlphaEffect>.write = new Action<NetworkWriter, AlphaEffect>(AlphaEffectFunctions.Serialize);
			Writer<OutlineEffect>.write = new Action<NetworkWriter, OutlineEffect>(OutlineEffectFunctions.Serialize);
			Writer<TextHint>.write = new Action<NetworkWriter, TextHint>(TextHintFunctions.Serialize);
			Writer<TranslationHint>.write = new Action<NetworkWriter, TranslationHint>(TranslationHintFunctions.Serialize);
			Writer<AmmoHintParameter>.write = new Action<NetworkWriter, AmmoHintParameter>(AmmoHintParameterFunctions.Serialize);
			Writer<Scp330HintParameter>.write = new Action<NetworkWriter, Scp330HintParameter>(Scp330HintParameterFunctions.Serialize);
			Writer<ItemCategoryHintParameter>.write = new Action<NetworkWriter, ItemCategoryHintParameter>(ItemCategoryHintParameterFunctions.Serialize);
			Writer<ItemHintParameter>.write = new Action<NetworkWriter, ItemHintParameter>(ItemHintParameterFunctions.Serialize);
			Writer<ByteHintParameter>.write = new Action<NetworkWriter, ByteHintParameter>(ByteHintParameterFunctions.Serialize);
			Writer<DoubleHintParameter>.write = new Action<NetworkWriter, DoubleHintParameter>(DoubleHintParameterFunctions.Serialize);
			Writer<FloatHintParameter>.write = new Action<NetworkWriter, FloatHintParameter>(FloatHintParameterFunctions.Serialize);
			Writer<IntHintParameter>.write = new Action<NetworkWriter, IntHintParameter>(IntHintParameterFunctions.Serialize);
			Writer<LongHintParameter>.write = new Action<NetworkWriter, LongHintParameter>(LongHintParameterFunctions.Serialize);
			Writer<PackedLongHintParameter>.write = new Action<NetworkWriter, PackedLongHintParameter>(PackedLongHintParameterFunctions.Serialize);
			Writer<PackedULongHintParameter>.write = new Action<NetworkWriter, PackedULongHintParameter>(PackedULongHintParameterFunctions.Serialize);
			Writer<SByteHintParameter>.write = new Action<NetworkWriter, SByteHintParameter>(SByteHintParameterFunctions.Serialize);
			Writer<ShortHintParameter>.write = new Action<NetworkWriter, ShortHintParameter>(ShortHintParameterFunctions.Serialize);
			Writer<StringHintParameter>.write = new Action<NetworkWriter, StringHintParameter>(StringHintParameterFunctions.Serialize);
			Writer<TimespanHintParameter>.write = new Action<NetworkWriter, TimespanHintParameter>(TimespanHintParameterFunctions.Serialize);
			Writer<UIntHintParameter>.write = new Action<NetworkWriter, UIntHintParameter>(UIntHintParameterFunctions.Serialize);
			Writer<ULongHintParameter>.write = new Action<NetworkWriter, ULongHintParameter>(ULongHintParameterFunctions.Serialize);
			Writer<UShortHintParameter>.write = new Action<NetworkWriter, UShortHintParameter>(UShortHintParameterFunctions.Serialize);
			Writer<HintMessage>.write = new Action<NetworkWriter, HintMessage>(HintMessageParameterFunctions.Serialize);
			Writer<ObjectiveCompletionMessage>.write = new Action<NetworkWriter, ObjectiveCompletionMessage>(ObjectiveCompletionMessageUtility.WriteCompletionMessage);
			Writer<WaveUpdateMessage>.write = new Action<NetworkWriter, WaveUpdateMessage>(WaveUpdateMessageUtility.WriteUpdateMessage);
			Writer<SpawnableWaveBase>.write = new Action<NetworkWriter, SpawnableWaveBase>(WaveUtils.WriteWave);
			Writer<UnitNameMessage>.write = new Action<NetworkWriter, UnitNameMessage>(UnitNameMessageHandler.WriteUnitName);
			Writer<DecalCleanupMessage>.write = new Action<NetworkWriter, DecalCleanupMessage>(DecalCleanupMessageExtensions.WriteDecalCleanupMessage);
			Writer<AuthenticationResponse>.write = new Action<NetworkWriter, AuthenticationResponse>(AuthenticationResponseFunctions.SerializeAuthenticationResponse);
			Writer<SearchInvalidation>.write = new Action<NetworkWriter, SearchInvalidation>(SearchInvalidationFunctions.Serialize);
			Writer<SearchRequest>.write = new Action<NetworkWriter, SearchRequest>(SearchRequestFunctions.Serialize);
			Writer<SearchSession>.write = new Action<NetworkWriter, SearchSession>(SearchSessionFunctions.Serialize);
			Writer<DisarmedPlayersListMessage>.write = new Action<NetworkWriter, DisarmedPlayersListMessage>(DisarmedPlayersListMessageSerializers.Serialize);
			Writer<DisarmMessage>.write = new Action<NetworkWriter, DisarmMessage>(DisarmMessageSerializers.Serialize);
			Writer<PickupSyncInfo>.write = new Action<NetworkWriter, PickupSyncInfo>(PickupSyncInfoSerializer.WritePickupSyncInfo);
			Writer<StatusMessage>.write = new Action<NetworkWriter, StatusMessage>(StatusMessageFunctions.Serialize);
			Writer<ItemCooldownMessage>.write = new Action<NetworkWriter, ItemCooldownMessage>(ItemCooldownMessageFunctions.Serialize);
			Writer<SyncScp330Message>.write = new Action<NetworkWriter, SyncScp330Message>(Scp330NetworkHandler.SerializeSyncMessage);
			Writer<SelectScp330Message>.write = new Action<NetworkWriter, SelectScp330Message>(Scp330NetworkHandler.SerializeSelectMessage);
			Writer<FlashlightNetworkHandler.FlashlightMessage>.write = new Action<NetworkWriter, FlashlightNetworkHandler.FlashlightMessage>(FlashlightNetworkHandler.Serialize);
			Writer<RadioStatusMessage>.write = new Action<NetworkWriter, RadioStatusMessage>(RadioMessages.WriteRadioStatusMessage);
			Writer<ClientRadioCommandMessage>.write = new Action<NetworkWriter, ClientRadioCommandMessage>(RadioMessages.WriteClientRadioCommandMessage);
			Writer<ShotBacktrackData>.write = new Action<NetworkWriter, ShotBacktrackData>(ShotBacktrackDataSerializer.WriteBacktrackData);
			Writer<AttachmentCodeSync.AttachmentCodeMessage>.write = new Action<NetworkWriter, AttachmentCodeSync.AttachmentCodeMessage>(AttachmentCodeSync.WriteAttachmentCodeMessage);
			Writer<AttachmentCodeSync.AttachmentCodePackMessage>.write = new Action<NetworkWriter, AttachmentCodeSync.AttachmentCodePackMessage>(AttachmentCodeSync.WriteAttachmentCodePackMessage);
			Writer<AttachmentsChangeRequest>.write = new Action<NetworkWriter, AttachmentsChangeRequest>(AttachmentsMessageSerializers.WriteAttachmentsChangeRequest);
			Writer<AttachmentsSetupPreference>.write = new Action<NetworkWriter, AttachmentsSetupPreference>(AttachmentsMessageSerializers.WriteAttachmentsSetupPreference);
			Writer<ReserveAmmoSync.ReserveAmmoMessage>.write = new Action<NetworkWriter, ReserveAmmoSync.ReserveAmmoMessage>(ReserveAmmoSync.WriteReserveAmmoMessage);
			Writer<AutosyncMessage>.write = new Action<NetworkWriter, AutosyncMessage>(AutosyncMessageHandler.WriteAutosyncMessage);
			Writer<Enum>.write = new Action<NetworkWriter, Enum>(AutosyncMessageUtils.WriteSubheader);
			Writer<ThrowableNetworkHandler.ThrowableItemRequestMessage>.write = new Action<NetworkWriter, ThrowableNetworkHandler.ThrowableItemRequestMessage>(ThrowableNetworkHandler.SerializeRequestMsg);
			Writer<ThrowableNetworkHandler.ThrowableItemAudioMessage>.write = new Action<NetworkWriter, ThrowableNetworkHandler.ThrowableItemAudioMessage>(ThrowableNetworkHandler.SerializeAudioMsg);
			Writer<Hitmarker.HitmarkerMessage>.write = new Action<NetworkWriter, Hitmarker.HitmarkerMessage>(GeneratedNetworkCode._Write_Hitmarker/HitmarkerMessage);
			Writer<Escape.EscapeMessage>.write = new Action<NetworkWriter, Escape.EscapeMessage>(GeneratedNetworkCode._Write_Escape/EscapeMessage);
			Writer<ServerShutdown.ServerShutdownMessage>.write = new Action<NetworkWriter, ServerShutdown.ServerShutdownMessage>(GeneratedNetworkCode._Write_ServerShutdown/ServerShutdownMessage);
			Writer<VoiceChatMuteIndicator.SyncMuteMessage>.write = new Action<NetworkWriter, VoiceChatMuteIndicator.SyncMuteMessage>(GeneratedNetworkCode._Write_VoiceChat.VoiceChatMuteIndicator/SyncMuteMessage);
			Writer<VoiceChatPrivacySettings.VcPrivacyMessage>.write = new Action<NetworkWriter, VoiceChatPrivacySettings.VcPrivacyMessage>(GeneratedNetworkCode._Write_VoiceChat.VoiceChatPrivacySettings/VcPrivacyMessage);
			Writer<PersonalRadioPlayback.TransmitterPositionMessage>.write = new Action<NetworkWriter, PersonalRadioPlayback.TransmitterPositionMessage>(GeneratedNetworkCode._Write_VoiceChat.Playbacks.PersonalRadioPlayback/TransmitterPositionMessage);
			Writer<LockerWaypoint.LockerWaypointAssignMessage>.write = new Action<NetworkWriter, LockerWaypoint.LockerWaypointAssignMessage>(GeneratedNetworkCode._Write_RelativePositioning.LockerWaypoint/LockerWaypointAssignMessage);
			Writer<VoiceChatReceivePrefs.GroupMuteFlagsMessage>.write = new Action<NetworkWriter, VoiceChatReceivePrefs.GroupMuteFlagsMessage>(GeneratedNetworkCode._Write_PlayerRoles.Voice.VoiceChatReceivePrefs/GroupMuteFlagsMessage);
			Writer<OverwatchVoiceChannelSelector.ChannelMuteFlagsMessage>.write = new Action<NetworkWriter, OverwatchVoiceChannelSelector.ChannelMuteFlagsMessage>(GeneratedNetworkCode._Write_PlayerRoles.Spectating.OverwatchVoiceChannelSelector/ChannelMuteFlagsMessage);
			Writer<SpectatorNetworking.SpectatedNetIdSyncMessage>.write = new Action<NetworkWriter, SpectatorNetworking.SpectatedNetIdSyncMessage>(GeneratedNetworkCode._Write_PlayerRoles.Spectating.SpectatorNetworking/SpectatedNetIdSyncMessage);
			Writer<Scp106PocketItemManager.WarningMessage>.write = new Action<NetworkWriter, Scp106PocketItemManager.WarningMessage>(GeneratedNetworkCode._Write_PlayerRoles.PlayableScps.Scp106.Scp106PocketItemManager/WarningMessage);
			Writer<ZombieConfirmationBox.ScpReviveBlockMessage>.write = new Action<NetworkWriter, ZombieConfirmationBox.ScpReviveBlockMessage>(GeneratedNetworkCode._Write_PlayerRoles.PlayableScps.Scp049.Zombies.ZombieConfirmationBox/ScpReviveBlockMessage);
			Writer<DynamicHumeShieldController.ShieldBreakMessage>.write = new Action<NetworkWriter, DynamicHumeShieldController.ShieldBreakMessage>(GeneratedNetworkCode._Write_PlayerRoles.PlayableScps.HumeShield.DynamicHumeShieldController/ShieldBreakMessage);
			Writer<FpcRotationOverrideMessage>.write = new Action<NetworkWriter, FpcRotationOverrideMessage>(GeneratedNetworkCode._Write_PlayerRoles.FirstPersonControl.NetworkMessages.FpcRotationOverrideMessage);
			Writer<FpcNoclipToggleMessage>.write = new Action<NetworkWriter, FpcNoclipToggleMessage>(GeneratedNetworkCode._Write_PlayerRoles.FirstPersonControl.NetworkMessages.FpcNoclipToggleMessage);
			Writer<EmotionSync.EmotionSyncMessage>.write = new Action<NetworkWriter, EmotionSync.EmotionSyncMessage>(GeneratedNetworkCode._Write_PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.EmotionSync/EmotionSyncMessage);
			Writer<EmotionPresetType>.write = new Action<NetworkWriter, EmotionPresetType>(GeneratedNetworkCode._Write_PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.EmotionPresetType);
			Writer<WearableSync.WearableSyncMessage>.write = new Action<NetworkWriter, WearableSync.WearableSyncMessage>(GeneratedNetworkCode._Write_PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.WearableSync/WearableSyncMessage);
			Writer<WearableElements>.write = new Action<NetworkWriter, WearableElements>(GeneratedNetworkCode._Write_PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.WearableElements);
			Writer<ExplosionUtils.GrenadeExplosionMessage>.write = new Action<NetworkWriter, ExplosionUtils.GrenadeExplosionMessage>(GeneratedNetworkCode._Write_Utils.ExplosionUtils/GrenadeExplosionMessage);
			Writer<SeedSynchronizer.SeedMessage>.write = new Action<NetworkWriter, SeedSynchronizer.SeedMessage>(GeneratedNetworkCode._Write_MapGeneration.SeedSynchronizer/SeedMessage);
			Writer<global::CustomPlayerEffects.AntiScp207.BreakMessage>.write = new Action<NetworkWriter, global::CustomPlayerEffects.AntiScp207.BreakMessage>(GeneratedNetworkCode._Write_CustomPlayerEffects.AntiScp207/BreakMessage);
			Writer<InfluenceUpdateMessage>.write = new Action<NetworkWriter, InfluenceUpdateMessage>(GeneratedNetworkCode._Write_Respawning.InfluenceUpdateMessage);
			Writer<Faction>.write = new Action<NetworkWriter, Faction>(GeneratedNetworkCode._Write_PlayerRoles.Faction);
			Writer<StripdownNetworking.StripdownResponse>.write = new Action<NetworkWriter, StripdownNetworking.StripdownResponse>(GeneratedNetworkCode._Write_CommandSystem.Commands.RemoteAdmin.Stripdown.StripdownNetworking/StripdownResponse);
			Writer<string[]>.write = new Action<NetworkWriter, string[]>(GeneratedNetworkCode._Write_System.String[]);
			Writer<AchievementManager.AchievementMessage>.write = new Action<NetworkWriter, AchievementManager.AchievementMessage>(GeneratedNetworkCode._Write_Achievements.AchievementManager/AchievementMessage);
			Writer<HumeShieldSubEffect.HumeBlockMsg>.write = new Action<NetworkWriter, HumeShieldSubEffect.HumeBlockMsg>(GeneratedNetworkCode._Write_InventorySystem.Items.Usables.Scp244.Hypothermia.HumeShieldSubEffect/HumeBlockMsg);
			Writer<Hypothermia.ForcedHypothermiaMessage>.write = new Action<NetworkWriter, Hypothermia.ForcedHypothermiaMessage>(GeneratedNetworkCode._Write_InventorySystem.Items.Usables.Scp244.Hypothermia.Hypothermia/ForcedHypothermiaMessage);
			Writer<Scp1576SpectatorWarningHandler.SpectatorWarningMessage>.write = new Action<NetworkWriter, Scp1576SpectatorWarningHandler.SpectatorWarningMessage>(GeneratedNetworkCode._Write_InventorySystem.Items.Usables.Scp1576.Scp1576SpectatorWarningHandler/SpectatorWarningMessage);
			Writer<Scp1344DetectionMessage>.write = new Action<NetworkWriter, Scp1344DetectionMessage>(GeneratedNetworkCode._Write_InventorySystem.Items.Usables.Scp1344.Scp1344DetectionMessage);
			Writer<Scp1344StatusMessage>.write = new Action<NetworkWriter, Scp1344StatusMessage>(GeneratedNetworkCode._Write_InventorySystem.Items.Usables.Scp1344.Scp1344StatusMessage);
			Writer<Scp1344Status>.write = new Action<NetworkWriter, Scp1344Status>(GeneratedNetworkCode._Write_InventorySystem.Items.Usables.Scp1344.Scp1344Status);
			Writer<KeycardItem.UseMessage>.write = new Action<NetworkWriter, KeycardItem.UseMessage>(GeneratedNetworkCode._Write_InventorySystem.Items.Keycards.KeycardItem/UseMessage);
			Writer<DamageIndicatorMessage>.write = new Action<NetworkWriter, DamageIndicatorMessage>(GeneratedNetworkCode._Write_InventorySystem.Items.Firearms.BasicMessages.DamageIndicatorMessage);
			Writer<ServerConfigSynchronizer.PredefinedBanTemplate>.write = new Action<NetworkWriter, ServerConfigSynchronizer.PredefinedBanTemplate>(GeneratedNetworkCode._Write_ServerConfigSynchronizer/PredefinedBanTemplate);
			Writer<Broadcast.BroadcastFlags>.write = new Action<NetworkWriter, Broadcast.BroadcastFlags>(GeneratedNetworkCode._Write_Broadcast/BroadcastFlags);
			Writer<KeyCode>.write = new Action<NetworkWriter, KeyCode>(GeneratedNetworkCode._Write_UnityEngine.KeyCode);
			Writer<PlayerInfoArea>.write = new Action<NetworkWriter, PlayerInfoArea>(GeneratedNetworkCode._Write_PlayerInfoArea);
			Writer<PlayerInteract.AlphaPanelOperations>.write = new Action<NetworkWriter, PlayerInteract.AlphaPanelOperations>(GeneratedNetworkCode._Write_PlayerInteract/AlphaPanelOperations);
			Writer<RoundSummary.SumInfo_ClassList>.write = new Action<NetworkWriter, RoundSummary.SumInfo_ClassList>(GeneratedNetworkCode._Write_RoundSummary/SumInfo_ClassList);
			Writer<RoundSummary.LeadingTeam>.write = new Action<NetworkWriter, RoundSummary.LeadingTeam>(GeneratedNetworkCode._Write_RoundSummary/LeadingTeam);
			Writer<ServerRoles.BadgePreferences>.write = new Action<NetworkWriter, ServerRoles.BadgePreferences>(GeneratedNetworkCode._Write_ServerRoles/BadgePreferences);
			Writer<ServerRoles.BadgeVisibilityPreferences>.write = new Action<NetworkWriter, ServerRoles.BadgeVisibilityPreferences>(GeneratedNetworkCode._Write_ServerRoles/BadgeVisibilityPreferences);
			Writer<QueryProcessor.CommandData[]>.write = new Action<NetworkWriter, QueryProcessor.CommandData[]>(GeneratedNetworkCode._Write_RemoteAdmin.QueryProcessor/CommandData[]);
			Writer<QueryProcessor.CommandData>.write = new Action<NetworkWriter, QueryProcessor.CommandData>(GeneratedNetworkCode._Write_RemoteAdmin.QueryProcessor/CommandData);
			Writer<DecontaminationController.DecontaminationStatus>.write = new Action<NetworkWriter, DecontaminationController.DecontaminationStatus>(GeneratedNetworkCode._Write_LightContainmentZoneDecontamination.DecontaminationController/DecontaminationStatus);
			Writer<LightShadows>.write = new Action<NetworkWriter, LightShadows>(GeneratedNetworkCode._Write_UnityEngine.LightShadows);
			Writer<LightType>.write = new Action<NetworkWriter, LightType>(GeneratedNetworkCode._Write_UnityEngine.LightType);
			Writer<LightShape>.write = new Action<NetworkWriter, LightShape>(GeneratedNetworkCode._Write_UnityEngine.LightShape);
			Writer<PrimitiveType>.write = new Action<NetworkWriter, PrimitiveType>(GeneratedNetworkCode._Write_UnityEngine.PrimitiveType);
			Writer<PrimitiveFlags>.write = new Action<NetworkWriter, PrimitiveFlags>(GeneratedNetworkCode._Write_AdminToys.PrimitiveFlags);
			Writer<Scp914KnobSetting>.write = new Action<NetworkWriter, Scp914KnobSetting>(GeneratedNetworkCode._Write_Scp914.Scp914KnobSetting);
			Writer<ElevatorGroup>.write = new Action<NetworkWriter, ElevatorGroup>(GeneratedNetworkCode._Write_Interactables.Interobjects.ElevatorGroup);
			Writer<ItemIdentifier[]>.write = new Action<NetworkWriter, ItemIdentifier[]>(GeneratedNetworkCode._Write_InventorySystem.Items.ItemIdentifier[]);
			Writer<ItemIdentifier>.write = new Action<NetworkWriter, ItemIdentifier>(GeneratedNetworkCode._Write_InventorySystem.Items.ItemIdentifier);
			Writer<ItemType>.write = new Action<NetworkWriter, ItemType>(GeneratedNetworkCode._Write_ItemType);
			Writer<ushort[]>.write = new Action<NetworkWriter, ushort[]>(GeneratedNetworkCode._Write_System.UInt16[]);
			Writer<CandyKindID>.write = new Action<NetworkWriter, CandyKindID>(GeneratedNetworkCode._Write_InventorySystem.Items.Usables.Scp330.CandyKindID);
			Writer<JailbirdWearState>.write = new Action<NetworkWriter, JailbirdWearState>(GeneratedNetworkCode._Write_InventorySystem.Items.Jailbird.JailbirdWearState);
			Writer<Team>.write = new Action<NetworkWriter, Team>(GeneratedNetworkCode._Write_PlayerRoles.Team);
			Reader<byte>.read = new Func<NetworkReader, byte>(NetworkReaderExtensions.ReadByte);
			Reader<byte?>.read = new Func<NetworkReader, byte?>(NetworkReaderExtensions.ReadByteNullable);
			Reader<sbyte>.read = new Func<NetworkReader, sbyte>(NetworkReaderExtensions.ReadSByte);
			Reader<sbyte?>.read = new Func<NetworkReader, sbyte?>(NetworkReaderExtensions.ReadSByteNullable);
			Reader<char>.read = new Func<NetworkReader, char>(NetworkReaderExtensions.ReadChar);
			Reader<char?>.read = new Func<NetworkReader, char?>(NetworkReaderExtensions.ReadCharNullable);
			Reader<bool>.read = new Func<NetworkReader, bool>(NetworkReaderExtensions.ReadBool);
			Reader<bool?>.read = new Func<NetworkReader, bool?>(NullableBoolReaderWriter.ReadNullableBool);
			Reader<short>.read = new Func<NetworkReader, short>(NetworkReaderExtensions.ReadShort);
			Reader<short?>.read = new Func<NetworkReader, short?>(NetworkReaderExtensions.ReadShortNullable);
			Reader<ushort>.read = new Func<NetworkReader, ushort>(NetworkReaderExtensions.ReadUShort);
			Reader<ushort?>.read = new Func<NetworkReader, ushort?>(NetworkReaderExtensions.ReadUShortNullable);
			Reader<int>.read = new Func<NetworkReader, int>(NetworkReaderExtensions.ReadInt);
			Reader<int?>.read = new Func<NetworkReader, int?>(NetworkReaderExtensions.ReadIntNullable);
			Reader<uint>.read = new Func<NetworkReader, uint>(NetworkReaderExtensions.ReadUInt);
			Reader<uint?>.read = new Func<NetworkReader, uint?>(NetworkReaderExtensions.ReadUIntNullable);
			Reader<long>.read = new Func<NetworkReader, long>(NetworkReaderExtensions.ReadLong);
			Reader<long?>.read = new Func<NetworkReader, long?>(NetworkReaderExtensions.ReadLongNullable);
			Reader<ulong>.read = new Func<NetworkReader, ulong>(NetworkReaderExtensions.ReadULong);
			Reader<ulong?>.read = new Func<NetworkReader, ulong?>(NetworkReaderExtensions.ReadULongNullable);
			Reader<float>.read = new Func<NetworkReader, float>(NetworkReaderExtensions.ReadFloat);
			Reader<float?>.read = new Func<NetworkReader, float?>(NetworkReaderExtensions.ReadFloatNullable);
			Reader<double>.read = new Func<NetworkReader, double>(NetworkReaderExtensions.ReadDouble);
			Reader<double?>.read = new Func<NetworkReader, double?>(NetworkReaderExtensions.ReadDoubleNullable);
			Reader<decimal>.read = new Func<NetworkReader, decimal>(NetworkReaderExtensions.ReadDecimal);
			Reader<decimal?>.read = new Func<NetworkReader, decimal?>(NetworkReaderExtensions.ReadDecimalNullable);
			Reader<string>.read = new Func<NetworkReader, string>(NetworkReaderExtensions.ReadString);
			Reader<byte[]>.read = new Func<NetworkReader, byte[]>(NetworkReaderExtensions.ReadBytesAndSize);
			Reader<ArraySegment<byte>>.read = new Func<NetworkReader, ArraySegment<byte>>(NetworkReaderExtensions.ReadArraySegmentAndSize);
			Reader<Vector2>.read = new Func<NetworkReader, Vector2>(NetworkReaderExtensions.ReadVector2);
			Reader<Vector2?>.read = new Func<NetworkReader, Vector2?>(NetworkReaderExtensions.ReadVector2Nullable);
			Reader<Vector3>.read = new Func<NetworkReader, Vector3>(NetworkReaderExtensions.ReadVector3);
			Reader<Vector3?>.read = new Func<NetworkReader, Vector3?>(NetworkReaderExtensions.ReadVector3Nullable);
			Reader<Vector4>.read = new Func<NetworkReader, Vector4>(NetworkReaderExtensions.ReadVector4);
			Reader<Vector4?>.read = new Func<NetworkReader, Vector4?>(NetworkReaderExtensions.ReadVector4Nullable);
			Reader<Vector2Int>.read = new Func<NetworkReader, Vector2Int>(NetworkReaderExtensions.ReadVector2Int);
			Reader<Vector2Int?>.read = new Func<NetworkReader, Vector2Int?>(NetworkReaderExtensions.ReadVector2IntNullable);
			Reader<Vector3Int>.read = new Func<NetworkReader, Vector3Int>(NetworkReaderExtensions.ReadVector3Int);
			Reader<Vector3Int?>.read = new Func<NetworkReader, Vector3Int?>(NetworkReaderExtensions.ReadVector3IntNullable);
			Reader<Color>.read = new Func<NetworkReader, Color>(NetworkReaderExtensions.ReadColor);
			Reader<Color?>.read = new Func<NetworkReader, Color?>(NetworkReaderExtensions.ReadColorNullable);
			Reader<Color32>.read = new Func<NetworkReader, Color32>(NetworkReaderExtensions.ReadColor32);
			Reader<Color32?>.read = new Func<NetworkReader, Color32?>(NetworkReaderExtensions.ReadColor32Nullable);
			Reader<Quaternion>.read = new Func<NetworkReader, Quaternion>(NetworkReaderExtensions.ReadQuaternion);
			Reader<Quaternion?>.read = new Func<NetworkReader, Quaternion?>(NetworkReaderExtensions.ReadQuaternionNullable);
			Reader<Rect>.read = new Func<NetworkReader, Rect>(NetworkReaderExtensions.ReadRect);
			Reader<Rect?>.read = new Func<NetworkReader, Rect?>(NetworkReaderExtensions.ReadRectNullable);
			Reader<Plane>.read = new Func<NetworkReader, Plane>(NetworkReaderExtensions.ReadPlane);
			Reader<Plane?>.read = new Func<NetworkReader, Plane?>(NetworkReaderExtensions.ReadPlaneNullable);
			Reader<Ray>.read = new Func<NetworkReader, Ray>(NetworkReaderExtensions.ReadRay);
			Reader<Ray?>.read = new Func<NetworkReader, Ray?>(NetworkReaderExtensions.ReadRayNullable);
			Reader<Matrix4x4>.read = new Func<NetworkReader, Matrix4x4>(NetworkReaderExtensions.ReadMatrix4x4);
			Reader<Matrix4x4?>.read = new Func<NetworkReader, Matrix4x4?>(NetworkReaderExtensions.ReadMatrix4x4Nullable);
			Reader<Guid>.read = new Func<NetworkReader, Guid>(NetworkReaderExtensions.ReadGuid);
			Reader<Guid?>.read = new Func<NetworkReader, Guid?>(NetworkReaderExtensions.ReadGuidNullable);
			Reader<NetworkIdentity>.read = new Func<NetworkReader, NetworkIdentity>(NetworkReaderExtensions.ReadNetworkIdentity);
			Reader<NetworkBehaviour>.read = new Func<NetworkReader, NetworkBehaviour>(NetworkReaderExtensions.ReadNetworkBehaviour);
			Reader<NetworkBehaviourSyncVar>.read = new Func<NetworkReader, NetworkBehaviourSyncVar>(NetworkReaderExtensions.ReadNetworkBehaviourSyncVar);
			Reader<Transform>.read = new Func<NetworkReader, Transform>(NetworkReaderExtensions.ReadTransform);
			Reader<GameObject>.read = new Func<NetworkReader, GameObject>(NetworkReaderExtensions.ReadGameObject);
			Reader<Uri>.read = new Func<NetworkReader, Uri>(NetworkReaderExtensions.ReadUri);
			Reader<Texture2D>.read = new Func<NetworkReader, Texture2D>(NetworkReaderExtensions.ReadTexture2D);
			Reader<Sprite>.read = new Func<NetworkReader, Sprite>(NetworkReaderExtensions.ReadSprite);
			Reader<DateTime>.read = new Func<NetworkReader, DateTime>(NetworkReaderExtensions.ReadDateTime);
			Reader<DateTime?>.read = new Func<NetworkReader, DateTime?>(NetworkReaderExtensions.ReadDateTimeNullable);
			Reader<TimeSnapshotMessage>.read = new Func<NetworkReader, TimeSnapshotMessage>(GeneratedNetworkCode._Read_Mirror.TimeSnapshotMessage);
			Reader<ReadyMessage>.read = new Func<NetworkReader, ReadyMessage>(GeneratedNetworkCode._Read_Mirror.ReadyMessage);
			Reader<NotReadyMessage>.read = new Func<NetworkReader, NotReadyMessage>(GeneratedNetworkCode._Read_Mirror.NotReadyMessage);
			Reader<AddPlayerMessage>.read = new Func<NetworkReader, AddPlayerMessage>(GeneratedNetworkCode._Read_Mirror.AddPlayerMessage);
			Reader<SceneMessage>.read = new Func<NetworkReader, SceneMessage>(GeneratedNetworkCode._Read_Mirror.SceneMessage);
			Reader<SceneOperation>.read = new Func<NetworkReader, SceneOperation>(GeneratedNetworkCode._Read_Mirror.SceneOperation);
			Reader<CommandMessage>.read = new Func<NetworkReader, CommandMessage>(GeneratedNetworkCode._Read_Mirror.CommandMessage);
			Reader<RpcMessage>.read = new Func<NetworkReader, RpcMessage>(GeneratedNetworkCode._Read_Mirror.RpcMessage);
			Reader<SpawnMessage>.read = new Func<NetworkReader, SpawnMessage>(GeneratedNetworkCode._Read_Mirror.SpawnMessage);
			Reader<ChangeOwnerMessage>.read = new Func<NetworkReader, ChangeOwnerMessage>(GeneratedNetworkCode._Read_Mirror.ChangeOwnerMessage);
			Reader<ObjectSpawnStartedMessage>.read = new Func<NetworkReader, ObjectSpawnStartedMessage>(GeneratedNetworkCode._Read_Mirror.ObjectSpawnStartedMessage);
			Reader<ObjectSpawnFinishedMessage>.read = new Func<NetworkReader, ObjectSpawnFinishedMessage>(GeneratedNetworkCode._Read_Mirror.ObjectSpawnFinishedMessage);
			Reader<ObjectDestroyMessage>.read = new Func<NetworkReader, ObjectDestroyMessage>(GeneratedNetworkCode._Read_Mirror.ObjectDestroyMessage);
			Reader<ObjectHideMessage>.read = new Func<NetworkReader, ObjectHideMessage>(GeneratedNetworkCode._Read_Mirror.ObjectHideMessage);
			Reader<EntityStateMessage>.read = new Func<NetworkReader, EntityStateMessage>(GeneratedNetworkCode._Read_Mirror.EntityStateMessage);
			Reader<NetworkPingMessage>.read = new Func<NetworkReader, NetworkPingMessage>(GeneratedNetworkCode._Read_Mirror.NetworkPingMessage);
			Reader<NetworkPongMessage>.read = new Func<NetworkReader, NetworkPongMessage>(GeneratedNetworkCode._Read_Mirror.NetworkPongMessage);
			Reader<AlphaWarheadSyncInfo>.read = new Func<NetworkReader, AlphaWarheadSyncInfo>(AlphaWarheadSyncInfoSerializer.ReadAlphaWarheadSyncInfo);
			Reader<RecyclablePlayerId>.read = new Func<NetworkReader, RecyclablePlayerId>(RecyclablePlayerIdReaderWriter.ReadRecyclablePlayerId);
			Reader<ServerConfigSynchronizer.AmmoLimit>.read = new Func<NetworkReader, ServerConfigSynchronizer.AmmoLimit>(AmmoLimitSerializer.ReadAmmoLimit);
			Reader<TeslaHitMsg>.read = new Func<NetworkReader, TeslaHitMsg>(TeslaHitMsgSerializers.Deserialize);
			Reader<EncryptedChannelManager.EncryptedMessageOutside>.read = new Func<NetworkReader, EncryptedChannelManager.EncryptedMessageOutside>(EncryptedChannelFunctions.DeserializeEncryptedMessageOutside);
			Reader<Offset>.read = new Func<NetworkReader, Offset>(OffsetSerializer.ReadOffset);
			Reader<LowPrecisionQuaternion>.read = new Func<NetworkReader, LowPrecisionQuaternion>(LowPrecisionQuaternionSerializer.ReadLowPrecisionQuaternion);
			Reader<SSSEntriesPack>.read = new Func<NetworkReader, SSSEntriesPack>(SSSNetworkMessageFunctions.DeserializeSSSEntriesPack);
			Reader<SSSClientResponse>.read = new Func<NetworkReader, SSSClientResponse>(SSSNetworkMessageFunctions.DeserializeSSSClientResponse);
			Reader<SSSUserStatusReport>.read = new Func<NetworkReader, SSSUserStatusReport>(SSSNetworkMessageFunctions.DeserializeSSSVersionSelfReport);
			Reader<SSSUpdateMessage>.read = new Func<NetworkReader, SSSUpdateMessage>(SSSNetworkMessageFunctions.DeserializeSSSUpdateMessage);
			Reader<AudioMessage>.read = new Func<NetworkReader, AudioMessage>(AudioMessageReadersWriters.DeserializeVoiceMessage);
			Reader<VoiceMessage>.read = new Func<NetworkReader, VoiceMessage>(VoiceMessageReadersWriters.DeserializeVoiceMessage);
			Reader<SubtitleMessage>.read = new Func<NetworkReader, SubtitleMessage>(SubtitleMessageExtensions.Deserialize);
			Reader<RoundRestartMessage>.read = new Func<NetworkReader, RoundRestartMessage>(RoundRestartMessageReaderWriter.ReadRoundRestartMessage);
			Reader<RelativePosition>.read = new Func<NetworkReader, RelativePosition>(RelativePositionSerialization.ReadRelativePosition);
			Reader<DamageHandlerBase>.read = new Func<NetworkReader, DamageHandlerBase>(DamageHandlerReaderWriter.ReadDamageHandler);
			Reader<SyncedStatMessages.StatMessage>.read = new Func<NetworkReader, SyncedStatMessages.StatMessage>(SyncedStatMessages.Deserialize);
			Reader<RoleTypeId>.read = new Func<NetworkReader, RoleTypeId>(PlayerRoleEnumsReadersWriters.ReadRoleType);
			Reader<RoleSyncInfo>.read = new Func<NetworkReader, RoleSyncInfo>(PlayerRolesNetUtils.ReadRoleSyncInfo);
			Reader<RoleSyncInfoPack>.read = new Func<NetworkReader, RoleSyncInfoPack>(PlayerRolesNetUtils.ReadRoleSyncInfoPack);
			Reader<SubroutineMessage>.read = new Func<NetworkReader, SubroutineMessage>(SubroutineMessageReaderWriter.ReadSubroutineMessage);
			Reader<SpectatorSpawnReason>.read = new Func<NetworkReader, SpectatorSpawnReason>(SpectatorSpawnReasonReaderWriter.ReadSpawnReason);
			Reader<ScpSpawnPreferences.SpawnPreferences>.read = new Func<NetworkReader, ScpSpawnPreferences.SpawnPreferences>(ScpSpawnPreferences.ReadSpawnPreferences);
			Reader<RagdollData>.read = new Func<NetworkReader, RagdollData>(RagdollDataReaderWriter.ReadRagdollData);
			Reader<SyncedGravityMessages.GravityMessage>.read = new Func<NetworkReader, SyncedGravityMessages.GravityMessage>(SyncedGravityMessages.Deserialize);
			Reader<FpcFromClientMessage>.read = new Func<NetworkReader, FpcFromClientMessage>(FpcMessagesReadersWriters.ReadFpcFromClientMessage);
			Reader<FpcPositionMessage>.read = new Func<NetworkReader, FpcPositionMessage>(FpcMessagesReadersWriters.ReadFpcPositionMessage);
			Reader<FpcPositionOverrideMessage>.read = new Func<NetworkReader, FpcPositionOverrideMessage>(FpcMessagesReadersWriters.ReadFpcRotationOverrideMessage);
			Reader<FpcFallDamageMessage>.read = new Func<NetworkReader, FpcFallDamageMessage>(FpcMessagesReadersWriters.ReadFpcFallDamageMessage);
			Reader<SubcontrollerRpcHandler.SubcontrollerRpcMessage>.read = new Func<NetworkReader, SubcontrollerRpcHandler.SubcontrollerRpcMessage>(SubcontrollerRpcHandler.ReadSubcontrollerRpcMessage);
			Reader<AnimationCurve>.read = new Func<NetworkReader, AnimationCurve>(AnimationCurveReaderWriter.ReadAnimationCurve);
			Reader<HintEffect[]>.read = new Func<NetworkReader, HintEffect[]>(HintEffectArrayReaderWriter.ReadHintEffectArray);
			Reader<HintEffect>.read = new Func<NetworkReader, HintEffect>(HintEffectReaderWriter.ReadHintEffect);
			Reader<HintParameter[]>.read = new Func<NetworkReader, HintParameter[]>(HintParameterArrayReaderWriter.ReadHintParameterArray);
			Reader<HintParameter>.read = new Func<NetworkReader, HintParameter>(HintParameterReaderWriter.ReadHintParameter);
			Reader<Hint>.read = new Func<NetworkReader, Hint>(HintReaderWriter.ReadHint);
			Reader<ReferenceHub>.read = new Func<NetworkReader, ReferenceHub>(ReferenceHubReaderWriter.ReadReferenceHub);
			Reader<AlphaCurveHintEffect>.read = new Func<NetworkReader, AlphaCurveHintEffect>(AlphaCurveHintEffectFunctions.Deserialize);
			Reader<AlphaEffect>.read = new Func<NetworkReader, AlphaEffect>(AlphaEffectFunctions.Deserialize);
			Reader<OutlineEffect>.read = new Func<NetworkReader, OutlineEffect>(OutlineEffectFunctions.Deserialize);
			Reader<TextHint>.read = new Func<NetworkReader, TextHint>(TextHintFunctions.Deserialize);
			Reader<TranslationHint>.read = new Func<NetworkReader, TranslationHint>(TranslationHintFunctions.Deserialize);
			Reader<AmmoHintParameter>.read = new Func<NetworkReader, AmmoHintParameter>(AmmoHintParameterFunctions.Deserialize);
			Reader<Scp330HintParameter>.read = new Func<NetworkReader, Scp330HintParameter>(Scp330HintParameterFunctions.Deserialize);
			Reader<ItemCategoryHintParameter>.read = new Func<NetworkReader, ItemCategoryHintParameter>(ItemCategoryHintParameterFunctions.Deserialize);
			Reader<ItemHintParameter>.read = new Func<NetworkReader, ItemHintParameter>(ItemHintParameterFunctions.Deserialize);
			Reader<ByteHintParameter>.read = new Func<NetworkReader, ByteHintParameter>(ByteHintParameterFunctions.Deserialize);
			Reader<DoubleHintParameter>.read = new Func<NetworkReader, DoubleHintParameter>(DoubleHintParameterFunctions.Deserialize);
			Reader<FloatHintParameter>.read = new Func<NetworkReader, FloatHintParameter>(FloatHintParameterFunctions.Deserialize);
			Reader<IntHintParameter>.read = new Func<NetworkReader, IntHintParameter>(IntHintParameterFunctions.Deserialize);
			Reader<LongHintParameter>.read = new Func<NetworkReader, LongHintParameter>(LongHintParameterFunctions.Deserialize);
			Reader<PackedLongHintParameter>.read = new Func<NetworkReader, PackedLongHintParameter>(PackedLongHintParameterFunctions.Deserialize);
			Reader<PackedULongHintParameter>.read = new Func<NetworkReader, PackedULongHintParameter>(PackedULongHintParameterFunctions.Deserialize);
			Reader<SByteHintParameter>.read = new Func<NetworkReader, SByteHintParameter>(SByteHintParameterFunctions.Deserialize);
			Reader<ShortHintParameter>.read = new Func<NetworkReader, ShortHintParameter>(ShortHintParameterFunctions.Deserialize);
			Reader<StringHintParameter>.read = new Func<NetworkReader, StringHintParameter>(StringHintParameterFunctions.Deserialize);
			Reader<TimespanHintParameter>.read = new Func<NetworkReader, TimespanHintParameter>(TimespanHintParameterFunctions.Deserialize);
			Reader<UIntHintParameter>.read = new Func<NetworkReader, UIntHintParameter>(UIntHintParameterFunctions.Deserialize);
			Reader<ULongHintParameter>.read = new Func<NetworkReader, ULongHintParameter>(ULongHintParameterFunctions.Deserialize);
			Reader<UShortHintParameter>.read = new Func<NetworkReader, UShortHintParameter>(UShortHintParameterFunctions.Deserialize);
			Reader<HintMessage>.read = new Func<NetworkReader, HintMessage>(HintMessageParameterFunctions.Deserialize);
			Reader<ObjectiveCompletionMessage>.read = new Func<NetworkReader, ObjectiveCompletionMessage>(ObjectiveCompletionMessageUtility.ReadCompletionMessage);
			Reader<WaveUpdateMessage>.read = new Func<NetworkReader, WaveUpdateMessage>(WaveUpdateMessageUtility.ReadUpdateMessage);
			Reader<UnitNameMessage>.read = new Func<NetworkReader, UnitNameMessage>(UnitNameMessageHandler.ReadUnitName);
			Reader<DecalCleanupMessage>.read = new Func<NetworkReader, DecalCleanupMessage>(DecalCleanupMessageExtensions.ReadRadioStatusMessage);
			Reader<AuthenticationResponse>.read = new Func<NetworkReader, AuthenticationResponse>(AuthenticationResponseFunctions.DeserializeAuthenticationResponse);
			Reader<SearchInvalidation>.read = new Func<NetworkReader, SearchInvalidation>(SearchInvalidationFunctions.Deserialize);
			Reader<SearchRequest>.read = new Func<NetworkReader, SearchRequest>(SearchRequestFunctions.Deserialize);
			Reader<SearchSession>.read = new Func<NetworkReader, SearchSession>(SearchSessionFunctions.Deserialize);
			Reader<DisarmedPlayersListMessage>.read = new Func<NetworkReader, DisarmedPlayersListMessage>(DisarmedPlayersListMessageSerializers.Deserialize);
			Reader<DisarmMessage>.read = new Func<NetworkReader, DisarmMessage>(DisarmMessageSerializers.Deserialize);
			Reader<PickupSyncInfo>.read = new Func<NetworkReader, PickupSyncInfo>(PickupSyncInfoSerializer.ReadPickupSyncInfo);
			Reader<StatusMessage>.read = new Func<NetworkReader, StatusMessage>(StatusMessageFunctions.Deserialize);
			Reader<ItemCooldownMessage>.read = new Func<NetworkReader, ItemCooldownMessage>(ItemCooldownMessageFunctions.Deserialize);
			Reader<SyncScp330Message>.read = new Func<NetworkReader, SyncScp330Message>(Scp330NetworkHandler.DeserializeSyncMessage);
			Reader<SelectScp330Message>.read = new Func<NetworkReader, SelectScp330Message>(Scp330NetworkHandler.DeserializeSelectMessage);
			Reader<FlashlightNetworkHandler.FlashlightMessage>.read = new Func<NetworkReader, FlashlightNetworkHandler.FlashlightMessage>(FlashlightNetworkHandler.Deserialize);
			Reader<RadioStatusMessage>.read = new Func<NetworkReader, RadioStatusMessage>(RadioMessages.ReadRadioStatusMessage);
			Reader<ClientRadioCommandMessage>.read = new Func<NetworkReader, ClientRadioCommandMessage>(RadioMessages.ReadClientRadioCommandMessage);
			Reader<ShotBacktrackData>.read = new Func<NetworkReader, ShotBacktrackData>(ShotBacktrackDataSerializer.ReadBacktrackData);
			Reader<AttachmentCodeSync.AttachmentCodeMessage>.read = new Func<NetworkReader, AttachmentCodeSync.AttachmentCodeMessage>(AttachmentCodeSync.ReadAttachmentCodeMessage);
			Reader<AttachmentCodeSync.AttachmentCodePackMessage>.read = new Func<NetworkReader, AttachmentCodeSync.AttachmentCodePackMessage>(AttachmentCodeSync.ReadAttachmentCodePackMessage);
			Reader<AttachmentsChangeRequest>.read = new Func<NetworkReader, AttachmentsChangeRequest>(AttachmentsMessageSerializers.ReadAttachmentsChangeRequest);
			Reader<AttachmentsSetupPreference>.read = new Func<NetworkReader, AttachmentsSetupPreference>(AttachmentsMessageSerializers.ReadAttachmentsSetupPreference);
			Reader<ReserveAmmoSync.ReserveAmmoMessage>.read = new Func<NetworkReader, ReserveAmmoSync.ReserveAmmoMessage>(ReserveAmmoSync.ReadReserveAmmoMessage);
			Reader<AutosyncMessage>.read = new Func<NetworkReader, AutosyncMessage>(AutosyncMessageHandler.ReadAutosyncMessage);
			Reader<ThrowableNetworkHandler.ThrowableItemRequestMessage>.read = new Func<NetworkReader, ThrowableNetworkHandler.ThrowableItemRequestMessage>(ThrowableNetworkHandler.DeserializeRequestMsg);
			Reader<ThrowableNetworkHandler.ThrowableItemAudioMessage>.read = new Func<NetworkReader, ThrowableNetworkHandler.ThrowableItemAudioMessage>(ThrowableNetworkHandler.DeserializeAudioMsg);
			Reader<Hitmarker.HitmarkerMessage>.read = new Func<NetworkReader, Hitmarker.HitmarkerMessage>(GeneratedNetworkCode._Read_Hitmarker/HitmarkerMessage);
			Reader<Escape.EscapeMessage>.read = new Func<NetworkReader, Escape.EscapeMessage>(GeneratedNetworkCode._Read_Escape/EscapeMessage);
			Reader<ServerShutdown.ServerShutdownMessage>.read = new Func<NetworkReader, ServerShutdown.ServerShutdownMessage>(GeneratedNetworkCode._Read_ServerShutdown/ServerShutdownMessage);
			Reader<VoiceChatMuteIndicator.SyncMuteMessage>.read = new Func<NetworkReader, VoiceChatMuteIndicator.SyncMuteMessage>(GeneratedNetworkCode._Read_VoiceChat.VoiceChatMuteIndicator/SyncMuteMessage);
			Reader<VoiceChatPrivacySettings.VcPrivacyMessage>.read = new Func<NetworkReader, VoiceChatPrivacySettings.VcPrivacyMessage>(GeneratedNetworkCode._Read_VoiceChat.VoiceChatPrivacySettings/VcPrivacyMessage);
			Reader<PersonalRadioPlayback.TransmitterPositionMessage>.read = new Func<NetworkReader, PersonalRadioPlayback.TransmitterPositionMessage>(GeneratedNetworkCode._Read_VoiceChat.Playbacks.PersonalRadioPlayback/TransmitterPositionMessage);
			Reader<LockerWaypoint.LockerWaypointAssignMessage>.read = new Func<NetworkReader, LockerWaypoint.LockerWaypointAssignMessage>(GeneratedNetworkCode._Read_RelativePositioning.LockerWaypoint/LockerWaypointAssignMessage);
			Reader<VoiceChatReceivePrefs.GroupMuteFlagsMessage>.read = new Func<NetworkReader, VoiceChatReceivePrefs.GroupMuteFlagsMessage>(GeneratedNetworkCode._Read_PlayerRoles.Voice.VoiceChatReceivePrefs/GroupMuteFlagsMessage);
			Reader<OverwatchVoiceChannelSelector.ChannelMuteFlagsMessage>.read = new Func<NetworkReader, OverwatchVoiceChannelSelector.ChannelMuteFlagsMessage>(GeneratedNetworkCode._Read_PlayerRoles.Spectating.OverwatchVoiceChannelSelector/ChannelMuteFlagsMessage);
			Reader<SpectatorNetworking.SpectatedNetIdSyncMessage>.read = new Func<NetworkReader, SpectatorNetworking.SpectatedNetIdSyncMessage>(GeneratedNetworkCode._Read_PlayerRoles.Spectating.SpectatorNetworking/SpectatedNetIdSyncMessage);
			Reader<Scp106PocketItemManager.WarningMessage>.read = new Func<NetworkReader, Scp106PocketItemManager.WarningMessage>(GeneratedNetworkCode._Read_PlayerRoles.PlayableScps.Scp106.Scp106PocketItemManager/WarningMessage);
			Reader<ZombieConfirmationBox.ScpReviveBlockMessage>.read = new Func<NetworkReader, ZombieConfirmationBox.ScpReviveBlockMessage>(GeneratedNetworkCode._Read_PlayerRoles.PlayableScps.Scp049.Zombies.ZombieConfirmationBox/ScpReviveBlockMessage);
			Reader<DynamicHumeShieldController.ShieldBreakMessage>.read = new Func<NetworkReader, DynamicHumeShieldController.ShieldBreakMessage>(GeneratedNetworkCode._Read_PlayerRoles.PlayableScps.HumeShield.DynamicHumeShieldController/ShieldBreakMessage);
			Reader<FpcRotationOverrideMessage>.read = new Func<NetworkReader, FpcRotationOverrideMessage>(GeneratedNetworkCode._Read_PlayerRoles.FirstPersonControl.NetworkMessages.FpcRotationOverrideMessage);
			Reader<FpcNoclipToggleMessage>.read = new Func<NetworkReader, FpcNoclipToggleMessage>(GeneratedNetworkCode._Read_PlayerRoles.FirstPersonControl.NetworkMessages.FpcNoclipToggleMessage);
			Reader<EmotionSync.EmotionSyncMessage>.read = new Func<NetworkReader, EmotionSync.EmotionSyncMessage>(GeneratedNetworkCode._Read_PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.EmotionSync/EmotionSyncMessage);
			Reader<EmotionPresetType>.read = new Func<NetworkReader, EmotionPresetType>(GeneratedNetworkCode._Read_PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.EmotionPresetType);
			Reader<WearableSync.WearableSyncMessage>.read = new Func<NetworkReader, WearableSync.WearableSyncMessage>(GeneratedNetworkCode._Read_PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.WearableSync/WearableSyncMessage);
			Reader<WearableElements>.read = new Func<NetworkReader, WearableElements>(GeneratedNetworkCode._Read_PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.WearableElements);
			Reader<ExplosionUtils.GrenadeExplosionMessage>.read = new Func<NetworkReader, ExplosionUtils.GrenadeExplosionMessage>(GeneratedNetworkCode._Read_Utils.ExplosionUtils/GrenadeExplosionMessage);
			Reader<SeedSynchronizer.SeedMessage>.read = new Func<NetworkReader, SeedSynchronizer.SeedMessage>(GeneratedNetworkCode._Read_MapGeneration.SeedSynchronizer/SeedMessage);
			Reader<global::CustomPlayerEffects.AntiScp207.BreakMessage>.read = new Func<NetworkReader, global::CustomPlayerEffects.AntiScp207.BreakMessage>(GeneratedNetworkCode._Read_CustomPlayerEffects.AntiScp207/BreakMessage);
			Reader<InfluenceUpdateMessage>.read = new Func<NetworkReader, InfluenceUpdateMessage>(GeneratedNetworkCode._Read_Respawning.InfluenceUpdateMessage);
			Reader<Faction>.read = new Func<NetworkReader, Faction>(GeneratedNetworkCode._Read_PlayerRoles.Faction);
			Reader<StripdownNetworking.StripdownResponse>.read = new Func<NetworkReader, StripdownNetworking.StripdownResponse>(GeneratedNetworkCode._Read_CommandSystem.Commands.RemoteAdmin.Stripdown.StripdownNetworking/StripdownResponse);
			Reader<string[]>.read = new Func<NetworkReader, string[]>(GeneratedNetworkCode._Read_System.String[]);
			Reader<AchievementManager.AchievementMessage>.read = new Func<NetworkReader, AchievementManager.AchievementMessage>(GeneratedNetworkCode._Read_Achievements.AchievementManager/AchievementMessage);
			Reader<HumeShieldSubEffect.HumeBlockMsg>.read = new Func<NetworkReader, HumeShieldSubEffect.HumeBlockMsg>(GeneratedNetworkCode._Read_InventorySystem.Items.Usables.Scp244.Hypothermia.HumeShieldSubEffect/HumeBlockMsg);
			Reader<Hypothermia.ForcedHypothermiaMessage>.read = new Func<NetworkReader, Hypothermia.ForcedHypothermiaMessage>(GeneratedNetworkCode._Read_InventorySystem.Items.Usables.Scp244.Hypothermia.Hypothermia/ForcedHypothermiaMessage);
			Reader<Scp1576SpectatorWarningHandler.SpectatorWarningMessage>.read = new Func<NetworkReader, Scp1576SpectatorWarningHandler.SpectatorWarningMessage>(GeneratedNetworkCode._Read_InventorySystem.Items.Usables.Scp1576.Scp1576SpectatorWarningHandler/SpectatorWarningMessage);
			Reader<Scp1344DetectionMessage>.read = new Func<NetworkReader, Scp1344DetectionMessage>(GeneratedNetworkCode._Read_InventorySystem.Items.Usables.Scp1344.Scp1344DetectionMessage);
			Reader<Scp1344StatusMessage>.read = new Func<NetworkReader, Scp1344StatusMessage>(GeneratedNetworkCode._Read_InventorySystem.Items.Usables.Scp1344.Scp1344StatusMessage);
			Reader<Scp1344Status>.read = new Func<NetworkReader, Scp1344Status>(GeneratedNetworkCode._Read_InventorySystem.Items.Usables.Scp1344.Scp1344Status);
			Reader<KeycardItem.UseMessage>.read = new Func<NetworkReader, KeycardItem.UseMessage>(GeneratedNetworkCode._Read_InventorySystem.Items.Keycards.KeycardItem/UseMessage);
			Reader<DamageIndicatorMessage>.read = new Func<NetworkReader, DamageIndicatorMessage>(GeneratedNetworkCode._Read_InventorySystem.Items.Firearms.BasicMessages.DamageIndicatorMessage);
			Reader<ServerConfigSynchronizer.PredefinedBanTemplate>.read = new Func<NetworkReader, ServerConfigSynchronizer.PredefinedBanTemplate>(GeneratedNetworkCode._Read_ServerConfigSynchronizer/PredefinedBanTemplate);
			Reader<Broadcast.BroadcastFlags>.read = new Func<NetworkReader, Broadcast.BroadcastFlags>(GeneratedNetworkCode._Read_Broadcast/BroadcastFlags);
			Reader<KeyCode>.read = new Func<NetworkReader, KeyCode>(GeneratedNetworkCode._Read_UnityEngine.KeyCode);
			Reader<PlayerInfoArea>.read = new Func<NetworkReader, PlayerInfoArea>(GeneratedNetworkCode._Read_PlayerInfoArea);
			Reader<PlayerInteract.AlphaPanelOperations>.read = new Func<NetworkReader, PlayerInteract.AlphaPanelOperations>(GeneratedNetworkCode._Read_PlayerInteract/AlphaPanelOperations);
			Reader<RoundSummary.SumInfo_ClassList>.read = new Func<NetworkReader, RoundSummary.SumInfo_ClassList>(GeneratedNetworkCode._Read_RoundSummary/SumInfo_ClassList);
			Reader<RoundSummary.LeadingTeam>.read = new Func<NetworkReader, RoundSummary.LeadingTeam>(GeneratedNetworkCode._Read_RoundSummary/LeadingTeam);
			Reader<ServerRoles.BadgePreferences>.read = new Func<NetworkReader, ServerRoles.BadgePreferences>(GeneratedNetworkCode._Read_ServerRoles/BadgePreferences);
			Reader<ServerRoles.BadgeVisibilityPreferences>.read = new Func<NetworkReader, ServerRoles.BadgeVisibilityPreferences>(GeneratedNetworkCode._Read_ServerRoles/BadgeVisibilityPreferences);
			Reader<QueryProcessor.CommandData[]>.read = new Func<NetworkReader, QueryProcessor.CommandData[]>(GeneratedNetworkCode._Read_RemoteAdmin.QueryProcessor/CommandData[]);
			Reader<QueryProcessor.CommandData>.read = new Func<NetworkReader, QueryProcessor.CommandData>(GeneratedNetworkCode._Read_RemoteAdmin.QueryProcessor/CommandData);
			Reader<DecontaminationController.DecontaminationStatus>.read = new Func<NetworkReader, DecontaminationController.DecontaminationStatus>(GeneratedNetworkCode._Read_LightContainmentZoneDecontamination.DecontaminationController/DecontaminationStatus);
			Reader<LightShadows>.read = new Func<NetworkReader, LightShadows>(GeneratedNetworkCode._Read_UnityEngine.LightShadows);
			Reader<LightType>.read = new Func<NetworkReader, LightType>(GeneratedNetworkCode._Read_UnityEngine.LightType);
			Reader<LightShape>.read = new Func<NetworkReader, LightShape>(GeneratedNetworkCode._Read_UnityEngine.LightShape);
			Reader<PrimitiveType>.read = new Func<NetworkReader, PrimitiveType>(GeneratedNetworkCode._Read_UnityEngine.PrimitiveType);
			Reader<PrimitiveFlags>.read = new Func<NetworkReader, PrimitiveFlags>(GeneratedNetworkCode._Read_AdminToys.PrimitiveFlags);
			Reader<Scp914KnobSetting>.read = new Func<NetworkReader, Scp914KnobSetting>(GeneratedNetworkCode._Read_Scp914.Scp914KnobSetting);
			Reader<ElevatorGroup>.read = new Func<NetworkReader, ElevatorGroup>(GeneratedNetworkCode._Read_Interactables.Interobjects.ElevatorGroup);
			Reader<ItemIdentifier[]>.read = new Func<NetworkReader, ItemIdentifier[]>(GeneratedNetworkCode._Read_InventorySystem.Items.ItemIdentifier[]);
			Reader<ItemIdentifier>.read = new Func<NetworkReader, ItemIdentifier>(GeneratedNetworkCode._Read_InventorySystem.Items.ItemIdentifier);
			Reader<ItemType>.read = new Func<NetworkReader, ItemType>(GeneratedNetworkCode._Read_ItemType);
			Reader<ushort[]>.read = new Func<NetworkReader, ushort[]>(GeneratedNetworkCode._Read_System.UInt16[]);
			Reader<CandyKindID>.read = new Func<NetworkReader, CandyKindID>(GeneratedNetworkCode._Read_InventorySystem.Items.Usables.Scp330.CandyKindID);
			Reader<JailbirdWearState>.read = new Func<NetworkReader, JailbirdWearState>(GeneratedNetworkCode._Read_InventorySystem.Items.Jailbird.JailbirdWearState);
			Reader<Team>.read = new Func<NetworkReader, Team>(GeneratedNetworkCode._Read_PlayerRoles.Team);
		}
	}
}
