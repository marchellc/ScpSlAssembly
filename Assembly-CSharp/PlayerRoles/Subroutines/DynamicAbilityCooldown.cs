using Mirror;

namespace PlayerRoles.Subroutines;

public class DynamicAbilityCooldown : AbilityCooldown
{
	private bool _appendCooldownNext;

	public void Append(double cooldown)
	{
		base.Remaining = (float)cooldown;
		this._appendCooldownNext = true;
	}

	public override void WriteCooldown(NetworkWriter writer)
	{
		writer.WriteBool(this._appendCooldownNext);
		base.WriteCooldown(writer);
		this._appendCooldownNext = false;
	}

	public override void ReadCooldown(NetworkReader reader)
	{
		if (!reader.ReadBool())
		{
			base.ReadCooldown(reader);
		}
		else
		{
			base.NextUse = reader.ReadDouble();
		}
	}

	public override void Clear()
	{
		base.Clear();
		this._appendCooldownNext = false;
	}
}
