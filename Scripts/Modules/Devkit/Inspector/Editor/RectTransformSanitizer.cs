
using UnityEngine;
using UnityEditor;

namespace Modules.Devkit.Inspector
{
    /// <summary> RectTransformに潜む浮動小数点誤差(例: 7.629395e-06)を整数近傍へ丸めるEditor拡張. </summary>
    public static class RectTransformSanitizer
    {
        //----- params -----

        /// <summary> この閾値未満の「キリの良い値」からのズレは浮動小数点誤差とみなして丸める. </summary>
        private const float Threshold = 1e-3f;

        //----- method -----

        [MenuItem("CONTEXT/RectTransform/Sanitize Values")]
        private static void SanitizeContextMenu(MenuCommand command)
        {
            var rt = command.context as RectTransform;

            if (rt == null){ return; }

            Sanitize(rt);
        }

        private static void Sanitize(RectTransform rt)
        {
            // Prefabインスタンスのオーバーライドやsetterの再計算を迂回するため、SerializedObject経由で直接書き換える.
            var so = new SerializedObject(rt);

            var pAnchorMin = so.FindProperty("m_AnchorMin");
            var pAnchorMax = so.FindProperty("m_AnchorMax");
            var pPivot = so.FindProperty("m_Pivot");
            var pAnchoredPosition = so.FindProperty("m_AnchoredPosition");
            var pSizeDelta = so.FindProperty("m_SizeDelta");
            var pLocalScale = so.FindProperty("m_LocalScale");
            var pLocalPosition = so.FindProperty("m_LocalPosition");
            var pLocalEulerAnglesHint = so.FindProperty("m_LocalEulerAnglesHint");
            var pLocalRotation = so.FindProperty("m_LocalRotation");

            var currentAnchorMin = pAnchorMin.vector2Value;
            var currentAnchorMax = pAnchorMax.vector2Value;
            var currentPivot = pPivot.vector2Value;
            var currentAnchoredPosition = pAnchoredPosition.vector2Value;
            var currentSizeDelta = pSizeDelta.vector2Value;
            var currentLocalScale = pLocalScale.vector3Value;
            var currentLocalPosition = pLocalPosition.vector3Value;
            var currentEuler = pLocalEulerAnglesHint != null ? pLocalEulerAnglesHint.vector3Value : Vector3.zero;

            // anchor/pivot を補正(offsetMin/Max算出に影響するため先に求める).
            var anchorMin = SanitizeVector2(currentAnchorMin);
            var anchorMax = SanitizeVector2(currentAnchorMax);
            var pivot = SanitizeVector2(currentPivot);

            // offsetMin/Max を Unityの計算式で算出し、Sanitize後に anchoredPosition / sizeDelta を逆算する.
            var currentOffsetMin = currentAnchoredPosition - Vector2.Scale(currentSizeDelta, pivot);
            var currentOffsetMax = currentAnchoredPosition + Vector2.Scale(currentSizeDelta, Vector2.one - pivot);
            var offsetMin = SanitizeVector2(currentOffsetMin);
            var offsetMax = SanitizeVector2(currentOffsetMax);
            var sizeDelta = offsetMax - offsetMin;
            var anchoredPosition = offsetMin + Vector2.Scale(sizeDelta, pivot);

            var localScale = SanitizeVector3(currentLocalScale);
            var localEulerAnglesHint = SanitizeVector3(currentEuler);
            var localPosition = new Vector3(currentLocalPosition.x, currentLocalPosition.y, SanitizeFloat(currentLocalPosition.z));

            var changed = false;

            if (currentAnchorMin != anchorMin){ pAnchorMin.vector2Value = anchorMin; changed = true; }
            if (currentAnchorMax != anchorMax){ pAnchorMax.vector2Value = anchorMax; changed = true; }
            if (currentPivot != pivot){ pPivot.vector2Value = pivot; changed = true; }
            if (currentAnchoredPosition != anchoredPosition){ pAnchoredPosition.vector2Value = anchoredPosition; changed = true; }
            if (currentSizeDelta != sizeDelta){ pSizeDelta.vector2Value = sizeDelta; changed = true; }
            if (currentLocalScale != localScale){ pLocalScale.vector3Value = localScale; changed = true; }
            if (currentLocalPosition != localPosition){ pLocalPosition.vector3Value = localPosition; changed = true; }

            if (pLocalEulerAnglesHint != null && currentEuler != localEulerAnglesHint)
            {
                pLocalEulerAnglesHint.vector3Value = localEulerAnglesHint;

                // 実回転を保持するQuaternionもEuler Hintから再構築して誤差を消す.
                if (pLocalRotation != null)
                {
                    pLocalRotation.quaternionValue = Quaternion.Euler(localEulerAnglesHint);
                }

                changed = true;
            }

            if (!changed){ return; }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(rt);
        }

        private static Vector2 SanitizeVector2(Vector2 v)
        {
            return new Vector2(SanitizeFloat(v.x), SanitizeFloat(v.y));
        }

        private static Vector3 SanitizeVector3(Vector3 v)
        {
            return new Vector3(SanitizeFloat(v.x), SanitizeFloat(v.y), SanitizeFloat(v.z));
        }

        private static float SanitizeFloat(float value)
        {
            // 整数近傍(例: 0, 1, -1, 100)に落とす.
            var roundedInt = Mathf.Round(value);

            if (Mathf.Abs(value - roundedInt) < Threshold){ return roundedInt; }

            // 0.5刻み近傍(例: 0.5, 1.5, -0.5)に落とす. pivotやanchorの典型値を救済する.
            var roundedHalf = Mathf.Round(value * 2f) / 2f;

            if (Mathf.Abs(value - roundedHalf) < Threshold){ return roundedHalf; }

            return value;
        }
    }
}
