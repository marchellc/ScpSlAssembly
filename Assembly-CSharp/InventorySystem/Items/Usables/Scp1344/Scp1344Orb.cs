using System;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp1344;

public class Scp1344Orb : MonoBehaviour
{
	private Transform _tr;

	[field: SerializeField]
	public ParticleSystem ParticleSystem { get; private set; }

	public bool ParticleEmissionEnabled
	{
		get
		{
			return this.ParticleSystem.emission.enabled;
		}
		set
		{
			ParticleSystem.EmissionModule emission = this.ParticleSystem.emission;
			emission.enabled = value;
		}
	}

	public Color ParticleColor
	{
		get
		{
			return this.ParticleSystem.main.startColor.color;
		}
		set
		{
			ParticleSystem.MainModule main = this.ParticleSystem.main;
			main.startColor = new ParticleSystem.MinMaxGradient(value);
		}
	}

	public Vector3 Position
	{
		get
		{
			return this._tr.position;
		}
		set
		{
			this._tr.position = value;
		}
	}

	public Vector3 Scale
	{
		get
		{
			return this._tr.localScale;
		}
		set
		{
			this._tr.localScale = value;
		}
	}

	public event Action OnDestroyCallback;

	private void Awake()
	{
		this._tr = base.transform;
	}

	private void OnDestroy()
	{
		this.OnDestroyCallback?.Invoke();
	}
}
