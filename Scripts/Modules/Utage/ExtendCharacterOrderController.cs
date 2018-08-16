
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using UniRx;
using Utage;

namespace Modules.UtageExtension
{
    /// <summary>
    /// キャラクターのソートオーダーを強制的に最前面にする拡張.
    /// </summary>
	public class ExtendCharacterOrderController : MonoBehaviour
	{
        //----- params -----

        // ライティング（グレーアウトしない）.
        [System.Flags]
        public enum TargetMask
        {
            // しゃべっているキャラは.
            Talking = 0x1,
            // ページ内の新しいキャラクター.
            NewCharacerInPage = 0x1 << 1,
            // テキストのみ表示のときは、変化しない.
            NoChanageIfTextOnly = 0x1 << 2,
        }

        private class SortOrderRestore
        {
            public int SortOrder { get; private set; }
            public AdvGraphicObject Target { get; private set; }

            public SortOrderRestore(int sortOrder, AdvGraphicObject target)
            {
                SortOrder = sortOrder;
                Target = target;
            }
        }

        //----- field -----

        [SerializeField]
        private AdvEngine engine = null;
        [SerializeField, Extensions.EnumFlags]
        private TargetMask mask = TargetMask.Talking;

        private List<SortOrderRestore> managedObjects = null;

        private bool isChanged = false;

        //----- property -----

        public TargetMask Mask
		{
			get { return mask; }
			set { mask = value; }
		}

        //----- method -----

        void Awake()
        {
            if (engine == null)
            {
                engine = UnityUtility.FindObjectOfType<AdvEngine>();
            }

            managedObjects = new List<SortOrderRestore>();

            // テキストに変更があった場合.
            if (engine != null)
            {
                engine.Page.OnBeginPage.AddListener(OnBeginPage);
                engine.Page.OnChangeText.AddListener(OnChangeText);
            }
        }

        //ページの冒頭
        private void OnBeginPage(AdvPage page)
        {
            if (mask == 0)
            {
                // 表示なしなのでリセット.
                if (isChanged)
                {
                    ForceFrontSortOrder(page);
                    isChanged = false;
                }
            }
        }

        // テキストに変更があった場合.
        private void OnChangeText(AdvPage page)
        {
            if (mask == 0) { return; }

            isChanged = true;

            // テキストのみ表示で、前のキャラをそのまま表示.
            if (string.IsNullOrEmpty(page.CharacterLabel) && (mask & TargetMask.NoChanageIfTextOnly) == TargetMask.NoChanageIfTextOnly)
            {
                return;
            }

            ForceFrontSortOrder(page);
        }

        private void ForceFrontSortOrder(AdvPage page)
        {
            var layers = engine.GraphicManager.CharacterManager.AllGraphicsLayers();

            RestoreSortOrder();

            int? frontSortOrder = null;

            foreach (var layer in layers)
            {
                foreach (var keyValue in layer.CurrentGraphics)
                {
                    var obj = keyValue.Value;

                    var sortingOrder = obj.Layer.Canvas.sortingOrder;

                    if (!frontSortOrder.HasValue || frontSortOrder.Value < sortingOrder)
                    {
                        frontSortOrder = sortingOrder;
                    }

                    if(IsCharacterForceFront(page, layer))
                    {
                        managedObjects.Add(new SortOrderRestore(sortingOrder, obj));
                    }
                }
            }

            // 対象を最前面に.
            if (frontSortOrder.HasValue)
            {
                foreach (var item in managedObjects)
                {
                    item.Target.Layer.Canvas.sortingOrder = frontSortOrder.Value + 1;
                }
            }
        }

        private void RestoreSortOrder()
        {
            foreach (var item in managedObjects)
            {
                if(UnityUtility.IsNull(item.Target)) { continue; }

                item.Target.Layer.Canvas.sortingOrder = item.SortOrder;
            }

            managedObjects.Clear();
        }

        // 強制前面表示するか.
        private bool IsCharacterForceFront(AdvPage page, AdvGraphicLayer layer)
        {
            var pageBeginLayer = page.Engine.GraphicManager.CharacterManager.AllGraphicsLayers();

            // しゃべっているキャラ.
            if ((mask & TargetMask.Talking) == TargetMask.Talking)
            {
                if (layer.DefaultObject.name == page.CharacterLabel) { return true; }
            }

            // ページ内の新規キャラ.
            if ((mask & TargetMask.NewCharacerInPage) == TargetMask.NewCharacerInPage)
            {
                if (pageBeginLayer.Find(x => (x != null) && x.DefaultObject != null && (x.DefaultObject.name == layer.DefaultObject.name)) == null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}