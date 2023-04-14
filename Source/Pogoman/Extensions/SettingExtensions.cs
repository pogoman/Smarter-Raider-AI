using RimWorld;
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

        public static void SliderLabeled(this Listing_Standard ls, string label, ref int val, string format, int min = 0, int max = 1)
        {
            TextAnchor anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            ls.Gap(Gap);
            ls.LineRectSpilter(out Rect leftHalf, out Rect rightHalf, 0.3f, 70);
            Widgets.Label(rect: leftHalf, label: label);

            float result = Widgets.HorizontalSlider(rect: rightHalf.RightPart(pct: .90f).Rounded(), value: val, leftValue: min, rightValue: max, middleAlignment: true);
            val = (int)result;
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(rect: rightHalf.RightPart(pct: .10f).Rounded(), label: String.Format(format: format, arg0: val));
            Text.Anchor = anchor;
            ls.Gap(gapHeight: ls.verticalSpacing);
        }
    }
}
