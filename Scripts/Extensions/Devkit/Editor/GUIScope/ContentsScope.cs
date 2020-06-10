﻿﻿
using UnityEngine;

namespace Extensions.Devkit
{
    public sealed class ContentsScope : GUI.Scope
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public ContentsScope()
        {
            EditorLayoutTools.BeginContents();
        }

        protected override void CloseScope()
        {
            EditorLayoutTools.EndContents();
        }
    }
}
