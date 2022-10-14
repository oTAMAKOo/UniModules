
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Extensions;

namespace Modules.UI.DummyContent
{
	[ExecuteAlways]
	[DisallowMultipleComponent]
    public sealed partial class DummyText : MonoBehaviour
    {
        //----- params -----

        //----- field -----

		private Text textComponent = null;

		private TextMeshProUGUI textMeshProComponent = null;

		//----- property -----

		//----- method -----

		void Awake()
		{
			ImportText();
		}

		#if UNITY_EDITOR
        
		void OnEnable()
		{
			if (Application.isPlaying){ return; }

			ImportText();
		}

		#endif

		private void ImportText()
		{
			ApplyText(null);

			#if UNITY_EDITOR

			ApplyDummyText();

			#endif
		}

		private void ApplyText(string text)
		{
			GetTargetComponent();
			
			if (textMeshProComponent != null)
			{
				textMeshProComponent.ForceMeshUpdate(true);
				textMeshProComponent.SetText(text);
			}

			if (textComponent != null)
			{
				textComponent.text = text;
			}
		}

		private string GetTargetText()
		{
			GetTargetComponent();

			if (textMeshProComponent != null)
			{
				return textMeshProComponent.text;
			}

			if (textComponent != null)
			{
				return textComponent.text;
			}

			return null;
		}

		private void GetTargetComponent()
		{
			if (textMeshProComponent == null)
			{
				textMeshProComponent = UnityUtility.GetComponent<TextMeshProUGUI>(gameObject);
			}

			if (textComponent == null)
			{
				textComponent = UnityUtility.GetComponent<Text>(gameObject);
			}
		}
	}
}