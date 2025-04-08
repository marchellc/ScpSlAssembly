using System;
using DeathAnimations;

public class DisintegrateDeathAnimation : DeathAnimation
{
	public interface IDisintegrateDamageHandler
	{
		bool Disintegrate { get; }
	}
}
