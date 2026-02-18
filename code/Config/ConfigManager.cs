using GameSystems.Config;

namespace Sandbox.GameSystems.Config;

public sealed class ConfigManager : Component
{
	private static ConfigManager _instance;
	// Property for the Money Prefab
	[Property] public GameObject MoneyPrefab { get; set; }
	[Sync] public CommandConfig Commands { get; } = new();
	public ConfigManager()
	{
		_instance = this;
	}
	public static ConfigManager Instance => _instance;

	protected override void OnDestroy()
	{
		if ( _instance == this )
			_instance = null;
	}

	protected override void OnStart()
	{

	}
}
