
using UnityEngine;
using UnityEditor;
using System.IO;
using Extensions.Devkit;

namespace Modules.Devkit.AssetTuning
{
    public class AudioClipAssetTuner : AssetTuner
    {
        //----- params -----

        protected static readonly string[] DefaultPlatformNames = new string[]
        {
            "Standalone", "iOS", "Android",
        };

        //----- field -----

        //----- property -----

        protected virtual string[] PlatformNames
        {
            get { return DefaultPlatformNames; }
        }

        //----- method -----

		public override bool Validate(string path)
        {
            if (Path.GetExtension(path) != ".wav") { return false; }

            var asset = AssetDatabase.LoadMainAssetAtPath(path);

            var audioClip = asset as AudioClip;

            if (audioClip == null) { return false; }

            return true;
        }

        public override void OnAssetCreate(string path)
        {
            TuneAsset(path);
        }

        protected virtual void TuneAsset(string path)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);

            var audioClip = asset as AudioClip;

            if (audioClip == null) { return; }

            var audioImporter = AssetImporter.GetAtPath(path) as AudioImporter;

            SetStandardSettings(audioImporter, path);

            audioImporter.defaultSampleSettings = SetPlatformSettings(audioImporter.defaultSampleSettings, path);

            foreach (var platformName in PlatformNames)
            {
                var settings = audioImporter.GetOverrideSampleSettings(platformName);

                settings = SetPlatformSettings(settings, path);

                audioImporter.SetOverrideSampleSettings(platformName, settings);
            }

            UnityEditorUtility.SaveAsset(audioClip);
        }

        protected virtual void SetStandardSettings(AudioImporter audioImporter, string path)
        {
            audioImporter.loadInBackground = true;
        }

        protected virtual AudioImporterSampleSettings SetPlatformSettings(AudioImporterSampleSettings settings, string path)
        {
            settings.quality = 0.5f;
            settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;

            return settings;
        }
    }
}
