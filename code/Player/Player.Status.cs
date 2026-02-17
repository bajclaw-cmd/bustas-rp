using System;
using Entity.Interactable.Door;
using GameSystems;
using GameSystems.Player;
using GameSystems.UI;
using Sandbox.GameSystems;
using Sandbox.GameSystems.Database;

namespace Sandbox.GameSystems.Player
{

	public partial class Player
	{
		[Sync][Property, Group( "Status" )] public List<GameObject> Doors { get; private set; } = new();
		[Sync][Property, Group("Status")]  public List<GameObject> CanOwnDoors { get; private set; } = new();
		[Sync, HostSync][Property, Group( "Status" )] public float Balance { get; set; } = BustasConfig.StartingMoney;
		[Property, Group( "Status" )] public float Health { get; private set; } = 100f;
		[Property, Group( "Status" )] public float Hunger { get; private set; } = 100f;
		[Property, Group( "Status" )] public float MaxHealth { get; private set; } = 100f;
		[Property, Group( "Status" )] public float HungerMax { get; private set; } = 100f;
		[Property, Group( "Status" )] public bool Dead { get; private set; } = false;
		[Property, Group( "Status" )] public bool Starving { get; private set; } = false;
		[Property] private float _salaryTimerSeconds { get; set; } = BustasConfig.SalaryInterval;
		[Property] private float _starvingTimerSeconds { get; set; } = 20f;
		private Chat _chat { get; set; }
		private GameController _controller { get; set; }
		private static readonly uint _saveCooldown = 30;
		private TimeSince _lastUsed = 0; // Set the timer
		private TimeSince _lastUsedFood = 0;
		//Pereodiocal player data save in seconds
		private TimeSince _lastSaved = 0;

		// NLR tracking
		private bool _nlrWarningShown = false;
		private TimeSince _lastNLRWarning = 0;

		// Kill attribution
		public string LastAttacker { get; set; } = "";

		// Health regeneration
		private TimeSince _lastDamageTaken = 999f;
		private TimeSince _lastRegenTick = 0;

		// TODO add a "/sellallowneddoors" command to sell all doors owned by the player

		private void OnStartStatus()
		{
			_chat = Scene.Directory.FindByName( "Screen" )?.First()?.Components.Get<Chat>();
			if ( _chat is null ) { Log.Error( "Chat component not found" ); }
			_controller = GameController.Instance;

			// Load saved money from persistence
			if ( Networking.IsHost )
			{
				var steamId = Network.OwnerConnection?.SteamId ?? 0;
				if ( steamId > 0 )
				{
					var saved = SavedPlayer.LoadSavedPlayer( steamId );
					if ( saved != null )
					{
						Balance = saved.Money;
						Log.Info( $"Loaded saved balance: ${saved.Money:N0} for {steamId}" );
					}
				}
			}
		}

		private void OnFixedUpdateStatus()
		{
			if ( _lastUsed >= _salaryTimerSeconds && (Networking.IsHost) )
			{
				var networkPlayer = GetNetworkPlayer();
				if ( networkPlayer != null )
				{
					float salary = networkPlayer.Job.Salary;
					if ( networkPlayer.IsVIP )
					{
						salary *= BustasConfig.VIPSalaryMultiplier;
					}
					Balance += salary;
					BroadcastSalarySound();
					_lastUsed = 0;
				}
			}

			if ( _lastSaved >= _saveCooldown && (Networking.IsHost) )
			{

				if ( GetNetworkPlayer() != null )
				{
					SavedPlayer.SavePlayer( new SavedPlayer( this.GetNetworkPlayer() ) );
					_lastSaved = 0; // reset the timer
				}

			}

			if ( _lastUsedFood >= _starvingTimerSeconds && (Network.IsOwner) && (Starving) )
			{
				if ( Hunger > 0 )
				{
					Hunger -= 1;
				}
				_lastUsedFood = 0; // reset the timer
			}

			// Health regeneration (only when not recently damaged, not dead, and not starving)
			if ( !Dead && Health < MaxHealth && Health > 0 && Hunger > BustasConfig.HealthRegenMinHunger )
			{
				if ( _lastDamageTaken >= BustasConfig.HealthRegenDelay && _lastRegenTick >= BustasConfig.HealthRegenInterval )
				{
					Health += BustasConfig.HealthRegenRate;
					if ( Health > MaxHealth ) Health = MaxHealth;
					_lastRegenTick = 0;
				}
			}

			if ( Health < 1 || Hunger < 1 )
			{
				if ( !Dead )
				{
					OnDeath( LastAttacker );
					LastAttacker = "";
				}
				Dead = true;
				Health = 0;
				Hunger = 0;
			}
			if ( Health > MaxHealth ) { Health = MaxHealth; }
			if ( Hunger > HungerMax ) { Hunger = HungerMax; }

			// NLR violation check
			if ( !Dead )
			{
				var networkPlayer = GetNetworkPlayer();
				if ( networkPlayer != null && NLRManager.IsViolatingNLR( networkPlayer.Connection.Id, GameObject.Transform.Position ) )
				{
					if ( _lastNLRWarning >= 5f )
					{
						SendMessage( "NLR VIOLATION - Leave this area! You cannot return to your death location." );
						_lastNLRWarning = 0;
					}
				}
			}
		}

		/// <summary>
		/// Called when the player dies. Records NLR, cleans up weapons, and shows death screen.
		/// </summary>
		private void OnDeath( string killerName = "" )
		{
			var networkPlayer = GetNetworkPlayer();
			if ( networkPlayer != null )
			{
				// Record death position for NLR
				NLRManager.RecordDeath( networkPlayer.Connection.Id, GameObject.Transform.Position );
			}

			// Clean up weapons on death
			ClearInventory();

			// Show death screen
			DeathScreen?.Show( killerName );

			Log.Info( $"Player {networkPlayer?.Name ?? "Unknown"} has died." );
		}

		/// <summary>
		/// Called from DeathScreen when player clicks Respawn.
		/// </summary>
		public void PerformRespawn()
		{
			Dead = false;
			Health = MaxHealth;
			Hunger = HungerMax;
			LastAttacker = "";
			_lastDamageTaken = 999f;

			// Find a spawn point and teleport there
			var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToList();
			if ( spawnPoints.Count > 0 )
			{
				var random = new Random();
				var spawn = spawnPoints[random.Next( spawnPoints.Count )];
				GameObject.Transform.Position = spawn.Transform.Position;
				GameObject.Transform.Rotation = spawn.Transform.Rotation;
			}

			// Re-equip default items
			OnStartInventory();

			DeathScreen?.Hide();
			_nlrWarningShown = false;
		}

		/// <summary>
		/// Helper function to find the player's PlayerDetails
		/// </summary>
		/// <returns></returns>
		public NetworkPlayer GetNetworkPlayer()
		{
			return _controller.GetPlayerByGameObjectId( GameObject.Id );
		}

		/// <summary>
		/// Updates the player's balance. If the amount is negative, it checks if the player can afford it. Returns false if the player can't afford it.
		/// </summary>
		public bool UpdateBalance( float Amount )
		{
			// If the amount is a negative, check if the player can afford it
			if ( Amount < 0 )
			{
				if ( Balance < Math.Abs( Amount ) )
				{
					Sound.Play( "audio/error.sound" );
					return false;
				}
			}
			Balance += Amount;
			if ( Balance > BustasConfig.MaxWalletMoney )
			{
				Balance = BustasConfig.MaxWalletMoney;
			}
			return true;
		}

		public void SetBalance( float Amount )
		{
			Balance = Amount;
		}

		public void UpdateHunger( float Amount )
		{
			Hunger += Amount;
		}
		public void SetHunger( float Amount )
		{
			Hunger = Amount;
		}

		[Broadcast]
		private void BroadcastSalarySound()
		{
			if ( !Network.IsOwner ) return;
			Sound.Play( "audio/Notification.sound" );
		}

		public void SetHealth( float amount )
		{
			if ( amount < Health )
			{
				_lastDamageTaken = 0;
			}
			Health = amount;
		}

		public void SetMaxHealth( float amount )
		{
			MaxHealth = amount;
		}

		public void SellAllDoors()
		{
			Log.Info( "Selling All " + Doors.Count + " doors" );
			foreach ( var door in Doors )
			{
				DoorLogic doorLogic = door.Components.Get<DoorLogic>();
				doorLogic.RemoveDoorOwner(this);
			}

			for (int i = 0; i < CanOwnDoors.Count; i++)
			{
				GameObject door = CanOwnDoors[i];
				DoorLogic doorLogic = door.Components.Get<DoorLogic>();
				doorLogic.RemoveDoorOwner(this);
			}
			SendMessage( "All doors have been sold." );
		}

		public void TakeDoorOwnership( GameObject door )
		{
			door.Network.TakeOwnership();

			Log.Info( $"TakeOwnership to door : {door}" );

		}

		public void DropDoorOwnership( GameObject door )
		{
			door.Network.DropOwnership();

			Log.Info( $"DropOwnership to door : {door}" );
		}

		// TODO this would need to go to its own class. PlayerController or some shit
		public void SendMessage( string message )
		{
			using ( Rpc.FilterInclude( c => c.Id == GameObject.Network.OwnerId ) )
			{
				_chat?.NewSystemMessage( message );
			}
		}
	}
}
