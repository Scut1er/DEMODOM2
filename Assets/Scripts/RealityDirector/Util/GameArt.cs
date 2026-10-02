using System.Collections.Generic;
using RealityDirector.Core;
using UnityEngine;

namespace RealityDirector.Util
{
    public enum Face
    {
        Happy,
        Love,
        Mad,
        Sad
    }

    public enum BodyPose
    {
        Neutral,
        Happy,
        Mad,
        Sad,
        Scared
    }

    // Арт художника из Assets/Resources/Art. Нет файла — вернёт null, вызывающий код берёт рисованную заглушку.
    public static class GameArt
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Load(string path)
        {
            if (Cache.TryGetValue(path, out var sprite))
                return sprite;
            sprite = Resources.Load<Sprite>("Art/" + path);
            Cache[path] = sprite;
            return sprite;
        }

        // ---------- карты ----------

        public static Sprite CardFrame(ShowMood mood)
        {
            switch (mood)
            {
                case ShowMood.Drama: return Load("Cards/card_drama");
                case ShowMood.Family: return Load("Cards/card_family");
                default: return Load("Cards/card_trash");
            }
        }

        // ---------- участники ----------

        static string Prefix(string npcId)
        {
            switch (npcId)
            {
                case "npc_zloi": return "zloi";
                case "npc_dobryak": return "dobryak";
                default: return null;
            }
        }

        // По префиксу файлов из ассета персонажа (zloi → Characters/zloi_happy).
        public static Sprite HeadByPrefix(string prefix, Face face)
        {
            return string.IsNullOrEmpty(prefix) ? null : Load("Characters/" + prefix + "_" + face.ToString().ToLowerInvariant());
        }

        public static Sprite Head(string npcId, Face face)
        {
            string prefix = Prefix(npcId);
            return prefix == null ? null : Load("Characters/" + prefix + "_" + face.ToString().ToLowerInvariant());
        }

        public static Sprite Body(BodyPose pose)
        {
            return Load("Characters/body_" + pose.ToString().ToLowerInvariant());
        }

        // ---------- квартира ----------

        public static Sprite FloorTiles => Load("Location/floor_tiles");
        public static Sprite FloorParquetLight => Load("Location/floor_parquet_light");
        public static Sprite FloorParquetDark => Load("Location/floor_parquet_dark");
        public static Sprite WallStripes => Load("Location/wall_stripes");
        public static Sprite WallBathTiles => Load("Location/wall_bath_tiles");
        public static Sprite DoorWood => Load("Location/door_wood");
        public static Sprite DoorWhite => Load("Location/door_white");
        public static Sprite Bath => Load("Location/bath");
        public static Sprite Sofa => Load("Location/sofa");
        public static Sprite Plant => Load("Location/plant");

        // Пол/стена плиткой: текстура повторяется, а не растягивается.
        public static SpriteRenderer Tiled(Transform parent, string name, Sprite sprite, Vector3 pos, Vector2 size, int order)
        {
            var renderer = SpriteUtil.Show(parent, name, pos, sprite, order);
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size;
            return renderer;
        }

        // Вписать в рамку без искажения пропорций.
        public static void FitInside(SpriteRenderer renderer, Vector2 box)
        {
            if (renderer == null || renderer.sprite == null)
                return;
            Vector2 size = renderer.sprite.bounds.size;
            float k = Mathf.Min(box.x / Mathf.Max(size.x, 0.0001f), box.y / Mathf.Max(size.y, 0.0001f));
            renderer.transform.localScale = new Vector3(k, k, 1f);
        }
    }
}
