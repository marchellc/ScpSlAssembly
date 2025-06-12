using Mirror;
using UnityEngine;
using Utils;

namespace InventorySystem.Items.ThrowableProjectiles;

public class EffectGrenade : TimeGrenade
{
	public GameObject Effect;

	[SerializeField]
	private float _destroyTime;

	[SerializeField]
	private AudioSource _src;

	private bool _resyncAudio = true;

	public override void ToggleRenderers(bool state)
	{
		base.ToggleRenderers(state);
		this._resyncAudio = state;
	}

	protected override void Update()
	{
		base.Update();
		double time = NetworkTime.time;
		if (!this._resyncAudio || base.TargetTime < time)
		{
			return;
		}
		this._resyncAudio = false;
		if (!(this._src == null))
		{
			float length = this._src.clip.length;
			float num = (float)(base.TargetTime - time);
			if (num > length)
			{
				this._src.PlayDelayed(num - length);
				return;
			}
			this._src.Play();
			this._src.time = length - num;
		}
	}

	public virtual void PlayExplosionEffects(Vector3 position)
	{
		Transform obj = Object.Instantiate(this.Effect).transform;
		obj.position = position;
		obj.localScale = Vector3.one;
		Object.Destroy(obj.gameObject, this._destroyTime);
	}

	public override bool ServerFuseEnd()
	{
		if (!base.ServerFuseEnd())
		{
			return false;
		}
		ExplosionUtils.ServerSpawnEffect(base.transform.position, base.Info.ItemId);
		base.DestroySelf();
		return true;
	}

	public override bool Weaved()
	{
		return true;
	}
}
