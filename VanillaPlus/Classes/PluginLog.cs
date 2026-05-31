using System;
using System.Runtime.CompilerServices;
using Dalamud.Plugin.Services;

namespace VanillaPlus.Classes;

/// <summary>
/// Wrapper class around IPluginLog, mostly to allow .Debug(string message, string tag) to work.
/// </summary>
public ref struct PluginLog(IPluginLog log) {
    public void Exception(Exception e, [CallerMemberName] string caller = "")
        => log.Exception(e, caller);

    public void Debug(string message, string tag)
        => log.Debug($"[{tag}] {message}");

    public void Warning(string message, string tag)
        => log.Warning($"[{tag}] {message}");

    public void Info(string message, string tag)
        => log.Info($"[{tag}] {message}");

    public void Error(Exception e, string tag)
        => log.Error(e, $"[{tag}] {e.Message}");

    //
    // Internal log functions are not meant to be called by individual game modifications. You should use the tagged variants.
    //

    public void InternalError(string message)
        => log.Error(message);

    public void InternalError(Exception e, string message)
        => log.Error(e, message);

    public void InternalDebug(string message)
        => log.Debug(message);

    public void InternalWarning(string message)
        => log.Warning(message);

    public void InternalInfo(string message)
        => log.Info(message);
}
