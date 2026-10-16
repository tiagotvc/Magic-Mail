// Menu Magic Mail > Áudio > Gerar tema de aventura: composes the theme (MagicMusicComposer) and writes it into the project —
// tema-aventura.wav (ready to play) and tema-aventura.mid (the same notes, for a DAW) in Assets/Magic Mail/Audio/Music. The
// WAV is imported as compressed music that streams, so it does not sit decoded in memory.
using System.IO;
using MagicMail.Audio;
using UnityEditor;
using UnityEngine;

namespace MagicMail.EditorTools
{
    public static class MagicMusicMenu
    {
        const string Folder = "Assets/Magic Mail/Audio/Music/";

        [MenuItem("Magic Mail/Áudio/Gerar tema de aventura")]
        static void Generate()
        {
            var disk = Path.Combine(Directory.GetParent(Application.dataPath).FullName, Folder);
            Directory.CreateDirectory(disk);
            var wavPath = Folder + "tema-aventura.wav";
            if (File.Exists(Path.Combine(disk, "tema-aventura.wav")) &&
                !EditorUtility.DisplayDialog("Tema de aventura", "Já existe um tema gerado. Gerar de novo substitui os arquivos (o .wav e o .mid). Continuar?", "Gerar", "Cancelar"))
                return;

            EditorUtility.DisplayProgressBar("Tema de aventura", "Compondo e sintetizando…", 0.3f);
            try
            {
                var (pcm, seconds) = MagicMusicComposer.RenderWav();
                MagicMusicComposer.WriteWav(Path.Combine(disk, "tema-aventura.wav"), pcm);
                MagicMusicComposer.WriteMidi(Path.Combine(disk, "tema-aventura.mid"));
                AssetDatabase.ImportAsset(wavPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(Folder + "tema-aventura.mid", ImportAssetOptions.ForceUpdate);

                if (AssetImporter.GetAtPath(wavPath) is AudioImporter importer)
                {
                    var settings = importer.defaultSampleSettings;
                    settings.loadType = AudioClipLoadType.Streaming;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality = 0.7f;
                    importer.defaultSampleSettings = settings;
                    importer.SaveAndReimport();
                }
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(wavPath);
                Selection.activeObject = clip;
                EditorGUIUtility.PingObject(clip);
                Debug.Log($"[Música] tema de aventura gerado: {seconds:0.0}s em {wavPath}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
