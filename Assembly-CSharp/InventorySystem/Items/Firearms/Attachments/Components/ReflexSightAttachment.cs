using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mirror;
using PlayerRoles;
using PlayerRoles.Spectating;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Attachments.Components
{
	public class ReflexSightAttachment : SerializableAttachment, ICustomizableAttachment
	{
		public Vector2 ConfigIconOffset
		{
			get
			{
				return this._configIconOffset;
			}
		}

		public AttachmentConfigWindow ConfigWindow
		{
			get
			{
				return this._configWindow;
			}
		}

		public float ConfigIconScale
		{
			get
			{
				return this._configIconSize;
			}
		}

		public override bool AllowCmdsWhileHolstered
		{
			get
			{
				return true;
			}
		}

		public int CurTextureIndex { get; set; }

		public int CurSizeIndex { get; set; }

		public int CurColorIndex { get; set; }

		public int CurBrightnessIndex { get; set; }

		public void SetValues(int texture, int color, int size, int brightness)
		{
			this.CurTextureIndex = Mathf.Clamp(texture, 0, this.TextureOptions.Length - 1);
			this.CurColorIndex = Mathf.Clamp(color, 0, ReflexSightAttachment.Colors.Length - 1);
			this.CurSizeIndex = Mathf.Clamp(size, 0, ReflexSightAttachment.Sizes.Length - 1);
			this.CurBrightnessIndex = Mathf.Clamp(brightness, 0, ReflexSightAttachment.BrightnessLevels.Length - 1);
			Action onValuesChanged = this.OnValuesChanged;
			if (onValuesChanged == null)
			{
				return;
			}
			onValuesChanged();
		}

		public void ModifyValues(int? texture = null, int? color = null, int? size = null, int? brightness = null)
		{
			this.SetValues(texture.GetValueOrDefault(this.CurTextureIndex), color.GetValueOrDefault(this.CurColorIndex), size.GetValueOrDefault(this.CurSizeIndex), brightness.GetValueOrDefault(this.CurBrightnessIndex));
			this.SaveValues();
			this.SendCmd(delegate(NetworkWriter writer)
			{
				ReflexSightSyncData reflexSightSyncData = new ReflexSightSyncData(this);
				writer.WriteBool(true);
				reflexSightSyncData.Write(writer);
			});
		}

		public void SaveValues()
		{
			int preset = AttachmentPreferences.GetPreset(base.Firearm.ItemTypeId);
			this.<SaveValues>g__SaveAs|45_0(preset);
			this.<SaveValues>g__SaveAs|45_0(0);
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			this.SetDatabaseEntry(new ReflexSightSyncData(reader), new ushort?(serial));
		}

		public override void ClientProcessRpcInstance(NetworkReader reader)
		{
			base.ClientProcessRpcInstance(reader);
			ReflexSightSyncData reflexSightSyncData = new ReflexSightSyncData(reader);
			reflexSightSyncData.Apply(this);
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			bool flag = reader.ReadBool();
			if (!flag)
			{
				if (this._serverPrefsReceived)
				{
					return;
				}
				if (!base.Firearm.ServerIsPersonal)
				{
					return;
				}
				this._serverPrefsReceived = true;
			}
			else if (!AttachmentsServerHandler.AnyWorkstationsNearby(base.Firearm.Owner))
			{
				return;
			}
			ReflexSightSyncData reflexSightSyncData = new ReflexSightSyncData(reader);
			this.SetDatabaseEntry(reflexSightSyncData, null);
			this.ServerSendData(reflexSightSyncData, flag);
		}

		internal override void OnAdded()
		{
			base.OnAdded();
			if (base.IsLocalPlayer)
			{
				this.SendUserPrefs(false);
			}
			ReflexSightSyncData reflexSightSyncData;
			if (this.TryGetFromDatabase(out reflexSightSyncData))
			{
				reflexSightSyncData.Apply(this);
			}
			else
			{
				this.SetValues(this._defaultReticle, this._defaultColorId, 4, 0);
			}
			if (!base.IsServer || base.Firearm.ServerIsPersonal)
			{
				return;
			}
			this.ServerSendData(reflexSightSyncData, false);
		}

		internal override void SpectatorInit()
		{
			base.SpectatorInit();
			ReflexSightSyncData reflexSightSyncData;
			if (this.TryGetFromDatabase(out reflexSightSyncData))
			{
				reflexSightSyncData.Apply(this);
			}
		}

		internal override void OnEquipped()
		{
			base.OnEquipped();
			ReflexSightSyncData reflexSightSyncData;
			if (!base.IsServer || !this.IsEnabled || !this.TryGetFromDatabase(out reflexSightSyncData))
			{
				return;
			}
			this.ServerSendData(reflexSightSyncData, true);
		}

		internal override void OnClientReady()
		{
			base.OnClientReady();
			ReflexSightAttachment.SyncData.Clear();
		}

		protected override void Awake()
		{
			base.Awake();
			AttachmentSelectorBase.OnPresetLoaded = (Action)Delegate.Combine(AttachmentSelectorBase.OnPresetLoaded, new Action(this.LoadFromPreset));
			AttachmentSelectorBase.OnPresetSaved = (Action)Delegate.Combine(AttachmentSelectorBase.OnPresetSaved, new Action(this.SaveValues));
			AttachmentSelectorBase.OnAttachmentsReset = (Action)Delegate.Combine(AttachmentSelectorBase.OnAttachmentsReset, new Action(this.ResetToDefault));
			PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
		}

		private void OnDestroy()
		{
			AttachmentSelectorBase.OnPresetLoaded = (Action)Delegate.Remove(AttachmentSelectorBase.OnPresetLoaded, new Action(this.LoadFromPreset));
			AttachmentSelectorBase.OnPresetSaved = (Action)Delegate.Remove(AttachmentSelectorBase.OnPresetSaved, new Action(this.SaveValues));
			AttachmentSelectorBase.OnAttachmentsReset = (Action)Delegate.Remove(AttachmentSelectorBase.OnAttachmentsReset, new Action(this.ResetToDefault));
			PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
		}

		private void OnRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			if (!this.IsEnabled || !base.IsServer)
			{
				return;
			}
			if (!(newRole is SpectatorRole))
			{
				return;
			}
			ReflexSightSyncData reflexSightSyncData;
			if (!this.TryGetFromDatabase(out reflexSightSyncData))
			{
				return;
			}
			this.SendRpc(userHub, new Action<NetworkWriter>(reflexSightSyncData.Write));
		}

		private void ServerSendData(ReflexSightSyncData data, bool onlySpectators)
		{
			ReferenceHub owner = base.Firearm.Owner;
			if (onlySpectators)
			{
				this.SendRpc(new Func<ReferenceHub, bool>(owner.IsSpectatedBy), new Action<NetworkWriter>(data.Write));
				return;
			}
			this.SendRpc((ReferenceHub x) => x == owner || owner.IsSpectatedBy(x), new Action<NetworkWriter>(data.Write));
		}

		private void SetDatabaseEntry(ReflexSightSyncData data, ushort? serial = null)
		{
			ushort num = serial.GetValueOrDefault();
			if (serial == null)
			{
				num = base.ItemSerial;
				serial = new ushort?(num);
			}
			ReflexSightAttachment.SyncData.GetOrAdd(serial.Value, () => new Dictionary<byte, ReflexSightSyncData>())[base.Index] = data;
		}

		private bool TryGetFromDatabase(out ReflexSightSyncData data)
		{
			Dictionary<byte, ReflexSightSyncData> dictionary;
			if (ReflexSightAttachment.SyncData.TryGetValue(base.ItemSerial, out dictionary))
			{
				return dictionary.TryGetValue(base.Index, out data);
			}
			data = default(ReflexSightSyncData);
			return false;
		}

		private string GetPrefsKey(int preset, string setting)
		{
			return string.Format("ReflexSight_{0}_{1}_{2}_{3}", new object[]
			{
				preset,
				base.Firearm.ItemTypeId,
				base.Index,
				setting
			});
		}

		private void SendUserPrefs(bool changeRequest)
		{
			if (!base.IsLocalPlayer)
			{
				return;
			}
			int preset = AttachmentPreferences.GetPreset(base.Firearm.ItemTypeId);
			ReflexSightSyncData data = new ReflexSightSyncData(PlayerPrefsSl.Get(this.GetPrefsKey(preset, "Color"), this._defaultColorId), PlayerPrefsSl.Get(this.GetPrefsKey(preset, "Brightness"), 0), PlayerPrefsSl.Get(this.GetPrefsKey(preset, "Size"), 4), PlayerPrefsSl.Get(this.GetPrefsKey(preset, "Texture"), this._defaultReticle));
			this.SendCmd(delegate(NetworkWriter writer)
			{
				writer.WriteBool(changeRequest);
				data.Write(writer);
			});
			if (!changeRequest)
			{
				return;
			}
			data.Apply(this);
		}

		private void LoadFromPreset()
		{
			this.SendUserPrefs(true);
		}

		private void ResetToDefault()
		{
			this.ModifyValues(new int?(this._defaultReticle), new int?(this._defaultColorId), new int?(4), new int?(0));
		}

		[CompilerGenerated]
		private void <SaveValues>g__SaveAs|45_0(int presetId)
		{
			PlayerPrefsSl.Set(this.GetPrefsKey(presetId, "Texture"), this.CurTextureIndex);
			PlayerPrefsSl.Set(this.GetPrefsKey(presetId, "Color"), this.CurColorIndex);
			PlayerPrefsSl.Set(this.GetPrefsKey(presetId, "Size"), this.CurSizeIndex);
			PlayerPrefsSl.Set(this.GetPrefsKey(presetId, "Brightness"), this.CurBrightnessIndex);
		}

		private static readonly Dictionary<ushort, Dictionary<byte, ReflexSightSyncData>> SyncData = new Dictionary<ushort, Dictionary<byte, ReflexSightSyncData>>();

		public static readonly float[] Sizes = new float[] { 0.6f, 0.7f, 0.8f, 0.9f, 1f, 1.2f, 1.4f, 1.6f, 1.8f };

		public static readonly float[] BrightnessLevels = new float[] { 0f, 0.17f, 0.31f, 0.45f, 0.68f, 1f };

		public static readonly Color32[] Colors = new Color32[]
		{
			new Color32(byte.MaxValue, 0, 0, byte.MaxValue),
			new Color32(0, 42, byte.MaxValue, byte.MaxValue),
			new Color32(0, 166, byte.MaxValue, byte.MaxValue),
			new Color32(0, byte.MaxValue, 0, byte.MaxValue),
			new Color32(byte.MaxValue, byte.MaxValue, 0, byte.MaxValue),
			new Color32(byte.MaxValue, 85, 0, byte.MaxValue),
			new Color32(byte.MaxValue, 0, 107, byte.MaxValue),
			new Color32(156, 0, byte.MaxValue, byte.MaxValue),
			new Color32(byte.MaxValue, 0, 172, byte.MaxValue),
			new Color32(78, 0, byte.MaxValue, byte.MaxValue),
			new Color32(177, byte.MaxValue, 0, byte.MaxValue),
			new Color32(0, byte.MaxValue, 166, byte.MaxValue)
		};

		public Action OnValuesChanged;

		public ReflexSightReticlePack TextureOptions;

		private const int DefaultSize = 4;

		private const int DefaultBrightness = 0;

		private const int UnnamedPresetIndex = 0;

		private const string TexturePrefsKey = "Texture";

		private const string ColorPrefsKey = "Color";

		private const string SizePrefsKey = "Size";

		private const string BrightnessPrefsKey = "Brightness";

		private bool _serverPrefsReceived;

		[SerializeField]
		private int _defaultColorId;

		[SerializeField]
		private int _defaultReticle;

		[SerializeField]
		private Vector2 _configIconOffset;

		[SerializeField]
		private float _configIconSize;

		[SerializeField]
		private AttachmentConfigWindow _configWindow;
	}
}
