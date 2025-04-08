using System;
using UnityEngine;
using UnityEngine.UI;

namespace UserSettings.GUIElements
{
	public class UserSettingsCategories : MonoBehaviour
	{
		public void ResetSelection()
		{
			this.SelectCategory(0);
		}

		private void Awake()
		{
			for (int i = 0; i < this._categories.Length; i++)
			{
				this.ToggleCategory(i, i == 0);
				int iCopy = i;
				this._categories[i].Activator.onClick.AddListener(delegate
				{
					this.SelectCategory(iCopy);
				});
			}
		}

		private void Update()
		{
			this._layoutGroup.CalculateLayoutInputHorizontal();
			this._layoutGroup.SetLayoutHorizontal();
		}

		private void ToggleCategory(int index, bool isVisible)
		{
			UserSettingsCategories.Category category = this._categories[index];
			category.Group.SetActive(isVisible);
			category.Highlight.SetActive(isVisible);
		}

		private void SelectCategory(int cat)
		{
			this.ToggleCategory(this._prev, false);
			this.ToggleCategory(cat, true);
			this._prev = cat;
		}

		[SerializeField]
		private UserSettingsCategories.Category[] _categories;

		[SerializeField]
		private HorizontalLayoutGroup _layoutGroup;

		private int _prev;

		[Serializable]
		private struct Category
		{
			public GameObject Group;

			public GameObject Highlight;

			public Button Activator;
		}
	}
}
