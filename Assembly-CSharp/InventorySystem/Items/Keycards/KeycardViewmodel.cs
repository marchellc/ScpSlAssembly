using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Keycards;

public class KeycardViewmodel : StandardAnimatedViemodel
{
	private static readonly List<Renderer> GetCompNonAlloc = new List<Renderer>();

	private static readonly int UseHash = Animator.StringToHash("Use");

	private static readonly int IdleHash = Animator.StringToHash("Idle");

	private static readonly int StartInspectHash = Animator.StringToHash("InspectStart");

	private static readonly int AllowInspectHash = Animator.StringToHash("InspectValid");

	private bool _wasInspecting;

	[SerializeField]
	private Transform _keycardGfxSpawn;

	[SerializeField]
	private KeycardGfx _spawnedGfx;

	[SerializeField]
	private AudioSource _equipSoundSource;

	[SerializeField]
	private AudioSource _inspectSource;

	public bool IsIdle
	{
		get
		{
			if (this.AnimatorStateInfo(0).tagHash == KeycardViewmodel.IdleHash)
			{
				return !this.AnimatorInTransition(0);
			}
			return false;
		}
	}

	public override void InitAny()
	{
		base.InitAny();
		KeycardItem.OnKeycardUsed += OnAnyKeycardUsed;
		if (!base.ItemId.TryGetTemplate<KeycardItem>(out var item))
		{
			return;
		}
		if (this._spawnedGfx != null)
		{
			KeycardDetailSynchronizer.RegisterReceiver(this._spawnedGfx);
			return;
		}
		this._spawnedGfx = Object.Instantiate(item.KeycardGfx, this._keycardGfxSpawn);
		this._spawnedGfx.transform.ResetTransform();
		KeycardViewmodel.GetCompNonAlloc.Clear();
		this._spawnedGfx.GetComponentsInChildren(includeInactive: true, KeycardViewmodel.GetCompNonAlloc);
		int layer = base.gameObject.layer;
		foreach (Renderer item2 in KeycardViewmodel.GetCompNonAlloc)
		{
			item2.gameObject.layer = layer;
		}
	}

	public override void InitSpectator(ReferenceHub ply, ItemIdentifier id, bool wasEquipped)
	{
		base.InitSpectator(ply, id, wasEquipped);
		if (wasEquipped)
		{
			this._equipSoundSource.Stop();
		}
		this.AnimatorForceUpdate(base.SkipEquipTime);
		this.UpdateInspecting(skipAnimator: true);
	}

	private void Update()
	{
		this.UpdateInspecting(skipAnimator: false);
	}

	private void UpdateInspecting(bool skipAnimator)
	{
		double value;
		bool flag = KeycardItem.StartInspectTimes.TryGetValue(base.ItemId.SerialNumber, out value);
		if (this._wasInspecting == flag)
		{
			return;
		}
		this._wasInspecting = flag;
		this.AnimatorSetBool(KeycardViewmodel.AllowInspectHash, flag);
		if (!flag)
		{
			this._inspectSource.Stop();
			return;
		}
		this.AnimatorSetTrigger(KeycardViewmodel.StartInspectHash);
		float num = (float)(NetworkTime.time - value);
		bool flag2 = num < this._inspectSource.clip.length;
		if (flag2)
		{
			this._inspectSource.Play();
		}
		if (skipAnimator)
		{
			if (flag2)
			{
				this._inspectSource.time = num;
			}
			this.AnimatorForceUpdate(num, fastMode: false);
		}
	}

	protected virtual void PlayUseAnimation(bool success)
	{
		this.AnimatorSetTrigger(KeycardViewmodel.UseHash);
	}

	protected virtual void OnDestroy()
	{
		KeycardItem.OnKeycardUsed -= OnAnyKeycardUsed;
	}

	private void OnAnyKeycardUsed(ushort serial, bool success)
	{
		if (serial == base.ItemId.SerialNumber)
		{
			this.PlayUseAnimation(success);
		}
	}
}
