using System;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using TMPro;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.GUI
{
	public class Scp079CamNameGui : Scp079GuiElementBase
	{
		internal override void Init(Scp079Role role, ReferenceHub owner)
		{
			base.Init(role, owner);
			role.SubroutineModule.TryGetSubroutine<Scp079CurrentCameraSync>(out this._curCamSync);
		}

		private void Update()
		{
			Scp079Camera scp079Camera;
			if (!this._curCamSync.TryGetCurrentCamera(out scp079Camera))
			{
				return;
			}
			this._label.text = scp079Camera.Label;
		}

		[SerializeField]
		private TextMeshProUGUI _label;

		private Scp079CurrentCameraSync _curCamSync;
	}
}
