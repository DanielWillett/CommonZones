using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CommonZones;

internal static class Chat
{
    /// <summary>
    /// Send a message in chat using the translation file.
    /// </summary>
    /// <param name="player"><see cref="UnturnedPlayer"/> to send the chat to.</param>
    /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
    /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
    /// <param name="textColor">The color of the chat.</param>
    /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
    internal static void SendChat(this UnturnedPlayer player, string text, Color textColor, params string[] formatting) =>
        SendChat(player.Player.channel.owner, text, textColor, formatting);
    /// <summary>
    /// Send a message in chat using the translation file.
    /// </summary>
    /// <param name="player"><see cref="UnturnedPlayer"/> to send the chat to.</param>
    /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
    /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
    /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
    internal static void SendChat(this UnturnedPlayer player, string text, params string[] formatting) =>
        SendChat(player.Player.channel.owner, text, formatting);
    /// <summary>
    /// Send a message in chat using the translation file.
    /// </summary>
    /// <param name="player"><see cref="Player"/> to send the chat to.</param>
    /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
    /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
    /// <param name="textColor">The color of the chat.</param>
    /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
    internal static void SendChat(this Player player, string text, Color textColor, params string[] formatting) =>
        SendChat(player.channel.owner, text, textColor, formatting);
    /// <summary>
    /// Send a message in chat using the translation file.
    /// </summary>
    /// <param name="player"><see cref="Player"/> to send the chat to.</param>
    /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
    /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
    /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
    internal static void SendChat(this Player player, string text, params string[] formatting) =>
        SendChat(player.channel.owner, text, formatting);
    /// <summary>
    /// Send a message in chat using the translation file.
    /// </summary>
    /// <param name="player"><see cref="SteamPlayer"/> to send the chat to.</param>
    /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
    /// </para><para>After localization, the chat message can only be &lt;2047 bytes, encoded in UTF-8 format.</para></param>
    /// <param name="textColor">The color of the chat.</param>
    /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
    internal static void SendChat(this SteamPlayer player, string text, Color textColor, params string[] formatting)
    {
        string localizedString = Translation.Translate(text, formatting);
        if (Encoding.UTF8.GetByteCount(localizedString) <= MAX_CHAT_MESSAGE_SIZE)
            ChatManager.say(player.playerID.steamID, localizedString, textColor, EChatMode.SAY, true);
        else
        {
            L.LogWarning($"'{localizedString}' is too long, sending default message instead, consider shortening your translation of {text}.");
            string newMessage;
            try
            {
                newMessage = string.Format(text, formatting);
            }
            catch (FormatException)
            {
                newMessage = text + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                L.LogWarning("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + text + "\"");
            }
            if (Encoding.UTF8.GetByteCount(newMessage) <= MAX_CHAT_MESSAGE_SIZE)
                ChatManager.say(player.playerID.steamID, newMessage, textColor, EChatMode.SAY, true);
            else
                L.LogError("There's been an error sending a chat message. Default message for \"" + text + "\" is longer than "
                    + MAX_CHAT_MESSAGE_SIZE.ToString(CommonZones.Locale) + " bytes in UTF-8. Arguments may be too long.");
        }
    }
    /// <summary>
    /// Send a message in chat using the translation file.
    /// </summary>
    /// <param name="player"><see cref="SteamPlayer"/> to send the chat to.</param>
    /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
    /// </para><para>After localization, the chat message can only be &lt;2047 bytes, encoded in UTF-8 format.</para></param>
    /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
    internal static void SendChat(this SteamPlayer player, string text, params string[] formatting)
    {
        string localizedString = Translation.Translate(text, out Color textColor, formatting);
        if (Encoding.UTF8.GetByteCount(localizedString) <= MAX_CHAT_MESSAGE_SIZE)
            ChatManager.say(player.playerID.steamID, localizedString, textColor, EChatMode.SAY, true);
        else
        {
            L.LogWarning($"'{localizedString}' is too long, sending default message instead, consider shortening your translation of {text}.");
            string newMessage;
            try
            {
                newMessage = string.Format(text, formatting);
            }
            catch (FormatException)
            {
                newMessage = text + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                L.LogWarning("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + text + "\"");
            }
            if (Encoding.UTF8.GetByteCount(newMessage) <= MAX_CHAT_MESSAGE_SIZE)
                ChatManager.say(player.playerID.steamID, newMessage, textColor, EChatMode.SAY, true);
            else
                L.LogError("There's been an error sending a chat message. Default message for \"" + text + "\" is longer than "
                    + MAX_CHAT_MESSAGE_SIZE.ToString(CommonZones.Locale) + " bytes in UTF-8. Arguments may be too long.");
        }
    }
    /// <summary>
    /// Max amount of bytes that can be sent in an Unturned Chat Message.
    /// </summary>
    const int MAX_CHAT_MESSAGE_SIZE = 2047;
    /// <summary>
    /// Send a message in chat using the translation file.
    /// </summary>
    /// <param name="player"><see cref="CSteamID"/> to send the chat to.</param>
    /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
    /// <param name="textColor">The color of the chat.</param>
    /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
    internal static void SendChat(this CSteamID player, string text, Color textColor, params string[] formatting)
    {
        string localizedString = Translation.Translate(text, formatting);
        if (Encoding.UTF8.GetByteCount(localizedString) <= MAX_CHAT_MESSAGE_SIZE)
            ChatManager.say(player, localizedString, textColor, EChatMode.SAY, true);
        else
        {
            L.LogWarning($"'{localizedString}' is too long, sending default message instead, consider shortening your translation of {text}.");
            string newMessage;
            try
            {
                newMessage = string.Format(text, formatting);
            }
            catch (FormatException)
            {
                newMessage = text + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                L.LogWarning("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + text + "\"");
            }
            if (Encoding.UTF8.GetByteCount(newMessage) <= MAX_CHAT_MESSAGE_SIZE)
                ChatManager.say(player, newMessage, textColor, EChatMode.SAY, true);
            else
                L.LogError("There's been an error sending a chat message. Default message for \"" + text + "\" is longer than "
                    + MAX_CHAT_MESSAGE_SIZE.ToString(CommonZones.Locale) + " bytes in UTF-8. Arguments may be too long.");
        }
    }
    /// <summary>
    /// Send a message in chat using the translation file, automatically extrapolates the color.
    /// </summary>
    /// <param name="player"><see cref="CSteamID"/> to send the chat to.</param>
    /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
    /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
    internal static void SendChat(this CSteamID player, string text, params string[] formatting)
    {
        string localizedString = Translation.Translate(text, out Color textColor, formatting);
        if (Encoding.UTF8.GetByteCount(localizedString) <= MAX_CHAT_MESSAGE_SIZE)
            ChatManager.say(player, localizedString, textColor, EChatMode.SAY, true);
        else
        {
            L.LogWarning($"'{localizedString}' is too long, sending default message instead, consider shortening your translation of {text}.");
            string newMessage;
            try
            {
                newMessage = string.Format(text, formatting);
            }
            catch (FormatException)
            {
                newMessage = text + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                L.LogWarning("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + text + "\"");
            }
            if (Encoding.UTF8.GetByteCount(newMessage) <= MAX_CHAT_MESSAGE_SIZE)
                ChatManager.say(player, newMessage, textColor, EChatMode.SAY, true);
            else
                L.LogError("There's been an error sending a chat message. Default message for \"" + text + "\" is longer than "
                    + MAX_CHAT_MESSAGE_SIZE.ToString(CommonZones.Locale) + " bytes in UTF-8. Arguments may be too long.");
        }
    }
    /// <summary>
    /// Send a message in chat to everyone.
    /// </summary>
    /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
    /// <param name="textColor">The color of the chat.</param>
    /// <param name="formatting">list of strings to replace the {#}s in the translations.</param>
    internal static void Broadcast(string text, Color textColor, params string[] formatting)
    {
        string localizedString = Translation.Translate(text, formatting);
        if (Encoding.UTF8.GetByteCount(localizedString) > MAX_CHAT_MESSAGE_SIZE)
        {
            L.LogWarning($"'{localizedString}' is too long, sending default message instead, consider shortening your translation of {text}.");
            try
            {
                localizedString = string.Format(text, formatting);
            }
            catch (FormatException)
            {
                localizedString = text + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                L.LogWarning("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + text + "\"");
            }
            if (Encoding.UTF8.GetByteCount(localizedString) > MAX_CHAT_MESSAGE_SIZE)
            {
                L.LogError("There's been an error sending a chat message. Default message for \"" + text + "\" is longer than "
                    + MAX_CHAT_MESSAGE_SIZE.ToString(CommonZones.Locale) + " bytes in UTF-8. Arguments may be too long.");
                return;
            }
        }
        for (int i = 0; i < Provider.clients.Count; ++i)
        {
            ChatManager.say(Provider.clients[i].playerID.steamID, localizedString, textColor, EChatMode.SAY, true);
        }
    }
    /// <summary>
    /// Send a message in chat to everyone in <paramref name="players"/>.
    /// </summary>
    /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
    /// <param name="textColor">The color of the chat.</param>
    /// <param name="formatting">list of strings to replace the {#}s in the translations.</param>
    /// <param name="players">Enumerator of players to send the message to.</param>
    internal static void Broadcast(IEnumerator<SteamPlayer> players, string text, Color textColor, params string[] formatting)
    {
        string localizedString = Translation.Translate(text, formatting);
        if (Encoding.UTF8.GetByteCount(localizedString) > MAX_CHAT_MESSAGE_SIZE)
        {
            L.LogWarning($"'{localizedString}' is too long, sending default message instead, consider shortening your translation of {text}.");
            try
            {
                localizedString = string.Format(text, formatting);
            }
            catch (FormatException)
            {
                localizedString = text + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                L.LogWarning("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + text + "\"");
            }
            if (Encoding.UTF8.GetByteCount(localizedString) > MAX_CHAT_MESSAGE_SIZE)
            {
                L.LogError("There's been an error sending a chat message. Default message for \"" + text + "\" is longer than "
                    + MAX_CHAT_MESSAGE_SIZE.ToString(CommonZones.Locale) + " bytes in UTF-8. Arguments may be too long.");
                return;
            }
        }
        if (players == null) players = Provider.clients.GetEnumerator();
        while (players.MoveNext())
        {
            ChatManager.say(players.Current.playerID.steamID, localizedString, textColor, EChatMode.SAY, true);
        }
        players.Dispose();
    }
    /// <summary>
    /// Send a message in chat to everyone.
    /// </summary>
    /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para>
    /// <para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
    /// <param name="formatting">list of strings to replace the {#}s in the translations.</param>
    internal static void Broadcast(string text, params string[] formatting)
    {
        string localizedString = Translation.Translate(text, out Color textColor, formatting);
        if (Encoding.UTF8.GetByteCount(localizedString) > MAX_CHAT_MESSAGE_SIZE)
        {
            L.LogWarning($"'{localizedString}' is too long, sending default message instead, consider shortening your translation of {text}.");
            try
            {
                localizedString = string.Format(text, formatting);
            }
            catch (FormatException)
            {
                localizedString = text + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                L.LogWarning("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + text + "\"");
            }
            if (Encoding.UTF8.GetByteCount(localizedString) > MAX_CHAT_MESSAGE_SIZE)
            {
                L.LogError("There's been an error sending a chat message. Default message for \"" + text + "\" is longer than "
                    + MAX_CHAT_MESSAGE_SIZE.ToString(CommonZones.Locale) + " bytes in UTF-8. Arguments may be too long.");
                return;
            }
        }
        for (int i = 0; i < Provider.clients.Count; ++i)
        {
            ChatManager.say(Provider.clients[i].playerID.steamID, localizedString, textColor, EChatMode.SAY, true);
        }
    }
    /// <summary>
    /// Send a message in chat to everyone in <paramref name="players"/>.
    /// </summary>
    /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para>
    /// <para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
    /// <param name="formatting">list of strings to replace the {#}s in the translations.</param>
    internal static void Broadcast(IEnumerator<SteamPlayer> players, string text, params string[] formatting)
    {
        string localizedString = Translation.Translate(text, out Color textColor, formatting);
        if (Encoding.UTF8.GetByteCount(localizedString) > MAX_CHAT_MESSAGE_SIZE)
        {
            L.LogWarning($"'{localizedString}' is too long, sending default message instead, consider shortening your translation of {text}.");
            try
            {
                localizedString = string.Format(text, formatting);
            }
            catch (FormatException)
            {
                localizedString = text + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                L.LogWarning("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + text + "\"");
            }
            if (Encoding.UTF8.GetByteCount(localizedString) > MAX_CHAT_MESSAGE_SIZE)
            {
                L.LogError("There's been an error sending a chat message. Default message for \"" + text + "\" is longer than "
                    + MAX_CHAT_MESSAGE_SIZE.ToString(CommonZones.Locale) + " bytes in UTF-8. Arguments may be too long.");
                return;
            }
        }
        if (players == null) players = Provider.clients.GetEnumerator();
        while (players.MoveNext())
        {
            ChatManager.say(players.Current.playerID.steamID, localizedString, textColor, EChatMode.SAY, true);
        }
        players.Dispose();
    }
    /// <summary>
    /// Send a message in chat to everyone except for those in the list of excluded <see cref="CSteamID"/>s.
    /// </summary>
    /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
    /// <param name="textColor">The color of the chat.</param>
    /// <param name="formatting">list of strings to replace the {#}s in the translations.</param>
    internal static void BroadcastToAllExcept(ulong[] excluded, string text, Color textColor, params string[] formatting)
    {
        string localizedString = Translation.Translate(text, formatting);
        if (Encoding.UTF8.GetByteCount(localizedString) > MAX_CHAT_MESSAGE_SIZE)
        {
            L.LogWarning($"'{localizedString}' is too long, sending default message instead, consider shortening your translation of {text}.");
            try
            {
                localizedString = string.Format(text, formatting);
            }
            catch (FormatException)
            {
                localizedString = text + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                L.LogWarning("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + text + "\"");
            }
            if (Encoding.UTF8.GetByteCount(localizedString) > MAX_CHAT_MESSAGE_SIZE)
            {
                L.LogError("There's been an error sending a chat message. Default message for \"" + text + "\" is longer than "
                    + MAX_CHAT_MESSAGE_SIZE.ToString(CommonZones.Locale) + " bytes in UTF-8. Arguments may be too long.");
                return;
            }
        }
        bool a = false;
        for (int i = 0; i < Provider.clients.Count; ++i)
        {
            SteamPlayer sp = Provider.clients[i];
            for (int j = 0; i < excluded.Length; i++)
            {
                if (excluded[j] == sp.playerID.steamID.m_SteamID) a = true;
            }
            if (a)
                a = false;
            else
                ChatManager.say(sp.playerID.steamID, localizedString, textColor, EChatMode.SAY, true);
        }
    }
    /// <summary>
    /// Send a message in chat to everyone except for those in the list of excluded <see cref="CSteamID"/>s.
    /// </summary>
    /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
    /// <param name="textColor">The color of the chat.</param>
    /// <param name="formatting">list of strings to replace the {#}s in the translations.</param>
    internal static void BroadcastToAllExcept(ulong[] excluded, string text, params string[] formatting)
    {
        string localizedString = Translation.Translate(text, out Color textColor, formatting);
        if (Encoding.UTF8.GetByteCount(localizedString) > MAX_CHAT_MESSAGE_SIZE)
        {
            L.LogWarning($"'{localizedString}' is too long, sending default message instead, consider shortening your translation of {text}.");
            try
            {
                localizedString = string.Format(text, formatting);
            }
            catch (FormatException)
            {
                localizedString = text + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                L.LogWarning("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + text + "\"");
            }
            if (Encoding.UTF8.GetByteCount(localizedString) > MAX_CHAT_MESSAGE_SIZE)
            {
                L.LogError("There's been an error sending a chat message. Default message for \"" + text + "\" is longer than "
                    + MAX_CHAT_MESSAGE_SIZE.ToString(CommonZones.Locale) + " bytes in UTF-8. Arguments may be too long.");
                return;
            }
        }
        bool a = false;
        for (int i = 0; i < Provider.clients.Count; ++i)
        {
            SteamPlayer sp = Provider.clients[i];
            for (int j = 0; i < excluded.Length; i++)
            {
                if (excluded[j] == sp.playerID.steamID.m_SteamID) a = true;
            }
            if (a)
                a = false;
            else
                ChatManager.say(sp.playerID.steamID, localizedString, textColor, EChatMode.SAY, true);
        }
    }
}