using System;
using System.Collections.Generic;
using PlayerRoles;

namespace InventorySystem.Configs
{
	public static class StartingInventories
	{
		// Note: this type is marked as 'beforefieldinit'.
		static StartingInventories()
		{
			Dictionary<RoleTypeId, InventoryRoleInfo> dictionary = new Dictionary<RoleTypeId, InventoryRoleInfo>();
			dictionary[RoleTypeId.Scientist] = new InventoryRoleInfo(new ItemType[]
			{
				ItemType.KeycardScientist,
				ItemType.Medkit
			}, new Dictionary<ItemType, ushort>());
			Dictionary<RoleTypeId, InventoryRoleInfo> dictionary2 = dictionary;
			RoleTypeId roleTypeId = RoleTypeId.FacilityGuard;
			ItemType[] array = new ItemType[]
			{
				ItemType.KeycardGuard,
				ItemType.GunFSP9,
				ItemType.Medkit,
				ItemType.GrenadeFlash,
				ItemType.Radio,
				ItemType.ArmorLight
			};
			Dictionary<ItemType, ushort> dictionary3 = new Dictionary<ItemType, ushort>();
			dictionary3[ItemType.Ammo9x19] = 60;
			dictionary2[roleTypeId] = new InventoryRoleInfo(array, dictionary3);
			Dictionary<RoleTypeId, InventoryRoleInfo> dictionary4 = dictionary;
			RoleTypeId roleTypeId2 = RoleTypeId.NtfPrivate;
			ItemType[] array2 = new ItemType[]
			{
				ItemType.KeycardMTFOperative,
				ItemType.GunCrossvec,
				ItemType.Medkit,
				ItemType.Radio,
				ItemType.ArmorCombat
			};
			Dictionary<ItemType, ushort> dictionary5 = new Dictionary<ItemType, ushort>();
			dictionary5[ItemType.Ammo9x19] = 160;
			dictionary5[ItemType.Ammo556x45] = 40;
			dictionary4[roleTypeId2] = new InventoryRoleInfo(array2, dictionary5);
			Dictionary<RoleTypeId, InventoryRoleInfo> dictionary6 = dictionary;
			RoleTypeId roleTypeId3 = RoleTypeId.NtfSergeant;
			ItemType[] array3 = new ItemType[]
			{
				ItemType.KeycardMTFOperative,
				ItemType.GunE11SR,
				ItemType.Medkit,
				ItemType.GrenadeHE,
				ItemType.Radio,
				ItemType.ArmorCombat
			};
			Dictionary<ItemType, ushort> dictionary7 = new Dictionary<ItemType, ushort>();
			dictionary7[ItemType.Ammo9x19] = 40;
			dictionary7[ItemType.Ammo556x45] = 120;
			dictionary6[roleTypeId3] = new InventoryRoleInfo(array3, dictionary7);
			Dictionary<RoleTypeId, InventoryRoleInfo> dictionary8 = dictionary;
			RoleTypeId roleTypeId4 = RoleTypeId.NtfSpecialist;
			ItemType[] array4 = new ItemType[]
			{
				ItemType.KeycardMTFOperative,
				ItemType.GunE11SR,
				ItemType.Medkit,
				ItemType.GrenadeHE,
				ItemType.Radio,
				ItemType.ArmorCombat
			};
			Dictionary<ItemType, ushort> dictionary9 = new Dictionary<ItemType, ushort>();
			dictionary9[ItemType.Ammo9x19] = 40;
			dictionary9[ItemType.Ammo556x45] = 120;
			dictionary8[roleTypeId4] = new InventoryRoleInfo(array4, dictionary9);
			Dictionary<RoleTypeId, InventoryRoleInfo> dictionary10 = dictionary;
			RoleTypeId roleTypeId5 = RoleTypeId.NtfCaptain;
			ItemType[] array5 = new ItemType[]
			{
				ItemType.KeycardMTFCaptain,
				ItemType.GunFRMG0,
				ItemType.Adrenaline,
				ItemType.Medkit,
				ItemType.GrenadeHE,
				ItemType.Radio,
				ItemType.ArmorHeavy
			};
			Dictionary<ItemType, ushort> dictionary11 = new Dictionary<ItemType, ushort>();
			dictionary11[ItemType.Ammo9x19] = 40;
			dictionary11[ItemType.Ammo556x45] = 260;
			dictionary10[roleTypeId5] = new InventoryRoleInfo(array5, dictionary11);
			Dictionary<RoleTypeId, InventoryRoleInfo> dictionary12 = dictionary;
			RoleTypeId roleTypeId6 = RoleTypeId.ChaosConscript;
			ItemType[] array6 = new ItemType[]
			{
				ItemType.KeycardChaosInsurgency,
				ItemType.GunAK,
				ItemType.Medkit,
				ItemType.Painkillers,
				ItemType.ArmorCombat
			};
			Dictionary<ItemType, ushort> dictionary13 = new Dictionary<ItemType, ushort>();
			dictionary13[ItemType.Ammo762x39] = 120;
			dictionary12[roleTypeId6] = new InventoryRoleInfo(array6, dictionary13);
			Dictionary<RoleTypeId, InventoryRoleInfo> dictionary14 = dictionary;
			RoleTypeId roleTypeId7 = RoleTypeId.ChaosRifleman;
			ItemType[] array7 = new ItemType[]
			{
				ItemType.KeycardChaosInsurgency,
				ItemType.GunAK,
				ItemType.Medkit,
				ItemType.Painkillers,
				ItemType.ArmorCombat
			};
			Dictionary<ItemType, ushort> dictionary15 = new Dictionary<ItemType, ushort>();
			dictionary15[ItemType.Ammo762x39] = 120;
			dictionary14[roleTypeId7] = new InventoryRoleInfo(array7, dictionary15);
			Dictionary<RoleTypeId, InventoryRoleInfo> dictionary16 = dictionary;
			RoleTypeId roleTypeId8 = RoleTypeId.ChaosMarauder;
			ItemType[] array8 = new ItemType[]
			{
				ItemType.KeycardChaosInsurgency,
				ItemType.GunShotgun,
				ItemType.GunRevolver,
				ItemType.Medkit,
				ItemType.Painkillers,
				ItemType.ArmorCombat
			};
			Dictionary<ItemType, ushort> dictionary17 = new Dictionary<ItemType, ushort>();
			dictionary17[ItemType.Ammo44cal] = 24;
			dictionary17[ItemType.Ammo12gauge] = 42;
			dictionary16[roleTypeId8] = new InventoryRoleInfo(array8, dictionary17);
			Dictionary<RoleTypeId, InventoryRoleInfo> dictionary18 = dictionary;
			RoleTypeId roleTypeId9 = RoleTypeId.ChaosRepressor;
			ItemType[] array9 = new ItemType[]
			{
				ItemType.KeycardChaosInsurgency,
				ItemType.GunLogicer,
				ItemType.Medkit,
				ItemType.Adrenaline,
				ItemType.ArmorHeavy
			};
			Dictionary<ItemType, ushort> dictionary19 = new Dictionary<ItemType, ushort>();
			dictionary19[ItemType.Ammo762x39] = 200;
			dictionary18[roleTypeId9] = new InventoryRoleInfo(array9, dictionary19);
			StartingInventories.DefinedInventories = dictionary;
		}

		public static readonly Dictionary<RoleTypeId, InventoryRoleInfo> DefinedInventories;
	}
}
