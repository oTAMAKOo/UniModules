
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System;

namespace Modules.Net.WebRequest
{
    public sealed class ColumnHeader : MultiColumnHeader
    {
        //----- params -----

        //----- field -----

        [NonSerialized]
        private bool initialized = false;

        //----- property -----

        //----- method -----

        public ColumnHeader(MultiColumnHeaderState state) : base(state) { }

        public void Initialize()
        {
            if (initialized) { return; }

            height = 22;
            canSort = false;

            initialized = true;
        }

        protected override void AddColumnHeaderContextMenuItems(GenericMenu menu) { }
    }
}