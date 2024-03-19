using Lavender.Common.Menus;
using Lavender.Common.Registers;

namespace Lavender.Client.Menus;

public partial class LoadupMenuMain : LoadableMenu
{
	private void OnPlayButtonPressed()
	{
		EnvManager.GotoScene(Register.SceneTable["loadup_menu_selection"]);
	}

	private void OnQuitButtonPressed()
	{
		GetTree().Quit();
	}
	
}
