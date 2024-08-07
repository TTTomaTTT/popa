using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.KaiserHamburg;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedKaiserHamburgSystem))]
[AutoGenerateComponentState]
public sealed partial class KaiserHamburgComponent : Component
{

    /// <summary>
    ///     The action for the Raise Schütze ability
    /// </summary>
    [DataField("actionKaiserSchutzeRaiseArmy", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionKaiserSchutzeRaiseArmy = "ActionKaiserSchutzeRaiseArmy";

    [DataField("actionBurgerSchutzeRaiseArmyEntity")]
    public EntityUid? ActionBurgerSchutzeRaiseArmyEntity;

    /// <summary>
    ///     The amount of damage one use of Raise Army consumes
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("hungerPerSchutzeArmyUse", required: true)]
    public float HungerPerSchutzeArmyUse = 50f;

    /// <summary>
    ///     The entity prototype of the mob that Raise Army summons
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("armySchutzeSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ArmySchutzeSpawnId = "MobBurgerSchutze";

    /// <summary>
    ///     The action for the Raise Brigadefuhrer ability
    /// </summary>
    [DataField("actionKaiserBrigadefuhrerRaiseArmy", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionKaiserBrigadefuhrerRaiseArmy = "ActionKaiserBrigadefuhrerRaiseArmy";

    [DataField("actionBurgerBrigadefuhrerRaiseArmyEntity")]
    public EntityUid? ActionBurgerBrigadefuhrerRaiseArmyEntity;

    /// <summary>
    ///     The amount of damage one use of Raise Army consumes
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("hungerPerBrigadefuhrerArmyUse", required: true)]
    public float HungerPerBrigadefuhrerArmyUse = 150f;

    /// <summary>
    ///     The entity prototype of the mob that Raise Army summons
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("armyBrigadefuhrerSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ArmyBrigadefuhrerSpawnId = "MobBurgerBrigadefuhrer";

    /// <summary>
    /// The current order that the Rat Kaiser assigned.
    /// </summary>
    [DataField("currentOrders"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public KaiserHamburgOrderType CurrentOrder = KaiserHamburgOrderType.Loose;

    /// <summary>
    /// The streitmacht that the rat kaiser is currently controlling
    /// </summary>
    [DataField("streitmacht")]
    public HashSet<EntityUid> Streitmacht = new();

    [DataField("actionOrderFollow", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionOrderFollow = "ActionKaiserHamburgOrderFollow";

    [DataField("actionOrderFollowEntity")]
    public EntityUid? ActionOrderFollowEntity;

    [DataField("actionOrderKillBurger", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionOrderKillBurger = "ActionKaiserHamburgOrderKillBurger";

    [DataField("actionOrderKillBurgerEntity")]
    public EntityUid? ActionOrderKillBurgerEntity;

    [DataField("actionOrderLoose", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionOrderLoose = "ActionKaiserHamburgOrderLoose";

    [DataField("actionOrderLooseEntity")]
    public EntityUid? ActionOrderLooseEntity;

    /// <summary>
    /// A dictionary with an order type to the corresponding callout dataset.
    /// </summary>
    [DataField("orderCallouts")]
    public Dictionary<KaiserHamburgOrderType, string> OrderCallouts = new()
    {
        { KaiserHamburgOrderType.Follow, "KaiserHamburgCommandFollow" },
        { KaiserHamburgOrderType.KillBurger, "KaiserHamburgCommandKillBurger" },
        { KaiserHamburgOrderType.Loose, "KaiserHamburgCommandLoose" }
    };

}

[Serializable, NetSerializable]
public enum KaiserHamburgOrderType : byte
{
    Follow,
    KillBurger,
    Loose
}
