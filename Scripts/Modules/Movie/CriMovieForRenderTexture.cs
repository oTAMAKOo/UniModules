
using UnityEngine;
using CriWare;

namespace Modules.Movie
{
    public sealed class CriMovieForRenderTexture : CriManaMovieMaterial
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private RenderTexture renderTexture = null;

        //----- property -----

        public RenderTexture Target
        {
            get { return renderTexture; }
            set { renderTexture = value; }
        }

        //----- method -----

        public override void CriInternalUpdate()
        {
            base.CriInternalUpdate();

            player.OnWillRenderObject(this);
            
            if (isMaterialAvailable && renderTexture != null)
            {
                Graphics.Blit(null, renderTexture, material);
            }
        }
    }
}