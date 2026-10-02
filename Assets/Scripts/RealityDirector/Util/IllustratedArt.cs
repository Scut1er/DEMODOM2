using UnityEngine;

namespace RealityDirector.Util
{
    public static class IllustratedArt
    {
        static Sprite _wood;
        static Sprite _tile;
        static Sprite _bath;
        static Sprite _wall;
        static Sprite _sofa;
        static Sprite _bed;
        static Sprite _table;
        static Sprite _stove;
        static Sprite _fridge;
        static Sprite _shower;
        static Sprite _toilet;
        static Sprite _sink;
        static Sprite _plant;
        static Sprite _window;
        static Sprite _boards;
        static Sprite _rug;
        static Sprite _flame;
        static Sprite _glow;
        static Sprite _angry;
        static Sprite _kind;
        static Sprite _iconAnger;
        static Sprite _iconFire;
        static Sprite _iconWater;
        static Sprite _iconDoor;
        static Sprite _iconCast;
        static Sprite _iconCam;
        static Sprite _iconPen;
        static Sprite _iconTear;
        static Sprite _iconDevil;
        static Sprite _iconFamily;
        static Sprite _iconClap;
        static Sprite _slateBoard;
        static Sprite _slateStick;

        public static Sprite Wood => Live(ref _wood, () => Floor(false, false));
        public static Sprite Tile => Live(ref _tile, () => Floor(true, false));
        public static Sprite BathTile => Live(ref _bath, () => Floor(true, true));
        public static Sprite Wall => Live(ref _wall, PaintWall);
        public static Sprite Sofa => Live(ref _sofa, PaintSofa);
        public static Sprite Bed => Live(ref _bed, PaintBed);
        public static Sprite Table => Live(ref _table, PaintTable);
        public static Sprite Stove => Live(ref _stove, PaintStove);
        public static Sprite Fridge => Live(ref _fridge, PaintFridge);
        public static Sprite Shower => Live(ref _shower, PaintShower);
        public static Sprite Toilet => Live(ref _toilet, PaintToilet);
        public static Sprite Sink => Live(ref _sink, PaintSink);
        public static Sprite Plant => Live(ref _plant, PaintPlant);
        public static Sprite Window => Live(ref _window, PaintWindow);
        public static Sprite Boards => Live(ref _boards, PaintBoards);
        public static Sprite Rug => Live(ref _rug, PaintRug);
        public static Sprite Flame => Live(ref _flame, PaintFlame);
        public static Sprite Glow => Live(ref _glow, PaintGlow);
        public static Sprite PersonAngry => Live(ref _angry, () => PaintPerson(true));
        public static Sprite PersonKind => Live(ref _kind, () => PaintPerson(false));
        public static Sprite IconAnger => Live(ref _iconAnger, PaintIconAnger);
        public static Sprite IconFire => Live(ref _iconFire, PaintIconFire);
        public static Sprite IconWater => Live(ref _iconWater, PaintIconWater);
        public static Sprite IconDoor => Live(ref _iconDoor, PaintIconDoor);
        public static Sprite IconCast => Live(ref _iconCast, PaintIconCast);
        public static Sprite IconCamera => Live(ref _iconCam, PaintIconCamera);
        public static Sprite IconPen => Live(ref _iconPen, PaintIconPen);
        public static Sprite IconTear => Live(ref _iconTear, PaintIconTear);
        public static Sprite IconDevil => Live(ref _iconDevil, PaintIconDevil);
        public static Sprite IconFamily => Live(ref _iconFamily, PaintIconFamily);
        public static Sprite IconClap => Live(ref _iconClap, PaintIconClap);
        public static Sprite SlateBoard => Live(ref _slateBoard, PaintSlateBoard);
        public static Sprite SlateStick => Live(ref _slateStick, PaintSlateStick);

        // ??= сравнивает ссылку и не видит уничтоженный Unity-объект после выхода из Play Mode.
        static Sprite Live(ref Sprite slot, System.Func<Sprite> paint)
        {
            if (slot == null)
                slot = paint();
            return slot;
        }

        static Sprite Floor(bool tile, bool cool)
        {
            var p = new Pix(160, 160);
            if (!tile)
            {
                var baseC = new Color(0.62f, 0.45f, 0.28f);
                p.Clear(baseC);
                for (int row = 0; row < 8; row++)
                {
                    int y = row * 20;
                    float shift = (row % 2) * 18;
                    p.Rect(0, y, 160, 2, new Color(0.38f, 0.26f, 0.16f, 0.9f));
                    for (int plank = 0; plank < 5; plank++)
                    {
                        int x = (int)(plank * 36 + shift) % 160;
                        p.Rect(x, y, 2, 20, new Color(0.36f, 0.24f, 0.14f, 0.55f));
                    }
                }

                p.Grain(0.06f);
            }
            else
            {
                var grout = cool ? new Color(0.72f, 0.78f, 0.8f) : new Color(0.78f, 0.74f, 0.68f);
                var tileC = cool ? new Color(0.84f, 0.9f, 0.91f) : new Color(0.9f, 0.86f, 0.78f);
                p.Clear(grout);
                for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    var c = Color.Lerp(tileC, cool ? new Color(0.75f, 0.84f, 0.86f) : new Color(0.84f, 0.78f, 0.68f), Hash(x, y) * 0.35f);
                    p.Rect(x * 20 + 1, y * 20 + 1, 18, 18, c);
                }
            }

            p.InsetFrame(6, new Color(0.22f, 0.16f, 0.12f, 0.55f));
            return p.Bake();
        }

        static Sprite PaintWall()
        {
            var p = new Pix(24, 96);
            p.Clear(new Color(0.78f, 0.74f, 0.68f));
            p.Rect(0, 0, 24, 10, new Color(0.42f, 0.32f, 0.24f));
            p.Rect(0, 86, 24, 10, new Color(0.55f, 0.48f, 0.4f));
            p.Rect(2, 12, 3, 72, new Color(1f, 1f, 1f, 0.18f));
            p.Grain(0.04f);
            return p.Bake();
        }

        static Sprite PaintPerson(bool angry)
        {
            var p = new Pix(80, 128, true);
            var skin = new Color(0.93f, 0.76f, 0.63f);
            var skinShade = new Color(0.78f, 0.58f, 0.46f);
            var hair = angry ? new Color(0.1f, 0.07f, 0.06f) : new Color(0.45f, 0.28f, 0.16f);
            var shirt = angry ? new Color(0.55f, 0.13f, 0.12f) : new Color(0.9f, 0.88f, 0.82f);
            var over = angry ? new Color(0.32f, 0.1f, 0.1f) : new Color(0.32f, 0.5f, 0.44f);
            var pants = new Color(0.22f, 0.27f, 0.38f);
            var shoe = new Color(0.14f, 0.1f, 0.09f);

            p.Ellipse(40, 14, 22, 7, new Color(0f, 0f, 0f, 0.28f));
            p.Round(30, 16, 9, 30, pants);
            p.Round(43, 16, 9, 30, pants);
            p.Ellipse(33, 16, 7, 4, shoe);
            p.Ellipse(49, 16, 7, 4, shoe);
            p.Round(16, 42, 8, 28, skin);
            p.Round(56, 40, 8, 30, skin);
            p.Ellipse(40, 58, 18, 22, shirt);
            p.Round(24, 48, 16, 26, over);
            p.Ellipse(40, 78, 6, 5, skinShade);
            p.Ellipse(40, 96, 15, 17, skin);
            p.Ellipse(40, 104, 16, 12, hair);
            p.Rect(26, 96, 28, 8, hair);
            if (!angry)
                p.Ellipse(22, 98, 6, 10, hair);

            p.Ellipse(34, 96, 3, 3, Color.white);
            p.Ellipse(48, 96, 3, 3, Color.white);
            p.Ellipse(35, 95, 1, 2, new Color(0.12f, 0.1f, 0.1f));
            p.Ellipse(49, 95, 1, 2, new Color(0.12f, 0.1f, 0.1f));
            if (angry)
            {
                p.Line(30, 102, 38, 99, new Color(0.15f, 0.08f, 0.07f), 2);
                p.Line(50, 102, 42, 99, new Color(0.15f, 0.08f, 0.07f), 2);
                p.Line(34, 86, 48, 86, new Color(0.45f, 0.2f, 0.18f), 2);
            }
            else
            {
                p.Line(30, 101, 38, 102, new Color(0.25f, 0.16f, 0.12f), 2);
                p.Line(44, 102, 52, 101, new Color(0.25f, 0.16f, 0.12f), 2);
                p.Ellipse(41, 87, 5, 2, new Color(0.75f, 0.4f, 0.38f));
            }

            p.Ellipse(40, 62, 2, 2, Dark(shirt, 0.7f));
            p.Ellipse(40, 56, 2, 2, Dark(shirt, 0.7f));
            return p.Bake();
        }

        static Sprite PaintSofa()
        {
            var p = new Pix(180, 100, true);
            var wood = new Color(0.4f, 0.26f, 0.16f);
            var cloth = new Color(0.48f, 0.3f, 0.26f);
            var seat = new Color(0.62f, 0.4f, 0.34f);
            p.Rect(16, 6, 10, 8, wood);
            p.Rect(154, 6, 10, 8, wood);
            p.Rect(16, 86, 10, 8, wood);
            p.Rect(154, 86, 10, 8, wood);
            p.Round(14, 16, 152, 70, cloth);
            p.Round(28, 22, 124, 36, seat);
            p.Line(90, 24, 90, 56, Dark(seat, 0.75f), 2);
            p.Round(20, 58, 140, 22, Dark(cloth, 0.82f));
            p.Round(14, 20, 16, 58, Dark(cloth, 0.9f));
            p.Round(150, 20, 16, 58, Dark(cloth, 0.9f));
            return p.Bake();
        }

        static Sprite PaintBed()
        {
            var p = new Pix(150, 210, true);
            var wood = new Color(0.36f, 0.24f, 0.16f);
            p.Round(8, 8, 134, 194, wood);
            p.Round(18, 28, 114, 160, new Color(0.9f, 0.9f, 0.92f));
            p.Round(22, 36, 106, 100, new Color(0.55f, 0.64f, 0.74f));
            p.Line(28, 90, 122, 78, new Color(0.42f, 0.52f, 0.64f), 3);
            p.Round(28, 150, 40, 24, new Color(0.95f, 0.95f, 0.97f));
            p.Round(80, 150, 40, 24, new Color(0.86f, 0.88f, 0.92f));
            p.Round(18, 8, 114, 16, Dark(wood, 0.8f));
            return p.Bake();
        }

        static Sprite PaintTable()
        {
            var p = new Pix(140, 90, true);
            var wood = new Color(0.55f, 0.36f, 0.2f);
            var leg = new Color(0.32f, 0.2f, 0.12f);
            p.Rect(10, 8, 8, 22, leg);
            p.Rect(122, 8, 8, 22, leg);
            p.Rect(10, 60, 8, 22, leg);
            p.Rect(122, 60, 8, 22, leg);
            p.Round(8, 16, 124, 58, wood);
            p.Grain(0.05f);
            p.Rect(16, 62, 108, 3, new Color(1f, 1f, 1f, 0.18f));
            return p.Bake();
        }

        static Sprite PaintStove()
        {
            var p = new Pix(90, 90, true);
            p.Round(4, 4, 82, 82, new Color(0.22f, 0.22f, 0.24f));
            p.Round(10, 10, 70, 70, new Color(0.16f, 0.16f, 0.18f));
            p.Ellipse(32, 55, 12, 10, new Color(0.35f, 0.35f, 0.38f));
            p.Ellipse(58, 55, 12, 10, new Color(0.35f, 0.35f, 0.38f));
            p.Ellipse(32, 30, 12, 10, new Color(0.35f, 0.35f, 0.38f));
            p.Ellipse(58, 30, 12, 10, new Color(0.35f, 0.35f, 0.38f));
            p.Ellipse(32, 55, 4, 3, new Color(0.12f, 0.12f, 0.13f));
            p.Ellipse(58, 55, 4, 3, new Color(0.12f, 0.12f, 0.13f));
            p.Ellipse(32, 30, 4, 3, new Color(0.12f, 0.12f, 0.13f));
            p.Ellipse(58, 30, 4, 3, new Color(0.12f, 0.12f, 0.13f));
            return p.Bake();
        }

        static Sprite PaintFridge()
        {
            var p = new Pix(80, 150, true);
            var body = new Color(0.86f, 0.9f, 0.92f);
            p.Round(6, 4, 68, 142, new Color(0.7f, 0.74f, 0.76f));
            p.Round(10, 8, 60, 134, body);
            p.Line(14, 78, 66, 78, new Color(0.62f, 0.66f, 0.68f), 2);
            p.Round(58, 96, 6, 28, new Color(0.55f, 0.58f, 0.6f));
            p.Round(58, 40, 6, 22, new Color(0.55f, 0.58f, 0.6f));
            p.Rect(14, 120, 28, 10, new Color(0.75f, 0.8f, 0.82f));
            return p.Bake();
        }

        static Sprite PaintShower()
        {
            var p = new Pix(110, 110, true);
            p.Round(8, 8, 94, 94, new Color(0.75f, 0.82f, 0.84f));
            p.Round(18, 18, 74, 74, new Color(0.9f, 0.94f, 0.95f));
            p.Ellipse(55, 78, 8, 5, new Color(0.65f, 0.72f, 0.75f));
            p.Rect(52, 40, 6, 36, new Color(0.72f, 0.78f, 0.8f));
            p.Ellipse(55, 36, 16, 6, new Color(0.8f, 0.86f, 0.88f));
            p.Rect(86, 16, 8, 78, new Color(0.82f, 0.86f, 0.84f, 0.85f));
            return p.Bake();
        }

        static Sprite PaintToilet()
        {
            var p = new Pix(60, 90, true);
            p.Round(8, 48, 44, 34, new Color(0.82f, 0.84f, 0.86f));
            p.Ellipse(30, 28, 22, 16, new Color(0.95f, 0.96f, 0.97f));
            p.Ellipse(30, 28, 12, 8, new Color(0.75f, 0.86f, 0.9f));
            p.Round(18, 40, 24, 8, new Color(0.9f, 0.91f, 0.92f));
            return p.Bake();
        }

        static Sprite PaintSink()
        {
            var p = new Pix(100, 70, true);
            p.Round(6, 10, 88, 50, new Color(0.9f, 0.91f, 0.92f));
            p.Ellipse(50, 34, 28, 14, new Color(0.78f, 0.86f, 0.9f));
            p.Ellipse(50, 34, 8, 4, new Color(0.55f, 0.6f, 0.64f));
            p.Rect(46, 48, 8, 14, new Color(0.7f, 0.74f, 0.76f));
            return p.Bake();
        }

        static Sprite PaintPlant()
        {
            var p = new Pix(70, 90, true);
            p.Round(22, 4, 26, 24, new Color(0.55f, 0.32f, 0.2f));
            p.Ellipse(35, 48, 16, 18, new Color(0.22f, 0.48f, 0.28f));
            p.Ellipse(22, 58, 12, 14, new Color(0.28f, 0.55f, 0.3f));
            p.Ellipse(50, 60, 12, 13, new Color(0.18f, 0.42f, 0.24f));
            p.Ellipse(36, 70, 10, 12, new Color(0.3f, 0.58f, 0.32f));
            return p.Bake();
        }

        static Sprite PaintWindow()
        {
            var p = new Pix(90, 50, true);
            p.Round(2, 2, 86, 46, new Color(0.85f, 0.82f, 0.76f));
            p.Rect(8, 8, 32, 34, new Color(0.62f, 0.78f, 0.88f));
            p.Rect(50, 8, 32, 34, new Color(0.7f, 0.84f, 0.92f));
            p.Rect(42, 8, 6, 34, new Color(0.9f, 0.88f, 0.82f));
            p.Rect(10, 10, 14, 8, new Color(1f, 1f, 1f, 0.35f));
            return p.Bake();
        }

        static Sprite PaintBoards()
        {
            var p = new Pix(70, 120, true);
            var wood = new Color(0.48f, 0.32f, 0.18f);
            p.Round(8, 4, 54, 112, new Color(0.32f, 0.22f, 0.14f));
            p.Round(14, 10, 42, 100, new Color(0.24f, 0.18f, 0.14f));
            p.Round(8, 28, 54, 12, wood);
            p.Round(8, 54, 54, 12, wood);
            p.Round(8, 80, 54, 12, wood);
            p.Line(16, 18, 54, 104, new Color(0.62f, 0.44f, 0.24f), 4);
            p.Line(54, 18, 16, 104, new Color(0.55f, 0.38f, 0.2f), 4);
            return p.Bake();
        }

        static Sprite PaintRug()
        {
            var p = new Pix(140, 90, true);
            p.Round(4, 4, 132, 82, new Color(0.55f, 0.18f, 0.16f));
            p.Round(12, 12, 116, 66, new Color(0.72f, 0.28f, 0.22f));
            p.Rect(20, 40, 100, 8, new Color(0.86f, 0.75f, 0.45f, 0.8f));
            return p.Bake();
        }

        static Sprite PaintFlame()
        {
            var p = new Pix(32, 48, true);
            p.Ellipse(16, 10, 8, 6, new Color(1f, 0.35f, 0.05f, 0.9f));
            p.Ellipse(16, 22, 10, 12, new Color(1f, 0.5f, 0.08f, 0.95f));
            p.Ellipse(16, 32, 7, 10, new Color(1f, 0.85f, 0.3f, 1f));
            p.Ellipse(16, 38, 3, 5, new Color(1f, 0.96f, 0.75f, 1f));
            return p.Bake();
        }

        static Sprite PaintGlow()
        {
            var p = new Pix(64, 64, true);
            p.Ellipse(32, 32, 28, 22, new Color(1f, 0.45f, 0.1f, 0.35f));
            p.Ellipse(32, 32, 14, 10, new Color(1f, 0.75f, 0.3f, 0.45f));
            return p.Bake();
        }

        static Sprite PaintIconAnger()
        {
            var p = new Pix(128, 128, true);
            p.Clear(new Color(0.45f, 0.12f, 0.12f));
            p.Ellipse(64, 58, 36, 40, new Color(0.93f, 0.76f, 0.63f));
            p.Ellipse(64, 78, 38, 22, new Color(0.12f, 0.08f, 0.07f));
            p.Ellipse(50, 62, 6, 6, Color.white);
            p.Ellipse(78, 62, 6, 6, Color.white);
            p.Ellipse(51, 60, 3, 4, new Color(0.15f, 0.08f, 0.07f));
            p.Ellipse(79, 60, 3, 4, new Color(0.15f, 0.08f, 0.07f));
            p.Line(40, 74, 58, 68, new Color(0.2f, 0.08f, 0.07f), 3);
            p.Line(88, 74, 70, 68, new Color(0.2f, 0.08f, 0.07f), 3);
            p.Line(48, 42, 80, 42, new Color(0.55f, 0.16f, 0.14f), 4);
            return p.Bake();
        }

        static Sprite PaintIconFire()
        {
            var p = new Pix(128, 128, true);
            p.Clear(new Color(0.35f, 0.16f, 0.08f));
            p.Round(40, 18, 48, 78, new Color(0.86f, 0.9f, 0.92f));
            p.Line(46, 58, 82, 58, new Color(0.6f, 0.64f, 0.66f), 2);
            p.Ellipse(64, 92, 10, 16, new Color(1f, 0.45f, 0.05f));
            p.Ellipse(50, 100, 8, 14, new Color(1f, 0.7f, 0.15f));
            p.Ellipse(78, 98, 8, 12, new Color(1f, 0.3f, 0.05f));
            p.Ellipse(64, 108, 5, 8, new Color(1f, 0.95f, 0.7f));
            return p.Bake();
        }

        static Sprite PaintIconWater()
        {
            var p = new Pix(128, 128, true);
            p.Clear(new Color(0.15f, 0.28f, 0.42f));
            p.Round(36, 28, 56, 70, new Color(0.75f, 0.82f, 0.86f));
            p.Ellipse(64, 58, 18, 10, new Color(0.45f, 0.7f, 0.85f));
            p.Ellipse(64, 96, 10, 14, new Color(0.55f, 0.82f, 0.95f));
            p.Ellipse(46, 88, 6, 9, new Color(0.7f, 0.88f, 0.98f));
            p.Ellipse(82, 84, 6, 9, new Color(0.6f, 0.84f, 0.96f));
            return p.Bake();
        }

        static Sprite PaintIconDoor()
        {
            var p = new Pix(128, 128, true);
            p.Clear(new Color(0.28f, 0.2f, 0.14f));
            p.Round(28, 16, 72, 100, new Color(0.5f, 0.33f, 0.18f));
            p.Round(36, 24, 56, 84, new Color(0.62f, 0.42f, 0.24f));
            p.Rect(78, 16, 18, 100, new Color(1f, 0.86f, 0.55f, 0.9f));
            p.Ellipse(78, 68, 4, 4, new Color(0.85f, 0.75f, 0.4f));
            return p.Bake();
        }

        static Sprite PaintIconCast()
        {
            var p = new Pix(128, 128, true);
            p.Clear(new Color(0.25f, 0.18f, 0.16f));
            p.Ellipse(46, 58, 22, 26, new Color(0.93f, 0.76f, 0.63f));
            p.Ellipse(46, 72, 24, 14, new Color(0.12f, 0.08f, 0.07f));
            p.Ellipse(84, 50, 20, 24, new Color(0.95f, 0.82f, 0.7f));
            p.Ellipse(84, 64, 22, 14, new Color(0.48f, 0.3f, 0.18f));
            p.Round(28, 22, 28, 22, new Color(0.55f, 0.14f, 0.13f));
            p.Round(70, 16, 28, 20, new Color(0.35f, 0.52f, 0.46f));
            return p.Bake();
        }

        static Sprite PaintIconCamera()
        {
            var p = new Pix(128, 128, true);
            p.Clear(new Color(0.12f, 0.12f, 0.14f));
            p.Round(18, 40, 92, 52, new Color(0.18f, 0.18f, 0.2f));
            p.Round(24, 46, 70, 40, new Color(0.28f, 0.28f, 0.32f));
            p.Ellipse(78, 66, 22, 22, new Color(0.15f, 0.16f, 0.2f));
            p.Ellipse(78, 66, 12, 12, new Color(0.45f, 0.62f, 0.78f));
            p.Ellipse(78, 66, 5, 5, new Color(0.85f, 0.92f, 0.96f));
            p.Rect(30, 78, 22, 10, new Color(0.1f, 0.1f, 0.12f));
            return p.Bake();
        }

        static Sprite PaintIconPen()
        {
            var p = new Pix(128, 128, true);
            p.Clear(new Color(0.22f, 0.18f, 0.28f));
            p.Round(30, 28, 68, 78, new Color(0.93f, 0.9f, 0.82f));
            p.Line(42, 78, 84, 78, new Color(0.7f, 0.62f, 0.5f), 2);
            p.Line(42, 66, 80, 66, new Color(0.7f, 0.62f, 0.5f), 2);
            p.Line(42, 54, 76, 54, new Color(0.7f, 0.62f, 0.5f), 2);
            p.Line(28, 24, 96, 100, new Color(0.15f, 0.12f, 0.18f), 4);
            p.Line(90, 96, 102, 108, new Color(0.75f, 0.62f, 0.28f), 4);
            return p.Bake();
        }

        static Sprite PaintIconTear()
        {
            var p = new Pix(64, 64, true);
            var blue = new Color(0.22f, 0.48f, 0.82f, 1f);
            var drop = new Color(0.82f, 0.94f, 1f, 1f);
            p.Ellipse(32, 32, 30, 30, blue);
            p.Ellipse(32, 24, 11, 12, drop);
            p.Ellipse(32, 34, 8, 8, drop);
            p.Ellipse(32, 42, 4, 6, drop);
            p.Ellipse(28, 28, 3, 2, new Color(1f, 1f, 1f, 0.85f));
            return p.Bake();
        }

        static Sprite PaintIconDevil()
        {
            var p = new Pix(64, 64, true);
            var red = new Color(0.72f, 0.14f, 0.13f, 1f);
            var face = new Color(0.93f, 0.28f, 0.22f, 1f);
            var ink = new Color(0.28f, 0.04f, 0.05f, 1f);
            p.Ellipse(32, 32, 30, 30, red);
            p.Ellipse(20, 50, 6, 5, ink);
            p.Ellipse(44, 50, 6, 5, ink);
            p.Line(16, 40, 22, 52, ink, 3);
            p.Line(48, 40, 42, 52, ink, 3);
            p.Ellipse(32, 28, 16, 14, face);
            p.Ellipse(26, 30, 2, 3, ink);
            p.Ellipse(38, 30, 2, 3, ink);
            p.Line(24, 20, 32, 16, ink, 2);
            p.Line(32, 16, 40, 20, ink, 2);
            return p.Bake();
        }

        static Sprite PaintSlateBoard()
        {
            var p = new Pix(480, 320, true);
            var black = new Color(0.04f, 0.04f, 0.05f, 1f);
            var edge = new Color(0.9f, 0.88f, 0.82f, 1f);
            var line = new Color(0.22f, 0.21f, 0.2f, 1f);
            p.Round(4, 4, 472, 312, black);
            p.Rect(4, 4, 472, 8, edge);
            p.Rect(4, 308, 472, 8, edge);
            p.Rect(4, 4, 8, 312, edge);
            p.Rect(468, 4, 8, 312, edge);
            p.Rect(28, 78, 424, 3, line);
            p.Rect(28, 168, 424, 3, line);
            p.Rect(250, 78, 3, 90, line);
            p.Ellipse(40, 286, 9, 9, edge);
            p.Ellipse(440, 286, 9, 9, edge);
            p.Ellipse(40, 286, 4, 4, black);
            p.Ellipse(440, 286, 4, 4, black);
            return p.Bake();
        }

        static Sprite PaintSlateStick()
        {
            var p = new Pix(480, 108, true);
            var white = new Color(0.95f, 0.93f, 0.88f, 1f);
            var black = new Color(0.03f, 0.03f, 0.04f, 1f);
            var rail = new Color(0.9f, 0.88f, 0.82f, 1f);
            p.Rect(0, 0, 480, 108, rail);
            for (int y = 14; y < 94; y++)
            {
                for (int x = 8; x < 472; x++)
                {
                    int band = ((x + y * 2) / 26) & 1;
                    p.Rect(x, y, 1, 1, band == 0 ? black : white);
                }
            }

            p.Rect(0, 0, 480, 12, rail);
            p.Rect(0, 96, 480, 12, rail);
            p.Rect(0, 0, 8, 108, rail);
            p.Rect(472, 0, 8, 108, rail);
            return p.Bake();
        }

        static Sprite PaintIconClap()
        {
            var p = new Pix(64, 64, true);
            var board = new Color(0.96f, 0.93f, 0.86f, 1f);
            var ink = new Color(0.1f, 0.09f, 0.1f, 1f);
            var stripe = new Color(0.96f, 0.93f, 0.86f, 1f);
            p.Round(6, 8, 52, 34, board);
            p.Rect(12, 16, 40, 3, new Color(0.72f, 0.66f, 0.56f, 1f));
            p.Rect(12, 24, 30, 3, new Color(0.72f, 0.66f, 0.56f, 1f));
            p.Round(4, 38, 56, 16, ink);
            p.Line(8, 42, 20, 52, stripe, 5);
            p.Line(22, 42, 34, 52, stripe, 5);
            p.Line(36, 42, 48, 52, stripe, 5);
            return p.Bake();
        }

        static Sprite PaintIconFamily()
        {
            var p = new Pix(64, 64, true);
            var green = new Color(0.2f, 0.55f, 0.32f, 1f);
            var shirt = new Color(0.78f, 0.93f, 0.78f, 1f);
            var skin = new Color(0.93f, 0.76f, 0.63f, 1f);
            p.Ellipse(32, 32, 30, 30, green);
            p.Round(12, 16, 16, 16, shirt);
            p.Round(36, 16, 16, 16, shirt);
            p.Round(24, 18, 16, 14, new Color(0.55f, 0.78f, 0.58f, 1f));
            p.Ellipse(20, 36, 8, 8, skin);
            p.Ellipse(44, 36, 8, 8, skin);
            p.Ellipse(32, 34, 6, 6, skin);
            p.Line(16, 30, 48, 30, skin, 3);
            return p.Bake();
        }

        static Color Dark(Color c, float k)
        {
            return new Color(c.r * k, c.g * k, c.b * k, c.a);
        }

        static float Hash(int x, int y)
        {
            int n = x * 374761393 + y * 668265263;
            n = (n ^ (n >> 13)) * 1274126177;
            return (n & 0x7fffffff) / (float)int.MaxValue;
        }

        sealed class Pix
        {
            readonly int _w;
            readonly int _h;
            readonly Color[] _px;

            public Pix(int w, int h, bool clear = false)
            {
                _w = w;
                _h = h;
                _px = new Color[w * h];
                if (clear)
                {
                    var empty = new Color(0f, 0f, 0f, 0f);
                    for (int i = 0; i < _px.Length; i++)
                        _px[i] = empty;
                }
            }

            public void Clear(Color c)
            {
                for (int i = 0; i < _px.Length; i++)
                    _px[i] = c;
            }

            public void Grain(float amount)
            {
                for (int y = 0; y < _h; y++)
                for (int x = 0; x < _w; x++)
                {
                    int i = y * _w + x;
                    if (_px[i].a < 0.05f)
                        continue;
                    float n = (Hash(x, y) - 0.5f) * amount;
                    var c = _px[i];
                    c.r = Mathf.Clamp01(c.r + n);
                    c.g = Mathf.Clamp01(c.g + n);
                    c.b = Mathf.Clamp01(c.b + n);
                    _px[i] = c;
                }
            }

            public void InsetFrame(int t, Color c)
            {
                Rect(0, 0, _w, t, c);
                Rect(0, _h - t, _w, t, c);
                Rect(0, 0, t, _h, c);
                Rect(_w - t, 0, t, _h, c);
            }

            public void Rect(int x, int y, int w, int h, Color c)
            {
                for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                    Plot(xx, yy, c);
            }

            public void Round(int x, int y, int w, int h, Color c)
            {
                Rect(x + 2, y, w - 4, h, c);
                Rect(x, y + 2, w, h - 4, c);
            }

            public void Ellipse(int cx, int cy, int rx, int ry, Color c)
            {
                if (rx < 1 || ry < 1)
                    return;
                long rx2 = (long)rx * rx;
                long ry2 = (long)ry * ry;
                for (int y = -ry; y <= ry; y++)
                for (int x = -rx; x <= rx; x++)
                {
                    if (x * x * ry2 + y * y * rx2 <= rx2 * ry2)
                        Plot(cx + x, cy + y, c);
                }
            }

            public void Line(int x0, int y0, int x1, int y1, Color c, int thick)
            {
                int dx = Mathf.Abs(x1 - x0);
                int dy = Mathf.Abs(y1 - y0);
                int sx = x0 < x1 ? 1 : -1;
                int sy = y0 < y1 ? 1 : -1;
                int err = dx - dy;
                int guard = 0;
                while (guard++ < 400)
                {
                    if (thick <= 1)
                        Plot(x0, y0, c);
                    else
                        Ellipse(x0, y0, thick, thick, c);
                    if (x0 == x1 && y0 == y1)
                        break;
                    int e2 = err * 2;
                    if (e2 > -dy)
                    {
                        err -= dy;
                        x0 += sx;
                    }

                    if (e2 < dx)
                    {
                        err += dx;
                        y0 += sy;
                    }
                }
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
                _px[i] = new Color(
                    (c.r * c.a + d.r * d.a * (1f - c.a)) / a,
                    (c.g * c.a + d.g * d.a * (1f - c.a)) / a,
                    (c.b * c.a + d.b * d.a * (1f - c.a)) / a,
                    a);
            }

            public Sprite Bake()
            {
                var tex = new Texture2D(_w, _h, TextureFormat.RGBA32, false);
                tex.SetPixels(_px);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.Apply();
                tex.hideFlags = HideFlags.HideAndDontSave;
                var sprite = Sprite.Create(tex, new Rect(0f, 0f, _w, _h), new Vector2(0.5f, 0.5f), 100f);
                sprite.hideFlags = HideFlags.HideAndDontSave;
                return sprite;
            }
        }
    }
}
