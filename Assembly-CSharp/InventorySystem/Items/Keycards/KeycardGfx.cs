using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.AutoIcons;
using TMPro;
using UnityEngine;

namespace InventorySystem.Items.Keycards;

public class KeycardGfx : MonoBehaviour
{
	private static readonly int TintColorHash = Shader.PropertyToID("_Tint");

	private static readonly int PermsColorHash = Shader.PropertyToID("_PermsColor");

	private static readonly int ContainmentLevelStepHash = Shader.PropertyToID("_ContainmentStep");

	private static readonly int ArmoryLevelStepHash = Shader.PropertyToID("_ArmoryStep");

	private static readonly int AdminLevelStepHash = Shader.PropertyToID("_AdminStep");

	private static readonly int IgnoreDistanceTintHash = Shader.PropertyToID("_IgnoreDistanceTint");

	private static readonly float[] LevelToStep = new float[4] { 1f, 0.45f, 0.1f, 0f };

	private IIdentifierProvider _idProvider;

	private KeycardMaterialHandler _keycardMaterial;

	[SerializeField]
	private Renderer _mainRenderer;

	private IIdentifierProvider IdProvider => this._idProvider ?? (this._idProvider = base.GetComponentInParent<IIdentifierProvider>(includeInactive: true));

	private KeycardMaterialHandler Material => this._keycardMaterial ?? (this._keycardMaterial = new KeycardMaterialHandler(this._mainRenderer));

	public ItemIdentifier ParentId => this.IdProvider.ItemId;

	public bool IsSubjectForIconRendering { get; private set; }

	[field: SerializeField]
	public float ExtraWeight { get; private set; }

	[field: SerializeField]
	public TMP_Text[] KeycardLabels { get; private set; }

	[field: SerializeField]
	public TMP_Text[] NameFields { get; private set; }

	[field: SerializeField]
	public Renderer[] SerialNumberDigits { get; private set; }

	[field: SerializeField]
	public MeshFilter RankFilter { get; private set; }

	[field: SerializeField]
	public GameObject[] ElementVariants { get; private set; }

	public void SetTint(Color color)
	{
		Material instance = this.Material.Instance;
		instance.SetColor(KeycardGfx.TintColorHash, color);
		if (this.IsSubjectForIconRendering)
		{
			instance.SetFloat(KeycardGfx.IgnoreDistanceTintHash, 1f);
		}
	}

	public void SetAsIconSubject()
	{
		this.IsSubjectForIconRendering = true;
	}

	public void SetPermissions(KeycardLevels levels, Color? color = null)
	{
		if (!(this._mainRenderer == null))
		{
			Material instance = this.Material.Instance;
			instance.SetFloat(KeycardGfx.ContainmentLevelStepHash, KeycardGfx.LevelToStep[levels.Containment]);
			instance.SetFloat(KeycardGfx.ArmoryLevelStepHash, KeycardGfx.LevelToStep[levels.Armory]);
			instance.SetFloat(KeycardGfx.AdminLevelStepHash, KeycardGfx.LevelToStep[levels.Admin]);
			if (color.HasValue)
			{
				instance.SetColor(KeycardGfx.PermsColorHash, color.Value);
			}
		}
	}

	public void OnAllDetailsApplied()
	{
		if (this.IdProvider is KeycardViewmodel { IsLocal: not false } keycardViewmodel && keycardViewmodel.ParentItem.TryGetComponent<AutoIconApplier>(out var component))
		{
			component.UpdateIcon();
		}
	}

	private void Awake()
	{
		if (base.enabled)
		{
			KeycardDetailSynchronizer.RegisterReceiver(this);
		}
	}

	private void OnDestroy()
	{
		KeycardDetailSynchronizer.UnregisterReceiver(this);
		this._keycardMaterial?.Cleanup();
	}
}
