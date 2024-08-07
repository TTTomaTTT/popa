using Content.Shared.Actions;

namespace Content.Shared.KaiserHamburg;

public sealed partial class KaiserHamburgRaiseArmySchutzeActionEvent : InstantActionEvent
{

}

public sealed partial class KaiserHamburgRaiseArmyBrigadefuhrerActionEvent : InstantActionEvent
{

}

public sealed partial class KaiserHamburgOrderActionEvent : InstantActionEvent
{
    /// <summary>
    /// The type of order being given
    /// </summary>
    [DataField("type")]
    public KaiserHamburgOrderType Type;
}
