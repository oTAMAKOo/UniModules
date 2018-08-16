
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Utage;

namespace Modules.UtageExtension
{
    public class AdvExtendCommandEmotion : AdvCommand
    {
        //----- params -----

        //----- field -----

        private AdvCharacterInfo characterInfo = null;
        private string layerName = null;
        protected float fadeTime = 0f;

        //----- property -----

        //----- method -----

        public AdvExtendCommandEmotion(StringGridRow row, AdvSettingDataManager dataManager) : base(row)
        {
            characterInfo = AdvCharacterInfo.Create(this, dataManager);

            if (characterInfo.Graphic != null)
            {
                AddLoadGraphic(characterInfo.Graphic);
            }

            //表示レイヤー
            layerName = ParseCellOptional<string>(AdvColumnName.Arg3, "");

            if (!string.IsNullOrEmpty(layerName) && !dataManager.LayerSetting.Contains(layerName, AdvLayerSettingData.LayerType.Sprite))
            {
                //表示レイヤーが見つからない
                Debug.LogError(ToErrorString(layerName + " is not contained in layer setting"));
            }

            fadeTime = ParseCellOptional<float>(AdvColumnName.Arg6, 0f);
        }

        public override void DoCommand(AdvEngine engine)
        {
            if (characterInfo.IsHide)
            {
                // 表示オフの指定なので、表示キャラフェードアウト.
                engine.GraphicManager.SpriteManager.FadeOut(characterInfo.Label, engine.Page.ToSkippedTime(fadeTime));
            }
            else if (CheckCharacterInfo(engine))
            {
                // グラフィック表示処理.
                engine.GraphicManager.SpriteManager.DrawCharacter(
                    layerName
                    , characterInfo.Label
                    , new AdvGraphicOperaitonArg(this, this.characterInfo.Graphic.Main, fadeTime));
            }

            // 基本以外のコマンド引数の適用.
            var obj = engine.GraphicManager.SpriteManager.FindObject(this.characterInfo.Label);

            if (obj != null)
            {
                // その他の適用（モーション名など）.
                obj.TargetObject.SetCommandArg(this);
            }
        }

        private bool CheckCharacterInfo(AdvEngine engine)
        {
            if (!string.IsNullOrEmpty(characterInfo.Pattern))
            {
                return true;
            }

            return false;
        }
    }
}
