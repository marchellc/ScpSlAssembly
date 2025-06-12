using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Keycards.Snake;
using MEC;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Items.Keycards;

public class ChaosKeycardViewmodel : KeycardViewmodel
{
	[Serializable]
	private class LedColumn
	{
		public string LabelPrefix;

		[SerializeField]
		private LedDiode[] _diodes;

		[SerializeField]
		private bool _maskContainment;

		[SerializeField]
		private bool _maskArmory;

		[SerializeField]
		private bool _maskAdmin;

		private int _highestLevel;

		private KeycardLevels LevelMask => new KeycardLevels(this._maskContainment ? 3 : 0, this._maskArmory ? 3 : 0, this._maskAdmin ? 3 : 0);

		private DoorPermissionFlags PermsMask => this.LevelMask.Permissions;

		public void Init(Renderer rend, DoorPermissionFlags keycardPerms)
		{
			this._highestLevel = new KeycardLevels(this.PermsMask & keycardPerms).HighestLevelValue;
			for (int i = 0; i < this._diodes.Length; i++)
			{
				LedDiode ledDiode = this._diodes[i];
				ledDiode.PropertyBlock = new MaterialPropertyBlock();
				rend.GetPropertyBlock(ledDiode.PropertyBlock, ledDiode.MaterialId);
				ledDiode.HasPermission = i < this._highestLevel;
			}
		}

		public void SetColorIdle(Color inactive, Color active)
		{
			LedDiode[] diodes = this._diodes;
			foreach (LedDiode obj in diodes)
			{
				obj.SetColor(obj.HasPermission ? active : inactive);
			}
		}

		public string SetPermissionColor(int index, Color active, Color inactive, Color denied, Color granted, DoorPermissionFlags requiredPerms)
		{
			int highestLevelValue = new KeycardLevels(requiredPerms & this.PermsMask).HighestLevelValue;
			int num = index + 1;
			LedDiode ledDiode = this._diodes[index];
			if (num > highestLevelValue)
			{
				ledDiode.SetColor(ledDiode.HasPermission ? active : inactive);
				return null;
			}
			if (num > this._highestLevel)
			{
				ledDiode.SetColor(denied);
				return "\nEMULATING SIGNAL " + this.LabelPrefix + num + "...  [ FAILED ]";
			}
			ledDiode.SetColor(granted);
			return "\nEMULATING SIGNAL " + this.LabelPrefix + num + "...  [ SUCCESS ]";
		}

		public void UpdateGfx(Renderer renderer)
		{
			LedDiode[] diodes = this._diodes;
			foreach (LedDiode ledDiode in diodes)
			{
				if (ledDiode.Modified)
				{
					ledDiode.Modified = false;
					renderer.SetPropertyBlock(ledDiode.PropertyBlock, ledDiode.MaterialId);
				}
			}
		}
	}

	[Serializable]
	private class LedDiode
	{
		private static readonly int ColorHash = Shader.PropertyToID("_EmissiveColor");

		private Color? _lastColor;

		public int MaterialId;

		public bool Modified { get; set; }

		public bool HasPermission { get; set; }

		public MaterialPropertyBlock PropertyBlock { get; set; }

		public void SetColor(Color color)
		{
			if (!(this._lastColor == color))
			{
				this.PropertyBlock.SetColor(LedDiode.ColorHash, color);
				this.Modified = true;
				this._lastColor = color;
			}
		}
	}

	private static readonly int ButtonTriggerHash = Animator.StringToHash("ButtonTrigger");

	private static readonly int ButtonDirXHash = Animator.StringToHash("ButtonDirectionX");

	private static readonly int ButtonDirYHash = Animator.StringToHash("ButtonDirectionY");

	public const float SnakeBootupTime = 1.7f;

	private const int IdleLayer = 1;

	[Multiline]
	[SerializeField]
	private string _defaultText;

	[SerializeField]
	private SnakeDisplay _snakeDisplay;

	[SerializeField]
	private GameObject _loadingRoot;

	[SerializeField]
	private GameObject _normalRoot;

	[SerializeField]
	private Slider _loadingSlider;

	[SerializeField]
	private LedColumn[] _diodes;

	[SerializeField]
	private TMP_Text _consoleText;

	[SerializeField]
	[ColorUsage(false, true)]
	private Color _diodeOff;

	[SerializeField]
	[ColorUsage(false, true)]
	private Color _diodeBlue;

	[SerializeField]
	[ColorUsage(false, true)]
	private Color _diodeGreen;

	[SerializeField]
	[ColorUsage(false, true)]
	private Color _diodeRed;

	[SerializeField]
	[ColorUsage(false, true)]
	private Color _diodeNeutral;

	[SerializeField]
	private Renderer _diodeRenderer;

	private CoroutineHandle _lastHandle;

	private DoorPermissionFlags _animReqPerms;

	private string _animReqName;

	public override void InitAny()
	{
		base.InitAny();
		ChaosKeycardItem.OnSnakeMovementDirChanged += OnDirChanged;
		ChaosKeycardItem.OnDetailedUse += OnDetailedUse;
		this.TryGetPermissions(out var perms);
		LedColumn[] diodes = this._diodes;
		for (int i = 0; i < diodes.Length; i++)
		{
			diodes[i].Init(this._diodeRenderer, perms);
		}
		this.SetIdle();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		ChaosKeycardItem.OnSnakeMovementDirChanged -= OnDirChanged;
		ChaosKeycardItem.OnDetailedUse -= OnDetailedUse;
	}

	internal override void OnEquipped()
	{
		base.OnEquipped();
		this.SetIdle();
	}

	private void OnDetailedUse(ushort serial, DoorPermissionFlags flags, string reqName)
	{
		if (serial == base.ItemId.SerialNumber)
		{
			this._animReqPerms = flags;
			this._animReqName = Regex.Replace(reqName, "([a-z0-9])([A-Z])", "$1_$2").ToUpperInvariant();
			this._lastHandle.IsRunning = false;
			this._lastHandle = Timing.RunCoroutine(this.UseAnimation().CancelWith(base.gameObject), Segment.Update);
		}
	}

	private void OnDirChanged(ushort? netSerial, Vector2Int dir)
	{
		if (base.IsLocal)
		{
			if (netSerial.HasValue)
			{
				return;
			}
		}
		else if (netSerial != base.ItemId.SerialNumber)
		{
			return;
		}
		this.AnimatorSetInt(ChaosKeycardViewmodel.ButtonDirXHash, dir.x);
		this.AnimatorSetInt(ChaosKeycardViewmodel.ButtonDirYHash, dir.y);
		this.AnimatorSetTrigger(ChaosKeycardViewmodel.ButtonTriggerHash);
	}

	private void SetRoot(UnityEngine.Object targetActive, float idleWeight = 1f)
	{
		this.AnimatorSetLayerWeight(1, idleWeight);
		this._loadingRoot.SetActive((object)targetActive == this._loadingRoot);
		this._normalRoot.SetActive((object)targetActive == this._normalRoot);
		this._snakeDisplay.gameObject.SetActive((object)targetActive == this._snakeDisplay);
	}

	private void SetIdle()
	{
		this._consoleText.text = this._defaultText;
		LedColumn[] diodes = this._diodes;
		for (int i = 0; i < diodes.Length; i++)
		{
			diodes[i].SetColorIdle(this._diodeOff, this._diodeBlue);
		}
	}

	private IEnumerator<float> UseAnimation()
	{
		this.SetIdle();
		this._consoleText.text = "INITIATOR DETECTED [ 13.56 MHz ] \nSIGNATURE: " + this._animReqName + " \n\nTRANSCEIVING...";
		for (int i = 0; i < 3; i++)
		{
			yield return Timing.WaitForSeconds(0.2f);
			LedColumn[] diodes = this._diodes;
			for (int j = 0; j < diodes.Length; j++)
			{
				string text = diodes[j].SetPermissionColor(i, this._diodeBlue, this._diodeOff, this._diodeRed, this._diodeGreen, this._animReqPerms);
				if (!string.IsNullOrEmpty(text))
				{
					this._consoleText.text += text;
				}
			}
		}
		this._consoleText.text += "\n\nEMULATION STOPPING...";
		yield return Timing.WaitForSeconds(1.8f);
		this.SetIdle();
	}

	private bool TryGetPermissions(out DoorPermissionFlags perms)
	{
		if (base.ItemId.TryGetTemplate<KeycardItem>(out var item))
		{
			perms = item.GetPermissions(null);
			return true;
		}
		perms = DoorPermissionFlags.None;
		return false;
	}

	protected override void LateUpdate()
	{
		base.LateUpdate();
		LedColumn[] diodes = this._diodes;
		for (int i = 0; i < diodes.Length; i++)
		{
			diodes[i].UpdateGfx(this._diodeRenderer);
		}
		if (!KeycardItem.StartInspectTimes.TryGetValue(base.ItemId.SerialNumber, out var value))
		{
			this.SetRoot(this._normalRoot);
			return;
		}
		float num = (float)(NetworkTime.time - value) / 1.7f;
		if (num < 1f)
		{
			this.SetRoot(this._loadingRoot, Mathf.Clamp01(1f - num));
			this._loadingSlider.value = EaseInUtils.ChoppyLoading(num, 0.1666666716337204);
		}
		else
		{
			this.SetRoot(this._snakeDisplay, 0f);
		}
	}
}
