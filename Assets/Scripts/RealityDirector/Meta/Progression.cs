using System.Collections.Generic;
using RealityDirector.Capture;
using RealityDirector.Core;
using UnityEngine;

namespace RealityDirector.Meta
{
    public enum ViewerWishId
    {
        None,
        Cry,
        Hug,
        Fight,
        HoldTone
    }

    public static class Progression
    {
        public const int SeasonLength = 6;
        public const int BasePayout = 130;
        public const int MaxLevel = 5;

        static readonly int[] CastCost = { 70, 110, 160, 220 };
        static readonly int[] CrewCost = { 90, 130, 180, 240 };

        public static int EventSlots(int writerLevel, int episodeIndex)
        {
            if (episodeIndex <= 0)
                return 1;

            switch (Mathf.Clamp(writerLevel, 1, MaxLevel))
            {
                case 1: return 2;
                case 2: return 3;
                case 3: return 3;
                case 4: return 4;
                default: return 5;
            }
        }

        public static int ContractSlots(int writerLevel)
        {
            return writerLevel >= 4 ? 2 : 1;
        }

        public static bool CategoryOpen(string category, int writerLevel)
        {
            if (string.IsNullOrEmpty(category) || category == "Sponsor")
                return true;
            if (writerLevel >= 4)
                return true;
            if (category == "Provocation" || category == "Environment" || category == "Comedy" || category == "Control")
                return true;
            if (writerLevel >= 2 && (category == "Social" || category == "Confession" || category == "DeckManagement"))
                return true;
            if (writerLevel >= 3 && category == "Reveal")
                return true;
            return false;
        }

        public static int CaptureSlots(int operatorLevel)
        {
            return Mathf.Clamp(operatorLevel, 1, MaxLevel);
        }

        public static int UpgradeCost(bool cast, int level)
        {
            if (level < 1 || level >= MaxLevel)
                return 0;
            return (cast ? CastCost : CrewCost)[level - 1];
        }

        public static float HypeBonus(int castLevel)
        {
            float t = (Mathf.Clamp(castLevel, 1, MaxLevel) - 1) / 4f;
            return t * 0.25f;
        }

        public static int Payout(float score, int castLevel, bool wishDone)
        {
            float mult = score / 10f;
            mult *= 1f + HypeBonus(castLevel);
            if (wishDone)
                mult *= 1.3f;
            return Mathf.Max(0, Mathf.RoundToInt(BasePayout * mult));
        }

        public static bool WishMet(ViewerWishId wish, IReadOnlyList<CapturedMoment> moments, SeasonTone tone)
        {
            switch (wish)
            {
                case ViewerWishId.Cry: return HasTag(moments, MomentTags.Crying);
                case ViewerWishId.Hug: return HasMood(moments, ShowMood.Family);
                case ViewerWishId.Fight: return HasTag(moments, MomentTags.Fight);
                case ViewerWishId.HoldTone:
                    return tone != null && tone.TryLead(out ShowMood lead) && HasMood(moments, lead);
                default: return false;
            }
        }

        static bool HasTag(IReadOnlyList<CapturedMoment> moments, string tag)
        {
            if (moments == null)
                return false;
            for (int i = 0; i < moments.Count; i++)
            {
                if (moments[i].grade == CaptureGrade.Blank)
                    continue;
                if (moments[i].tags != null && moments[i].tags.Contains(tag))
                    return true;
            }

            return false;
        }

        static bool HasMood(IReadOnlyList<CapturedMoment> moments, ShowMood mood)
        {
            if (moments == null)
                return false;
            for (int i = 0; i < moments.Count; i++)
            {
                if (moments[i].grade == CaptureGrade.Blank)
                    continue;
                if (mood == ShowMood.Family && moments[i].grade != CaptureGrade.Cast)
                    continue;
                if (moments[i].mood == mood)
                    return true;
            }

            return false;
        }
    }
}
