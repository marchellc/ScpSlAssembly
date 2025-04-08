using System;

public class BanDetails
{
	public override string ToString()
	{
		return string.Concat(new string[]
		{
			this.OriginalName.Replace(";", ":"),
			";",
			this.Id.Replace(";", ":"),
			";",
			Convert.ToString(this.Expires),
			";",
			this.Reason.Replace(";", ":"),
			";",
			this.Issuer.Replace(";", ":"),
			";",
			Convert.ToString(this.IssuanceTime)
		});
	}

	public string OriginalName;

	public string Id;

	public long Expires;

	public string Reason;

	public string Issuer;

	public long IssuanceTime;
}
