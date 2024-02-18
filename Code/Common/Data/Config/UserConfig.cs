using Godot;

namespace Lavender.Common.Data.Config;

public class UserConfig
{
    public float MouseSensitivity { get; set; } = 100f;

    public string Username
    {
        get
        {
            return UsernameValue;
        }
        set
        {
            SetUsername(value);
        }
    }

    private string UsernameValue { get; set; } = "";

    private void SetUsername(string name)
    {
        string usrName = name;
        if (string.IsNullOrWhiteSpace(usrName))
        {
            uint rndNum = GD.Randi() % 512;
            usrName = $"User_{rndNum}";
        }

        if (usrName.Length > 16)
        {
            usrName = usrName.Substring(0, 16);
        }

        UsernameValue = usrName;
    }
}