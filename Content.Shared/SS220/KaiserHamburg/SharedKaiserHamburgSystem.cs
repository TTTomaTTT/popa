using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.KaiserHamburg;

public abstract class SharedKaiserHamburgSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<KaiserHamburgComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<KaiserHamburgComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<KaiserHamburgComponent, KaiserHamburgOrderActionEvent>(OnOrderAction);

        SubscribeLocalEvent<KaiserHamburgStreitmachtComponent, ComponentShutdown>(OnStreitmachtShutdown);
        SubscribeLocalEvent<KaiserHamburgStreitmachtComponent, MobStateChangedEvent>(OnStreitmachtDie);
    }

    private void OnStartup(EntityUid uid, KaiserHamburgComponent component, ComponentStartup args)
    {
        if (!TryComp(uid, out ActionsComponent? comp))
            return;

        _action.AddAction(uid, ref component.ActionBurgerSchutzeRaiseArmyEntity, component.ActionKaiserSchutzeRaiseArmy, component: comp);
        _action.AddAction(uid, ref component.ActionBurgerBrigadefuhrerRaiseArmyEntity, component.ActionKaiserBrigadefuhrerRaiseArmy, component: comp);
        _action.AddAction(uid, ref component.ActionOrderFollowEntity, component.ActionOrderFollow, component: comp);
        _action.AddAction(uid, ref component.ActionOrderKillBurgerEntity, component.ActionOrderKillBurger, component: comp);
        _action.AddAction(uid, ref component.ActionOrderLooseEntity, component.ActionOrderLoose, component: comp);

        UpdateActions(uid, component);
    }

    private void OnShutdown(EntityUid uid, KaiserHamburgComponent component, ComponentShutdown args)
    {
        foreach (var streitmacht in component.Streitmacht)
        {
            if (TryComp(streitmacht, out KaiserHamburgStreitmachtComponent? streitmachtComp))
                streitmachtComp.Kaiser = null;
        }

        if (!TryComp(uid, out ActionsComponent? comp))
            return;

        _action.RemoveAction(uid, component.ActionBurgerSchutzeRaiseArmyEntity, comp);
        _action.RemoveAction(uid, component.ActionBurgerBrigadefuhrerRaiseArmyEntity, comp);
        _action.RemoveAction(uid, component.ActionOrderFollowEntity, comp);
        _action.RemoveAction(uid, component.ActionOrderKillBurgerEntity, comp);
        _action.RemoveAction(uid, component.ActionOrderLooseEntity, comp);
    }

    private void OnOrderAction(EntityUid uid, KaiserHamburgComponent component, KaiserHamburgOrderActionEvent args)
    {
        if (component.CurrentOrder == args.Type)
            return;
        args.Handled = true;

        component.CurrentOrder = args.Type;
        Dirty(uid, component);

        DoCommandHamburgCallout(uid, component);
        UpdateActions(uid, component);
        UpdateAllStreitmacht(uid, component);
    }

    private void OnStreitmachtShutdown(EntityUid uid, KaiserHamburgStreitmachtComponent component, ComponentShutdown args)
    {
        if (TryComp(component.Kaiser, out KaiserHamburgComponent? kaiserHamburgComponent))
            kaiserHamburgComponent.Streitmacht.Remove(uid);
    }

    //ss220 rat streitmacht fix begin
    private void OnStreitmachtDie(EntityUid uid, KaiserHamburgStreitmachtComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        EnsureComp<ItemComponent>(uid);

        _tagSystem.AddTag(uid, "Trash");
    }
    //ss220 rat streitmacht fix end

    private void UpdateActions(EntityUid uid, KaiserHamburgComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _action.SetToggled(component.ActionOrderFollowEntity, component.CurrentOrder == KaiserHamburgOrderType.Follow);
        _action.SetToggled(component.ActionOrderKillBurgerEntity, component.CurrentOrder == KaiserHamburgOrderType.KillBurger);
        _action.SetToggled(component.ActionOrderLooseEntity, component.CurrentOrder == KaiserHamburgOrderType.Loose);
        _action.StartUseDelay(component.ActionOrderFollowEntity);
        _action.StartUseDelay(component.ActionOrderKillBurgerEntity);
        _action.StartUseDelay(component.ActionOrderLooseEntity);
    }

    public void UpdateAllStreitmacht(EntityUid uid, KaiserHamburgComponent component)
    {
        foreach (var streitmacht in component.Streitmacht)
        {
            UpdateStreitmachtNpc(streitmacht, component.CurrentOrder);
        }
    }

    public virtual void UpdateStreitmachtNpc(EntityUid uid, KaiserHamburgOrderType orderType)
    {

    }

    public virtual void DoCommandHamburgCallout(EntityUid uid, KaiserHamburgComponent component)
    {

    }

}
