using System;
using TMPro;
using UnityEngine;

namespace UserSettings.ControlsSettings
{
	public class KeybindListGenerator : MonoBehaviour
	{
		private void Awake()
		{
			foreach (ActionCategory actionCategory in KeybindListGenerator.CategorySortingOrder)
			{
				this.SpawnHeader(actionCategory);
				foreach (NewInput.ActionDefinition actionDefinition in NewInput.DefinedActions)
				{
					if (actionDefinition.Category == actionCategory)
					{
						this.SpawnInstance(actionDefinition);
					}
				}
				foreach (KeybindListGenerator.CategorizedSetting categorizedSetting in this._categorizedSettings)
				{
					if (categorizedSetting.Category == actionCategory)
					{
						categorizedSetting.Root.SetAsLastSibling();
					}
				}
			}
		}

		private void SpawnHeader(ActionCategory cat)
		{
			GameObject gameObject = global::UnityEngine.Object.Instantiate<GameObject>(this._header, base.transform);
			gameObject.SetActive(true);
			gameObject.GetComponentInChildren<TMP_Text>().text = Translations.Get<ActionCategory>(cat);
		}

		private void SpawnInstance(NewInput.ActionDefinition definition)
		{
			GameObject gameObject = global::UnityEngine.Object.Instantiate<GameObject>(this._template, base.transform);
			gameObject.SetActive(true);
			gameObject.GetComponentInChildren<KeybindEntry>().Init(definition.Name);
		}

		[SerializeField]
		private GameObject _template;

		[SerializeField]
		private GameObject _header;

		[SerializeField]
		private KeybindListGenerator.CategorizedSetting[] _categorizedSettings;

		private static readonly ActionCategory[] CategorySortingOrder = new ActionCategory[]
		{
			ActionCategory.Movement,
			ActionCategory.Gameplay,
			ActionCategory.Weapons,
			ActionCategory.Communication,
			ActionCategory.Scp079,
			ActionCategory.System
		};

		[Serializable]
		private struct CategorizedSetting
		{
			public ActionCategory Category;

			public Transform Root;
		}
	}
}
