using UnityEngine;

namespace Interactables.Interobjects.DoorUtils;

public readonly struct KeycardLevels
{
	public const int MaxLevel = 3;

	public static readonly DoorPermissionFlags[] AdminLevels = new DoorPermissionFlags[4]
	{
		DoorPermissionFlags.None,
		DoorPermissionFlags.Checkpoints | DoorPermissionFlags.Intercom,
		DoorPermissionFlags.ExitGates,
		DoorPermissionFlags.AlphaWarhead
	};

	public readonly int Containment;

	public readonly int Armory;

	public readonly int Admin;

	public int HighestLevelValue => Mathf.Max(this.Containment, Mathf.Max(this.Armory, this.Admin));

	public DoorPermissionFlags Permissions
	{
		get
		{
			DoorPermissionFlags doorPermissionFlags = DoorPermissionFlags.None;
			for (int i = 0; i < this.Containment; i++)
			{
				int num = 16 << i;
				doorPermissionFlags = (DoorPermissionFlags)((uint)doorPermissionFlags | (uint)(ushort)num);
			}
			for (int j = 0; j < this.Armory; j++)
			{
				int num2 = 128 << j;
				doorPermissionFlags = (DoorPermissionFlags)((uint)doorPermissionFlags | (uint)(ushort)num2);
			}
			for (int k = 0; k < this.Admin; k++)
			{
				doorPermissionFlags |= KeycardLevels.AdminLevels[k + 1];
			}
			return doorPermissionFlags;
		}
	}

	public KeycardLevels(DoorPermissionFlags flags)
	{
		this.Containment = 0;
		this.Armory = 0;
		this.Admin = 0;
		for (int i = 0; i < 3; i++)
		{
			DoorPermissionFlags flag = (DoorPermissionFlags)(16 << i);
			if (flags.HasFlagAny(flag))
			{
				this.Containment = i + 1;
			}
		}
		for (int j = 0; j < 3; j++)
		{
			DoorPermissionFlags flag2 = (DoorPermissionFlags)(128 << j);
			if (flags.HasFlagAny(flag2))
			{
				this.Armory = j + 1;
			}
		}
		for (int num = 3; num > 0; num--)
		{
			if (flags.HasFlagAny(KeycardLevels.AdminLevels[num]))
			{
				this.Admin = num;
				break;
			}
		}
	}

	public KeycardLevels(int containment, int armory, int admin, bool clamp = true)
	{
		if (clamp)
		{
			containment = Mathf.Clamp(containment, 0, 3);
			armory = Mathf.Clamp(armory, 0, 3);
			admin = Mathf.Clamp(admin, 0, 3);
		}
		this.Containment = containment;
		this.Armory = armory;
		this.Admin = admin;
	}
}
