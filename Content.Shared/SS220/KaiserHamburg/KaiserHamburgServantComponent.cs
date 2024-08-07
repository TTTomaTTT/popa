using Robust.Shared.GameStates;

namespace Content.Shared.KaiserHamburg;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedKaiserHamburgSystem))]
[AutoGenerateComponentState]
public sealed partial class KaiserHamburgStreitmachtComponent : Component
{
    /// <summary>
    /// The kaiser
    /// </summary>
    [DataField("kaiser")]
    [AutoNetworkedField]
    public EntityUid? Kaiser;
}
