using Lavender.Common.Menus;
using Lavender.Common.Registers;

namespace Lavender.Client.Menus;

public partial class LoadupMenuSelection : LoadableMenu
{
	void OnJoinButtonPressed()
	{
		Overseer.GotoScene(Register.SceneTable["loadup_menu_join"]);
	}

	void OnBackButtonPressed()
	{
		Overseer.GotoScene(Register.SceneTable["loadup_menu_main"]);
	}

	void OnSinglePlayerButtonPressed()
	{
		// Overseer.JoinServer(ipAddress, 8778)
		Overseer.JoinSinglePlayer();
	}
}
