using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealityDirector.Cards
{
    // Реквизит карт и значки сцены — рисуются кодом, с контуром и объёмом, пока у художника нет своих.
    // Опора спрайтов реквизита — снизу по центру: ставится ровно «на пол» точки.
    public static class PropArt
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        static readonly Color Ink = new Color(0.12f, 0.06f, 0.08f, 1f);

        public static Sprite Get(string key)
        {
            if (Cache.TryGetValue(key, out var s) && s != null)
                return s;
            s = Paint(key);
            Cache[key] = s;
            return s;
        }

        static Sprite Paint(string key)
        {
            switch (key)
            {
                case "AlcoholCrate": return Crate();
                case "OpenMic": return Mic();
                case "GiftBox": return Gift();
                case "KaraokeMachine": return Karaoke();
                case "OilSpill": return Oil();
                case "RomanticSpeaker": return Speaker();
                case "AnxietyLight": return Lamp();
                case "HiddenCameraProp": return HiddenCam();
                case "Spotlight": return Spot();
                case "UnattendedPhone": return Phone(false);
                case "RedButton": return Button();
                case "Poster_Leviathan": return Poster();
                case "Tripod": return Tripod();
                case "ColaCan": return Can();
                case "Bottle": return BottleIcon();
                case "Stink": return Cloud();
                case "Heart": return Heart();
                case "Bolt": return Bolt();
                case "Note": return Note();
                case "Padlock": return Padlock();
                case "WifiOff": return WifiOff();
                case "PhoneIcon": return Phone(true);
                case "Eye": return Eye();
                case "Drop": return Drop();
                case "Star": return Star();
                case "Question": return Glyph('?');
                case "Bang": return Glyph('!');
                case "Megaphone": return Megaphone();
                case "Cone": return Cone();
                case "Chain": return Chain();
                case "Handshake": return Handshake();
                default: return Glyph('?');
            }
        }

        // ---------- Реквизит ----------

        static Sprite Crate()
        {
            var p = new Pix(150, 140);
            // бутылки торчат из ящика
            Color[] glass = { new Color(0.22f, 0.55f, 0.28f), new Color(0.62f, 0.12f, 0.12f), new Color(0.35f, 0.2f, 0.5f), new Color(0.75f, 0.42f, 0.12f) };
            for (int i = 0; i < 4; i++)
            {
                int x = 30 + i * 26;
                int top = 120 - (i % 2) * 10;
                p.RoundRect(x, 60, 20, top - 72, 6, glass[i], Dark(glass[i], 0.65f));
                p.Rect(x + 6, top - 14, 8, 16, Dark(glass[i], 0.75f));
                p.Rect(x + 5, top, 10, 5, new Color(0.85f, 0.7f, 0.3f));
                p.Rect(x + 4, 66, 3, top - 82, new Color(1f, 1f, 1f, 0.35f));
                p.Rect(x + 3, 72, 14, 12, new Color(0.95f, 0.9f, 0.75f));
                p.Rect(x + 5, 76, 10, 4, new Color(0.75f, 0.12f, 0.1f));
            }

            var wood = new Color(0.62f, 0.4f, 0.22f);
            p.RoundRect(10, 6, 130, 66, 4, wood, Dark(wood, 0.72f));
            for (int i = 0; i < 3; i++)
                p.Rect(10, 6 + i * 22, 130, 2, Dark(wood, 0.55f));
            p.Rect(12, 8, 10, 62, Dark(wood, 0.85f));
            p.Rect(128, 8, 10, 62, Dark(wood, 0.85f));
            // пламя-клеймо на ящике
            p.Ellipse(75, 38, 14, 16, new Color(0.95f, 0.35f, 0.08f));
            p.Ellipse(75, 34, 7, 9, new Color(1f, 0.85f, 0.3f));
            p.Outline(Ink, 3);
            return p.Bake();
        }

        static Sprite Mic()
        {
            var p = new Pix(90, 170);
            var metal = new Color(0.22f, 0.22f, 0.26f);
            p.Ellipse(45, 10, 30, 8, Dark(metal, 0.8f));
            p.Rect(42, 12, 6, 120, metal);
            p.Rect(43, 14, 2, 116, new Color(1f, 1f, 1f, 0.3f));
            p.RoundRect(32, 126, 26, 34, 12, new Color(0.55f, 0.55f, 0.6f), new Color(0.25f, 0.25f, 0.3f));
            for (int y = 132; y < 156; y += 5)
                p.Rect(34, y, 22, 1, new Color(0.15f, 0.15f, 0.18f));
            p.Ellipse(45, 118, 5, 5, new Color(1f, 0.15f, 0.1f));
            p.Outline(Ink, 3);
            return p.Bake();
        }

        static Sprite Gift()
        {
            var p = new Pix(120, 120);
            var box = new Color(0.85f, 0.15f, 0.25f);
            var gold = new Color(1f, 0.82f, 0.3f);
            p.RoundRect(14, 6, 92, 70, 4, box, Dark(box, 0.65f));
            p.RoundRect(8, 70, 104, 22, 4, Dark(box, 1.1f), Dark(box, 0.8f));
            p.Rect(53, 6, 14, 86, gold);
            p.Rect(8, 76, 104, 8, gold);
            p.Ellipse(44, 100, 18, 11, gold);
            p.Ellipse(76, 100, 18, 11, gold);
            p.Ellipse(44, 100, 8, 5, Dark(gold, 0.7f));
            p.Ellipse(76, 100, 8, 5, Dark(gold, 0.7f));
            p.Ellipse(60, 96, 8, 8, Dark(gold, 0.85f));
            p.Rect(20, 12, 6, 54, new Color(1f, 1f, 1f, 0.25f));
            p.Outline(Ink, 3);
            return p.Bake();
        }

        static Sprite Karaoke()
        {
            var p = new Pix(130, 170);
            var body = new Color(0.32f, 0.12f, 0.42f);
            p.RoundRect(14, 6, 102, 104, 8, body, Dark(body, 0.6f));
            p.Ellipse(65, 48, 32, 32, new Color(0.12f, 0.06f, 0.16f));
            p.Ellipse(65, 48, 22, 22, new Color(0.3f, 0.2f, 0.36f));
            p.Ellipse(65, 48, 8, 8, new Color(0.12f, 0.06f, 0.16f));
            p.RoundRect(24, 86, 82, 18, 3, new Color(0.15f, 0.9f, 0.85f), new Color(0.1f, 0.55f, 0.75f));
            for (int i = 0; i < 4; i++)
                p.Rect(32 + i * 18, 92, 10, 6, new Color(1f, 0.4f, 0.8f));
            // микрофон на шнуре
            p.Rect(98, 110, 4, 34, new Color(0.15f, 0.15f, 0.18f));
            p.RoundRect(91, 140, 18, 24, 8, new Color(0.7f, 0.7f, 0.76f), new Color(0.35f, 0.35f, 0.4f));
            p.Rect(20, 12, 6, 90, new Color(1f, 1f, 1f, 0.18f));
            p.Outline(Ink, 3);
            return p.Bake();
        }

        static Sprite Oil()
        {
            var p = new Pix(180, 70);
            var oil = new Color(0.12f, 0.1f, 0.08f, 0.92f);
            p.Ellipse(90, 30, 82, 24, oil);
            p.Ellipse(40, 38, 30, 14, oil);
            p.Ellipse(140, 24, 32, 12, oil);
            // радужный отблеск
            p.Ellipse(80, 36, 34, 6, new Color(0.4f, 0.3f, 0.9f, 0.5f));
            p.Ellipse(96, 32, 26, 4, new Color(0.3f, 0.9f, 0.6f, 0.45f));
            p.Ellipse(112, 28, 16, 3, new Color(1f, 0.85f, 0.3f, 0.5f));
            p.Ellipse(58, 22, 10, 3, new Color(1f, 1f, 1f, 0.45f));
            return p.Bake(new Vector2(0.5f, 0.5f));
        }

        static Sprite Speaker()
        {
            var p = new Pix(90, 120);
            var wood = new Color(0.55f, 0.18f, 0.32f);
            p.RoundRect(8, 6, 74, 108, 8, wood, Dark(wood, 0.6f));
            p.Ellipse(45, 40, 26, 26, new Color(0.15f, 0.06f, 0.1f));
            p.Ellipse(45, 40, 14, 14, new Color(0.4f, 0.15f, 0.25f));
            HeartShape(p, 45, 90, 14, new Color(1f, 0.45f, 0.65f));
            p.Outline(Ink, 3);
            return p.Bake();
        }

        static Sprite Lamp()
        {
            var p = new Pix(100, 170);
            var metal = new Color(0.24f, 0.24f, 0.28f);
            p.Line(50, 8, 20, 4, metal, 3);
            p.Line(50, 8, 80, 4, metal, 3);
            p.Rect(47, 8, 6, 110, metal);
            p.RoundRect(22, 112, 56, 34, 6, new Color(0.35f, 0.35f, 0.4f), new Color(0.18f, 0.18f, 0.22f));
            p.Ellipse(50, 146, 22, 16, new Color(1f, 0.22f, 0.15f));
            p.Ellipse(44, 150, 7, 5, new Color(1f, 0.75f, 0.65f));
            p.Outline(Ink, 3);
            return p.Bake();
        }

        static Sprite HiddenCam()
        {
            // Плюшевый мишка, у которого вместо глаза — красный объектив.
            var p = new Pix(100, 110);
            var fur = new Color(0.62f, 0.42f, 0.26f);
            p.Ellipse(50, 34, 32, 30, fur);
            p.Ellipse(50, 76, 26, 24, fur);
            p.Ellipse(30, 96, 10, 10, fur);
            p.Ellipse(70, 96, 10, 10, fur);
            p.Ellipse(30, 96, 5, 5, Dark(fur, 0.7f));
            p.Ellipse(70, 96, 5, 5, Dark(fur, 0.7f));
            p.Ellipse(50, 68, 10, 8, new Color(0.85f, 0.7f, 0.55f));
            p.Ellipse(50, 70, 3, 2, Ink);
            p.Ellipse(40, 82, 4, 4, Ink);
            p.Ellipse(60, 82, 6, 6, new Color(0.1f, 0.1f, 0.12f));
            p.Ellipse(60, 82, 3, 3, new Color(1f, 0.15f, 0.1f));
            p.Ellipse(50, 34, 16, 14, new Color(0.85f, 0.7f, 0.55f));
            p.Outline(Ink, 3);
            return p.Bake();
        }

        static Sprite Spot()
        {
            var p = new Pix(110, 170);
            var metal = new Color(0.2f, 0.2f, 0.24f);
            p.Line(55, 70, 22, 4, metal, 2);
            p.Line(55, 70, 88, 4, metal, 2);
            p.Line(55, 70, 55, 4, metal, 2);
            p.Rect(52, 70, 6, 40, metal);
            p.RoundRect(20, 106, 70, 50, 8, new Color(0.3f, 0.3f, 0.34f), new Color(0.12f, 0.12f, 0.15f));
            p.Ellipse(82, 131, 14, 22, new Color(1f, 0.95f, 0.6f));
            p.Ellipse(84, 131, 7, 12, Color.white);
            p.Rect(26, 150, 50, 4, new Color(1f, 1f, 1f, 0.2f));
            p.Outline(Ink, 3);
            return p.Bake();
        }

        static Sprite Phone(bool icon)
        {
            var p = new Pix(70, 110);
            p.RoundRect(6, 6, 58, 98, 10, new Color(0.15f, 0.15f, 0.18f), new Color(0.08f, 0.08f, 0.1f));
            p.RoundRect(12, 16, 46, 78, 4, new Color(0.35f, 0.75f, 1f), new Color(0.2f, 0.4f, 0.85f));
            p.RoundRect(16, 62, 34, 12, 4, Color.white, new Color(0.85f, 0.9f, 1f));
            p.RoundRect(22, 42, 34, 12, 4, new Color(0.4f, 1f, 0.55f), new Color(0.25f, 0.8f, 0.4f));
            p.Ellipse(56, 92, 9, 9, new Color(1f, 0.2f, 0.2f));
            p.Outline(Ink, icon ? 2 : 3);
            return p.Bake(icon ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0f));
        }

        static Sprite Button()
        {
            var p = new Pix(110, 120);
            var metal = new Color(0.3f, 0.3f, 0.34f);
            p.RoundRect(14, 6, 82, 60, 6, metal, Dark(metal, 0.6f));
            // жёлто-чёрная предупреждающая лента
            for (int i = 0; i < 9; i++)
                p.Line(16 + i * 10, 28, 24 + i * 10, 46, i % 2 == 0 ? new Color(1f, 0.85f, 0.1f) : Ink, 3);
            p.Ellipse(55, 76, 34, 14, new Color(0.25f, 0.25f, 0.28f));
            p.Ellipse(55, 84, 28, 22, new Color(0.9f, 0.08f, 0.08f));
            p.Ellipse(46, 92, 9, 6, new Color(1f, 0.6f, 0.55f));
            p.Outline(Ink, 3);
            return p.Bake();
        }

        static Sprite Poster()
        {
            var p = new Pix(110, 150);
            var navy = new Color(0.08f, 0.12f, 0.28f);
            p.RoundRect(6, 6, 98, 138, 3, navy, Dark(navy, 0.6f));
            p.Rect(6, 6, 98, 4, new Color(1f, 0.8f, 0.3f));
            p.Rect(6, 140, 98, 4, new Color(1f, 0.8f, 0.3f));
            // корабль-левиафан
            p.Ellipse(55, 58, 40, 12, new Color(0.65f, 0.7f, 0.8f));
            p.Rect(36, 64, 30, 18, new Color(0.55f, 0.6f, 0.7f));
            p.Rect(58, 70, 6, 26, new Color(0.45f, 0.5f, 0.6f));
            p.Ellipse(55, 40, 46, 5, new Color(0.9f, 0.3f, 0.1f, 0.8f));
            for (int i = 0; i < 5; i++)
                p.Rect(22 + i * 15, 112, 10, 14, new Color(1f, 0.8f, 0.3f));
            p.Outline(Ink, 2);
            return p.Bake(new Vector2(0.5f, 0.5f));
        }

        static Sprite Tripod()
        {
            var p = new Pix(110, 170);
            var metal = new Color(0.2f, 0.2f, 0.24f);
            p.Line(55, 80, 18, 4, metal, 3);
            p.Line(55, 80, 92, 4, metal, 3);
            p.Line(55, 80, 55, 4, metal, 3);
            p.RoundRect(22, 86, 66, 46, 6, new Color(0.18f, 0.18f, 0.22f), new Color(0.08f, 0.08f, 0.1f));
            p.Ellipse(55, 109, 16, 16, new Color(0.35f, 0.35f, 0.42f));
            p.Ellipse(55, 109, 9, 9, new Color(0.1f, 0.3f, 0.6f));
            p.Ellipse(52, 112, 3, 3, Color.white);
            p.Ellipse(80, 126, 5, 5, new Color(1f, 0.15f, 0.1f));
            p.RoundRect(30, 132, 30, 14, 3, new Color(0.25f, 0.25f, 0.3f), new Color(0.15f, 0.15f, 0.18f));
            p.Outline(Ink, 3);
            return p.Bake();
        }

        static Sprite Can()
        {
            var p = new Pix(50, 80);
            var red = new Color(0.86f, 0.1f, 0.12f);
            p.RoundRect(8, 6, 34, 68, 6, red, Dark(red, 0.6f));
            p.Rect(8, 66, 34, 6, new Color(0.8f, 0.8f, 0.85f));
            p.Rect(8, 6, 34, 4, new Color(0.7f, 0.7f, 0.75f));
            p.Line(10, 30, 40, 44, Color.white, 2);
            p.Rect(12, 12, 5, 50, new Color(1f, 1f, 1f, 0.35f));
            p.Outline(Ink, 2);
            return p.Bake();
        }

        // ---------- Значки ----------

        static Sprite BottleIcon()
        {
            var p = new Pix(40, 80);
            var glass = new Color(0.25f, 0.6f, 0.3f);
            p.RoundRect(8, 4, 24, 48, 6, glass, Dark(glass, 0.6f));
            p.Rect(15, 50, 10, 20, Dark(glass, 0.75f));
            p.Rect(14, 68, 12, 6, new Color(0.85f, 0.7f, 0.3f));
            p.Rect(10, 18, 20, 12, new Color(0.95f, 0.9f, 0.75f));
            p.Outline(Ink, 2);
            return p.Bake(new Vector2(0.5f, 0.5f));
        }

        static Sprite Cloud()
        {
            var p = new Pix(96, 64);
            var c = new Color(1f, 1f, 1f, 0.9f);
            p.Ellipse(30, 30, 24, 20, c);
            p.Ellipse(56, 36, 28, 24, c);
            p.Ellipse(74, 26, 18, 16, c);
            p.Ellipse(48, 22, 30, 14, c);
            return p.Bake(new Vector2(0.5f, 0.5f));
        }

        static Sprite Heart()
        {
            var p = new Pix(64, 60);
            HeartShape(p, 32, 30, 26, new Color(1f, 0.3f, 0.5f));
            p.Ellipse(22, 38, 6, 4, new Color(1f, 0.8f, 0.85f));
            p.Outline(Ink, 2);
            return p.Bake(new Vector2(0.5f, 0.5f));
        }

        static void HeartShape(Pix p, int cx, int cy, int r, Color c)
        {
            p.Ellipse(cx - r / 2, cy + r / 4, r / 2 + 1, r / 2 + 1, c);
            p.Ellipse(cx + r / 2, cy + r / 4, r / 2 + 1, r / 2 + 1, c);
            p.Poly(new[] { new Vector2(cx - r, cy + r / 6f), new Vector2(cx + r, cy + r / 6f), new Vector2(cx, cy - r) }, c);
        }

        static Sprite Bolt()
        {
            var p = new Pix(50, 70);
            p.Poly(new[] { new Vector2(30, 68), new Vector2(8, 30), new Vector2(24, 30), new Vector2(16, 2), new Vector2(42, 42), new Vector2(26, 42) }, new Color(1f, 0.85f, 0.2f));
            p.Outline(new Color(0.5f, 0.05f, 0.02f), 2);
            return p.Bake(new Vector2(0.5f, 0.5f));
        }

        static Sprite Note()
        {
            var p = new Pix(50, 64);
            var c = Color.white;
            p.Ellipse(16, 12, 12, 9, c);
            p.Rect(25, 12, 5, 46, c);
            p.Poly(new[] { new Vector2(25, 58), new Vector2(44, 48), new Vector2(44, 38), new Vector2(29, 46) }, c);
            p.Outline(Ink, 2);
            return p.Bake(new Vector2(0.5f, 0.5f));
        }

        static Sprite Padlock()
        {
            var p = new Pix(60, 76);
            var gold = new Color(1f, 0.78f, 0.25f);
            p.Ring(30, 46, 18, 6, new Color(0.7f, 0.7f, 0.76f));
            p.Rect(12, 46, 6, 6, new Color(1f, 1f, 1f, 0f));
            p.RoundRect(6, 4, 48, 40, 6, gold, Dark(gold, 0.65f));
            p.Ellipse(30, 28, 5, 5, Ink);
            p.Rect(28, 12, 4, 14, Ink);
            p.Outline(Ink, 2);
            return p.Bake(new Vector2(0.5f, 0.5f));
        }

        static Sprite Chain()
        {
            var p = new Pix(120, 30);
            var steel = new Color(0.72f, 0.72f, 0.78f);
            for (int i = 0; i < 6; i++)
                p.Ring(10 + i * 20, 15, 10, 3, steel);
            p.Outline(Ink, 2);
            return p.Bake(new Vector2(0.5f, 0.5f));
        }

        static Sprite WifiOff()
        {
            var p = new Pix(70, 64);
            var c = Color.white;
            p.Arc(35, 10, 50, 6, c);
            p.Arc(35, 10, 34, 6, c);
            p.Arc(35, 10, 18, 6, c);
            p.Ellipse(35, 12, 5, 5, c);
            p.Line(10, 56, 60, 6, new Color(1f, 0.2f, 0.15f), 4);
            p.Outline(Ink, 2);
            return p.Bake(new Vector2(0.5f, 0.5f));
        }

        static Sprite Eye()
        {
            var p = new Pix(70, 44);
            p.Ellipse(35, 22, 32, 18, Color.white);
            p.Ellipse(35, 22, 12, 12, new Color(0.55f, 0.15f, 0.6f));
            p.Ellipse(35, 22, 6, 6, Ink);
            p.Ellipse(31, 26, 3, 3, Color.white);
            p.Outline(Ink, 2);
            return p.Bake(new Vector2(0.5f, 0.5f));
        }

        static Sprite Drop()
        {
            var p = new Pix(40, 56);
            var c = new Color(0.45f, 0.75f, 1f);
            p.Ellipse(20, 18, 16, 16, c);
            p.Poly(new[] { new Vector2(5, 22), new Vector2(35, 22), new Vector2(20, 54) }, c);
            p.Ellipse(14, 22, 4, 6, new Color(1f, 1f, 1f, 0.7f));
            p.Outline(new Color(0.08f, 0.15f, 0.3f), 2);
            return p.Bake(new Vector2(0.5f, 0.5f));
        }

        static Sprite Star()
        {
            var p = new Pix(56, 56);
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? 26f : 11f;
                pts[i] = new Vector2(28 + Mathf.Cos(a) * r, 28 + Mathf.Sin(a) * r);
            }

            p.Poly(pts, new Color(1f, 0.9f, 0.3f));
            p.Outline(Ink, 2);
            return p.Bake(new Vector2(0.5f, 0.5f));
        }

        static Sprite Megaphone()
        {
            var p = new Pix(80, 56);
            var red = new Color(0.9f, 0.2f, 0.15f);
            p.Poly(new[] { new Vector2(22, 20), new Vector2(22, 36), new Vector2(72, 52), new Vector2(72, 4) }, red);
            p.RoundRect(6, 18, 18, 20, 3, new Color(0.25f, 0.25f, 0.3f), new Color(0.12f, 0.12f, 0.15f));
            p.Rect(68, 4, 6, 48, new Color(1f, 0.85f, 0.3f));
            p.Outline(Ink, 2);
            return p.Bake(new Vector2(0.5f, 0.5f));
        }

        static Sprite Handshake()
        {
            var p = new Pix(80, 50);
            var skin = new Color(0.95f, 0.75f, 0.6f);
            p.RoundRect(4, 14, 40, 22, 8, new Color(0.3f, 0.5f, 0.9f), new Color(0.2f, 0.35f, 0.7f));
            p.RoundRect(36, 14, 40, 22, 8, new Color(0.9f, 0.4f, 0.3f), new Color(0.7f, 0.25f, 0.2f));
            p.Ellipse(40, 25, 14, 11, skin);
            p.Outline(Ink, 2);
            return p.Bake(new Vector2(0.5f, 0.5f));
        }

        // Мягкий конус света софита — опора у вершины.
        static Sprite Cone()
        {
            const int w = 128, h = 128;
            var p = new Pix(w, h);
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                float half = Mathf.Lerp(8f, w / 2f, t);
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Abs(x - w / 2f) / half;
                    if (d > 1f)
                        continue;
                    float a = (1f - d * d) * (1f - t * 0.55f) * 0.6f;
                    p.Set(x, h - 1 - y, new Color(1f, 0.96f, 0.75f, a));
                }
            }

            return p.Bake(new Vector2(0.5f, 1f));
        }

        static Sprite Glyph(char c)
        {
            var p = new Pix(40, 64);
            var white = Color.white;
            if (c == '!')
            {
                p.RoundRect(14, 20, 12, 40, 5, white, white);
                p.Ellipse(20, 8, 7, 7, white);
            }
            else
            {
                p.Arc(20, 42, 15, 6, white);
                p.Rect(17, 18, 7, 14, white);
                p.Ellipse(20, 8, 6, 6, white);
                p.Rect(28, 32, 7, 12, white);
            }

            p.Outline(Ink, 3);
            return p.Bake(new Vector2(0.5f, 0.5f));
        }

        static Color Dark(Color c, float k)
        {
            return new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);
        }

        // Холст с заливками, градиентом сверху вниз, контуром.
        sealed class Pix
        {
            readonly int _w;
            readonly int _h;
            readonly Color[] _px;

            public Pix(int w, int h)
            {
                _w = w;
                _h = h;
                _px = new Color[w * h];
            }

            public void Set(int x, int y, Color c)
            {
                if ((uint)x >= (uint)_w || (uint)y >= (uint)_h)
                    return;
                _px[y * _w + x] = c;
            }

            void Plot(int x, int y, Color c)
            {
                if ((uint)x >= (uint)_w || (uint)y >= (uint)_h)
                    return;
                int i = y * _w + x;
                if (c.a >= 0.99f)
                {
                    _px[i] = c;
                    return;
                }

                Color d = _px[i];
                float a = c.a + d.a * (1f - c.a);
                if (a <= 0.001f)
                    return;
                _px[i] = new Color((c.r * c.a + d.r * d.a * (1f - c.a)) / a, (c.g * c.a + d.g * d.a * (1f - c.a)) / a, (c.b * c.a + d.b * d.a * (1f - c.a)) / a, a);
            }

            public void Rect(int x, int y, int w, int h, Color c)
            {
                for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                    Plot(xx, yy, c);
            }

            // Скруглённый прямоугольник с градиентом: top — сверху, bottom — снизу.
            public void RoundRect(int x, int y, int w, int h, int r, Color top, Color bottom)
            {
                for (int yy = y; yy < y + h; yy++)
                {
                    float t = h <= 1 ? 0f : (yy - y) / (float)(h - 1);
                    var c = Color.Lerp(bottom, top, t);
                    for (int xx = x; xx < x + w; xx++)
                    {
                        int dx = xx < x + r ? x + r - xx : xx > x + w - 1 - r ? xx - (x + w - 1 - r) : 0;
                        int dy = yy < y + r ? y + r - yy : yy > y + h - 1 - r ? yy - (y + h - 1 - r) : 0;
                        if (dx * dx + dy * dy > r * r)
                            continue;
                        Plot(xx, yy, c);
                    }
                }
            }

            public void Ellipse(int cx, int cy, int rx, int ry, Color c)
            {
                if (rx < 1 || ry < 1)
                    return;
                for (int y = -ry; y <= ry; y++)
                for (int x = -rx; x <= rx; x++)
                {
                    float v = (x * x) / (float)(rx * rx) + (y * y) / (float)(ry * ry);
                    if (v <= 1f)
                        Plot(cx + x, cy + y, c);
                }
            }

            public void Ring(int cx, int cy, int r, int thick, Color c)
            {
                for (int y = -r - thick; y <= r + thick; y++)
                for (int x = -r - thick; x <= r + thick; x++)
                {
                    float d = Mathf.Sqrt(x * x + y * y);
                    if (d >= r - thick * 0.5f && d <= r + thick * 0.5f)
                        Plot(cx + x, cy + y, c);
                }
            }

            // Верхняя дуга (для вай-фая и вопроса).
            public void Arc(int cx, int cy, int r, int thick, Color c)
            {
                for (int y = 0; y <= r + thick; y++)
                for (int x = -r - thick; x <= r + thick; x++)
                {
                    float d = Mathf.Sqrt(x * x + y * y);
                    if (d >= r - thick * 0.5f && d <= r + thick * 0.5f && y >= Mathf.Abs(x) * 0.45f)
                        Plot(cx + x, cy + y, c);
                }
            }

            public void Line(int x0, int y0, int x1, int y1, Color c, int thick)
            {
                int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
                for (int i = 0; i <= steps; i++)
                {
                    float t = steps == 0 ? 0f : i / (float)steps;
                    int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                    int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                    Ellipse(x, y, Mathf.Max(1, thick), Mathf.Max(1, thick), c);
                }
            }

            public void Poly(Vector2[] pts, Color c)
            {
                float minY = float.MaxValue, maxY = float.MinValue;
                for (int i = 0; i < pts.Length; i++)
                {
                    minY = Mathf.Min(minY, pts[i].y);
                    maxY = Mathf.Max(maxY, pts[i].y);
                }

                var xs = new List<float>();
                for (int y = Mathf.FloorToInt(minY); y <= Mathf.CeilToInt(maxY); y++)
                {
                    xs.Clear();
                    float sy = y + 0.5f;
                    for (int i = 0; i < pts.Length; i++)
                    {
                        Vector2 a = pts[i], b = pts[(i + 1) % pts.Length];
                        if ((a.y <= sy && b.y > sy) || (b.y <= sy && a.y > sy))
                            xs.Add(a.x + (sy - a.y) / (b.y - a.y) * (b.x - a.x));
                    }

                    xs.Sort();
                    for (int k = 0; k + 1 < xs.Count; k += 2)
                    {
                        for (int x = Mathf.RoundToInt(xs[k]); x <= Mathf.RoundToInt(xs[k + 1]); x++)
                            Plot(x, y, c);
                    }
                }
            }

            // Контур вокруг непрозрачного — как у арта художника.
            public void Outline(Color c, int t)
            {
                var src = (Color[])_px.Clone();
                for (int y = 0; y < _h; y++)
                for (int x = 0; x < _w; x++)
                {
                    if (src[y * _w + x].a > 0.5f)
                        continue;
                    bool near = false;
                    for (int dy = -t; dy <= t && !near; dy++)
                    for (int dx = -t; dx <= t && !near; dx++)
                    {
                        if (dx * dx + dy * dy > t * t)
                            continue;
                        int nx = x + dx, ny = y + dy;
                        if ((uint)nx < (uint)_w && (uint)ny < (uint)_h && src[ny * _w + nx].a > 0.5f)
                            near = true;
                    }

                    if (near)
                        _px[y * _w + x] = c;
                }
            }

            public Sprite Bake()
            {
                return Bake(new Vector2(0.5f, 0f));
            }

            public Sprite Bake(Vector2 pivot)
            {
                var tex = new Texture2D(_w, _h, TextureFormat.RGBA32, false);
                tex.SetPixels(_px);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.Apply();
                tex.hideFlags = HideFlags.HideAndDontSave;
                var sprite = Sprite.Create(tex, new Rect(0f, 0f, _w, _h), pivot, 100f);
                sprite.hideFlags = HideFlags.HideAndDontSave;
                return sprite;
            }
        }
    }
}
