// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.Reflection;
using osu.Framework.Localisation;

namespace osu.Game.Rulesets.Diva.Localization
{
    /// <summary>
    /// DIVA ruleset bilingual strings (zh/en), mirroring BMS / Ez2Lazer localisation.
    /// Uses <see cref="CultureInfo.CurrentUICulture"/> (same as osu! language selection).
    /// </summary>
    public static class DivaLocalizationManager
    {
        internal static void AutoFillEnglish(Type type)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(DivaLocalisableString))
                {
                    if (field.GetValue(null) is DivaLocalisableString instance && instance.English == null)
                        instance.English = field.Name.Replace("_", " ");
                }
            }
        }

        public class DivaLocalisableString : ILocalisableStringData
        {
            public string Chinese { get; }

            public string? English { get; set; }

            public DivaLocalisableString(string chinese, string? english = null)
            {
                Chinese = chinese;
                English = english;
            }

            public static implicit operator string(DivaLocalisableString s) => s.getString();

            public static implicit operator LocalisableString(DivaLocalisableString s) => new LocalisableString((ILocalisableStringData)s);

            public string Format(params object[] args) => string.Format(getString(), args);

            public override string ToString() => getString();

            private string getString()
            {
                string lang = CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.Ordinal) ? "zh" : "en";
                return lang == "zh" ? Chinese : (English ?? Chinese);
            }

            public string GetLocalised(LocalisationParameters parameters) => getString();

            public bool Equals(ILocalisableStringData? other)
            {
                if (other is not DivaLocalisableString divaOther)
                    return false;

                return Chinese == divaOther.Chinese && English == divaOther.English;
            }
        }
    }
}
