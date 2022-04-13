using Rocket.API.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CommonZones;
internal static class Translation
{
    private static readonly Dictionary<string, TranslationData> Localization;
    private static readonly Dictionary<string, TranslationData> DefaultLocalization;
    static Translation()
    {
        int c = CommonZones.I.DefaultTranslations.Count();
        Localization = new Dictionary<string, TranslationData>(c);
        DefaultLocalization = new Dictionary<string, TranslationData>(c);
    }
    internal static void InitTranslations()
    {
        Localization.Clear();
        foreach (TranslationListEntry entry in CommonZones.I.Translations.Instance)
        {
            if (!Localization.ContainsKey(entry.Id))
            {
                Localization.Add(entry.Id, new TranslationData(entry.Value.Replace("{{", "<").Replace("}}", ">")));
            }
            else
            {
                L.LogWarning("Duplicate translation key: " + entry.Id);
            }
        }
        DefaultLocalization.Clear();
        foreach (TranslationListEntry entry in CommonZones.I.DefaultTranslations)
        {
            if (!DefaultLocalization.ContainsKey(entry.Id))
            {
                DefaultLocalization.Add(entry.Id, new TranslationData(entry.Value.Replace("{{", "<").Replace("}}", ">")));
            }
            else
            {
                L.LogWarning("Duplicate default translation key: " + entry.Id);
            }
        }
    }
    internal static string ObjectTranslate(string key, params object[] formatting)
    {
        if (key == null)
        {
            string args = formatting?.Length == 0 ? string.Empty : string.Join(", ", formatting);
            return args;
        }
        if (key.Length == 0)
        {
            return formatting.Length > 0 ? string.Join(", ", formatting) : "";
        }
        if (formatting != null)
            for (int i = 0; i < formatting.Length; ++i)
                if (formatting[i] == null)
                    formatting[i] = "null";
        if (Localization.TryGetValue(key, out TranslationData data))
        {
            try
            {
                return string.Format(data.Original, formatting);
            }
            catch (FormatException ex)
            {
                L.LogError("Translation error with " + key + " when " + (formatting == null ? 0 : formatting.Length) + " formatting arguments were supplied.");
                L.LogError(ex);
                if (DefaultLocalization.TryGetValue(key, out data))
                {
                    try
                    {
                        return string.Format(data.Original, formatting);
                    }
                    catch (FormatException ex2)
                    {
                        L.LogError("Translation error with default value of " + key + " when " + (formatting == null ? 0 : formatting.Length) + " formatting arguments were supplied.");
                        L.LogError(ex2);
                        return data.Original + (formatting?.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                return data.Original + (formatting?.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
            }
        }
        else if (DefaultLocalization.TryGetValue(key, out data))
        {
            try
            {
                return string.Format(data.Original, formatting);
            }
            catch (FormatException ex)
            {
                L.LogError("Translation error with " + key + " when " + (formatting == null ? 0 : formatting.Length) + " formatting arguments were supplied.");
                L.LogError(ex);
                return data.Original + (formatting?.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
            }
        }
        return key + (formatting?.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
    }
    internal static string ObjectTranslate(string key, out Color color, params object[] formatting)
    {
        if (key == null)
        {
            string args = formatting?.Length == 0 ? string.Empty : string.Join(", ", formatting);
            color = Color.white;
            return args;
        }
        if (key.Length == 0)
        {
            color = Color.white;
            return formatting.Length > 0 ? string.Join(", ", formatting) : "";
        }
        if (formatting != null)
            for (int i = 0; i < formatting.Length; ++i)
                if (formatting[i] == null)
                    formatting[i] = "null";
        if (Localization.TryGetValue(key, out TranslationData data))
        {
            try
            {
                color = data.Color;
                return string.Format(data.Message, formatting);
            }
            catch (FormatException ex)
            {
                L.LogError("Translation error with " + key + " when " + (formatting == null ? 0 : formatting.Length) + " formatting arguments were supplied.");
                L.LogError(ex);
                if (DefaultLocalization.TryGetValue(key, out data))
                {
                    try
                    {
                        color = data.Color;
                        return string.Format(data.Message, formatting);
                    }
                    catch (FormatException ex2)
                    {
                        L.LogError("Translation error with default value of " + key + " when " + (formatting == null ? 0 : formatting.Length) + " formatting arguments were supplied.");
                        L.LogError(ex2);
                        color = data.Color;
                        return data.Message + (formatting?.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                color = data.Color;
                return data.Original + (formatting?.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
            }
        }
        else if (DefaultLocalization.TryGetValue(key, out data))
        {
            try
            {
                color = data.Color;
                return string.Format(data.Message, formatting);
            }
            catch (FormatException ex2)
            {
                L.LogError("Translation error with default value of " + key + " when " + (formatting == null ? 0 : formatting.Length) + " formatting arguments were supplied.");
                L.LogError(ex2);
                color = data.Color;
                return data.Message + (formatting?.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
            }
        }
        color = Color.white;
        return key + (formatting?.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
    }
    /// <summary>
    /// Tramslate an unlocalized string to a localized string using the Rocket translations file, provides the Original message (non-color removed)
    /// </summary>
    /// <param name="key">The unlocalized string to match with the translation dictionary.</param>
    /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
    /// <returns>A localized string based on the player's language.</returns>
    internal static string Translate(string key, params string[] formatting)
    {
        if (key == null)
        {
            string args = formatting?.Length == 0 ? string.Empty : string.Join(", ", formatting);
            return args;
        }
        if (key.Length == 0)
        {
            return formatting.Length > 0 ? string.Join(", ", formatting) : "";
        }
        if (formatting != null)
            for (int i = 0; i < formatting.Length; ++i)
                if (formatting[i] == null) 
                    formatting[i] = "null";
        if (Localization.TryGetValue(key, out TranslationData data))
        {
            try
            {
                return string.Format(data.Original, formatting);
            }
            catch (FormatException ex)
            {
                L.LogError("Translation error with " + key + " when " + (formatting == null ? 0 : formatting.Length) + " formatting arguments were supplied.");
                L.LogError(ex);
                if (DefaultLocalization.TryGetValue(key, out data))
                {
                    try
                    {
                        return string.Format(data.Original, formatting);
                    }
                    catch (FormatException ex2)
                    {
                        L.LogError("Translation error with default value of " + key + " when " + (formatting == null ? 0 : formatting.Length) + " formatting arguments were supplied.");
                        L.LogError(ex2);
                        return data.Original + (formatting?.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                return data.Original + (formatting?.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
            }
        }
        else
        {
            foreach (TranslationListEntry translation in CommonZones.I.DefaultTranslations)
            {
                if (translation.Id.Equals(key, StringComparison.Ordinal))
                {
                    try
                    {
                        return string.Format(translation.Value, formatting);
                    }
                    catch (FormatException ex)
                    {
                        L.LogError("Translation error with " + key + " when " + (formatting == null ? 0 : formatting.Length) + " formatting arguments were supplied.");
                        L.LogError(ex);
                        return translation.Value + (formatting?.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
            }
        }
        return key + (formatting?.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
    }
    /// <summary>
    /// Tramslate an unlocalized string to a localized string using the Rocket translations file, provides the color-removed message along with the color.
    /// </summary>
    /// <param name="key">The unlocalized string to match with the translation dictionary.</param>
    /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
    /// <param name="color">Color of the message.</param>
    /// <returns>A localized string based on the player's language.</returns>
    internal static string Translate(string key, out Color color, params string[] formatting)
    {
        if (key == null)
        {
            string args = formatting?.Length == 0 ? string.Empty : string.Join(", ", formatting);
            color = Color.white;
            return args;
        }
        if (key.Length == 0)
        {
            color = Color.white;
            return formatting.Length > 0 ? string.Join(", ", formatting) : "";
        }
        if (formatting != null)
            for (int i = 0; i < formatting.Length; ++i)
                if (formatting[i] == null)
                    formatting[i] = "null";
        if (Localization.TryGetValue(key, out TranslationData data))
        {
            try
            {
                color = data.Color;
                return string.Format(data.Message, formatting);
            }
            catch (FormatException ex)
            {
                L.LogError("Translation error with " + key + " when " + (formatting == null ? 0 : formatting.Length) + " formatting arguments were supplied.");
                L.LogError(ex);
                if (DefaultLocalization.TryGetValue(key, out data))
                {
                    try
                    {
                        color = data.Color;
                        return string.Format(data.Message, formatting);
                    }
                    catch (FormatException ex2)
                    {
                        L.LogError("Translation error with default value of " + key + " when " + (formatting == null ? 0 : formatting.Length) + " formatting arguments were supplied.");
                        L.LogError(ex2);
                        color = data.Color;
                        return data.Message + (formatting?.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                color = data.Color;
                return data.Original + (formatting?.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
            }
        }
        else if (DefaultLocalization.TryGetValue(key, out data))
        {
            try
            {
                color = data.Color;
                return string.Format(data.Message, formatting);
            }
            catch (FormatException ex2)
            {
                L.LogError("Translation error with default value of " + key + " when " + (formatting == null ? 0 : formatting.Length) + " formatting arguments were supplied.");
                L.LogError(ex2);
                color = data.Color;
                return data.Message + (formatting?.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
            }
        }
        color = Color.white;
        return key + (formatting?.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
    }

    private readonly struct TranslationData
    {
        public static readonly TranslationData Nil = new TranslationData("default", "<color=#ffffff>default</color>", true, Color.white);
        public readonly string Message;
        public readonly string Original;
        public readonly Color Color;
        public readonly bool UseColor;
        public TranslationData(string message, string original, bool useColor, Color color)
        {
            this.Message = message;
            this.Original = original;
            this.UseColor = useColor;
            this.Color = color;
        }
        public TranslationData(string Original)
        {
            this.Original = Original;
            this.Color = GetColorFromMessage(Original, out Message, out UseColor);
        }
        public static TranslationData GetPlaceholder(string key) => new TranslationData(key, key, false, Color.white);
        public static Color GetColorFromMessage(string Original, out string InnerText, out bool found)
        {
            if (Original.Length < 23)
            {
                InnerText = Original;
                found = false;
                return Color.white;
            }
            if (Original.StartsWith("<color=#") && Original[8] != '{' && Original.EndsWith("</color>"))
            {
                IEnumerator<char> characters = Original.Skip(8).GetEnumerator();
                int start = 8;
                int length = 0;
                while (characters.MoveNext())
                {
                    if (characters.Current == '>') break; // keep moving until the ending > is found.
                    length++;
                }
                characters.Dispose();
                int msgStart = start + length + 1;
                InnerText = Original.Substring(msgStart, Original.Length - msgStart - 8);
                found = true;
                return Original.Substring(start, length).Hex();
            }
            else
            {
                InnerText = Original;
                found = false;
                return Color.white;
            }
        }
        public override readonly string ToString() =>
            $"Original: {Original}, Inner text: {Message}, {(UseColor ? $"Color: {Color} ({ColorUtility.ToHtmlStringRGBA(Color)}." : "Unable to find color.")}";
    }
    /// <summary>Convert an HTMLColor string to a actual color.</summary>
    /// <param name="htmlColorCode">A hexadecimal/HTML color key.</param>
    internal static Color Hex(this string htmlColorCode)
    {
        string code = "#";
        if (htmlColorCode.Length > 0 && htmlColorCode[0] != '#')
            code += htmlColorCode;
        else
            code = htmlColorCode;
        if (ColorUtility.TryParseHtmlString(code, out Color color))
            return color;
        else if (ColorUtility.TryParseHtmlString(htmlColorCode, out color))
            return color;
        else return Color.white;
    }
}
