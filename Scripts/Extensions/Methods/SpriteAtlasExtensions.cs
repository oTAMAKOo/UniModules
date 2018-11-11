
using UnityEngine;
using UnityEngine.U2D;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;

namespace Extensions
{
    public static class SpriteAtlasExtensions
    {
        public static IEnumerable<Sprite> GetAllSprites(this SpriteAtlas atlas)
        {
            var sprites = new Sprite[atlas.spriteCount];

            atlas.GetSprites(sprites);

            foreach (var sprite in sprites)
            {
                sprite.name = sprite.name.Replace("(Clone)", "");
            }

            return sprites;
        }

        public static string[] GetListOfSprites(this SpriteAtlas atlas, string match)
        {
            var sprites = atlas.GetAllSprites().ToArray();

            if (string.IsNullOrEmpty(match)) { return sprites.Select(x => x.name).ToArray(); }

            var list = new List<string>();

            for (int i = 0, imax = sprites.Length; i < imax; ++i)
            {
                var sprite = sprites[i];

                if (sprite != null && !string.IsNullOrEmpty(sprite.name) && string.Equals(match, sprite.name, StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(sprite.name);

                    return list.ToArray();
                }
            }

            var keywords = match.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < keywords.Length; ++i) keywords[i] = keywords[i].ToLower();

            for (int i = 0, imax = sprites.Length; i < imax; ++i)
            {
                var sprite = sprites[i];

                if (sprite != null && !string.IsNullOrEmpty(sprite.name))
                {
                    string tl = sprite.name.ToLower();
                    int matches = 0;

                    for (int b = 0; b < keywords.Length; ++b)
                    {
                        if (tl.Contains(keywords[b])) ++matches;
                    }
                    if (matches == keywords.Length) list.Add(sprite.name);
                }
            }

            return list.ToArray();
        }
    }
}
