using osu.Game.Configuration;
using osu.Game.Rulesets.Configuration;

namespace osu.Game.Rulesets.Diva.Configuration
{
    public partial class DivaRulesetConfigManager : RulesetConfigManager<DivaRulesetSettings>
    {
        public DivaRulesetConfigManager(SettingsStore settings, RulesetInfo ruleset, int? variant = null)
            : base(settings, ruleset, variant)
        {
        }

        protected override void InitialiseDefaults()
        {
            base.InitialiseDefaults();

            SetDefault(DivaRulesetSettings.UseXBoxButtons, false);
            SetDefault(DivaRulesetSettings.EnableVisualBursts, true);
            SetDefault(DivaRulesetSettings.NoteSize, 40.0, 24.0, 64.0, 1.0);
            SetDefault(DivaRulesetSettings.ApproachDuration, 1800.0, 1200.0, 3000.0, 50.0);
        }
    }

    public enum DivaRulesetSettings
    {
        UseXBoxButtons,
        EnableVisualBursts,
        NoteSize,
        ApproachDuration,
    }
}
