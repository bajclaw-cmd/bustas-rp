using System;
using Entity.Interactable;
using Sandbox.Entity;
using Sandbox.GameSystems;
using Sandbox.GameSystems.Player;

namespace Entity.Vehicle
{
	/// <summary>
	/// Core vehicle component. Handles ownership, locking, entering/exiting,
	/// damage, and destruction. Attach to a vehicle prefab GameObject.
	/// </summary>
	public class VehicleLogic : BaseEntity
	{
		[Sync] public VehicleType CurrentVehicleType { get; private set; } = VehicleType.Sedan;
		[Sync] public bool IsLocked { get; private set; } = false;
		[Sync] public Guid OwnerConnectionId { get; private set; }
		[Sync] public string OwnerName { get; private set; } = "";
		[Sync] public Guid DriverConnectionId { get; private set; }
		[Sync] public bool IsOccupied { get; private set; } = false;
		[Sync] public float CurrentHealth { get; private set; }
		[Sync] public float MaxHealth { get; private set; } = 100f;

		/// <summary>
		/// Whether this vehicle is destroyed (exploded).
		/// </summary>
		public bool IsDestroyed => CurrentHealth <= 0f;

		/// <summary>
		/// Health as a 0-1 fraction.
		/// </summary>
		public float HealthFraction => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;

		private VehicleConfig _config;
		private GameObject _driverObject;
		private float _currentSpeed;
		private const float AccelerationRate = 300f;
		private const float BrakeRate = 500f;
		private const float FrictionRate = 150f;
		private const float TurnSpeed = 80f;

		protected override void OnStart()
		{
			base.OnStart();
			GameObject.Tags.Add( "Vehicle" );
			_config = VehicleConfigs.Get( CurrentVehicleType );
			MaxHealth = _config.MaxHealth;
			CurrentHealth = MaxHealth;
			EntityName = _config.DisplayName;
		}

		/// <summary>
		/// Set the vehicle type and initialize config. Call immediately after spawning.
		/// </summary>
		[Broadcast]
		public void SetVehicleType( VehicleType type )
		{
			CurrentVehicleType = type;
			_config = VehicleConfigs.Get( type );
			MaxHealth = _config.MaxHealth;
			CurrentHealth = MaxHealth;
			EntityName = _config.DisplayName;
		}

		/// <summary>
		/// Set the owner of this vehicle.
		/// </summary>
		[Broadcast]
		public void SetOwner( Guid connectionId, string playerName )
		{
			OwnerConnectionId = connectionId;
			OwnerName = playerName;
		}

		/// <summary>
		/// E key: Enter or exit the vehicle.
		/// </summary>
		public override void InteractUse( SceneTraceResult tr, GameObject player )
		{
			if ( IsDestroyed ) return;

			var playerComp = player.Components.Get<Player>();
			if ( playerComp == null ) return;

			var networkPlayer = playerComp.GetNetworkPlayer();
			if ( networkPlayer == null ) return;

			var connectionId = networkPlayer.Connection.Id;

			// If the player is the current driver, exit
			if ( IsOccupied && DriverConnectionId == connectionId )
			{
				ExitVehicle( player );
				return;
			}

			// If vehicle is occupied by someone else, can't enter
			if ( IsOccupied )
			{
				playerComp.SendMessage( "This vehicle is already occupied." );
				return;
			}

			// If locked and not the owner, can't enter
			if ( IsLocked && connectionId != OwnerConnectionId )
			{
				playerComp.SendMessage( "This vehicle is locked." );
				return;
			}

			EnterVehicle( player, connectionId );
		}

		/// <summary>
		/// R key: Lock/unlock the vehicle (owner only, must be inside).
		/// </summary>
		public override void InteractSpecial( SceneTraceResult tr, GameObject player )
		{
			if ( IsDestroyed ) return;

			var playerComp = player.Components.Get<Player>();
			if ( playerComp == null ) return;

			var networkPlayer = playerComp.GetNetworkPlayer();
			if ( networkPlayer == null ) return;

			// Only owner can lock/unlock
			if ( networkPlayer.Connection.Id != OwnerConnectionId )
			{
				playerComp.SendMessage( "Only the vehicle owner can lock/unlock." );
				return;
			}

			ToggleLock();
			playerComp.SendMessage( IsLocked ? "Vehicle locked." : "Vehicle unlocked." );
		}

		[Broadcast]
		private void EnterVehicle( GameObject player, Guid connectionId )
		{
			IsOccupied = true;
			DriverConnectionId = connectionId;
			_driverObject = player;

			// Tell the player they're in a vehicle
			var playerComp = player.Components.Get<Player>();
			if ( playerComp != null )
			{
				playerComp.CurrentVehicle = GameObject;
			}

			// Hide the player model and disable their collider
			var collider = player.Components.Get<Collider>();
			if ( collider != null ) collider.Enabled = false;

			// Parent player to vehicle so they move with it
			player.SetParent( GameObject );
			player.Transform.LocalPosition = Vector3.Up * 30f;

			_currentSpeed = 0f;
			Log.Info( $"Player entered {EntityName}" );
		}

		[Broadcast]
		private void ExitVehicle( GameObject player )
		{
			IsOccupied = false;
			DriverConnectionId = Guid.Empty;

			// Unparent player and place them beside the vehicle
			player.SetParent( null );
			player.Transform.Position = GameObject.Transform.Position + GameObject.Transform.Rotation.Right * 80f + Vector3.Up * 10f;

			// Tell the player they're no longer in a vehicle
			var playerComp = player.Components.Get<Player>();
			if ( playerComp != null )
			{
				playerComp.CurrentVehicle = null;
			}

			// Re-enable their collider
			var collider = player.Components.Get<Collider>();
			if ( collider != null ) collider.Enabled = true;

			_driverObject = null;
			_currentSpeed = 0f;
			Log.Info( $"Player exited {EntityName}" );
		}

		[Broadcast]
		private void ToggleLock()
		{
			IsLocked = !IsLocked;
			Log.Info( $"{EntityName} is now {(IsLocked ? "locked" : "unlocked")}" );
		}

		protected override void OnFixedUpdate()
		{
			base.OnFixedUpdate();

			if ( !IsOccupied || _driverObject == null || IsDestroyed ) return;
			if ( IsProxy ) return;

			float maxSpeed = _config?.GetMaxSpeed() ?? BustasConfig.VehicleSpeedMedium;
			float dt = Time.Delta;

			// Get input from the driver
			float forwardInput = 0f;
			float turnInput = 0f;

			if ( Input.Down( "Forward" ) ) forwardInput = 1f;
			else if ( Input.Down( "Backward" ) ) forwardInput = -1f;

			if ( Input.Down( "Left" ) ) turnInput = -1f;
			else if ( Input.Down( "Right" ) ) turnInput = 1f;

			// Brake if pressing opposite direction of travel
			bool isBraking = (forwardInput < 0 && _currentSpeed > 10f) || (forwardInput > 0 && _currentSpeed < -10f);

			if ( isBraking )
			{
				// Apply braking
				float brakeAmount = BrakeRate * dt;
				if ( _currentSpeed > 0 ) _currentSpeed = Math.Max( 0, _currentSpeed - brakeAmount );
				else _currentSpeed = Math.Min( 0, _currentSpeed + brakeAmount );
			}
			else if ( forwardInput != 0 )
			{
				// Accelerate
				_currentSpeed += forwardInput * AccelerationRate * dt;
				_currentSpeed = Math.Clamp( _currentSpeed, -maxSpeed * 0.4f, maxSpeed );
			}
			else
			{
				// Apply friction when no input
				float frictionAmount = FrictionRate * dt;
				if ( _currentSpeed > 0 ) _currentSpeed = Math.Max( 0, _currentSpeed - frictionAmount );
				else if ( _currentSpeed < 0 ) _currentSpeed = Math.Min( 0, _currentSpeed + frictionAmount );
			}

			// Steering (only when moving)
			float speedFactor = Math.Abs( _currentSpeed ) / maxSpeed;
			if ( speedFactor > 0.01f && turnInput != 0 )
			{
				float turnAmount = turnInput * TurnSpeed * Math.Min( speedFactor, 1f ) * dt;
				// Reverse steering when going backwards
				if ( _currentSpeed < 0 ) turnAmount = -turnAmount;
				GameObject.Transform.Rotation *= Rotation.FromYaw( turnAmount );
			}

			// Move the vehicle
			if ( Math.Abs( _currentSpeed ) > 1f )
			{
				var forward = GameObject.Transform.Rotation.Forward;
				GameObject.Transform.Position += forward * _currentSpeed * dt;
			}

			// Exit vehicle with Use key
			if ( Input.Pressed( "Use" ) && _driverObject != null )
			{
				ExitVehicle( _driverObject );
			}
		}

		/// <summary>
		/// Apply damage to the vehicle with a damage type multiplier.
		/// </summary>
		public void ApplyDamage( float baseDamage, VehicleDamageType damageType )
		{
			if ( IsDestroyed ) return;

			float multiplier = damageType switch
			{
				VehicleDamageType.Collision => BustasConfig.VehicleDamageCollisionMultiplier,
				VehicleDamageType.Weapon => BustasConfig.VehicleDamageWeaponMultiplier,
				VehicleDamageType.Explosion => BustasConfig.VehicleDamageExplosionMultiplier,
				_ => 1.0f
			};

			float damage = baseDamage * multiplier;
			CurrentHealth = Math.Max( 0f, CurrentHealth - damage );

			Log.Info( $"{EntityName} took {damage:F0} damage ({damageType}). Health: {CurrentHealth:F0}/{MaxHealth:F0}" );

			if ( CurrentHealth <= 0f )
			{
				OnVehicleDestroyed();
			}
		}

		/// <summary>
		/// Repair the vehicle by a specified amount. Used by Mechanics.
		/// </summary>
		[Broadcast]
		public void Repair( float amount )
		{
			if ( IsDestroyed ) return;

			CurrentHealth = Math.Min( MaxHealth, CurrentHealth + amount );
			Log.Info( $"{EntityName} repaired by {amount:F0}. Health: {CurrentHealth:F0}/{MaxHealth:F0}" );
		}

		/// <summary>
		/// Fully repair the vehicle.
		/// </summary>
		[Broadcast]
		public void FullRepair()
		{
			CurrentHealth = MaxHealth;
		}

		/// <summary>
		/// Called when the vehicle is destroyed (health reaches 0).
		/// Ejects driver, plays explosion, and removes the vehicle.
		/// </summary>
		private void OnVehicleDestroyed()
		{
			Log.Info( $"{EntityName} has been destroyed!" );

			// Eject driver
			if ( IsOccupied && _driverObject != null )
			{
				ExitVehicle( _driverObject );
			}
			else if ( IsOccupied )
			{
				IsOccupied = false;
				DriverConnectionId = Guid.Empty;
			}

			// Remove from owner's vehicle list
			VehicleManager.RemoveVehicle( OwnerConnectionId, GameObject );

			// Destroy the game object after a short delay for explosion effect
			GameObject.Destroy();
		}

		/// <summary>
		/// Get the repair cost based on current damage.
		/// Scales linearly between min and max repair cost based on damage %.
		/// </summary>
		public float GetRepairCost()
		{
			if ( CurrentHealth >= MaxHealth ) return 0f;

			float damageFraction = 1f - HealthFraction;
			return MathX.Lerp( BustasConfig.VehicleRepairCostMin, BustasConfig.VehicleRepairCostMax, damageFraction );
		}
	}

	/// <summary>
	/// Types of damage a vehicle can receive.
	/// </summary>
	public enum VehicleDamageType
	{
		Collision,
		Weapon,
		Explosion
	}
}
