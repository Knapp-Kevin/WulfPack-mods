using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace WulfPack.RestedWhispers;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.wulfpack.restedwhispers";
    public const string PluginName = "Rested Whispers";
    public const string PluginVersion = "0.1.0";

    private const float PollIntervalSeconds = 0.5f;

    private ConfigEntry<bool> _enabled = null!;
    private ConfigEntry<float> _firstWarningSeconds = null!;
    private ConfigEntry<float> _finalWarningSeconds = null!;
    private ConfigEntry<bool> _notifyOnExpiration = null!;

    private float _nextPollTime;
    private bool _wasRested;
    private bool _firstWarningShown;
    private bool _finalWarningShown;

    private void Awake()
    {
        _enabled = Config.Bind(
            "General",
            "Enabled",
            true,
            "Enable Rested Whispers notifications.");

        _firstWarningSeconds = Config.Bind(
            "Warnings",
            "FirstWarningSeconds",
            120f,
            "Remaining Rested time at which the first warning is shown.");

        _finalWarningSeconds = Config.Bind(
            "Warnings",
            "FinalWarningSeconds",
            30f,
            "Remaining Rested time at which the final warning is shown.");

        _notifyOnExpiration = Config.Bind(
            "Warnings",
            "NotifyOnExpiration",
            true,
            "Show a message when the Rested effect expires.");

        Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    private void Update()
    {
        if (!_enabled.Value)
        {
            ResetState();
            return;
        }

        if (Time.unscaledTime < _nextPollTime)
        {
            return;
        }

        _nextPollTime = Time.unscaledTime + PollIntervalSeconds;
        CheckRestedState();
    }

    private void CheckRestedState()
    {
        Player? player = Player.m_localPlayer;
        if (player == null)
        {
            ResetState();
            return;
        }

        StatusEffect? rested = player.GetSEMan().GetStatusEffect(SEMan.s_statusEffectRested);
        if (rested == null)
        {
            if (_wasRested && _notifyOnExpiration.Value)
            {
                Notify("You feel weary.");
            }

            ResetState();
            return;
        }

        if (!_wasRested)
        {
            _wasRested = true;
            _firstWarningShown = false;
            _finalWarningShown = false;
        }

        // Permanent effect (m_ttl == 0): GetRemaningTime() goes negative and
        // would trip both thresholds at once. Latch above is set first, so a
        // later real expiry still reports.
        if (rested.m_ttl <= 0f)
        {
            return;
        }

        EvaluateThresholds(rested);
    }

    private void EvaluateThresholds(StatusEffect rested)
    {
        float remaining = rested.GetRemaningTime();
        float finalThreshold = Mathf.Max(0f, _finalWarningSeconds.Value);
        float firstThreshold = Mathf.Max(finalThreshold, _firstWarningSeconds.Value);

        if (!_finalWarningShown && remaining <= finalThreshold)
        {
            _finalWarningShown = true;
            _firstWarningShown = true;
            Notify("You long for the warmth of a fire.");
            return;
        }

        if (!_firstWarningShown && remaining <= firstThreshold)
        {
            _firstWarningShown = true;
            Notify("You're getting tired.");
        }
    }

    private static void Notify(string message)
    {
        if (MessageHud.instance != null)
        {
            MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, message);
        }
    }

    private void ResetState()
    {
        _wasRested = false;
        _firstWarningShown = false;
        _finalWarningShown = false;
    }
}
