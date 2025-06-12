using Footprinting;
using Mirror;
using PlayerRoles.Ragdolls;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114;

public class Scp3114DamageHandler : AttackerDamageHandler, IRagdollInspectOverride
{
	public enum HandlerType : byte
	{
		Slap,
		Strangulation,
		SkinSteal
	}

	private DamageHandlerBase _replacedHandler;

	public override float Damage { get; set; }

	public override CassieAnnouncement CassieDeathAnnouncement => new CassieAnnouncement();

	public override Footprint Attacker { get; protected set; }

	public override string ServerLogsText => this.Subtype.ToString();

	public override bool AllowSelfDamage => false;

	public HandlerType Subtype { get; private set; }

	public bool StartingRagdoll { get; private set; }

	public override string RagdollInspectText => this.DeathTranslation.RagdollTranslation;

	public override string DeathScreenText => string.Empty;

	public string RagdollInspectFormatOverride
	{
		get
		{
			if (this.Subtype == HandlerType.SkinSteal)
			{
				if (this.StartingRagdoll)
				{
					return string.Empty;
				}
				if (!Translations.TryGet(Scp3114HudTranslation.InspectRagdollOverride, out var tr))
				{
					return "Missing translation. Owner: {0}";
				}
				return tr;
			}
			if (!this.StartingRagdoll)
			{
				return null;
			}
			if (!ReferenceHub.TryGetLocalHub(out var hub) || !hub.IsSCP())
			{
				return string.Empty;
			}
			return this.StartingDisguiseHint;
		}
	}

	private string StartingDisguiseHint
	{
		get
		{
			if (!Translations.TryGet(Scp3114HudTranslation.StartingRagdollHint, out var tr))
			{
				tr = "Missing starting ragdoll message for {0}";
			}
			return string.Format(tr, "{1}");
		}
	}

	private DeathTranslation DeathTranslation => this.Subtype switch
	{
		HandlerType.Slap => DeathTranslations.Scp3114Slap, 
		HandlerType.Strangulation => DeathTranslations.Asphyxiated, 
		_ => DeathTranslations.Unknown, 
	};

	public Scp3114DamageHandler(ReferenceHub attacker, float damage, HandlerType attackType)
	{
		this.Damage = damage;
		this.Subtype = attackType;
		this.Attacker = new Footprint(attacker);
	}

	public Scp3114DamageHandler()
	{
		this.Damage = 0f;
		this.Subtype = HandlerType.Slap;
		this.Attacker = default(Footprint);
	}

	public Scp3114DamageHandler(BasicRagdoll ragdoll, bool isStarting)
	{
		this.Damage = 0f;
		this._replacedHandler = ragdoll.Info.Handler;
		if (isStarting)
		{
			this.Subtype = HandlerType.Slap;
			this.StartingRagdoll = true;
		}
		else
		{
			this.Subtype = HandlerType.SkinSteal;
			this.StartingRagdoll = ragdoll.Info.Handler is Scp3114DamageHandler scp3114DamageHandler && scp3114DamageHandler.StartingRagdoll;
		}
	}

	public override void WriteAdditionalData(NetworkWriter writer)
	{
		base.WriteAdditionalData(writer);
		writer.WriteByte((byte)this.Subtype);
		writer.WriteBool(this.StartingRagdoll);
		if (this.Subtype == HandlerType.SkinSteal)
		{
			writer.WriteDamageHandler(this._replacedHandler);
		}
	}

	public override void ReadAdditionalData(NetworkReader reader)
	{
		base.ReadAdditionalData(reader);
		this.Subtype = (HandlerType)reader.ReadByte();
		this.StartingRagdoll = reader.ReadBool();
		if (this.Subtype == HandlerType.SkinSteal)
		{
			this._replacedHandler = reader.ReadDamageHandler();
		}
	}

	public override void ProcessRagdoll(BasicRagdoll ragdoll)
	{
		if (!(ragdoll is DynamicRagdoll dynamicRagdoll))
		{
			base.ProcessRagdoll(ragdoll);
		}
		else if (this.Subtype == HandlerType.SkinSteal)
		{
			this._replacedHandler?.ProcessRagdoll(ragdoll);
			Scp3114RagdollToBonesConverter.ConvertExisting(dynamicRagdoll);
		}
		else if (this.StartingRagdoll)
		{
			dynamicRagdoll.LinkedRigidbodies.ForEach(delegate(Rigidbody rb)
			{
				rb.linearVelocity = Physics.gravity;
			});
		}
		else
		{
			base.ProcessRagdoll(ragdoll);
		}
	}
}
