using Dalamud.Bindings.ImGui;

namespace BossMod;

public sealed class BossModuleConfigWindow : UIWindow
{
    private readonly BossModuleRegistry.Info _info;
    private readonly ConfigNode? _node;
    private readonly PartyRolesConfig _prc = Service.Config.Get<PartyRolesConfig>();
    private readonly WorldState _ws;
    private readonly UITree _tree = new();
    private readonly UITabs _tabs = new("ConfigTabs");

    public BossModuleConfigWindow(BossModuleRegistry.Info info, WorldState ws) : base($"{info.ModuleType.Name} config", true, new(1200, 800))
    {
        _info = info;
        _node = info.ConfigType != null ? Service.Config.Get<ConfigNode>(info.ConfigType) : null;
        _ws = ws;
        _tabs.Add("Encounter-specific config", DrawEncounterTab);
        _tabs.Add("Party roles assignment", DrawPartyRolesAssignmentsTab);
    }

    public override void Draw() => _tabs.Draw();

    private void DrawEncounterTab()
    {
        if (_info.HasPrePullHints)
        {
            var showPrePullHints = BossModuleManager.Config.ShowPrePullHintsFor(_info.PrimaryActorOID);
            if (ImGui.Checkbox("Show pre-fight hint popup for this encounter", ref showPrePullHints))
            {
                BossModuleManager.Config.SetShowPrePullHintsFor(_info.PrimaryActorOID, showPrePullHints);
            }

            if (!BossModuleManager.Config.ShowPrePullHints)
            {
                ImGui.SameLine();
                ImGui.TextDisabled("(globally disabled)");
            }

            if (_node != null)
            {
                ImGui.Separator();
            }
        }

        if (_node != null)
        {
            ConfigUI.DrawNode(_node, Service.Config, _tree, _ws);
        }
        else if (!_info.HasPrePullHints)
        {
            ImGui.TextUnformatted("This module does not expose any additional configuration");
        }
    }

    private void DrawPartyRolesAssignmentsTab()
    {
        if (_ws.Party.Player() != null)
        {
            ConfigUI.DrawNode(_prc, Service.Config, _tree, _ws);
        }
    }
}
