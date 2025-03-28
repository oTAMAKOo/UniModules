
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;
using UniRx;

namespace Modules.UI.Extension
{
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_InputField))]
    public abstract class UIInputField : UIComponent<TMP_InputField>
    {
        //----- params -----

        //----- field -----

        private Subject<string> onEndEdit = null;

        //----- property -----

        public TMP_InputField InputField { get { return component; } }

        public string text
        {
            get { return InputField.text; }
            set { InputField.text = value; }
        }

        //----- method -----

        public IObservable<string> OnEndEditAsObservable()
        {
            if (onEndEdit == null)
            {
                onEndEdit = new Subject<string>();

                void OnEndEditCallback(string text)
                {
                    onEndEdit.OnNext(text);
                }

                InputField.onEndEdit.AddListener(x => OnEndEditCallback(x));
            }

            return onEndEdit;
        }
    }
}
