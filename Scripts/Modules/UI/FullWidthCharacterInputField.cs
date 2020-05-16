
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Extensions;

namespace Modules.UI
{
    // 日本語入力の全角変換中に確定させない状態で
    // InputFieldからフォーカスを外すと変換中の文字が倍加するバグがあり、倍加させない

    [DisallowMultipleComponent]
    public sealed class FullWidthCharacterInputField : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        private InputField inputField = null;

        #if UNITY_EDITOR

        private Text inputFieldText = null;

        #endif

        private string prevText = "";
        private int pos = 0;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            if (inputField == null)
            {
                inputField = UnityUtility.GetComponent<InputField>(gameObject);

                #if UNITY_EDITOR

                // Editorでの入力中に他のInputFieldに入力中の文字が表示されるので.
                // 非フォーカス時に表示用の実テキストとは別のテキストオブジェクトを生成.

                var textComponent = inputField.textComponent;
                var parent = textComponent.transform.parent.gameObject;

                inputFieldText = UnityUtility.Instantiate<Text>(parent, textComponent.gameObject);

                OnFocuseChanged(inputField.isFocused);

                #endif
            }

            if (inputField != null)
            {
                inputField.ObserveEveryValueChanged(x => x.isFocused)
                    .TakeUntilDisable(this)
                    .Subscribe(x => OnFocuseChanged(x))
                    .AddTo(this);

                Observable.EveryUpdate()
                    .TakeUntilDisable(this)
                    .Subscribe(_ => UpdateContents())
                    .AddTo(this);

                Observable.EveryLateUpdate()
                    .TakeUntilDisable(this)
                    .Subscribe(_ => LateUpdateContents())
                    .AddTo(this);
            }
        }

        private void OnFocuseChanged(bool isFocused)
        {
            // フォーカスされた時に既存の文字が全選択されるのを解除する.
            if (isFocused)
            {
                inputField.MoveTextEnd(true);
            }

            #if UNITY_EDITOR

            UnityUtility.SetActive(inputFieldText, !isFocused);
            UnityUtility.SetActive(inputField.textComponent, isFocused);

            #endif
        }

        private void UpdateContents()
        {
            if (inputField == null) { return; }

            if (Input.GetMouseButtonDown(0))
            {
                var currentText = inputField.text;

                if (!string.IsNullOrEmpty(currentText))
                {
                    var currentTextLength = inputField.text.Length;

                    if (prevText != currentText)
                    {
                        if (string.IsNullOrEmpty(prevText))
                        {
                            if (currentTextLength > 1 && currentTextLength % 2 == 0)
                            {
                                var s1 = currentText.Substring(0, currentTextLength / 2);
                                var s2 = currentText.Substring(currentTextLength / 2, currentTextLength / 2);

                                if (s1 == s2)
                                {
                                    //未確定時
                                    currentText = s1;
                                }
                            }
                        }
                        else
                        {
                            var prevLength = prevText.Length;

                            if (currentTextLength - prevLength > 1)
                            {
                                if (prevLength + (pos - prevLength) * 2 == currentTextLength)
                                {
                                    var check = currentText.Substring(prevLength);
                                    var len = check.Length;

                                    if (len > 1 && len % 2 == 0)
                                    {
                                        var s1 = check.Substring(0, len / 2);
                                        var s2 = check.Substring(len / 2, len / 2);

                                        if (s1 == s2)
                                        {
                                            var t1 = currentText.Substring(0, prevLength);

                                            // 未確定時.
                                            currentText = t1 + s1;
                                        }
                                    }
                                }
                                else
                                {
                                    var backward = (currentTextLength - (prevLength + (pos - prevLength) * 2)) / 2;
                                    var forward = prevLength - backward;

                                    var check = currentText.Remove(currentTextLength - backward).Substring(forward);
                                    var len = check.Length;

                                    if (len > 1 && len % 2 == 0)
                                    {
                                        var s1 = check.Substring(0, len / 2);
                                        var s2 = check.Substring(len / 2, len / 2);

                                        if (s1 == s2)
                                        {
                                            var t1 = currentText.Substring(0, forward);
                                            var t2 = currentText.Substring(currentTextLength - backward, backward);

                                            currentText = t1 + s1 + t2;
                                        }
                                    }
                                }
                            }
                        }

                    }
                }
                
                inputField.text = currentText;
                prevText = currentText;
            }

            prevText = inputField.text;
            pos = inputField.selectionAnchorPosition;
        }

        private void LateUpdateContents()
        {
            if (inputField == null) { return; }

            #if UNITY_EDITOR

            if (!inputField.isFocused)
            {
                // フォーカスが当たってない時は常時上書き.
                inputFieldText.text = inputField.text;
            }

            #endif

            if (inputField.isFocused)
            {
                // 強制的にラベルを即時更新.
                // キャレットと表示されている文字列の位置を再計算.
                inputField.ForceLabelUpdate();
            }
        }
    }
}
