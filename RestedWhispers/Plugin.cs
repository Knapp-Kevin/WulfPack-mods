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
    private ConfigEntry<MessageHud.MessageType> _messagePosition = null!;

    private float _nextPollTime;
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

        _messagePosition = Config.Bind(
            "General",
            "MessagePosition",
            MessageHud.MessageType.Center,
            "Where notifications appear. Center is Valheim's large banner text; "
            + "TopLeft is the small corner text used for pickups.");

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
            // No expiry message here: Valheim already announces it through the
            // effect's own m_stopMessage ("You're no longer rested"). Clearing
            // state is what arms the warnings again for the next Rested cycle.
            ResetState();
            return;
        }

        // Permanent effect (m_ttl == 0): GetRemaningTime() goes negative and
        // would trip both thresholds at once.
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

    private void Notify(string message)
    {
        if (MessageHud.instance != null)
        {
            MessageHud.instance.ShowMessage(_messagePosition.Value, message);
        }
    }

    private void ResetState()
    {
        _firstWarningShown = false;
        _finalWarningShown = false;
    }
}
