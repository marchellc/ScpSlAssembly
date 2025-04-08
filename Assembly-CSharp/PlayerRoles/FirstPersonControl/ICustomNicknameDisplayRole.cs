using System;
using System.Text;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl
{
	public interface ICustomNicknameDisplayRole
	{
		void WriteNickname(ReferenceHub owner, StringBuilder sb, out Color texColor);
	}
}
