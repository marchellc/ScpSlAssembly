using System.Linq;
using UnityEngine;

public class CreditsEntry
{
	public string Title;

	public string Name;

	public bool Multi;

	public Color Color;

	public CreditsEntry()
	{
	}

	public CreditsEntry(string title, string name)
	{
		this.Multi = false;
		this.Title = title;
		this.Name = name;
		this.Color = Color.white;
	}

	public CreditsEntry(string title, string name, Color color)
	{
		this.Multi = false;
		this.Title = title;
		this.Name = name;
		this.Color = color;
	}

	public CreditsEntry(string name)
	{
		this.Multi = false;
		this.Title = "";
		this.Name = name;
	}

	public CreditsEntry(string[] names)
	{
		this.Multi = true;
		this.Title = "";
		this.Name = names.Aggregate("", (string current, string n) => current + n + "\n");
	}
}
