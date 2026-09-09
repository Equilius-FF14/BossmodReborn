namespace BossMod.Dawntrail.BeastMaster.ThirdBoard.YmirPiece;

public enum OID : uint {
    YmirPiece = 0x4C93,
    Helper = 0x233C,
    SahaginPiece = 0x4C95, // R2.000, x1
    YmirShell = 0x4C94, // R2.000, x1, Part type
}

public enum AID : uint {
    // Sahagin
    AutoAttackWater = 48626, // 4C95->player, no cast, single-target
    Teleport = 48484, // SahaginPiece->location, no cast, single-target
    WaterIIBoss = 48482, // 4C95->self, 3.0s cast, single-target
    WaterII = 48483, // Helper->location, 3.0s cast, range 6 circle
    TsunamiBoss = 48480, // 4C95->self, 8.0s cast, single-target
    Tsunami = 48481, // Helper->self, 8.0s cast, range 60 width 60 rect
    ParalyzingSpikes = 50532, // 4C95->self, 3.0s cast, single-target
    Dreadwash = 48485, // SahaginPiece->self, 8.0s cast, range 30 circle

    // Ymir
    AutoAttackHeadSnatch = 48477, // YmirPiece->self, no cast, range 7 ?-degree cone
    BlanketThunder = 48479, // YmirPiece->self, 5.0s cast, range 40 circle
}

public enum SID : uint {
    VulnerabilityDown = 2198,
    ParalyzingSpikes = 5434, // none->4C95, extra=0x64
}

public enum TetherID : uint {
    ParalyzingSpikesTether = 6, // 4C95->YmirPiece
}

sealed class Hint(BossModule module) : BossComponent(module) {
    public override void AddGlobalHints(Actor actor, GlobalHints hints) {
        hints.Add("This fight is easy, break shell, kill the snail then kill the 2nd boss.\n" +
                  "An interrupt is nice to prevent the Dreadwash spell, but not needed and you can just use your pet to take the damage down.");
    }
}

sealed class WaterII(BossModule module) : Components.SimpleAOEs(module, (uint)AID.WaterII, 6.0f);
sealed class Tsunami(BossModule module) : Components.SimpleKnockbacks(module, (uint)AID.Tsunami, 35.0f);
sealed class BlanketThunder(BossModule module) : Components.RaidwideCast(module, (uint)AID.BlanketThunder);
sealed class Dreadwash(BossModule module) : Components.CastInterruptHint(module, (uint)AID.Dreadwash);

sealed class ParalyzingSpikes(BossModule module) : Components.GenericInvincible(module, "Attacking boss with spikes debuff!") {
    private readonly List<Actor> avoidBosses = [];

    public override void OnCastStarted(Actor caster, ActorCastInfo spell) {
        if (spell.Action.ID == (uint)AID.ParalyzingSpikes) {
            avoidBosses.Add(caster);
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status) {
        if (status.ID == (uint)SID.ParalyzingSpikes) {
            avoidBosses.Remove(actor);
        }
    }

    protected override ReadOnlySpan<Actor> ForbiddenTargets(int slot, Actor actor) => CollectionsMarshal.AsSpan(avoidBosses);
}

sealed class VulnDown(BossModule module) : Components.GenericInvincible(module) {
    private readonly List<Actor> avoidBosses = [];

    public override void OnStatusGain(Actor actor, ref ActorStatus status) {
        if (status.ID == (uint)SID.VulnerabilityDown) {
            avoidBosses.Add(actor);
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.VulnerabilityDown) {
            avoidBosses.Remove(actor);
        }
    }

    protected override ReadOnlySpan<Actor> ForbiddenTargets(int slot, Actor actor) => CollectionsMarshal.AsSpan(avoidBosses);
}

sealed class HeadSnatch(BossModule module) : Components.Cleave(module, (uint)AID.AutoAttackHeadSnatch, new AOEShapeCone(7.0f, 15.0f.Degrees())) {
    private bool active = true;

    public override void OnStatusGain(Actor actor, ref ActorStatus status) {
        if (status.ID == (uint)SID.VulnerabilityDown) {
            active = true;
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status) {
        if (status.ID == (uint)SID.VulnerabilityDown) {
            active = false;
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints) {
        if (!active) {
            return;
        }

        base.AddHints(slot, actor, hints);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        if (!active) {
            return;
        }

        base.AddAIHints(slot, actor, assignment, hints);
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc) {
        if (!active) {
            return;
        }

        base.DrawArenaForeground(pcSlot, pc);
    }
}

[SkipLocalsInit]
sealed class YmirPieceStates : StateMachineBuilder {
    public YmirPieceStates(BossModule module) : base(module) {
        TrivialPhase()
            .DeactivateOnEnter<Hint>()
            .ActivateOnEnter<WaterII>()
            .ActivateOnEnter<Tsunami>()
            .ActivateOnEnter<HeadSnatch>()
            .ActivateOnEnter<BlanketThunder>()
            .ActivateOnEnter<ParalyzingSpikes>()
            .ActivateOnEnter<VulnDown>()
            .ActivateOnEnter<Dreadwash>()
            .Raw.Update = () => AllDeadOrDestroyed(YmirPiece.Bosses);
    }
}

[ModuleInfo(BossModuleInfo.Maturity.WIP,
    PrimaryActorOID = (uint)OID.YmirPiece,
    Contributors = "Equilius",
    Category = BossModuleInfo.Category.BeastMaster,
    GroupType = BossModuleInfo.GroupType.CFC,
    GroupID = 1090u,
    NameID = 14569u,
    SortOrder = 11)]
[SkipLocalsInit]
public sealed class YmirPiece : BossModule {
    public static readonly uint[] Bosses = [(uint)OID.YmirPiece, (uint)OID.SahaginPiece];

    public YmirPiece(WorldState ws, Actor primary) : base(ws, primary, new(120f, 0f), new ArenaBoundsRect(20f, 20f)) {
        ActivateComponent<Hint>();
    }

    protected override void DrawEnemies(int pcSlot, Actor pc) {
        Arena.Actors(this, Bosses);
    }
}
