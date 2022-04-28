using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CommonZones;

public static class L
{
    internal delegate void OutputToConsole(string value, ConsoleColor color);
    internal static OutputToConsole? OutputToConsoleMethod;
    internal static ICommandInputOutput? defaultIOHandler;
    internal static void LoadColoredConsole()
    {
        try
        {
            FieldInfo defaultIoHandlerFieldInfo = typeof(CommandWindow).GetField("defaultIOHandler", BindingFlags.Instance | BindingFlags.NonPublic);
            if (defaultIoHandlerFieldInfo != null)
            {
                defaultIOHandler = (ICommandInputOutput)defaultIoHandlerFieldInfo.GetValue(Dedicator.commandWindow);
                MethodInfo appendConsoleMethod = defaultIOHandler.GetType().GetMethod("outputToConsole", BindingFlags.NonPublic | BindingFlags.Instance);
                if (appendConsoleMethod != null)
                {
                    OutputToConsoleMethod = (OutputToConsole)appendConsoleMethod.CreateDelegate(typeof(OutputToConsole), defaultIOHandler);
                }
            }
        }
        catch (Exception ex)
        {
            CommandWindow.LogError("Couldn't get defaultIOHandler from CommandWindow:");
            CommandWindow.LogError(ex);
        }
    }
    private static void AddLine(string text, ConsoleColor color)
    {
        if (OutputToConsoleMethod != null)
        {
            OutputToConsoleMethod.Invoke(text, color);
            return;
        }
        switch (color)
        {
            case ConsoleColor.Gray:
            default:
                CommandWindow.Log(text);
                break;
            case ConsoleColor.Yellow:
                CommandWindow.LogWarning(text);
                break;
            case ConsoleColor.Red:
                CommandWindow.LogError(text);
                break;
        }
    }
    public static void Log(string info, ConsoleColor color = ConsoleColor.Gray)
    {
        try
        {
            string msg = "[CommonZones] " + info;
            if (OutputToConsoleMethod == null)
            {
                Rocket.Core.Logging.Logger.Log(info);
            }
            else
            {
                AddLine(msg, color);
                UnturnedLog.info($"[IN] {msg}");
                Rocket.Core.Logging.AsyncLoggerQueue.Current?.Enqueue(new Rocket.Core.Logging.LogEntry() { Message = info, RCON = true, Severity = Rocket.Core.Logging.ELogType.Info });
            }
        }
        catch (Exception ex)
        {
            CommandWindow.Log(info);
            LogError(ex);
        }
    }
    internal static void LogWarningEventCall(string warning, ConsoleColor color) => LogWarning(warning, color, "UncreatedNetworking");
    public static void LogWarning(string warning, ConsoleColor color = ConsoleColor.Yellow, [CallerMemberName] string method = "")
    {
        try
        {
            string msg = "[CommonZones] [" + method.ToUpper() + "] " + warning;
            if (OutputToConsoleMethod == null)
            {
                Rocket.Core.Logging.Logger.LogWarning(msg);
            }
            else
            {
                AddLine(msg, color);
                UnturnedLog.warn("[WA] " + msg);
                Rocket.Core.Logging.AsyncLoggerQueue.Current?.Enqueue(new Rocket.Core.Logging.LogEntry() { Message = msg, RCON = true, Severity = Rocket.Core.Logging.ELogType.Warning });
            }
        }
        catch (Exception ex)
        {
            CommandWindow.LogWarning(warning);
            LogError(ex);
        }
    }
    public static void LogError(string error, ConsoleColor color = ConsoleColor.Red, [CallerMemberName] string method = "")
    {
        try
        {
            string msg = "[CommonZones] [" + method.ToUpper() + "] " + error;
            if (OutputToConsoleMethod == null)
            {
                Rocket.Core.Logging.Logger.LogError(error);
            }
            else
            {
                AddLine(msg, color);
                UnturnedLog.warn("[ER] " + msg);
                Rocket.Core.Logging.AsyncLoggerQueue.Current?.Enqueue(new Rocket.Core.Logging.LogEntry() { Message = msg, RCON = true, Severity = Rocket.Core.Logging.ELogType.Error });
            }
        }
        catch (Exception ex)
        {
            CommandWindow.LogError(error);
            UnturnedLog.error(ex);
        }
    }
    internal static void LogErrorEventCall(Exception ex, ConsoleColor color) => LogError(ex, color, "UncreatedNetworking", "unknown", 0);
    public static void LogError(Exception ex, ConsoleColor color = ConsoleColor.Red, [CallerMemberName] string method = "", [CallerFilePath] string filepath = "", [CallerLineNumber] int ln = 0)
    {
        string message = $"[CommonZones] EXCEPTION - {ex.GetType().Name}\nSource: {filepath}::{method}( ... ) LN# {ln}\n\n{ex.Message}\n{ex.StackTrace}\n\nFINISHED";
        try
        {
            if (OutputToConsoleMethod == null)
            {
                Rocket.Core.Logging.Logger.LogError(message);
            }
            else
            {
                AddLine(message, color);
                UnturnedLog.warn($"[EX] {ex.Message}");
                UnturnedLog.warn($"[ST] {ex.StackTrace}");
                Rocket.Core.Logging.AsyncLoggerQueue.Current?.Enqueue(new Rocket.Core.Logging.LogEntry() { Message = message, RCON = true, Severity = Rocket.Core.Logging.ELogType.Exception });
            }
        }
        catch (Exception ex2)
        {
            Rocket.Core.Logging.Logger.LogError($"{message}\nEXCEPTION LOGGING \n\n{ex2.Message}\n{ex2.StackTrace}\n\nFINISHED");
        }
        if (ex.InnerException != null && ex.InnerException.InnerException == null)
        {
            LogError("INNER EXCEPTION: ", method: method);
            LogError(ex.InnerException, method: method, filepath: filepath, ln: ln);
        }
    }
}