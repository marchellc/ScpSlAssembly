using System;
using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Drawers;
using InventorySystem.GUI;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Keycards.Snake;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Keycards;

public class ChaosKeycardItem : KeycardItem, IItemAlertDrawer, IItemDrawer
{
	private enum ChaosMsgType
	{
		SnakeMsgSync,
		NewConnectionFullSync,
		MovementSwitch,
		UseDetails
	}

	public static readonly Dictionary<ushort, SnakeEngine> SnakeSessions = new Dictionary<ushort, SnakeEngine>();

	private static SnakeNetworkMessage _msgToSend;

	private static ChaosKeycardItem _selfTemplate;

	private SnakeEngine _localEngine;

	private float _remainingMoveCooldown;

	private float _snakeHintElapsed;

	private float _snakeHintFade;

	private string _snakeHintFormat;

	private object[] _snakeHintFormatContent;

	[SerializeField]
	private int _snakeStartLength;

	[SerializeField]
	private byte _snakeMaxLength;

	[SerializeField]
	private float _hintFadeSpeed;

	[SerializeField]
	private float _hintDuration;

	[SerializeField]
	private float _hintDurationAfterInteract;

	[SerializeField]
	private Vector2Int _snakeAreaSize;

	[SerializeField]
	private float _snakeGameoverTime;

	[SerializeField]
	private AnimationCurve _moveCooldownOverScore;

	public AlertContent Alert
	{
		get
		{
			if (this._snakeHintFade <= 0f)
			{
				return default(AlertContent);
			}
			if (this._snakeHintFormat == null)
			{
				this._snakeHintFormat = Translations.Get(InventoryGuiTranslation.SnakeHint);
			}
			if (this._snakeHintFormatContent == null)
			{
				this._snakeHintFormatContent = new object[4];
			}
			this._snakeHintFormatContent[0] = new ReadableKeyCode(ActionName.MoveForward);
			this._snakeHintFormatContent[1] = new ReadableKeyCode(ActionName.MoveLeft);
			this._snakeHintFormatContent[2] = new ReadableKeyCode(ActionName.MoveBackward);
			this._snakeHintFormatContent[3] = new ReadableKeyCode(ActionName.MoveRight);
			return new AlertContent(string.Format(this._snakeHintFormat, this._snakeHintFormatContent), this._snakeHintFade);
		}
	}

	public static event Action<ushort?, Vector2Int> OnSnakeMovementDirChanged;

	public static event Action<ushort, DoorPermissionFlags, string> OnDetailedUse;

	protected override void ClientProcessCustomRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessCustomRpcTemplate(reader, serial);
		switch ((ChaosMsgType)reader.ReadByte())
		{
		case ChaosMsgType.SnakeMsgSync:
			ChaosKeycardItem.GetEngineForSerial(serial).ProcessMessage(new SnakeNetworkMessage(reader));
			break;
		case ChaosMsgType.NewConnectionFullSync:
			ChaosKeycardItem.SnakeSessions.Clear();
			while (reader.Remaining > 0)
			{
				ChaosKeycardItem.GetEngineForSerial(reader.ReadUShort()).ProcessMessage(new SnakeNetworkMessage(reader));
			}
			break;
		case ChaosMsgType.MovementSwitch:
		{
			float x = Mathf.Sign(reader.ReadSByte());
			float y = Mathf.Sign(reader.ReadSByte());
			Vector2Int arg3 = Vector2Int.CeilToInt(new Vector2(x, y));
			ChaosKeycardItem.OnSnakeMovementDirChanged?.Invoke(serial, arg3);
			break;
		}
		case ChaosMsgType.UseDetails:
		{
			DoorPermissionFlags arg = (DoorPermissionFlags)reader.ReadUShort();
			string arg2 = reader.ReadString();
			ChaosKeycardItem.OnDetailedUse?.Invoke(serial, arg, arg2);
			break;
		}
		}
	}

	protected override void ServerProcessCustomCmd(NetworkReader reader)
	{
		base.ServerProcessCustomCmd(reader);
		switch ((ChaosMsgType)reader.ReadByte())
		{
		case ChaosMsgType.SnakeMsgSync:
		{
			SnakeNetworkMessage msg = new SnakeNetworkMessage(reader);
			if (msg.HasFlag(SnakeNetworkMessage.SyncFlags.Delta))
			{
				this.ServerSendMessage(msg);
			}
			break;
		}
		case ChaosMsgType.MovementSwitch:
		{
			sbyte x = reader.ReadSByte();
			sbyte y = reader.ReadSByte();
			base.ServerSendPublicRpc(delegate(NetworkWriter writer)
			{
				writer.WriteSubheader(MsgType.Custom);
				writer.WriteSubheader(ChaosMsgType.MovementSwitch);
				writer.WriteSByte(x);
				writer.WriteSByte(y);
			});
			break;
		}
		}
	}

	protected override void ServerOnNewPlayerConnected(ReferenceHub hub)
	{
		base.ServerOnNewPlayerConnected(hub);
		base.ServerSendTargetRpc(hub, delegate(NetworkWriter writer)
		{
			writer.WriteSubheader(MsgType.Custom);
			writer.WriteSubheader(ChaosMsgType.NewConnectionFullSync);
			foreach (KeyValuePair<ushort, SnakeEngine> snakeSession in ChaosKeycardItem.SnakeSessions)
			{
				writer.WriteUShort(snakeSession.Key);
				snakeSession.Value.WriteFullResyncMessage(writer);
			}
		});
	}

	protected override void OnUsed(IDoorPermissionRequester requester, bool success)
	{
		base.OnUsed(requester, success);
		base.ServerSendPublicRpc(delegate(NetworkWriter x)
		{
			x.WriteSubheader(MsgType.Custom);
			x.WriteSubheader(ChaosMsgType.UseDetails);
			x.WriteUShort((ushort)requester.PermissionsPolicy.RequiredPermissions);
			x.WriteString(requester.RequesterLogSignature);
		});
	}

	internal override void OnTemplateReloaded(bool wasEverLoaded)
	{
		base.OnTemplateReloaded(wasEverLoaded);
		ChaosKeycardItem._selfTemplate = this;
	}

	public override void OnAdded(ItemPickupBase pickup)
	{
		base.OnAdded(pickup);
		if (base.IsControllable)
		{
			this._localEngine = this.GetNewEngine(isLocalClient: true);
			ChaosKeycardItem.SnakeSessions[base.ItemSerial] = this._localEngine;
		}
	}

	public override void EquipUpdate()
	{
		base.EquipUpdate();
		if (!base.IsControllable)
		{
			return;
		}
		if (this.UpdateSnake())
		{
			this._snakeHintElapsed += Time.deltaTime;
			this.UpdateHint(this._snakeHintElapsed < this._hintDuration);
			return;
		}
		if (this._snakeHintElapsed < this._hintDuration)
		{
			this._snakeHintElapsed = 0f;
		}
		this.UpdateHint(targetVisible: false);
	}

	private bool UpdateSnake()
	{
		if (!KeycardItem.StartInspectTimes.TryGetValue(base.ItemSerial, out var value))
		{
			return false;
		}
		if (NetworkTime.time - value < 1.7000000476837158)
		{
			return true;
		}
		this.UpdateInput(KeyCode.UpArrow, ActionName.MoveForward, Vector2Int.up);
		this.UpdateInput(KeyCode.DownArrow, ActionName.MoveBackward, Vector2Int.down);
		this.UpdateInput(KeyCode.LeftArrow, ActionName.MoveLeft, Vector2Int.left);
		this.UpdateInput(KeyCode.RightArrow, ActionName.MoveRight, Vector2Int.right);
		if (this._remainingMoveCooldown > 0f)
		{
			this._remainingMoveCooldown -= Time.deltaTime;
		}
		else
		{
			this._localEngine.Move();
			this._remainingMoveCooldown = this._moveCooldownOverScore.Evaluate(this._localEngine.Score);
		}
		return true;
	}

	private void UpdateInput(KeyCode kc, ActionName action, Vector2Int input)
	{
		if ((base.GetActionDown(action) || Input.GetKeyDown(kc)) && this._localEngine.ProvideInput(input))
		{
			base.ClientSendCmd(delegate(NetworkWriter writer)
			{
				writer.WriteSubheader(MsgType.Custom);
				writer.WriteSubheader(ChaosMsgType.MovementSwitch);
				writer.WriteSByte((sbyte)input.x);
				writer.WriteSByte((sbyte)input.y);
			});
			this._hintDuration = this._hintDurationAfterInteract;
			ChaosKeycardItem.OnSnakeMovementDirChanged?.Invoke(null, input);
		}
	}

	private void UpdateHint(bool targetVisible)
	{
		this._snakeHintFade = Mathf.MoveTowards(this._snakeHintFade, targetVisible ? 1 : 0, Time.deltaTime * this._hintFadeSpeed);
	}

	private void ServerSendMessage(SnakeNetworkMessage msg)
	{
		ChaosKeycardItem._msgToSend = msg;
		base.ServerSendPublicRpc(delegate(NetworkWriter writer)
		{
			writer.WriteSubheader(MsgType.Custom);
			writer.WriteSubheader(ChaosMsgType.SnakeMsgSync);
			ChaosKeycardItem._msgToSend.WriteSelf(writer);
		});
	}

	private void ClientSendMessage(SnakeNetworkMessage msg)
	{
		ChaosKeycardItem._msgToSend = msg;
		base.ClientSendCmd(delegate(NetworkWriter writer)
		{
			writer.WriteSubheader(MsgType.Custom);
			writer.WriteSubheader(ChaosMsgType.SnakeMsgSync);
			ChaosKeycardItem._msgToSend.WriteSelf(writer);
		});
	}

	private SnakeEngine GetNewEngine(bool isLocalClient)
	{
		return new SnakeEngine(this._snakeAreaSize / 2, this._snakeStartLength, this._snakeMaxLength, this._snakeAreaSize, this._snakeGameoverTime, isLocalClient ? new Action<SnakeNetworkMessage>(ClientSendMessage) : null);
	}

	private static SnakeEngine GetEngineForSerial(ushort serial)
	{
		return ChaosKeycardItem.SnakeSessions.GetOrAdd(serial, () => ChaosKeycardItem._selfTemplate.GetNewEngine(isLocalClient: false));
	}
}
