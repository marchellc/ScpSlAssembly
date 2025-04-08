using System;
using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939
{
	public class Scp939FocusKeySync : KeySubroutine<Scp939Role>
	{
		protected override ActionName TargetKey
		{
			get
			{
				return ActionName.Sneak;
			}
		}

		protected override bool IsKeyHeld
		{
			get
			{
				return base.IsKeyHeld;
			}
			set
			{
				if (this.IsKeyHeld == value)
				{
					return;
				}
				base.IsKeyHeld = value;
				base.ClientSendCmd();
			}
		}

		protected override bool KeyPressable
		{
			get
			{
				return base.Owner.isLocalPlayer && (!Cursor.visible || this._focus.TargetState);
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

		private Scp939FocusAbility _focus;
	}
}
