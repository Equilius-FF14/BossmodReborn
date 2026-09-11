namespace BossMod.Global.CrucibleOfTheUnbroken.ThirdBoard.LakhamuPiece;

public enum OID : uint {
    LakhamuPiece = 0x4C9E,
    Helper = 0x233C,
    GolemPiece = 0x4C9F, // R2.200, x0 (spawn during fight)
    SandSphere = 0x4CA0, // R1.800, x0 (spawn during fight)
}

public enum AID : uint {
    AutoAttackStone = 50792, // LakhamuPiece->player, no cast, single-target
    AutoAttackGolem = 50398, // 4C9F->player, no cast, single-target
    EarthrenderBoss = 48559, // LakhamuPiece->self, 4.0s cast, single-target
    Earthrender = 48560, // Helper->location, 3.0s cast, range 6 circle
    RockslideGolem = 48554, // 4C9F->self, 6.0s cast, single-target
    Rockslide = 48555, // Helper->self, 7.0s cast, range 45 width 10 rect
    SandTempest = 48561, // LakhamuPiece->self, 5.0s cast, range 60 circle
    Burst = 48562, // 4CA0->self, 3.0s cast, range 12 circle
    EarthShakerBoss = 48557, // LakhamuPiece->self, 4.0+0.2s cast, single-target
    EarthShaker = 48558, // Helper->self, no cast, range 60 50-degree cone // TODO degree is a guess
    LandslipBoss = 48553, // LakhamuPiece->self, 7.5+1.0s cast, single-target // Actors facing direction will do a rect and the direction they look is the knockback
    Landslip = 48556, // Helper->self, 8.0s cast, range 45 width 10 rect
}

public enum SID : uint {
    EarthResistanceDown = 5025, // Helper->player, extra=0x1
    Blind = 5389, // LakhamuPiece->player, extra=0x0
}

public enum IconID : uint {
    EarthShake = 40, // player/4A09/49F0->self
}

sealed class Earthrender(BossModule module) : Components.SimpleAOEs(module, (uint)AID.Earthrender, 6.0f);
sealed class Rockslide(BossModule module) : Components.SimpleAOEs(module, (uint)AID.Rockslide, new AOEShapeRect(45.0f, 5.0f));
sealed class SandTempest(BossModule module) : Components.RaidwideCast(module, (uint)AID.SandTempest);
sealed class EarthShaker(BossModule module) : Components.BaitAwayIcon(module, new AOEShapeCone(60.0f, 25.0f.Degrees()), (uint)IconID.EarthShake,
    (uint)AID.EarthShaker, 3.3f);

sealed class Burst(BossModule module) : Components.GenericAOEs(module) {
    private readonly List<AOEInstance> aoes = [];
    private readonly AOEShapeCircle shape = new(12.0f);

    public override void OnActorCreated(Actor actor) {
        if (actor.OID == (uint)OID.SandSphere) {
            aoes.Add(new(shape, actor.Position, actor.Rotation, WorldState.FutureTime(5.9f)));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell) {
        if (spell.Action.ID == (uint)AID.Burst) {
            if (aoes.Count > 0) {
                aoes.RemoveAt(0);
            }
        }
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(aoes);
}

sealed class Landslip(BossModule module) : Components.SimpleKnockbacks(module, (uint)AID.Landslip, 20.0f, shape: new AOEShapeRect(45.0f, 5.0f), maxCasts: 1) {
    private readonly Rockslide? rockslide = module.FindComponent<Rockslide>();

    // Grid map
    private const float tileSize = 10.0f;
    private const float tileHalf = tileSize * 0.5f;
    private const int tiles = tilesFromCenter * 2;
    private const int tilesFromCenter = 2;

    public override void DrawArenaForeground(int pcSlot, Actor pc) {
        base.DrawArenaForeground(pcSlot, pc);

        if (rockslide == null || rockslide.Casters.Count == 0 || Casters.Count == 0) {
            return;
        }

        for (var y = 0; y < tiles; y++) {
            for (var x = 0; x < tiles; x++) {
                // Checking each tile
                var tile = Arena.Center + new WDir((x - tilesFromCenter + 0.5f) * tileSize, (y - tilesFromCenter + 0.5f) * tileSize); // center of tile

                // Checking the knockback against the tile
                foreach (var knockback in Casters) {
                    if (knockback.Shape == null || !knockback.Shape.Check(tile, knockback.Origin, knockback.Direction)) {
                        continue;
                    }

                    var destination = tile + knockback.Distance * knockback.Direction.ToDirection();
                    if (!Arena.InBounds(destination)) {
                        continue;
                    }

                    var tileSafe = true; // We have to check every rockslide cast since they can overlap
                    foreach (var aoe in rockslide.Casters) {
                        if (aoe.Check(destination)) {
                            tileSafe = false;
                            break;
                        }
                    }

                    if (tileSafe) {
                        Arena.ZoneRect(tile, new WDir(0, 1), tileHalf, tileHalf, tileHalf, Colors.SafeFromAOE);
                    }
                }
            }
        }
    }
}

sealed class LakhamuPieceStates : StateMachineBuilder {
    public LakhamuPieceStates(BossModule module) : base(module) {
        TrivialPhase()
            .ActivateOnEnter<Rockslide>()
            .ActivateOnEnter<Landslip>()
            .ActivateOnEnter<SandTempest>()
            .ActivateOnEnter<Burst>()
            .ActivateOnEnter<Earthrender>()
            .ActivateOnEnter<EarthShaker>();
    }
}

[ModuleInfo(BossModuleInfo.Maturity.WIP,
    PrimaryActorOID = (uint)OID.LakhamuPiece,
    Contributors = "Equilius",
    Category = BossModuleInfo.Category.CrucibleOfTheUnbroken,
    GroupType = BossModuleInfo.GroupType.CFC,
    GroupID = 1090u,
    NameID = 14580u,
    SortOrder = 13)]
public sealed class LakhamuPiece(WorldState ws, Actor primary) : BossModule(ws, primary, new(120f, 0f), new ArenaBoundsRect(20f, 20f));
