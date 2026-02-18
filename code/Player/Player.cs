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
		_camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault( x => x.IsMainCamera );

		if ( !Network.IsProxy )
		{
			// TODO: This should be moved off of the player and moved globally
			PlayerHud.Enabled = true;
			PlayerTabMenu.Enabled = true;
			LeaderBoard.Enabled = true;
			if ( MOTD != null ) MOTD.Enabled = true;
			if ( DeathScreen != null ) DeathScreen.Enabled = true;
			if ( BasicMenu != null ) BasicMenu.Enabled = true;
			if ( Phone != null ) Phone.Enabled = true;

			Log.Info( $"[Player] UI enabled - BasicMenu: {BasicMenu != null}, LeaderBoard: {LeaderBoard != null}, Phone: {Phone != null}" );
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
