using System;
using GameSystems.Admin;
using GameSystems.Jobs;
using Entity.Vehicle;
using GameSystems.CriminalEconomy;
using GameSystems.LawOrder;
using GameSystems.Phone;
using GameSystems.Player;
using GameSystems.UI;
using Sandbox.GameSystems.Database;
using Sandbox.GameSystems.Player;

namespace GameSystems
{
	public sealed class GameController : Component, Component.INetworkListener
	{
		private static readonly ulong[] DevSteamIDs = new ulong[] {
			76561198844028104, // Sousou
		    76561198137204749, // QueenPM
		    76561198161573319, // irlladdergoat
		    76561198237485902, // Bozy
		    76561198040274296, // Stefan
		    76561198006076880, // dancore
			76561198837197784, // EuroBlue 
			76561199092626415, // Mangro
			76561198243368782,  // Dada
			76561198071517597, // Red
			76561198047807813, // Dimmer
			76561197992814320  // Fnasp
		};
		private static GameController _instance;

		Chat chat { get; set; }
		private Database _database { get; set; } // Don't touch it's waiting when the time will come (when garry releases servers)


		// TODO: YOU CAN'T SYNC COMPLEX OBJECTS
		[HostSync] public NetDictionary<Guid, NetworkPlayer> Players { get; set; } = new();

		[HostSync] public NetDictionary<string, UserGroup> UserGroups { get; set; } = new()
		{
			{ "user", new UserGroup( "user", "User", PermissionLevel.User, Color.White ) },
			{ "moderator", new UserGroup( "moderator", "Moderator", PermissionLevel.Moderator, Color.Yellow ) },
			{ "admin", new UserGroup( "admin", "Admin", PermissionLevel.Admin, Color.Red ) },
			{ "superadmin", new UserGroup( "superadmin", "Super Admin", PermissionLevel.SuperAdmin, Color.Cyan ) },
			{ "developer", new UserGroup( "developer", "Developer", PermissionLevel.Developer, Color.Orange ) }
		};

		public GameController()
		{
			if ( _instance != null )
			{
				Log.Warning( "Only one instance of GameController is allowed." );
			}

			_instance = this;
		}

		public static GameController Instance => _instance;

		protected override void OnStart()
		{
			chat = Scene.Directory.FindByName( "Screen" )?.First()?.Components.Get<Chat>();
			if ( chat == null ) Log.Error( "Chat component not found" );

			if ( !FileSystem.Data.DirectoryExists( "playersdata" ) )
			{
				FileSystem.Data.CreateDirectory( "playersdata" );
			}
		}

		// This could probably be put in the network controller/helper.
		public void AddPlayer( GameObject player, Connection connection )
		{
			Log.Info( $"Adding player: {connection.Id} {connection.DisplayName}" );
			try
			{
				// Check if player is banned
				if ( BanManager.IsBanned( connection.SteamId ) )
				{
					var ban = BanManager.GetBan( connection.SteamId );
					var timeLeft = BanManager.GetBanTimeRemaining( connection.SteamId );
					Log.Info( $"Banned player tried to connect: {connection.DisplayName} ({connection.SteamId}) - {timeLeft} remaining" );
					connection.Disconnect();
					return;
				}

				var userGroups = new List<UserGroup>();
				// If the user is a Dev, assign the developer user group
				if ( DevSteamIDs.Contains( connection.SteamId ) )
				{
					userGroups.Add( UserGroups["developer"] );
				}
				// If the user is the host, assign the superadmin user group
				if ( connection.IsHost )
				{
					userGroups.Add( UserGroups["superadmin"] );
				}
				// If the user is not a dev or host, assign the "user" user group
				if ( userGroups.Count == 0 )
				{
					userGroups.Add( UserGroups["user"] );
				}
				Players.Add( connection.Id, new NetworkPlayer( player, connection, userGroups ) );
				if ( Rpc.Caller.IsHost )
				{
					chat?.NewSystemMessage( $"{connection.DisplayName} has joined the game." );
				}
			}
			catch ( Exception e )
			{
				Log.Warning( e );
			}
		}

		public void RemovePlayer( Connection connection )
		{
			Log.Info( $"Removing player: {connection.Id} {connection.DisplayName}" );
			try
			{
				// Find the player in the list
				if ( !Players.TryGetValue( connection.Id, out var player ) )
				{
					Log.Warning( $"Player not found in the list: {connection.Id}" );
					return;
				}

				// If Mayor is leaving, reset laws
				if ( player.Job?.Name == "Mayor" )
				{
					LawManager.OnMayorLeave();
					chat?.NewSystemMessage( "The Mayor has left. Laws have been reset." );
				}

				// Clean up wanted status for disconnecting player
				WantedManager.RemoveWanted( connection.Id );

				// Clean up hit contracts involving this player
				HitManager.RemovePlayerHits( connection.Id );

				// Clean up mug state for this player
				MugManager.RemovePlayer( connection.Id );

				// Destroy all vehicles owned by this player
				VehicleManager.DestroyAllVehicles( connection.Id );

				// Clean up phone/communication state
				ContactManager.ClearCache( connection.SteamId );
				MessageManager.ClearPlayer( connection.SteamId );
				EmergencyManager.ClearPlayer( connection.Id );
				AdvertManager.ClearPlayer( connection.Id );

				// Clean up NLR tracking
				NLRManager.ClearPlayer( connection.Id );

				// Perform clean up functions
				var playerStats = player.GameObject.Components.Get<Sandbox.GameSystems.Player.Player>();
				if ( playerStats != null )
				{
					playerStats.SellAllDoors();
				}

				//Saves player Data
				Log.Info( $"Saving players data: {connection.Id} {connection.DisplayName}" );
				SavedPlayer.SavePlayer( new SavedPlayer( player ) );

				// Remove the player from the list
				Players.Remove( connection.Id );
				if ( Rpc.Caller.IsHost )
				{
					chat?.NewSystemMessage( $"{connection.DisplayName} has left the game." );
				}
			}
			catch ( Exception e )
			{
				Log.Warning( e );
			}
		}

		void INetworkListener.OnDisconnected( Connection channel )
		{
			Log.Info( $"Player disconnected: {channel.Id}" );
			RemovePlayer( channel );
		}

		public NetworkPlayer GetPlayerByConnectionId( Guid connection )
		{
			if ( Players.TryGetValue( connection, out var player ) )
			{
				return player;
			}
			return null;
		}

		public NetworkPlayer GetPlayerByGameObjectId( Guid gameObjectId )
		{
			foreach ( var player in Players )
			{
				if ( player.Value.GameObject.Id == gameObjectId )
				{
					return player.Value;
				}
			}
			return null;
		}

		public NetworkPlayer GetPlayerByName( string name )
		{
			return Players.Values.FirstOrDefault(
				player => player.Connection.DisplayName.StartsWith( name, StringComparison.OrdinalIgnoreCase ) );
		}

		public NetworkPlayer GetMe()
		{
			return Players[Connection.Local.Id];
		}

		public NetworkPlayer GetPlayerBySteamID( ulong steamID )
		{
			return Players.Values.FirstOrDefault( player => player.Connection.SteamId == steamID );
		}

		/// <summary>
		/// Returns the UserGroup with the specified name.
		/// </summary>
		public UserGroup GetUserGroup( string name )
		{
			if ( UserGroups.TryGetValue( name, out UserGroup userGroup ) )
			{
				return userGroup;
			}
			return null;
		}

		public NetDictionary<Guid, NetworkPlayer> GetAllPlayers()
		{
			return Players;
		}

		/// <summary>
		/// Attempts to find a Player by SteamID first, then by Name.
		/// This should be used for user input,
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public NetworkPlayer PlayerLookup( string input )
		{
			NetworkPlayer foundNetworkPlayer = null;
			// Find the player
			// If args[0] can be parsed as ulong, then try to lookup with SteamID first
			if ( ulong.TryParse( input, out var steamID ) )
			{
				foundNetworkPlayer = GetPlayerBySteamID( steamID );
			}
			// If not found by SteamID, try to find by name
			foundNetworkPlayer ??= GetPlayerByName( input );
			return foundNetworkPlayer;
		}

		[Broadcast]
		public void SelectJob( Guid ownerId, JobResource job )
		{
			var networkPlayer = GetPlayerByConnectionId( ownerId );
			if ( networkPlayer != null )
			{
				// If leaving Mayor job, reset laws
				if ( networkPlayer.Job?.Name == "Mayor" && job?.Name != "Mayor" )
				{
					LawManager.OnMayorLeave();
					chat?.NewSystemMessage( "The Mayor has stepped down. Laws have been reset." );
				}

				networkPlayer.Job = job;

				// If becoming Mayor, activate law system
				if ( job?.Name == "Mayor" )
				{
					LawManager.OnMayorActive();
					chat?.NewSystemMessage( $"{networkPlayer.Name} is now the Mayor!" );
				}
			}
		}
	}

}
