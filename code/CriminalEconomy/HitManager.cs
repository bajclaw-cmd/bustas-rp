using System;
using Sandbox.GameSystems;

namespace GameSystems.CriminalEconomy
{
	/// <summary>
	/// Manages the hit/contract system. Any player can place a hit on another player.
	/// Only Hitmen can accept and complete hits. Hits expire after 10 minutes.
	/// </summary>
	public static class HitManager
	{
		public record HitContract( Guid ClientId, string ClientName, Guid TargetId, string TargetName, float Amount, RealTimeSince TimeSinceCreated )
		{
			/// <summary>
			/// The Hitman who accepted this contract. Null if not yet accepted.
			/// </summary>
			public Guid? AcceptedBy { get; set; } = null;
		}

		private static readonly Dictionary<Guid, HitContract> _activeHits = new();

		/// <summary>
		/// Place a hit on a target player. Returns true on success.
		/// </summary>
		public static bool PlaceHit( Guid clientId, string clientName, Guid targetId, string targetName, float amount )
		{
			CleanupExpired();

			// Minimum price check
			if ( amount < BustasConfig.HitMinPrice )
				return false;

			// Can't place a hit on yourself
			if ( clientId == targetId )
				return false;

			// Don't allow duplicate hits on same target
			if ( _activeHits.ContainsKey( targetId ) )
				return false;

			_activeHits[targetId] = new HitContract( clientId, clientName, targetId, targetName, amount, 0 );
			Log.Info( $"Hit placed on {targetName} for ${amount} by {clientName}" );
			return true;
		}

		/// <summary>
		/// Hitman accepts a hit on a target. Returns the contract, or null if not found.
		/// </summary>
		public static HitContract AcceptHit( Guid hitmanId, Guid targetId )
		{
			CleanupExpired();

			if ( !_activeHits.TryGetValue( targetId, out var contract ) )
				return null;

			// Already accepted by someone
			if ( contract.AcceptedBy != null )
				return null;

			// Hitman can't accept a hit on themselves
			if ( hitmanId == targetId )
				return null;

			// Can't accept your own hit
			if ( hitmanId == contract.ClientId )
				return null;

			contract.AcceptedBy = hitmanId;
			return contract;
		}

		/// <summary>
		/// Hitman accepts the first available (unaccepted) hit. Returns the contract, or null.
		/// </summary>
		public static HitContract AcceptFirstAvailable( Guid hitmanId )
		{
			CleanupExpired();

			foreach ( var kvp in _activeHits )
			{
				var contract = kvp.Value;
				if ( contract.AcceptedBy == null && contract.ClientId != hitmanId && contract.TargetId != hitmanId )
				{
					contract.AcceptedBy = hitmanId;
					return contract;
				}
			}
			return null;
		}

		/// <summary>
		/// Called when the target is killed. Returns the contract if a hitman completed it, or null.
		/// </summary>
		public static HitContract CompleteHit( Guid targetId )
		{
			if ( !_activeHits.TryGetValue( targetId, out var contract ) )
				return null;

			_activeHits.Remove( targetId );
			return contract;
		}

		/// <summary>
		/// Cancel/remove a hit on a target.
		/// </summary>
		public static void CancelHit( Guid targetId )
		{
			_activeHits.Remove( targetId );
		}

		/// <summary>
		/// Check if a player has an active hit on them.
		/// </summary>
		public static bool HasHit( Guid targetId )
		{
			if ( !_activeHits.TryGetValue( targetId, out var contract ) )
				return false;

			if ( contract.TimeSinceCreated >= BustasConfig.HitExpireDuration )
			{
				_activeHits.Remove( targetId );
				return false;
			}

			return true;
		}

		/// <summary>
		/// Get the hit contract for a target, or null.
		/// </summary>
		public static HitContract GetHit( Guid targetId )
		{
			if ( !HasHit( targetId ) )
				return null;
			return _activeHits[targetId];
		}

		/// <summary>
		/// Get the hit that a specific hitman has accepted, or null.
		/// </summary>
		public static HitContract GetAcceptedHit( Guid hitmanId )
		{
			CleanupExpired();
			foreach ( var kvp in _activeHits )
			{
				if ( kvp.Value.AcceptedBy == hitmanId )
					return kvp.Value;
			}
			return null;
		}

		/// <summary>
		/// Get all available (unaccepted) hits.
		/// </summary>
		public static List<HitContract> GetAvailableHits()
		{
			CleanupExpired();
			var available = new List<HitContract>();
			foreach ( var kvp in _activeHits )
			{
				if ( kvp.Value.AcceptedBy == null )
					available.Add( kvp.Value );
			}
			return available;
		}

		/// <summary>
		/// Number of active hits.
		/// </summary>
		public static int ActiveCount
		{
			get
			{
				CleanupExpired();
				return _activeHits.Count;
			}
		}

		/// <summary>
		/// Clean up expired hits.
		/// </summary>
		public static void CleanupExpired()
		{
			var expired = new List<Guid>();
			foreach ( var kvp in _activeHits )
			{
				if ( kvp.Value.TimeSinceCreated >= BustasConfig.HitExpireDuration )
				{
					expired.Add( kvp.Key );
				}
			}
			foreach ( var id in expired )
			{
				_activeHits.Remove( id );
			}
		}

		/// <summary>
		/// Remove all hits placed by or targeting a specific player (for disconnect cleanup).
		/// </summary>
		public static void RemovePlayerHits( Guid connectionId )
		{
			var toRemove = new List<Guid>();
			foreach ( var kvp in _activeHits )
			{
				if ( kvp.Value.ClientId == connectionId || kvp.Value.TargetId == connectionId || kvp.Value.AcceptedBy == connectionId )
				{
					toRemove.Add( kvp.Key );
				}
			}
			foreach ( var id in toRemove )
			{
				_activeHits.Remove( id );
			}
		}
	}
}
