using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Dataset;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Pointing;
using Content.Shared.KaiserHamburg;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Shared.Damage;
using Content.Server.Damage.Components;

namespace Content.Server.KaiserHamburg
{
    /// <inheritdoc/>
    public sealed class KaiserHamburgSystem : SharedKaiserHamburgSystem
    {
        [Dependency] private readonly AtmosphereSystem _atmos = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly HTNSystem _htn = default!;
        [Dependency] private readonly HungerSystem _hunger = default!;
        [Dependency] private readonly NPCSystem _npc = default!;
        [Dependency] private readonly PopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<KaiserHamburgComponent, KaiserHamburgRaiseArmySchutzeActionEvent>(OnRaiseArmySchutze);
            SubscribeLocalEvent<KaiserHamburgComponent, KaiserHamburgRaiseArmyBrigadefuhrerActionEvent>(OnRaiseArmyBrigadefuhrer);
            SubscribeLocalEvent<KaiserHamburgComponent, AfterPointedAtEvent>(OnPointedAt);
        }

        /// <summary>
        /// Summons an Schutze at the Kaiser, costing a small amount of hunger (hp?)
        /// </summary>
        private void OnRaiseArmySchutze(EntityUid uid, KaiserHamburgComponent component, KaiserHamburgRaiseArmySchutzeActionEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp<HungerComponent>(uid, out var hunger))
                return;

            //make sure the hunger doesn't go into the negatives
            if (hunger.CurrentHunger < component.HungerPerSchutzeArmyUse)
            {
                _popup.PopupEntity(Loc.GetString("kaiser-too-weak"), uid, uid);
                return;
            }
            args.Handled = true;
            _hunger.ModifyHunger(uid, -component.HungerPerSchutzeArmyUse, hunger);
            var treitmacht = Spawn(component.ArmySchutzeSpawnId, Transform(uid).Coordinates);
            var comp = EnsureComp<KaiserHamburgStreitmachtComponent>(treitmacht);
            comp.Kaiser = uid;
            Dirty(treitmacht, comp);

            component.Streitmacht.Add(treitmacht);
            _npc.SetBlackboard(treitmacht, NPCBlackboard.FollowTarget, new EntityCoordinates(uid, Vector2.Zero));
           UpdateStreitmachtNpc(treitmacht, component.CurrentOrder);
        }

        /// <summary>
        /// Summons an Brigadefuhrer at the Kaiser, costing a small amount of hunger (hp?)
        /// </summary>
        private void OnRaiseArmyBrigadefuhrer(EntityUid uid, KaiserHamburgComponent component, KaiserHamburgRaiseArmyBrigadefuhrerActionEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp<HungerComponent>(uid, out var hunger))
                return;

            //make sure the hunger doesn't go into the negatives
            if (hunger.CurrentHunger < component.HungerPerBrigadefuhrerArmyUse)
            {
                _popup.PopupEntity(Loc.GetString("kaiser-too-weak"), uid, uid);
                return;
            }
            args.Handled = true;
            _hunger.ModifyHunger(uid, -component.HungerPerBrigadefuhrerArmyUse, hunger);
            var treitmacht = Spawn(component.ArmyBrigadefuhrerSpawnId, Transform(uid).Coordinates);
            var comp = EnsureComp<KaiserHamburgStreitmachtComponent>(treitmacht);
            comp.Kaiser = uid;
            Dirty(treitmacht, comp);

            component.Streitmacht.Add(treitmacht);
            _npc.SetBlackboard(treitmacht, NPCBlackboard.FollowTarget, new EntityCoordinates(uid, Vector2.Zero));
           UpdateStreitmachtNpc(treitmacht, component.CurrentOrder);
        }

        private void OnPointedAt(EntityUid uid, KaiserHamburgComponent component, ref AfterPointedAtEvent args)
        {
            if (component.CurrentOrder != KaiserHamburgOrderType.KillBurger)
                return;

            foreach (var treitmacht in component.Streitmacht)
            {
                _npc.SetBlackboard(treitmacht, NPCBlackboard.CurrentOrderedTarget, args.Pointed);
            }
        }

        public override void UpdateStreitmachtNpc(EntityUid uid, KaiserHamburgOrderType orderType)
        {
            base.UpdateStreitmachtNpc(uid, orderType);

            if (!TryComp<HTNComponent>(uid, out var htn))
                return;

            if (htn.Plan != null)
                _htn.ShutdownPlan(htn);

            _npc.SetBlackboard(uid, NPCBlackboard.CurrentOrders, orderType);
            _htn.Replan(htn);
        }

        public override void DoCommandHamburgCallout(EntityUid uid, KaiserHamburgComponent component)
        {
            base.DoCommandHamburgCallout(uid, component);

            if (!component.OrderCallouts.TryGetValue(component.CurrentOrder, out var datasetId) ||
                !PrototypeManager.TryIndex<DatasetPrototype>(datasetId, out var datasetPrototype))
                return;

            var msg = Random.Pick(datasetPrototype.Values);
            _chat.TrySendInGameICMessage(uid, msg, InGameICChatType.Speak, true);
        }
    }
}
