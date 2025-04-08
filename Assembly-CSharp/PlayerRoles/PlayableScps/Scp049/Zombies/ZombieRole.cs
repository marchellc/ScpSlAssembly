using System;
using Mirror;
using PlayerRoles.PlayableScps.HUDs;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.Subroutines;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp049.Zombies
{
	public class ZombieRole : FpcStandardScp, ISubroutinedRole, IHumeShieldedRole, IHudScp, IAdvancedCameraController, ICameraController
	{
		public override float MaxHealth
		{
			get
			{
				return (float)this._syncMaxHealth;
			}
		}

		public override Vector3 CameraPosition
		{
			get
			{
				return this._consumeAbility.ProcessCamPos(base.CameraPosition);
			}
		}

		public override float HorizontalRotation
		{
			get
			{
				return this._consumeAbility.ProcessRotation().y;
			}
		}

		public override float VerticalRotation
		{
			get
			{
				return this._consumeAbility.ProcessRotation().x;
			}
		}

		public float RollRotation
		{
			get
			{
				return this._consumeAbility.ProcessRotation().z;
			}
		}

		public HumeShieldModuleBase HumeShieldModule { get; private set; }

		public SubroutineManagerModule SubroutineModule { get; private set; }

		public ScpHudBase HudPrefab { get; private set; }

		private void Awake()
		{
			this.SubroutineModule.TryGetSubroutine<ZombieConsumeAbility>(out this._consumeAbility);
		}

		public override void WritePublicSpawnData(NetworkWriter writer)
		{
			writer.WriteUShort(this._syncMaxHealth);
			writer.WriteBool(this._showConfirmationBox);
			base.WritePublicSpawnData(writer);
		}

		public override void ReadSpawnData(NetworkReader reader)
		{
			this._syncMaxHealth = reader.ReadUShort();
			this._showConfirmationBox = reader.ReadBool();
			base.ReadSpawnData(reader);
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			ReferenceHub owner;
			if (!base.TryGetOwner(out owner))
			{
				return;
			}
			if (!NetworkServer.active)
			{
				return;
			}
			bool flag = ReferenceHub.AllHubs.Any(delegate(ReferenceHub x)
			{
				Scp049Role scp049Role = x.roleManager.CurrentRole as Scp049Role;
				Scp049SenseAbility scp049SenseAbility;
				return scp049Role != null && scp049Role.SubroutineModule.TryGetSubroutine<Scp049SenseAbility>(out scp049SenseAbility) && scp049SenseAbility.SpecialZombies.Contains(owner);
			});
			int resurrectionsNumber = Scp049ResurrectAbility.GetResurrectionsNumber(owner);
			float num = (flag ? ((float)this._specialMaxHp) : base.MaxHealth);
			for (int i = 0; i < resurrectionsNumber; i++)
			{
				num *= this._revivesModifier;
			}
			this._syncMaxHealth = (ushort)(Mathf.RoundToInt(num / 10f) * 10);
			Scp049ResurrectAbility.RegisterPlayerResurrection(owner, 1);
			this._showConfirmationBox = resurrectionsNumber < 1;
		}

		public override void DisableRole(RoleTypeId newRole)
		{
			bool isLocalPlayer = base.IsLocalPlayer;
			base.DisableRole(newRole);
			this._syncMaxHealth = 0;
			if (!this._showConfirmationBox || !isLocalPlayer || newRole != RoleTypeId.Spectator)
			{
				return;
			}
			if (!ReferenceHub.AllHubs.Any((ReferenceHub x) => x.roleManager.CurrentRole is Scp049Role))
			{
				return;
			}
			global::UnityEngine.Object.Instantiate<GameObject>(this._confirmBoxPrefab);
		}

		[SerializeField]
		private GameObject _confirmBoxPrefab;

		[Tooltip("The maximum amount of health the special zombie will have.")]
		[SerializeField]
		private ushort _specialMaxHp = 600;

		[Tooltip("Modifier applied based on how many times the zombie was revived for.")]
		[SerializeField]
		private float _revivesModifier = 0.9f;

		private ushort _syncMaxHealth;

		private bool _showConfirmationBox;

		private ZombieConsumeAbility _consumeAbility;
	}
}
