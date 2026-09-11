namespace BossMod.Global.CrucibleOfTheUnbroken.ThirdBoard.CatoblepasPiece;

public enum OID : uint
{
    CatoblepasPiece = 0x4C9B,
    Helper = 0x233C,
    _Gen_Actor1ec0de = 0x1EC0DE, // R0.500, x1, EventObj type
    DemonicEyeCircle = 0x4C9C, // R1.500, x0 (spawn during fight)
    DemonicEyeDonut = 0x4C9D, // R1.500, x0 (spawn during fight)
}

public enum AID : uint
{
    AutoAttack = 49682, // CatoblepasPiece->player, no cast, single-target
    BestialRoar = 48504, // CatoblepasPiece->self, 3.0s cast, range 60 circle

    Farburst = 48507, // 4C9D->self, no cast, single-target
    Farburst1 = 48508, // Helper->self, 0.5s cast, range ?-50 donut
    _Weaponskill_Nearburst = 48505, // 4C9C->self, no cast, single-target
    _Weaponskill_Nearburst1 = 48506, // Helper->self, 0.5s cast, range 25 circle
    _Weaponskill_ShiftingGaze = 48509, // CatoblepasPiece->self, 3.0s cast, single-target
    _Weaponskill_ShiftingGaze1 = 48954, // CatoblepasPiece->4C9D/4C9C, no cast, single-target
}

public enum SID : uint
{
    _Gen_RampantHeart = 4596, // none->player, extra=0x0
    _Gen_ = 2056, // CatoblepasPiece->4C9D/4C9C, extra=0xAE
}

public enum TetherID : uint
{
    _Gen_Tether_chn_ice_mouth01x = 195, // 4C9D/4C9C->CatoblepasPiece
}

sealed class BestialRoar(BossModule module) : Components.RaidwideCast(module, (uint)AID.BestialRoar);

sealed class CatoblepasPieceStates : StateMachineBuilder
{
    public CatoblepasPieceStates(BossModule module) : base(module)
    {
        TrivialPhase();
    }
}

[ModuleInfo(BossModuleInfo.Maturity.Dummy,
    PrimaryActorOID = (uint)OID.CatoblepasPiece,
    Contributors = "Equilius",
    GroupType = BossModuleInfo.GroupType.CrucibleOfTheUnbroken,
    GroupID = 1090u,
    NameID = 14577u,
    SortOrder = 12)]
public sealed class CatoblepasPiece(WorldState ws, Actor primary) : BossModule(ws, primary, new(120f, -420f), new ArenaBoundsCircle(20f));
