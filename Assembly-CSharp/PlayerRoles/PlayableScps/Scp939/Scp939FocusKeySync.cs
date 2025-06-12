using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939;

public class Scp939FocusKeySync : KeySubroutine<Scp939Role>
{
	private Scp939FocusAbility _focus;

	protected override ActionName TargetKey => ActionName.Sneak;

	protected override bool IsKeyHeld
	{
		get
		{
			return base.IsKeyHeld;
		}
		set
		{
			if (this.IsKeyHeld != value)
			{
				base.IsKeyHeld = value;
				base.ClientSendCmd();
			}
		}
	}

	protected override bool KeyPressable
	{
		get
		{
			if (base.Role.IsControllable)
			{
				if (Cursor.visible && !this._focus.TargetState)
				{
					return base.Role.IsEmulatedDummy;
				}
				return true;
			}
			return false;
		}
	}

	public bool FocusKeyHeld { get; private set; }

	public override void ClientWriteCmd(NetworkWriter writer)
	{
		base.ClientWriteCmd(writer);
		writer.WriteBool(this.IsKeyHeld);
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		this.FocusKeyHeld = reader.ReadBool();
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this.FocusKeyHeld = false;
	}

	protected override void Awake()
	{
		base.Awake();
		base.GetSubroutine<Scp939FocusAbility>(out this._focus);
	}
}
