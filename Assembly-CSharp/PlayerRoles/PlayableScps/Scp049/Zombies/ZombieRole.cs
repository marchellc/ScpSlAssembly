using Mirror;
using PlayerRoles.PlayableScps.HUDs;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.Subroutines;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp049.Zombies;

public class ZombieRole : FpcStandardScp, ISubroutinedRole, IHumeShieldedRole, IHudScp, IAdvancedCameraController, ICameraController
{
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

	public override float MaxHealth => (int)this._syncMaxHealth;

	public override Vector3 CameraPosition => this._consumeAbility.ProcessCamPos(base.CameraPosition);

	public override float HorizontalRotation => this._consumeAbility.ProcessRotation().y;

	public override float VerticalRotation => this._consumeAbility.ProcessRotation().x;

	public float RollRotation => this._consumeAbility.ProcessRotation().z;

	[field: SerializeField]
	public HumeShieldModuleBase HumeShieldModule { get; private set; }

	[field: SerializeField]
	public SubroutineManagerModule SubroutineModule { get; private set; }

	[field: SerializeField]
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
		if (base.TryGetOwner(out var owner) && NetworkServer.active)
		{
			Scp049SenseAbility subroutine;
			bool num = ReferenceHub.AllHubs.Any((ReferenceHub x) => x.roleManager.CurrentRole is Scp049Role scp049Role && scp049Role.SubroutineModule.TryGetSubroutine<Scp049SenseAbility>(out subroutine) && subroutine.SpecialZombies.Contains(owner));
			int resurrectionsNumber = Scp049ResurrectAbility.GetResurrectionsNumber(owner);
			float num2 = (num ? ((float)(int)this._specialMaxHp) : base.MaxHealth);
			for (int num3 = 0; num3 < resurrectionsNumber; num3++)
			{
				num2 *= this._revivesModifier;
			}
			this._syncMaxHealth = (ushort)(Mathf.RoundToInt(num2 / 10f) * 10);
			Scp049ResurrectAbility.RegisterPlayerResurrection(owner);
			this._showConfirmationBox = resurrectionsNumber < 1;
		}
	}

	public override void DisableRole(RoleTypeId newRole)
	{
		bool isLocalPlayer = base.IsLocalPlayer;
		base.DisableRole(newRole);
		this._syncMaxHealth = 0;
		if (this._showConfirmationBox && isLocalPlayer && newRole == RoleTypeId.Spectator && ReferenceHub.AllHubs.Any((ReferenceHub x) => x.roleManager.CurrentRole is Scp049Role))
		{
			Object.Instantiate(this._confirmBoxPrefab);
		}
	}
}
