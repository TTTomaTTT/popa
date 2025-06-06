// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.Server.Actions;
using Content.Server.AlertLevel;
using Content.Server.Buckle.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Ghost;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.DarkReaper;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Shared.Projectiles;
using Content.Server.Projectiles;

namespace Content.Server.SS220.DarkReaper;

public sealed class DarkReaperSystem : SharedDarkReaperSystem
{
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly BuckleSystem _buckle = default!;
    [Dependency] private readonly ProjectileSystem _projectile = default!;

    private readonly ISawmill _sawmill = Logger.GetSawmill("DarkReaper");

    [ValidatePrototypeId<AlertPrototype>]
    private const string DeadscoreStage1Alert = "DeadscoreStage1";

    [ValidatePrototypeId<AlertPrototype>]
    private const string DeadscoreStage2Alert = "DeadscoreStage2";

    private const int MaxBooEntities = 30;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void ChangeForm(EntityUid uid, DarkReaperComponent comp, bool isMaterial)
    {
        var isTransitioning = comp.PhysicalForm != isMaterial;
        base.ChangeForm(uid, comp, isMaterial);

        if (isTransitioning && !isMaterial)
        {
            if (comp.ActivePortal != null)
            {
                QueueDel(comp.ActivePortal);
                comp.ActivePortal = null;
            }

            if (TryComp<EmbeddedContainerComponent>(uid, out var embeddedContainer))
                _projectile.DetachAllEmbedded((uid, embeddedContainer));
        }
    }

    protected override void CreatePortal(EntityUid uid, DarkReaperComponent comp)
    {
        base.CreatePortal(uid, comp);

        // Make lights blink
        BooInRadius(uid, 6);
    }

    protected override void OnAfterConsumed(EntityUid uid, DarkReaperComponent comp, AfterConsumed args)
    {
        base.OnAfterConsumed(uid, comp, args);

        if (!args.Cancelled && args.Target is EntityUid target)
        {
            if (comp.PhysicalForm && target.IsValid() && !EntityManager.IsQueuedForDeletion(target) && _mobState.IsDead(target))
            {
                if (!_container.TryGetContainer(uid, DarkReaperComponent.ConsumedContainerId, out var container))
                    return;

                if (!_container.CanInsert(target, container))
                    return;

                if (_buckle.IsBuckled(args.Target.Value))
                {
                    _buckle.TryUnbuckle(args.Target.Value, args.Target.Value, true);
                }

                // spawn gore
                Spawn(comp.EntityToSpawnAfterConsuming, Transform(target).Coordinates);

                // randomly drop inventory items
                if (_inventory.TryGetContainerSlotEnumerator(target, out var slots))
                {
                    while (slots.MoveNext(out var containerSlot))
                    {
                        if (containerSlot.ContainedEntity is not { } containedEntity)
                            continue;

                        if (!_random.Prob(comp.InventoryDropProbabilityOnConsumed))
                            continue;

                        if (!_container.TryRemoveFromContainer(containedEntity))
                            continue;

                        // set random rotation
                        _transform.SetLocalRotationNoLerp(containedEntity, Angle.FromDegrees(_random.NextDouble(0, 360)));

                        // apply random impulse
                        var maxAxisImp = comp.SpawnOnDeathImpulseStrength;
                        var impulseVec = new Vector2(_random.NextFloat(-maxAxisImp, maxAxisImp), _random.NextFloat(-maxAxisImp, maxAxisImp));
                        _physics.ApplyLinearImpulse(containedEntity, impulseVec);
                    }
                }

                _container.Insert(target, container);
                _damageable.TryChangeDamage(uid, comp.HealPerConsume, true, origin: args.Args.User);

                comp.Consumed++;
                var stageBefore = comp.CurrentStage;
                UpdateStage(uid, comp);

                // warn a crew if alert stage is reached
                if (comp.CurrentStage > stageBefore && comp.CurrentStage == comp.AlertStage)
                {
                    var reaperXform = Transform(uid);
                    var stationUid = _station.GetStationInMap(reaperXform.MapID);
                    if (stationUid != null)
                        _alertLevel.SetLevel(stationUid.Value, comp.AlertLevelOnAlertStage, true, true, true, false);

                    var announcement = Loc.GetString("dark-reaper-component-announcement");
                    var sender = Loc.GetString("comms-console-announcement-title-centcom");
                    _chat.DispatchStationAnnouncement(stationUid ?? uid, announcement, sender, false, null, Color.Red);//SS220 CluwneComms
                }

                // update consoom counter alert
                UpdateAlert(uid, comp);
                Dirty(uid, comp);
            }
        }
    }

    private void UpdateAlert(EntityUid uid, DarkReaperComponent comp)
    {
        _alerts.ClearAlert(uid, DeadscoreStage1Alert);
        _alerts.ClearAlert(uid, DeadscoreStage2Alert);

        string alert;
        if (comp.CurrentStage == 1)
            alert = DeadscoreStage1Alert;
        else if (comp.CurrentStage == 2)
            alert = DeadscoreStage2Alert;
        else
        {
            return;
        }

        if (!comp.ConsumedPerStage.TryGetValue(comp.CurrentStage - 1, out var severity))
            severity = 0;

        severity -= comp.Consumed;

        if (alert == DeadscoreStage1Alert && severity > 3)
        {
            severity = 3; // 3 is a max value our sprite can display at stage 1
            _sawmill.Error("Had to clamp alert severity. It shouldn't happen. Report it to Artur.");
        }
        else if (alert == DeadscoreStage2Alert && severity > 8)
        {
            severity = 8; // 8 is a max value our sprite can display at stage 2
            _sawmill.Error("Had to clamp alert severity. It shouldn't happen. Report it to Artur.");
        }

        if (severity <= 0)
        {
            _alerts.ClearAlert(uid, DeadscoreStage1Alert);
            _alerts.ClearAlert(uid, DeadscoreStage2Alert);
            return;
        }

        _alerts.ShowAlert(uid, alert, (short) severity);
    }

    protected override void OnCompInit(EntityUid uid, DarkReaperComponent comp, ComponentStartup args)
    {
        base.OnCompInit(uid, comp, args);

        _container.EnsureContainer<Container>(uid, DarkReaperComponent.ConsumedContainerId);

        if (!comp.RoflActionEntity.HasValue)
            _actions.AddAction(uid, ref comp.RoflActionEntity, comp.RoflAction);

        if (!comp.StunActionEntity.HasValue)
            _actions.AddAction(uid, ref comp.StunActionEntity, comp.StunAction);

        if (!comp.ConsumeActionEntity.HasValue)
            _actions.AddAction(uid, ref comp.ConsumeActionEntity, comp.ConsumeAction);

        if (!comp.MaterializeActionEntity.HasValue)
            _actions.AddAction(uid, ref comp.MaterializeActionEntity, comp.MaterializeAction);

        if (!comp.BloodMistActionEntity.HasValue)
            _actions.AddAction(uid, ref comp.BloodMistActionEntity, comp.BloodMistAction);

        UpdateAlert(uid, comp);
    }

    protected override void OnCompShutdown(EntityUid uid, DarkReaperComponent comp, ComponentShutdown args)
    {
        base.OnCompShutdown(uid, comp, args);

        _actions.RemoveAction(uid, comp.RoflActionEntity);
        _actions.RemoveAction(uid, comp.StunActionEntity);
        _actions.RemoveAction(uid, comp.ConsumeActionEntity);
        _actions.RemoveAction(uid, comp.MaterializeActionEntity);
        _actions.RemoveAction(uid, comp.BloodMistActionEntity);
    }

    protected override void DoStunAbility(EntityUid uid, DarkReaperComponent comp)
    {
        base.DoStunAbility(uid, comp);

        // Destroy lights in radius
        var lightQuery = GetEntityQuery<PoweredLightComponent>();
        var entities = _lookup.GetEntitiesInRange(uid, comp.StunAbilityLightBreakRadius);

        foreach (var entity in entities)
        {
            if (!lightQuery.TryGetComponent(entity, out var lightComp))
                continue;

            _poweredLight.TryDestroyBulb(entity, lightComp);
        }
    }

    private void BooInRadius(EntityUid uid, float radius)
    {
        var entities = _lookup.GetEntitiesInRange(uid, radius);

        var booCounter = 0;
        foreach (var ent in entities)
        {
            var handled = _ghost.DoGhostBooEvent(ent);

            if (handled)
                booCounter++;

            if (booCounter >= MaxBooEntities)
                break;
        }
    }

    protected override void DoRoflAbility(EntityUid uid, DarkReaperComponent comp)
    {
        base.DoRoflAbility(uid, comp);

        // Make lights blink
        BooInRadius(uid, 6);
    }
}
