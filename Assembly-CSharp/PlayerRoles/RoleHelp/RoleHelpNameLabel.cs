using System;
using TMPro;
using UnityEngine;

namespace PlayerRoles.RoleHelp
{
	public class RoleHelpNameLabel : MonoBehaviour
	{
		private void Awake()
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return;
			}
			PlayerRoleBase currentRole = referenceHub.roleManager.CurrentRole;
			this._text.text = string.Format(this._text.text, currentRole.RoleName, TranslationReader.Get("Class_Nicknames", (int)currentRole.RoleTypeId, "NO_TRANSLATION"));
		}

		[SerializeField]
		private TMP_Text _text;

		private const string NicknameFile = "Class_Nicknames";
	}
}
