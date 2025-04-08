using System;
using Mirror;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.NetworkMessages
{
	public readonly struct FpcRotationOverrideMessage : NetworkMessage
	{
		public FpcRotationOverrideMessage(Vector2 rotation)
		{
			this.Rotation = rotation;
		}

		public FpcRotationOverrideMessage(NetworkReader reader)
		{
			int num = reader.ReadInt();
			ushort num2 = (ushort)num;
			ushort num3 = (ushort)(num << 16);
			float num4 = Mathf.Lerp(-360f, 360f, (float)num3 / 65535f);
			float num5 = Mathf.Lerp(-360f, 360f, (float)num2 / 65535f);
			this.Rotation = new Vector2(num4, num5);
		}

		public void Write(NetworkWriter writer)
		{
			float num = Mathf.InverseLerp(-360f, 360f, this.Rotation.x);
			float num2 = Mathf.InverseLerp(-360f, 360f, this.Rotation.y);
			int num3 = (int)((ushort)Mathf.RoundToInt(num * 65535f));
			ushort num4 = (ushort)Mathf.RoundToInt(num2 * 65535f);
			int num5 = num3;
			num5 <<= 16;
			num5 += (int)num4;
			writer.WriteInt(num5);
		}

		public void ProcessMessage()
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return;
			}
			IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null || !fpcRole.FpcModule.ModuleReady)
			{
				return;
			}
			fpcRole.FpcModule.MouseLook.CurrentVertical = this.Rotation.x;
			fpcRole.FpcModule.MouseLook.CurrentHorizontal = this.Rotation.y;
		}

		private const float FullAngle = 360f;

		public readonly Vector2 Rotation;
	}
}
