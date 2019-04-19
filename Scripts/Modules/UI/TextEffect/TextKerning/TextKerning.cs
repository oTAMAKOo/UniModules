
// Reference form: http://forum.unity3d.com/threads/adjustable-character-spacing-free-script.288277/

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Extensions;

namespace Modules.UI.TextEffect
{
    [RequireComponent(typeof(Text))]
    public class TextKerning : BaseMeshEffect
    {
        //----- params -----

        private const string SupportedTagRegexPattersn = @"<b>|</b>|<i>|</i>|<size=.*?>|</size>|<color=.*?>|</color>|<material=.*?>|</material>";

        //----- field -----

        [SerializeField]
        private float spacing = 0f;
        [SerializeField]
        private float ttt = 0f;
        [SerializeField]
        private float yyy = 0f;

        private Text textComponent = null;

        //----- property -----

        private Text TextComponent
        {
            get { return textComponent ?? (textComponent = UnityUtility.GetComponent<Text>(this)); }
        }

        public float Spacing
        {
            get { return spacing; }

            set
            {
                if (spacing == value) { return; }

                spacing = value;

                if (graphic != null)
                {
                    graphic.SetVerticesDirty();
                }
            }
        }

        public float YYY
        {
            get { return yyy; }

            set
            {
                if (yyy == value) { return; }

                yyy = value;

                if (graphic != null)
                {
                    graphic.SetVerticesDirty();
                }
            }
        }

        public float TTT
        {
            get { return ttt; }

            set
            {
                if (ttt == value) { return; }

                ttt = value;

                if (graphic != null)
                {
                    graphic.SetVerticesDirty();
                }
            }
        }

        //----- method -----

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) { return; }

            var list = new List<UIVertex>();

            vh.GetUIVertexStream(list);

            ModifyVertices(list);

            vh.Clear();
            vh.AddUIVertexTriangleStream(list);
        }

        public void ModifyVertices(List<UIVertex> verts)
        {
            if (!IsActive()){ return; }

            var str = TextComponent.text;

            // Artificially insert line breaks for automatic line breaks.
            var lineInfos = TextComponent.cachedTextGenerator.lines;

            for (var i = lineInfos.Count - 1; i > 0; i--)
            {
                // Insert a \n at the location Unity wants to automatically line break.
                // Also, remove any space before the automatic line break location.
                str = str.Insert(lineInfos[i].startCharIdx, "\n");
                str = str.Remove(lineInfos[i].startCharIdx - 1, 1);
            }

            var lines = str.Split('\n');

            var pos = Vector3.zero;
            var xOffset = 0f;
            var letterOffset = spacing * TextComponent.fontSize / 100f;
            var alignmentFactor = 0f;

            // character index from the beginning of the text, including RichText tags and line breaks.
            var glyphIdx = 0;

            var isRichText = TextComponent.supportRichText;

            // when using RichText this will collect all tags (index, length, value)
            IEnumerator matchedTagCollection = null;

            Match currentMatchedTag = null;

            switch (TextComponent.alignment)
            {
                case TextAnchor.LowerLeft:
                case TextAnchor.MiddleLeft:
                case TextAnchor.UpperLeft:
                    alignmentFactor = 0f;
                    break;

                case TextAnchor.LowerCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.UpperCenter:
                    alignmentFactor = 0.5f;
                    break;

                case TextAnchor.LowerRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.UpperRight:
                    alignmentFactor = 1f;
                    break;
            }

            for (var lineIdx = 0; lineIdx < lines.Length; lineIdx++)
            {
                var line = lines[lineIdx];
                var lineLength = line.Length;

                if (isRichText)
                {
                    matchedTagCollection = GetRegexMatchedTagCollection(line, out lineLength);

                    currentMatchedTag = null;

                    if (matchedTagCollection.MoveNext())
                    {
                        currentMatchedTag = (Match)matchedTagCollection.Current;
                    }
                }

                var lineOffset = (lineLength - 1) * letterOffset * alignmentFactor;

                pos.x -= lineOffset;

                for (int charIdx = 0, actualCharIndex = 0; charIdx < line.Length; charIdx++, actualCharIndex++)
                {
                    if (isRichText)
                    {
                        if (currentMatchedTag != null && currentMatchedTag.Index == charIdx)
                        {
                            // skip matched RichText tag.
                            // -1 because next iteration will increment charIdx.
                            charIdx += currentMatchedTag.Length - 1;

                            // tag is not an actual character, cancel counter increment on this iteration
                            actualCharIndex--;

                            // glyph index is not incremented in for loop so skip entire length
                            glyphIdx += currentMatchedTag.Length;

                            // prepare next tag to detect
                            currentMatchedTag = null;

                            if (matchedTagCollection.MoveNext())
                            {
                                currentMatchedTag = (Match)matchedTagCollection.Current;
                            }

                            continue;
                        }
                    }

                    var idx1 = glyphIdx * 6 + 0;
                    var idx2 = glyphIdx * 6 + 1;
                    var idx3 = glyphIdx * 6 + 2;
                    var idx4 = glyphIdx * 6 + 3;
                    var idx5 = glyphIdx * 6 + 4;
                    var idx6 = glyphIdx * 6 + 5;

                    // Check for truncated text (doesn't generate verts for all characters)
                    if (idx6 > verts.Count - 1){ return; }

                    var vert1 = verts[idx1];
                    var vert2 = verts[idx2];
                    var vert3 = verts[idx3];
                    var vert4 = verts[idx4];
                    var vert5 = verts[idx5];
                    var vert6 = verts[idx6];

                    if (line[charIdx] == '1')
                    {
                        xOffset += TextComponent.fontSize / 100f * ttt; 
                    }

                    if (0 < charIdx && line[charIdx -1] == '1')
                    {
                        xOffset += TextComponent.fontSize / 100f * yyy;
                    }

                    pos = Vector3.right * (letterOffset * actualCharIndex + xOffset);

                    vert1.position += pos;
                    vert2.position += pos;
                    vert3.position += pos;
                    vert4.position += pos;
                    vert5.position += pos;
                    vert6.position += pos;

                    verts[idx1] = vert1;
                    verts[idx2] = vert2;
                    verts[idx3] = vert3;
                    verts[idx4] = vert4;
                    verts[idx5] = vert5;
                    verts[idx6] = vert6;

                    glyphIdx++;
                }

                xOffset = 0f;

                // Offset for carriage return character that still generates verts
                glyphIdx++;
            }
        }

        private IEnumerator GetRegexMatchedTagCollection(string line, out int lineLengthWithoutTags)
        {
            var matchedTagCollection = Regex.Matches(line, SupportedTagRegexPattersn);

            lineLengthWithoutTags = 0;

            var tagsLength = 0;

            if (matchedTagCollection.Count > 0)
            {
                foreach (Match matchedTag in matchedTagCollection)
                {
                    tagsLength += matchedTag.Length;
                }
            }

            lineLengthWithoutTags = line.Length - tagsLength;

            return matchedTagCollection.GetEnumerator();
        }
    }
}
