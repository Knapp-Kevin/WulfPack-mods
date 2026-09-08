using System;
using System.IO;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace WulfPack.RuneCompass;

/// <summary>
/// Applies edits to the config file without restarting the game.
/// </summary>
/// <remarks>
/// BepInEx 5 has no file watcher of its own — <c>ConfigFile.Reload()</c> exists but
/// nothing calls it — so a config edit would otherwise only take effect on the next
/// launch. For a HUD whose position, scale and opacity are meant to be tuned by eye, that
/// turns every adjustment into a relaunch plus a world load.
///
/// This polls the file's timestamp once a second and reloads on change.
/// <c>Reload()</c> pushes new values into the existing <c>ConfigEntry</c> objects, so the
/// controller's accessors pick them up on the next frame with no rewiring.
/// </remarks>
internal sealed class ConfigWatcher
{
    private const float PollSeconds = 1f;

    private readonly ConfigFile _config;
    private readonly ManualLogSource _log;
    private readonly string _path;

    private DateTime _lastWrite;
    private float _nextPoll;

    public ConfigWatcher(ConfigFile config, ManualLogSource log)
    {
        _config = config;
        _log = log;
        _path = config.ConfigFilePath;
        _lastWrite = ReadTimestamp();
    }

    public void Tick()
    {
        if (Time.unscaledTime < _nextPoll)
        {
            return;
        }

        _nextPoll = Time.unscaledTime + PollSeconds;

        DateTime current = ReadTimestamp();
        if (current == _lastWrite || current == default)
        {
            return;
        }

        _lastWrite = current;
        try
        {
            _config.Reload();
            _log.LogInfo("Rune Compass config reloaded.");
        }
        catch (Exception ex)
        {
            // A half-written file mid-save is the common case; the next poll picks it up.
            _log.LogWarning($"Rune Compass config reload failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private DateTime ReadTimestamp()
    {
        try
        {
            return File.Exists(_path) ? File.GetLastWriteTimeUtc(_path) : default;
        }
        catch (Exception)
        {
            return default;
        }
    }
}
