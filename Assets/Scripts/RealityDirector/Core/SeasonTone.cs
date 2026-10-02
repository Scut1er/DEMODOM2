using UnityEngine;

namespace RealityDirector.Core
{
    // Сезонный счёт. Концовка (её ещё нет) читает TryLead после 6-й серии.
    public class SeasonTone
    {
        public const int CardGain = 6;
        public const int PropGain = 8;
        public const int MomentGain = 18;
        public const int Cap = 100;
        public const float BlankTax = 0.15f;

        public int Drama;
        public int Trash;
        public int Family;

        public int Total => Drama + Trash + Family;

        public int Get(ShowMood mood)
        {
            switch (mood)
            {
                case ShowMood.Drama: return Drama;
                case ShowMood.Trash: return Trash;
                default: return Family;
            }
        }

        public int Add(ShowMood mood, int amount)
        {
            if (amount <= 0)
                return 0;

            int before = Get(mood);
            int after = Mathf.Min(Cap, before + amount);
            switch (mood)
            {
                case ShowMood.Drama: Drama = after; break;
                case ShowMood.Trash: Trash = after; break;
                default: Family = after; break;
            }

            return after - before;
        }

        public void Tax(float portion, out int drama, out int trash, out int family)
        {
            drama = Cut(ref Drama, portion);
            trash = Cut(ref Trash, portion);
            family = Cut(ref Family, portion);
        }

        static int Cut(ref int value, float portion)
        {
            if (value <= 0)
                return 0;
            int next = Mathf.FloorToInt(value * (1f - portion));
            if (next >= value)
                next = value - 1;
            int lost = value - next;
            value = Mathf.Max(0, next);
            return lost;
        }

        public void Reset()
        {
            Drama = 0;
            Trash = 0;
            Family = 0;
        }

        public bool TryLead(out ShowMood mood)
        {
            int max = Mathf.Max(Drama, Mathf.Max(Trash, Family));
            mood = ShowMood.Drama;
            if (max <= 0)
                return false;

            int hits = 0;
            if (Drama == max)
            {
                mood = ShowMood.Drama;
                hits++;
            }

            if (Trash == max)
            {
                mood = ShowMood.Trash;
                hits++;
            }

            if (Family == max)
            {
                mood = ShowMood.Family;
                hits++;
            }

            return hits == 1;
        }
    }
}
