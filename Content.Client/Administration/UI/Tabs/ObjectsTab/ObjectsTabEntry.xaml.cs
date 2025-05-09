using Content.Client.Administration.Managers;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Administration.UI.Tabs.ObjectsTab;

[GenerateTypedNameReferences]
public sealed partial class ObjectsTabEntry : PanelContainer
{
    public NetEntity AssocEntity;

    public Action<NetEntity>? OnTeleport;
    public Action<NetEntity>? OnDelete;

    public ObjectsTabEntry(IClientAdminManager manager, string name, NetEntity nent, StyleBox styleBox)
    {
        RobustXamlLoader.Load(this);

        AssocEntity = nent;
        EIDLabel.Text = nent.ToString();
        NameLabel.Text = name;
        BackgroundColorPanel.PanelOverride = styleBox;

        TeleportButton.Disabled = !manager.CanCommand("tpto");
        DeleteButton.Disabled = !manager.CanCommand("delete");

        TeleportButton.OnPressed += _ => OnTeleport?.Invoke(nent);
        DeleteButton.OnPressed += _ => OnDelete?.Invoke(nent);
    }
}
