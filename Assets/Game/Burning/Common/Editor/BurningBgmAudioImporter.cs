using UnityEditor;
using UnityEngine;

namespace FilmInspiredGames.Burning.Editor
{
    internal sealed class BurningBgmAudioImporter : AssetPostprocessor
    {
        private const string Folder = "Assets/Game/Burning/Resources/BurningBGM/";

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Folder))
            {
                return;
            }

            AudioImporter importer = (AudioImporter)assetImporter;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.65f;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = true;
        }
    }
}
