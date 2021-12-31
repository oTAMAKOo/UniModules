
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System;
using System.Linq;

namespace Modules.Devkit.MasterViewer
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

        protected override void AddColumnHeaderContextMenuItems(GenericMenu menu)
        {
            for (var index = 0; index < state.columns.Length; ++index)
            {
                var column = state.columns[index];

                var text = string.IsNullOrEmpty(column.contextMenuText) ? column.headerContent.text : column.contextMenuText;

                var content = new GUIContent(text);

                if (column.allowToggleVisibility)
                {
                    menu.AddItem(content, state.visibleColumns.Contains(index), ToggleVisibility, index);
                }
                else
                {
                    menu.AddDisabledItem(content);
                }
            }
        }

        private void ToggleVisibility(object userData)
        {
            base.ToggleVisibility((int) userData);
        }
    }
}