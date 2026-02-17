using System;
using Sandbox.GameSystems;

namespace Entity.Vehicle
{
	/// <summary>
	/// Tracks vehicle ownership and enforces per-player vehicle limits.
	/// </summary>
	public static class VehicleManager
	{
		private static readonly Dictionary<Guid, List<GameObject>> _playerVehicles = new();
		private static readonly Dictionary<Guid, RealTimeSince> _spawnCooldowns = new();

		/// <summary>
		/// Register a spawned vehicle for a player. Returns false if at limit.
		/// </summary>
		public static bool RegisterVehicle( Guid connectionId, GameObject vehicle )
		{
			if ( !_playerVehicles.ContainsKey( connectionId ) )
				_playerVehicles[connectionId] = new List<GameObject>();

			// Clean up any destroyed vehicles first
			CleanupDestroyedVehicles( connectionId );

			if ( _playerVehicles[connectionId].Count >= BustasConfig.MaxVehiclesPerPlayer )
				return false;

			_playerVehicles[connectionId].Add( vehicle );
			return true;
		}

		/// <summary>
		/// Remove a specific vehicle from a player's list.
		/// </summary>
		public static void RemoveVehicle( Guid connectionId, GameObject vehicle )
		{
			if ( !_playerVehicles.TryGetValue( connectionId, out var vehicles ) )
				return;

			vehicles.Remove( vehicle );
		}

		/// <summary>
		/// Get the number of vehicles a player currently owns.
		/// </summary>
		public static int GetVehicleCount( Guid connectionId )
		{
			if ( !_playerVehicles.TryGetValue( connectionId, out var vehicles ) )
				return 0;

			CleanupDestroyedVehicles( connectionId );
			return vehicles.Count;
		}

		/// <summary>
		/// Get all vehicles owned by a player.
		/// </summary>
		public static List<GameObject> GetPlayerVehicles( Guid connectionId )
		{
			if ( !_playerVehicles.TryGetValue( connectionId, out var vehicles ) )
				return new List<GameObject>();

			CleanupDestroyedVehicles( connectionId );
			return new List<GameObject>( vehicles );
		}

		/// <summary>
		/// Check if a player is on spawn cooldown.
		/// </summary>
		public static bool IsOnCooldown( Guid connectionId )
		{
			if ( !_spawnCooldowns.TryGetValue( connectionId, out var timeSince ) )
				return false;

			if ( timeSince >= BustasConfig.VehicleSpawnCooldown )
			{
				_spawnCooldowns.Remove( connectionId );
				return false;
			}

			return true;
		}

		/// <summary>
		/// Get remaining cooldown in seconds.
		/// </summary>
		public static float GetCooldownRemaining( Guid connectionId )
		{
			if ( !_spawnCooldowns.TryGetValue( connectionId, out var timeSince ) )
				return 0f;

			float remaining = BustasConfig.VehicleSpawnCooldown - timeSince;
			return remaining > 0 ? remaining : 0f;
		}

		/// <summary>
		/// Set spawn cooldown for a player.
		/// </summary>
		public static void SetCooldown( Guid connectionId )
		{
			_spawnCooldowns[connectionId] = 0;
		}

		/// <summary>
		/// Destroy all vehicles owned by a player (for disconnect cleanup).
		/// </summary>
		public static void DestroyAllVehicles( Guid connectionId )
		{
			if ( !_playerVehicles.TryGetValue( connectionId, out var vehicles ) )
				return;

			foreach ( var vehicle in vehicles.ToList() )
			{
				if ( vehicle != null && vehicle.IsValid )
				{
					vehicle.Destroy();
				}
			}

			_playerVehicles.Remove( connectionId );
			_spawnCooldowns.Remove( connectionId );
		}

		/// <summary>
		/// Remove destroyed/invalid vehicles from a player's list.
		/// </summary>
		private static void CleanupDestroyedVehicles( Guid connectionId )
		{
			if ( !_playerVehicles.TryGetValue( connectionId, out var vehicles ) )
				return;

			vehicles.RemoveAll( v => v == null || !v.IsValid );
		}
	}
}
