using BepInEx.Configuration;

namespace WulfPack.RuneCompass;

/// <summary>
/// The <c>[Storm]</c> configuration section: what counts as a storm, how hard it grips, and
/// how completely it takes each layer over.
/// </summary>
/// <remarks>
/// Split from <see cref="CompassConfig"/> when that file crossed the 250-line Section 4
/// limit. The seam is a real one rather than an arbitrary cut: everything here concerns
/// weather, and nothing here is read outside a storm.
///
/// <para>Clamps live on <see cref="CompassSettings"/> rather than here, so a value is bounded
/// wherever it is read rather than only where it happens to be bound.</para>
/// </remarks>
internal sealed class StormConfig
{
    private readonly ConfigEntry<bool> _enabled;
    private readonly ConfigEntry<string> _environments;
    private readonly ConfigEntry<string> _biomes;
    private readonly ConfigEntry<float> _turbulence;
    private readonly ConfigEntry<float> _attack;
    private readonly ConfigEntry<float> _release;
    private readonly ConfigEntry<bool> _independentLayers;
    private readonly ConfigEntry<float> _amplitudeCamera;
    private readonly ConfigEntry<float> _amplitudeArrow;

    public StormConfig(ConfigFile config)
    {
        (_enabled, _environments, _turbulence, _attack, _independentLayers) = Bind(config);
        _release = config.Bind(
            "Storm", "InterferenceReleaseSeconds", 12f,
            "Seconds for the storm to let go once the weather clears. Deliberately longer "
            + "than the attack: a storm's grip should slacken more slowly than it takes "
            + "hold, and unlike the attack there is nothing to hide here, so this can be as "
            + "long as you like. Clamped to a minimum of 0.5.");
        _biomes = config.Bind(
            "Storm", "StormBiomes", "Mistlands",
            "Comma-separated biomes where the compass behaves as though it were storming, "
            + "whatever the weather. The Mistlands is the reason this exists: it is hostile "
            + "to an instrument as a place rather than as a forecast, so no weather name "
            + "could express it. Valid names: Meadows, Swamp, Mountain, BlackForest, Plains, "
            + "AshLands, DeepNorth, Ocean, Mistlands. Leave empty to disable. NOTE that a "
            + "biome is permanent, so interference there is constant rather than passing.");
        _amplitudeCamera = BindAmplitude(
            config, "AmplitudeCamera", 0.85f,
            "the camera-view sector - background treatment, disturbed without drawing the eye");
        _amplitudeArrow = BindAmplitude(
            config, "AmplitudeArrow", 1f,
            "the character-facing arrow - the primary read, and against a still dial the "
            + "layer whose disturbance is most legible");
    }

    /// <summary>Fills in the storm half of the live settings.</summary>
    public void Apply(CompassSettings settings)
    {
        settings.InterferenceEnabled = () => _enabled.Value;
        settings.StormEnvironments = () => CompassSettings.ParseNames(_environments.Value);
        settings.StormBiomes = () => (Heightmap.Biome)CompassSettings.ParseBiomes(_biomes.Value);
        settings.StormTurbulence = () => CompassSettings.ClampTurbulence(_turbulence.Value);
        settings.InterferenceRampSeconds = () => CompassSettings.ClampRamp(_attack.Value);
        settings.InterferenceReleaseSeconds =
            () => CompassSettings.ClampRelease(_release.Value);
        settings.IndependentLayerInterference = () => _independentLayers.Value;
        settings.AmplitudeCamera = () => CompassSettings.ClampAmplitude(_amplitudeCamera.Value);
        settings.AmplitudeArrow = () => CompassSettings.ClampAmplitude(_amplitudeArrow.Value);
    }

    /// <summary>
    /// Storm detection and the two bounded values. Valheim exposes no storm flag of any
    /// kind, so storm state is composed from the environment name - which is what the game
    /// does internally too.
    /// </summary>
    private static StormEntries Bind(ConfigFile config)
    {
        ConfigEntry<bool> enabled = config.Bind(
            "Storm", "InterferenceEnabled", true,
            "Let storms disturb the compass. Set false to keep it accurate in all weather.");
        ConfigEntry<string> environments = config.Bind(
            "Storm", "StormEnvironments", "ThunderStorm",
            "Comma-separated environment names treated as storms. Ships with only "
            + "ThunderStorm, the one name confirmed present in the game's own code; every "
            + "other environment name lives in compressed asset data and would be a guess. "
            + "On the first world load Rune Compass logs every environment your install "
            + "actually has, with its wind range - add the stormy ones here from that list.");
        ConfigEntry<float> turbulence = config.Bind(
            "Storm", "StormTurbulence", 1f,
            "How violent a storm's grip is: scales the drift, wander and lurching together. "
            + "1 is a proper storm; below 1 is unsettled; above 1 gets wild. Clamped to 3. "
            + "This does not nudge the compass off true - during a storm the pointers are "
            + "CAPTURED, showing where the storm's field points rather than where you face, "
            + "so turning will not recover your direction.");
        ConfigEntry<float> ramp = config.Bind(
            "Storm", "InterferenceRampSeconds", 4f,
            "Seconds for interference to reach full strength, and to fade again. Clamped to "
            + "a minimum of 3: Valheim switches the environment name at the START of a "
            + "weather transition, so a fast ramp would make the compass react before the "
            + "sky does and turn it into a storm early-warning device. Raise this if "
            + "interference arrives ahead of the weather.");
        ConfigEntry<bool> independent = config.Bind(
            "Storm", "IndependentLayerInterference", true,
            "true: each disturbed layer is captured toward its OWN storm bearing, so the "
            + "pointers disagree with each other as well as with the world. false: all of "
            + "them are dragged toward one storm bearing, so they stay in agreement while "
            + "lying together. Neither is more correct; pick the one you prefer.");

        return new StormEntries(enabled, environments, turbulence, ramp, independent);
    }

    /// <summary>
    /// The storm entries, returned so the constructor performs every assignment itself.
    /// </summary>
    /// <remarks>
    /// C# only honours <c>readonly</c> for assignments made in the constructor body, so a
    /// helper that assigned the fields directly would have forced the fields open and raised
    /// five CS8618 nullability warnings against a build that holds itself to zero.
    /// </remarks>
    private readonly struct StormEntries
    {
        public readonly ConfigEntry<bool> Enabled;
        public readonly ConfigEntry<string> Environments;
        public readonly ConfigEntry<float> Turbulence;
        public readonly ConfigEntry<float> Ramp;
        public readonly ConfigEntry<bool> Independent;

        public StormEntries(
            ConfigEntry<bool> enabled,
            ConfigEntry<string> environments,
            ConfigEntry<float> turbulence,
            ConfigEntry<float> ramp,
            ConfigEntry<bool> independent)
        {
            Enabled = enabled;
            Environments = environments;
            Turbulence = turbulence;
            Ramp = ramp;
            Independent = independent;
        }

        public void Deconstruct(
            out ConfigEntry<bool> enabled,
            out ConfigEntry<string> environments,
            out ConfigEntry<float> turbulence,
            out ConfigEntry<float> ramp,
            out ConfigEntry<bool> independent)
        {
            enabled = Enabled;
            environments = Environments;
            turbulence = Turbulence;
            ramp = Ramp;
            independent = Independent;
        }
    }

    /// <summary>
    /// One per-layer interference amplitude, 0 to 1, tunable live so a storm can be
    /// balanced by eye without a rebuild.
    /// </summary>
    /// <remarks>
    /// <b>There is deliberately no key for the wind rune.</b> Its immunity is structural -
    /// <see cref="DirectionLayer.Immune"/> returns a literal zero - and a config key would
    /// turn that invariant into a value any edit could set to 0.3.
    /// </remarks>
    private static ConfigEntry<float> BindAmplitude(
        ConfigFile config, string key, float fallback, string what)
    {
        return config.Bind(
            "Storm", key, fallback,
            $"How completely a storm captures {what}. 0 leaves the layer perfectly true "
            + "whatever the weather; 1 hands it over entirely, so it shows where the storm "
            + "points rather than where you face. Clamped to 0-1.");
    }
}
