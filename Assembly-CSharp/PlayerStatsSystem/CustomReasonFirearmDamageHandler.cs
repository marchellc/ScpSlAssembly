using InventorySystem.Items.Firearms;
using Mirror;

namespace PlayerStatsSystem;

public class CustomReasonFirearmDamageHandler : FirearmDamageHandler
{
	private string _ragdollText;

	private byte _translationId;

	public override string RagdollInspectText => this._ragdollText;

	public CustomReasonFirearmDamageHandler()
	{
	}

	public CustomReasonFirearmDamageHandler(DeathTranslation deathTranslation, Firearm firearm, float damage, float penetration, bool useHumanMutlipliers = true)
		: base(firearm, damage, penetration, useHumanMutlipliers)
	{
		this._translationId = deathTranslation.Id;
	}

	public override void WriteAdditionalData(NetworkWriter writer)
	{
		writer.WriteByte(this._translationId);
		base.WriteAdditionalData(writer);
	}

	public override void ReadAdditionalData(NetworkReader reader)
	{
		this._translationId = reader.ReadByte();
		if (DeathTranslations.TranslationsById.TryGetValue(this._translationId, out var value))
		{
			this._ragdollText = value.RagdollTranslation;
		}
		base.ReadAdditionalData(reader);
	}
}
