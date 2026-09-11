namespace BossMod.Stormblood.Ultimate.UCOB;

sealed class P3HeavensfallTrio(BossModule module) : BossComponent(module)
{
    private Actor? _nael;
    private Actor? _twin;
    private Actor? _baha;
    private readonly WPos[] _safeSpots = new WPos[PartyState.MaxPartySize];
    private readonly UCOBConfig _config = Service.Config.Get<UCOBConfig>();

    public bool Active => _nael != null;

    private readonly Angle[] _offsetsNaelCenter = [10f.Degrees(), 80f.Degrees(), 100f.Degrees(), 170f.Degrees()];
    private readonly Angle[] _offsetsNaelSide = [60f.Degrees(), 80f.Degrees(), 100f.Degrees(), 120f.Degrees()];

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        Arena.Actor(_nael, Colors.Object, true, true);
        var safespot = _safeSpots[pcSlot];
        if (safespot != default)
        {
            Arena.ZoneCircleOutline(safespot, 1f, Colors.Safe);
        }
    }

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (actor.OID is var oid && oid == (uint)OID.NaelDeusDarnus && id == 0x1E43)
        {
            _nael = actor;
            InitIfReady();
        }
        else if (oid == (uint)OID.Twintania && id == 0x1E44)
        {
            _twin = actor;
            InitIfReady();
        }
        else if (oid == (uint)OID.BahamutPrime && id == 0x1E43)
        {
            _baha = actor;
            InitIfReady();
        }
    }

    private void InitIfReady()
    {
        if (_nael == null || _twin == null || _baha == null)
            return;

        var dirToNael = Angle.FromDirection(_nael.Position - Arena.Center);
        var dirToTwin = Angle.FromDirection(_twin.Position - Arena.Center);
        var dirToBaha = Angle.FromDirection(_baha.Position - Arena.Center);

        var twinRel = (dirToTwin - dirToNael).Normalized();
        var bahaRel = (dirToBaha - dirToNael).Normalized();
        var (offsetSymmetry, offsets) = twinRel.Rad * bahaRel.Rad < 0f // twintania & bahamut are on different sides => nael is in center
            ? (default, _offsetsNaelCenter)
            : ((twinRel + bahaRel) * 0.5f, _offsetsNaelSide);
        var dirSymmetry = dirToNael + offsetSymmetry;
        var assignments = _config.P3QuickmarchTrioAssignments.Resolve(Raid);
        var count = assignments.Count;
        for (var i = 0; i < count; ++i)
        {
            var p = assignments[i];
            var left = p.group < 4;
            var order = p.group & 3;
            var offset = offsets[order];
            var dir = dirSymmetry + (left ? offset : -offset);
            _safeSpots[p.slot] = Arena.Center + 20f * dir.ToDirection();
        }
    }
}

sealed class P3HeavensfallTowers(UCOB module) : Components.CastTowers(module, (uint)AID.MegaflareTower, 3f)
{
    private readonly UCOBConfig _config = Service.Config.Get<UCOBConfig>();
    private readonly Actor _nael = module.Nael()!;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        base.OnCastStarted(caster, spell);

        if (spell.Action.ID == WatchedAction && Towers.Count == 8)
        {
            var center = Arena.Center;
            var dirToNael = Angle.FromDirection(_nael.Position - center);

            Towers.Sort((a, b) =>
                TowerSortKey(Angle.FromDirection(a.Position - center), dirToNael)
                    .CompareTo(TowerSortKey(Angle.FromDirection(b.Position - center), dirToNael)));

            var assignments = _config.P3HeavensfallTrioTowers.Resolve(Raid);
            var count = assignments.Count;
            var towers = CollectionsMarshal.AsSpan(Towers);
            for (var i = 0; i < count; ++i)
            {
                var p = assignments[i];
                towers[p.group].ForbiddenSoakers = new(~(1ul << p.slot));
            }
        }
    }

    // order towers from nael's position CW
    private float TowerSortKey(Angle tower, Angle reference)
    {
        var cwDist = (reference - tower).Normalized().Deg;
        if (cwDist < -5f) // towers are ~22.5 degrees apart
        {
            cwDist += 360f;
        }
        return cwDist;
    }
}

sealed class P3HeavensfallFireball(BossModule module) : Components.StackWithIcon(module, (uint)IconID.Fireball, (uint)AID.Fireball, 4f, 5.3f, 8, 8);
