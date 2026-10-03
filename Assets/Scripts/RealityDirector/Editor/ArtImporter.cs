#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RealityDirector.Editor
{
    // Настройки импорта для арта в Assets/Resources/Art — художнику достаточно положить файл в нужную папку.
    //   Cards/       рамки карт                 — спрайт, пивот по центру
    //   Characters/  головы <prefix>_<face> и тела <body>_<pose> — спрайт, пивот снизу по центру
    //   Location/    floor_* и wall_*           — тайлятся (Repeat, FullRect); остальное — обычные объекты
    public class ArtImporter : AssetPostprocessor
    {
        const string Root = "Assets/Resources/Art/";

        void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.StartsWith(Root))
                return;

            var importer = (TextureImporter)assetImporter;
            string file = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

            float ppu = 100f;
            int maxSize = 1024;
            var alignment = SpriteAlignment.Center;
            var mesh = SpriteMeshType.Tight;
            var wrap = TextureWrapMode.Clamp;

            if (path.StartsWith(Root + "Cards/"))
            {
                maxSize = 2048;
            }
            else if (path.StartsWith(Root + "Characters/"))
            {
                alignment = SpriteAlignment.BottomCenter;
                // тело ~0.9 юнита в ширину, голова ~0.8 (тела: body_*, body2_*, body3_* — разные наряды)
                ppu = file.StartsWith("body") ? 760f : 875f;
            }
            else if (file.StartsWith("floor_") || file.StartsWith("wall_"))
            {
                mesh = SpriteMeshType.FullRect;
                wrap = TextureWrapMode.Repeat;
                ppu = file == "floor_tiles" ? 800f : 700f;
            }

            // Сначала общий блок настроек, потом отдельные поля — иначе блок перезапишет их старыми значениями.
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = mesh;
            settings.spriteAlignment = (int)alignment;
            settings.spritePixelsPerUnit = ppu;
            settings.wrapMode = wrap;
            settings.mipmapEnabled = false;
            settings.alphaIsTransparency = true;
            importer.SetTextureSettings(settings);

            importer.spritePixelsPerUnit = ppu;
            importer.wrapMode = wrap;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
        }
    }
}
#endif
