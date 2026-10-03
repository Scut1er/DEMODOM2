#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RealityDirector.Editor
{
    // Настройки импорта UI-паков (после ArtImporter): художнику достаточно положить PNG в нужную папку.
    //   Art/UI/Cards/Art/            арты карт по id (CARD_PROV_001.png…) — до 1024, качественное сжатие
    //   Art/UI/Cards/Frames/         рамки художницы — до 2048, качественное сжатие
    //   Art/UI/EpisodeMap/map_*      фон карты выпуска — до 2048, качественное сжатие
    //   остальное в Cards/, EventScreen/, HellTube/, EpisodeMap/, Illustrations/ — UI без сжатия
    //   *_9slice.png                 рамки и плашки: края 9-slice по размеру картинки (Image.Type.Sliced)
    public class UiArtImporter : AssetPostprocessor
    {
        const string Root = "Assets/Resources/Art/UI/";
        static readonly string[] Packs = { "Cards/", "EventScreen/", "HellTube/", "EpisodeMap/", "Illustrations/" };

        public override uint GetVersion() => 1;
        public override int GetPostprocessOrder() => 10;

        void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.StartsWith(Root) || !InPack(path))
                return;

            var importer = (TextureImporter)assetImporter;
            string file = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            int maxSize = 2048;
            var compression = TextureImporterCompression.Uncompressed;
            if (path.StartsWith(Root + "Cards/Art/"))
            {
                maxSize = 1024;
                compression = TextureImporterCompression.CompressedHQ;
            }
            else if (path.StartsWith(Root + "Cards/Frames/") || path.StartsWith(Root + "EpisodeMap/map_"))
            {
                compression = TextureImporterCompression.CompressedHQ;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePixelsPerUnit = 100f;
            settings.wrapMode = TextureWrapMode.Clamp;
            settings.filterMode = FilterMode.Bilinear;
            settings.mipmapEnabled = false;
            settings.alphaIsTransparency = true;
            if (file.EndsWith("_9slice"))
                settings.spriteBorder = Border(path);
            importer.SetTextureSettings(settings);

            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = compression;
        }

        static bool InPack(string path)
        {
            foreach (var pack in Packs)
            {
                if (path.StartsWith(Root + pack))
                    return true;
            }

            return false;
        }

        // Края 9-slice: треть короткой стороны (уголки и завитки паков в неё помещаются), не больше 56 px;
        // тонкие полоски (прогресс, строки) — половина высоты, чтобы скруглённые концы не тянулись.
        static Vector4 Border(string path)
        {
            if (!PngSize(path, out int w, out int h))
                return Vector4.zero;
            int shortSide = Mathf.Min(w, h);
            int b = shortSide <= 40 ? shortSide / 2 - 1 : Mathf.Clamp(Mathf.RoundToInt(shortSide * 0.3f), 6, 56);
            return new Vector4(b, b, b, b);
        }

        // Размер PNG из заголовка (IHDR) — в OnPreprocessTexture пикселей ещё нет.
        static bool PngSize(string path, out int w, out int h)
        {
            w = h = 0;
            try
            {
                using (var stream = System.IO.File.OpenRead(path))
                {
                    var head = new byte[24];
                    if (stream.Read(head, 0, 24) < 24 || head[1] != 'P' || head[2] != 'N' || head[3] != 'G')
                        return false;
                    w = head[16] << 24 | head[17] << 16 | head[18] << 8 | head[19];
                    h = head[20] << 24 | head[21] << 16 | head[22] << 8 | head[23];
                    return w > 0 && h > 0;
                }
            }
            catch (System.IO.IOException)
            {
                return false;
            }
        }
    }
}
#endif
