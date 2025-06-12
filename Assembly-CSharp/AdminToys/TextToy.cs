using System;
using System.Linq;
using System.Runtime.InteropServices;
using Mirror;
using PlayerRoles.FirstPersonControl;
using TMPro;
using UnityEngine;

namespace AdminToys;

public class TextToy : AdminToyBase
{
	public readonly SyncList<string> Arguments = new SyncList<string>();

	[SyncVar(hook = "OnDisplaySizeChanged")]
	private Vector2 _displaySize = TextToy.DefaultDisplaySize;

	[SyncVar(hook = "OnTextFormatedChanged")]
	private string _textFormat;

	private TMP_Text _textMesh;

	private RectTransform _rectTransform;

	public static Vector2 DefaultDisplaySize => new Vector2(200f, 50f);

	public override string CommandName => "Text";

	public Vector2 DisplaySize
	{
		get
		{
			return this._displaySize;
		}
		set
		{
			this.Network_displaySize = value;
		}
	}

	public string TextFormat
	{
		get
		{
			return this._textFormat;
		}
		set
		{
			this.Network_textFormat = value;
		}
	}

	public Vector2 Network_displaySize
	{
		get
		{
			return this._displaySize;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._displaySize, 32uL, OnDisplaySizeChanged);
		}
	}

	public string Network_textFormat
	{
		get
		{
			return this._textFormat;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._textFormat, 64uL, OnTextFormatedChanged);
		}
	}

	public override void OnSpawned(ReferenceHub admin, ArraySegment<string> arguments)
	{
		base.OnSpawned(admin, arguments);
		Vector3 position;
		Quaternion rotation;
		if (admin.roleManager.CurrentRole is IFpcRole fpcRole)
		{
			position = fpcRole.FpcModule.Position;
			rotation = Quaternion.Euler(0f, fpcRole.FpcModule.MouseLook.CurrentHorizontal, 0f);
		}
		else
		{
			position = admin.transform.position;
			rotation = Quaternion.Euler(0f, admin.transform.rotation.eulerAngles.y, 0f);
		}
		base.transform.SetPositionAndRotation(position, rotation);
		if (arguments.Array.Length > 2)
		{
			this.TextFormat = string.Join(' ', arguments.Array[2..]);
		}
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		this.Arguments.OnModified += RefreshText;
	}

	private void RefreshText()
	{
		if (!(this._textMesh == null))
		{
			if (this.Arguments.Count > 0 && !string.IsNullOrEmpty(this.TextFormat))
			{
				TMP_Text textMesh = this._textMesh;
				string textFormat = this.TextFormat;
				object[] args = this.Arguments.ToArray();
				textMesh.SetText(Misc.FormatAvailable(textFormat, args));
			}
			else
			{
				this._textMesh.SetText(this.TextFormat);
			}
		}
	}

	private void Awake()
	{
		if (!NetworkServer.active)
		{
			this._textMesh = base.GetComponentInChildren<TMP_Text>();
			this._rectTransform = this._textMesh.gameObject.GetComponent<RectTransform>();
		}
	}

	private void OnDisplaySizeChanged(Vector2 oldSize, Vector2 newSize)
	{
		if (!(this._rectTransform == null))
		{
			this._rectTransform.sizeDelta = newSize;
		}
	}

	private void OnTextFormatedChanged(string oldValue, string newValue)
	{
		this.RefreshText();
	}

	public TextToy()
	{
		base.InitSyncObject(this.Arguments);
	}

	public override bool Weaved()
	{
		return true;
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteVector2(this._displaySize);
			writer.WriteString(this._textFormat);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 0x20L) != 0L)
		{
			writer.WriteVector2(this._displaySize);
		}
		if ((base.syncVarDirtyBits & 0x40L) != 0L)
		{
			writer.WriteString(this._textFormat);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._displaySize, OnDisplaySizeChanged, reader.ReadVector2());
			base.GeneratedSyncVarDeserialize(ref this._textFormat, OnTextFormatedChanged, reader.ReadString());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 0x20L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._displaySize, OnDisplaySizeChanged, reader.ReadVector2());
		}
		if ((num & 0x40L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._textFormat, OnTextFormatedChanged, reader.ReadString());
		}
	}
}
