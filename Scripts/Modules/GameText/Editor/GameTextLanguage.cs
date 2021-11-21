
using System.Linq;
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
            public string Identifier { get; private set; }
            public int TextIndex { get; private set; }

            public Info(string language, string identifier, int textIndex)
            {
                Language = language;
                Identifier = identifier;
                TextIndex = textIndex;
            }
        }

        public static Info[] Infos { get; set; }

        public static Info GetCurrentInfo()
        {
            Info languageInfo = null;

            if (1 < Infos.Length)
            {
                var selection = Prefs.selection;

                languageInfo = Infos.ElementAtOrDefault(selection);
            }
            else
            {
                languageInfo = Infos.FirstOrDefault();
            }

            return languageInfo;
        }
    }
}
