using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PogoAI.Extensions
{
    public static class SettingExtensions
    {
        private static float gap = 12f;
        private static float lineGap = 3f;
        public static float Gap { get => gap; set => gap = value; }
        public static float LineGap { get => lineGap; set => lineGap = value; }

        public static Rect GetRect(this Listing_Standard listing_Standard, float? height = null)
        {
            return listing_Standard.GetRect(height ?? Text.LineHeight);
        }

        public static Rect LineRectSpilter(this Listing_Standard listing_Standard, out Rect leftHalf, float leftPartPct = 0.5f, float? height = null)
        {
            Rect lineRect = listing_Standard.GetRect(height);
            leftHalf = lineRect.LeftPart(leftPartPct).Rounded();
            return lineRect;
        }

        public static Rect LineRectSpilter(this Listing_Standard listing_Standard, out Rect leftHalf, out Rect rightHalf, float leftPartPct = 0.5f, float? height = null)
        {
            Rect lineRect = listing_Standard.LineRectSpilter(out leftHalf, leftPartPct, height);
            rightHalf = lineRect.RightPart(1f - leftPartPct).Rounded();
            return lineRect;
        }

        public static void AddLabeledTextField(this Listing_Standard listing_Standard, string label, ref string settingsValue, float leftPartPct = 0.5f, float? height = null)
        {
            listing_Standard.Gap(Gap);
            listing_Standard.LineRectSpilter(out Rect leftHalf, out Rect rightHalf, leftPartPct, height);

            Widgets.Label(leftHalf, label);

            string buffer = settingsValue.ToString();
            settingsValue = Widgets.TextField(rightHalf, buffer);
        }
    }
}
