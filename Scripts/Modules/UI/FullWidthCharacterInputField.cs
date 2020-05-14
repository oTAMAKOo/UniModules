
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

        private string prevText = "";
        private int pos = 0;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            if (inputField == null)
            {
                inputField = UnityUtility.GetComponent<InputField>(gameObject);
            }

            if (inputField != null)
            {
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

            //他のInputFieldに変換中の文字が表示されるのを防ぐため選択中のみ.

            if (!inputField.isFocused) { return; }

            // 強制的にラベルを即時更新.
            // キャレットと表示されている文字列の位置を再計算.
            inputField.ForceLabelUpdate();
        }
    }
}