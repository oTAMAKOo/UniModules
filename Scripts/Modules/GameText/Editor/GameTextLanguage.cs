
using Modules.Devkit.Prefs;

namespace Modules.GameText.Editor
{
    public static class GameTextLanguage
    {
        public static class Prefs
        {
            public static int selection
            {
                get { return ProjectPrefs.GetInt("GameTextLanguagePrefs-selection", -1); }
                set { ProjectPrefs.SetInt("GameTextLanguagePrefs-selection", value); }
            }
        }

        public sealed class Info
        {
            public string Language { get; private set; }
            public string AssetName { get; private set; }
            public int TextIndex { get; private set; }

            public Info(string language, string assetName, int textIndex)
            {
                Language = language;
                AssetName = assetName;
                TextIndex = textIndex;
            }
        }

        public static Info[] Infos { get; set; }
    }
}
