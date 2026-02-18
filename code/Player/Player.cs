using GameSystems;
using GameSystems.UI;

namespace Sandbox.GameSystems.Player;

/// <summary>
/// Represents your local player
/// </summary>
public partial class Player : Component, Component.INetworkSpawn
{
	[Property, Group( "References" )] public PlayerHUD PlayerHud { get; set; }
	[Property, Group( "References" )] public PlayerHUD PlayerTabMenu { get; set; }
	[Property, Group( "References" )] public LeaderBoard LeaderBoard { get; set; }
	[Property, Group( "References" )] public MOTD MOTD { get; set; }
	[Property, Group( "References" )] public DeathScreen DeathScreen { get; set; }
	[Property, Group( "References" )] public BasicMenu BasicMenu { get; set; }
	[Property, Group( "References" )] public Phone Phone { get; set; }
	private CameraComponent _camera;

	public string Name {get; set;}

	/// <summary>
	/// Reference to the vehicle the player is currently driving (null if not in a vehicle).
	/// </summary>
	public GameObject CurrentVehicle { get; set; }

	protected override void OnAwake()
	{
		Log.Info( $"[Player] OnAwake called. IsProxy={Network.IsProxy}" );
		_camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault( x => x.IsMainCamera );

		if ( !Network.IsProxy )
		{
			Log.Info( $"[Player] Enabling UI. HUD={PlayerHud != null} Tab={PlayerTabMenu != null} LB={LeaderBoard != null} BM={BasicMenu != null} DS={DeathScreen != null} MOTD={MOTD != null} Phone={Phone != null}" );

			if ( PlayerHud != null ) PlayerHud.Enabled = true;
			if ( PlayerTabMenu != null ) PlayerTabMenu.Enabled = true;
			if ( LeaderBoard != null ) LeaderBoard.Enabled = true;
			if ( MOTD != null ) MOTD.Enabled = true;
			if ( DeathScreen != null ) DeathScreen.Enabled = true;
			if ( BasicMenu != null ) BasicMenu.Enabled = true;
			if ( Phone != null ) Phone.Enabled = true;
		}
	}

	protected override void OnStart()
	{
		GameController.Instance.AddPlayer( GameObject, GameObject.Network.OwnerConnection);
		Name = this.Network.OwnerConnection.DisplayName;

		OnStartMovement();

		if ( !Network.IsProxy )
		{
			OnStartStatus();
			OnStartInventory();

			// Show MOTD on first spawn
			MOTD?.ShowOnFirstSpawn();
		}
	}

	protected override void OnUpdate()
	{
		OnUpdateMovement();
	}

	protected override void OnFixedUpdate()
	{
		OnFixedUpdateMovement();

		if ( !IsProxy )
		{
			OnFixedUpdateStatus();

			// Skip inventory and interaction when in a vehicle
			if ( CurrentVehicle == null )
			{
				OnFixedUpdateInventory();
				OnFixedUpdateInteraction();
			}
		}
	}

	/// <summary>
	/// Show the MOTD popup (called by /motd command).
	/// </summary>
	public void ShowMOTD()
	{
		MOTD?.Show();
	}

	public void OnNetworkSpawn( Connection owner )
	{
		OnNetworkSpawnOutfitter( owner );
	}
}
