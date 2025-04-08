using System;
using System.Text;
using MapGeneration;
using NorthwoodLib.Pools;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PlayerRoles.PlayableScps.Scp079.Map;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.GUI
{
	public class Scp079ScannerNotification : Scp079AccentedNotification
	{
		public Scp079ScannerNotification(HumanRole detectedHuman)
			: base(Scp079ScannerNotification.HumanFoundText(detectedHuman), detectedHuman.RoleColor.ToHex(), '$')
		{
		}

		public Scp079ScannerNotification(int retryTime)
			: base(Scp079ScannerNotification.NoneFoundText(retryTime), "#ff1111", '$')
		{
		}

		private static string HumanFoundText(HumanRole role)
		{
			string text = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.ScanResultPlayerDetected);
			Scp079Camera bestCamera = Scp079ScannerNotification.GetBestCamera(role.FpcModule.Position);
			Scp079HudTranslation scp079HudTranslation = ProceduralZoneMap.ZoneTranslations[bestCamera.Room.Zone];
			return string.Format(text, role.RoleName, bestCamera.Label, Translations.Get<Scp079HudTranslation>(scp079HudTranslation));
		}

		private static string NoneFoundText(int retryTime)
		{
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			stringBuilder.Append(Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.ScanResultNoneFound));
			if (retryTime > 0)
			{
				stringBuilder.Append("\n");
				stringBuilder.AppendFormat(Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.ScanResultRetryingMessage), retryTime);
			}
			return StringBuilderPool.Shared.ToStringReturn(stringBuilder);
		}

		private static Scp079Camera GetBestCamera(Vector3 pos)
		{
			RoomIdentifier roomIdentifier = RoomUtils.RoomAtPositionRaycasts(pos, true);
			Scp079Camera scp079Camera;
			Scp079Camera.TryGetClosestCamera(pos, null, out scp079Camera);
			if (roomIdentifier == null)
			{
				return scp079Camera;
			}
			bool flag = false;
			bool flag2 = false;
			float num = 0f;
			float num2 = 0f;
			Scp079Camera scp079Camera2 = null;
			Scp079Camera scp079Camera3 = null;
			foreach (Scp079InteractableBase scp079InteractableBase in Scp079InteractableBase.AllInstances)
			{
				Scp079Camera scp079Camera4 = scp079InteractableBase as Scp079Camera;
				if (scp079Camera4 != null && scp079Camera4.Room == roomIdentifier)
				{
					float sqrMagnitude = (scp079Camera4.Position - pos).sqrMagnitude;
					if (!flag || sqrMagnitude <= num)
					{
						if (!flag2 || sqrMagnitude < num2)
						{
							scp079Camera3 = scp079Camera4;
							num2 = sqrMagnitude;
							flag2 = true;
						}
						RaycastHit raycastHit;
						if (!Physics.Linecast(scp079Camera4.Position, pos, out raycastHit, Scp079ScannerNotification.Mask))
						{
							scp079Camera2 = scp079Camera4;
							flag = true;
							num = sqrMagnitude;
						}
					}
				}
			}
			if (flag)
			{
				return scp079Camera2;
			}
			if (!flag2)
			{
				return scp079Camera;
			}
			return scp079Camera3;
		}

		private static readonly CachedLayerMask Mask = new CachedLayerMask(new string[] { "Default" });

		private const string RedColorHex = "#ff1111";
	}
}
