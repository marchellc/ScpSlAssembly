using System;
using PlayerRoles.Voice;
using UnityEngine;

namespace PlayerRoles
{
	public class NoneRole : PlayerRoleBase, IVoiceRole
	{
		public VoiceModuleBase VoiceModule { get; private set; }

		public override RoleTypeId RoleTypeId
		{
			get
			{
				return RoleTypeId.None;
			}
		}

		public override Color RoleColor
		{
			get
			{
				return Color.white;
			}
		}

		public override Team Team
		{
			get
			{
				return Team.Dead;
			}
		}
	}
}
