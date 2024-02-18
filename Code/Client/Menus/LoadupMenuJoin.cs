using System;
using System.Linq;
using Godot;
using Lavender.Common.Menus;
using Lavender.Common.Registers;

namespace Lavender.Client.Menus;

public partial class LoadupMenuJoin : LoadableMenu
{
	public override void _Ready()
	{
		base._Ready();

		if (_joinButton == null || _backButton == null || _ipAddressInputBox == null)
			throw new Exception("One of the EXPORT's on this Node arnt set!");
	}

	private void OnConnectButtonPressed()
	{
		string ipAddress = _ipAddressInputBox.Text;
		
		
		ipAddress = string.Concat(ipAddress.Where(c => c == '.' || char.IsDigit(c)));
		if (ipAddress.Length > 16)
		{
			ipAddress = ipAddress.Substring(0, 16);
		}
		

		if (Overseer.JoinServer(ipAddress, 8778))
		{
			GD.Print("Attempting Join Server...");
			_joining = true;
			_joinButton.Text = "Joining...";
			_joinButton.Disabled = true;
			_backButton.Text = "Stop Joining";
		}
	}

	private void OnBackButtonPressed()
	{
		if(!_joining)
			Overseer.GotoScene(Register.SceneTable["loadup_menu_selection"]);
	}

	private void OnIPAddressBoxTextChanged()
	{
		string ipAddress = _ipAddressInputBox.Text;
		string tmpAddr = ipAddress;
		
		
		ipAddress = string.Concat(ipAddress.Where(c => c == '.' || char.IsDigit(c)));
		if (ipAddress.Length > 16)
		{
			ipAddress = ipAddress.Substring(0, 16);
		}

		if (tmpAddr != ipAddress)
		{
			_ipAddressInputBox.Text = ipAddress;
			_ipAddressInputBox.SetCaretColumn(_ipAddressInputBox.Text.Length);
		}
	}

	[Export] private TextEdit _ipAddressInputBox;
	[Export] private Button _joinButton;
	[Export] private Button _backButton;

	private bool _joining = false;
}
