using System.Collections.Generic;
using RealityDirector.Core;
using UnityEngine;

namespace RealityDirector.Util
{
    // Лица участника. Файла нет — берётся ближайшее по смыслу (см. FaceFallback), арт можно рисовать не целиком.
    public enum Face
    {
        Happy,
        Love,
        Mad,
        Sad,
        Neutral,
        Scared,
        Tired
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

        public const string DefaultBody = "body";

        // Префикс арта: из ассета участника (artPrefix), иначе встроенные участники квартиры.
        static string Prefix(string npcId)
        {
            var def = Actor(npcId);
            if (def != null && !string.IsNullOrEmpty(def.artPrefix))
                return def.artPrefix;
            switch (npcId)
            {
                case "npc_zloi": return "zloi";
                case "npc_dobryak": return "dobryak";
                case "npc_kira": return "kira";
                case "npc_max": return "max";
                default: return null;
            }
        }

        static Meta.ActorDefinition Actor(string npcId)
        {
            return Meta.ContentLibrary.Find<Meta.ActorDefinition>(npcId);
        }

        // Чего нет у художника — ближайшее по смыслу лицо, последним — любое, что есть.
        static readonly Face[][] FaceFallback =
        {
            new[] { Face.Happy, Face.Love, Face.Neutral },             // Happy
            new[] { Face.Love, Face.Happy, Face.Neutral },             // Love
            new[] { Face.Mad, Face.Neutral, Face.Sad },                // Mad
            new[] { Face.Sad, Face.Tired, Face.Neutral },              // Sad
            new[] { Face.Neutral, Face.Happy, Face.Love },             // Neutral
            new[] { Face.Scared, Face.Sad, Face.Neutral },             // Scared
            new[] { Face.Tired, Face.Sad, Face.Neutral }               // Tired
        };

        static readonly Face[] AnyFace = { Face.Neutral, Face.Happy, Face.Love, Face.Sad, Face.Mad, Face.Scared, Face.Tired };

        // По префиксу файлов из ассета персонажа (zloi → Characters/zloi_happy).
        public static Sprite HeadByPrefix(string prefix, Face face)
        {
            if (string.IsNullOrEmpty(prefix))
                return null;
            var chain = FaceFallback[(int)face];
            for (int i = 0; i < chain.Length; i++)
            {
                var sprite = ExactHead(prefix, chain[i]);
                if (sprite != null)
                    return sprite;
            }

            for (int i = 0; i < AnyFace.Length; i++)
            {
                var sprite = ExactHead(prefix, AnyFace[i]);
                if (sprite != null)
                    return sprite;
            }

            return null;
        }

        // Только этот файл, без подмены (для редактора участника).
        public static Sprite ExactHead(string prefix, Face face)
        {
            return string.IsNullOrEmpty(prefix) ? null : Load("Characters/" + prefix + "_" + face.ToString().ToLowerInvariant());
        }

        public static Sprite Head(string npcId, Face face)
        {
            return HeadByPrefix(Prefix(npcId), face);
        }

        // Наряд участника (bodyPrefix в ассете): body → body_neutral, body2 → body2_mad…
        public static string BodyPrefix(string npcId)
        {
            var def = Actor(npcId);
            if (def != null && !string.IsNullOrEmpty(def.bodyPrefix))
                return def.bodyPrefix;
            switch (npcId)
            {
                case "npc_kira": return "body3";
                case "npc_max": return "body2";
                default: return DefaultBody;
            }
        }

        // Позы нет в этом наряде — нейтральная того же наряда, наряда нет — общее тело.
        public static Sprite Body(string bodyPrefix, BodyPose pose)
        {
            if (string.IsNullOrEmpty(bodyPrefix))
                bodyPrefix = DefaultBody;
            return ExactBody(bodyPrefix, pose) ?? ExactBody(bodyPrefix, BodyPose.Neutral)
                ?? (bodyPrefix != DefaultBody ? Body(DefaultBody, pose) : null);
        }

        public static Sprite ExactBody(string bodyPrefix, BodyPose pose)
        {
            return Load("Characters/" + bodyPrefix + "_" + pose.ToString().ToLowerInvariant());
        }

        public static Sprite Body(BodyPose pose)
        {
            return Body(DefaultBody, pose);
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
        public static Sprite Palm => Load("Location/palm");
        public static Sprite Fridge => Load("Location/fridge");
        public static Sprite Stove => Load("Location/stove");
        public static Sprite Jacuzzi => Load("Location/jacuzzi");

        // ---------- босс ----------

        // Босс в полный рост, жест «вот, смотри» — подаёт договор.
        public static Sprite BossPresent => Load("Boss/boss_present");

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
