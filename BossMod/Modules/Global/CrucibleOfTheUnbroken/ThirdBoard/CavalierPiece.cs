namespace BossMod.Global.CrucibleOfTheUnbroken.ThirdBoard.CavalierPiece;

public enum OID : uint
{
    CavalierPiece = 0x4C8E,
    Helper = 0x233C,
    BoneBishop = 0x4C90, // R0.750, x0 (spawn during fight)
    BoneFragmentOrb = 0x4C92, // R1.000, x0 (spawn during fight)
    FeintedCavalierPiece = 0x4C8F, // R2.520, x5
}

public enum AID : uint
{
    AutoAttack = 49680, // CavalierPiece->player, no cast, single-target
    AutoAttackBlizzard = 48621, // 4C90->player, no cast, single-target
    SteelripperBoss = 48472, // CavalierPiece->self, 6.0+1.0s cast, single-target
    Steelripper = 48473, // Helper->self, 7.0s cast, range 60 130.000-degree cone
    Doubling = 48460, // CavalierPiece->self, 5.0s cast, single-target
    MenaceActor = 48463, // 4C8F->self, 5.4+0.6s cast, single-target
    Menace = 48464, // Helper->self, 6.0s cast, range 20 circle
    ValfodrActor = 48461, // 4C8F->self, 5.6+0.4s cast, single-target
    Valfodr = 48462, // Helper->self, 6.0s cast, range 60 width 8 rect
    CrushingBlade = 48471, // CavalierPiece->player, 5.0s cast, single-target
    FeintedCavalierTeleport = 50552, // 4C8F->CavalierPiece, no cast, single-target

    Unknown1 = 50551, // 4C92->CavalierPiece, no cast, single-target - most likely the orb teleport
}

public enum IconID : uint
{
    TankBusterKnockBack = 633, // CavalierPiece->player
}

public enum TetherID : uint
{
    DoublingTether = 398, // 4C92->CavalierPiece
}

sealed class Steelripper(BossModule module) : Components.SimpleAOEs(module, (uint)AID.Steelripper, new AOEShapeCone(60.0f, 65.0f.Degrees()));
sealed class Menace(BossModule module) : Components.SimpleAOEs(module, (uint)AID.Menace, 20.0f);
sealed class Valfodr(BossModule module) : Components.SimpleAOEs(module, (uint)AID.Valfodr, new AOEShapeRect(60.0f, 4.0f));

sealed class CrushingBlade(BossModule module) : Components.GenericKnockback(module)
{
    private const float KnockbackDistance = 15.0f;
    private BitMask affectedPlayers;
    private DateTime activation = default;
    private Actor? source = null;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.TankBusterKnockBack && Raid.FindSlot(targetID) is var slot && slot >= 0)
        {
            affectedPlayers[slot] = true;
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.CrushingBlade)
        {
            activation = Module.CastFinishAt(spell);
            source = caster;
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.CrushingBlade)
        {
            affectedPlayers.Reset();
        }
    }

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
    {
        if (affectedPlayers[slot] && activation != default && source != null)
        {
            return new Knockback[1] { new(source.Position, KnockbackDistance, activation) };
        }

        return [];
    }
}

sealed class CavalierPieceStates : StateMachineBuilder
{
    public CavalierPieceStates(BossModule module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Steelripper>()
            .ActivateOnEnter<Menace>()
            .ActivateOnEnter<Valfodr>()
            .ActivateOnEnter<CrushingBlade>();
    }
}

[ModuleInfo(BossModuleInfo.Maturity.WIP, PrimaryActorOID = (uint)OID.CavalierPiece, Contributors = "Equilius", GroupType = BossModuleInfo.GroupType.CrucibleOfTheUnbroken, GroupID = 1090u, NameID = 14564u, SortOrder = 2)]
public sealed class CavalierPiece(WorldState ws, Actor primary) : BossModule(ws, primary, new(120f, 0f), new ArenaBoundsSquare(20f))
{
    public override bool ShouldPrioritizeAllEnemies => true;

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.BoneBishop));
    }

    private readonly string[] _prePullHints =
    [
        "This fight is easy if you kill every pack wave together and before the 4th pack spawn otherwise it starts getting complicated."
    ];

    public override string[] PrePullHints => _prePullHints;
}
