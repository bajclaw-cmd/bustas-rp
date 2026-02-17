using System;
using Sandbox.GameSystems;

namespace GameSystems.LawOrder
{
	/// <summary>
	/// Manages the warrant system. Police Chief or Mayor can issue warrants.
	/// Warrants last 3 minutes, max 2 active at a time.
	/// Warrants allow police to legally enter a player's property.
	/// </summary>
	public static class WarrantManager
	{
		public record WarrantEntry( string TargetName, string Reason, Guid IssuedBy, RealTimeSince TimeSinceIssued );

		private static readonly Dictionary<Guid, WarrantEntry> _activeWarrants = new();

		/// <summary>
		/// Issue a warrant against a player. Returns true on success.
		/// </summary>
		public static bool IssueWarrant( Guid targetConnectionId, string targetName, string reason, Guid issuedBy )
		{
			CleanupExpired();

			// Check max active warrants
			if ( _activeWarrants.Count >= BustasConfig.MaxActiveWarrants )
				return false;

			// Don't allow duplicate warrants on same player
			if ( _activeWarrants.ContainsKey( targetConnectionId ) )
				return false;

			_activeWarrants[targetConnectionId] = new WarrantEntry( targetName, reason, issuedBy, 0 );
			Log.Info( $"Warrant issued for {targetName}: {reason}" );
			return true;
		}

		/// <summary>
		/// Check if a player has an active warrant.
		/// </summary>
		public static bool HasWarrant( Guid connectionId )
		{
			if ( !_activeWarrants.TryGetValue( connectionId, out var entry ) )
				return false;

			if ( entry.TimeSinceIssued >= BustasConfig.WarrantDuration )
			{
				_activeWarrants.Remove( connectionId );
				return false;
			}

			return true;
		}

		/// <summary>
		/// Get the warrant details for a player, or null if none.
		/// </summary>
		public static WarrantEntry GetWarrant( Guid connectionId )
		{
			if ( !HasWarrant( connectionId ) )
				return null;

			return _activeWarrants[connectionId];
		}

		/// <summary>
		/// Revoke a warrant on a player.
		/// </summary>
		public static void RevokeWarrant( Guid connectionId )
		{
			_activeWarrants.Remove( connectionId );
		}

		/// <summary>
		/// Get remaining warrant time in seconds.
		/// </summary>
		public static float GetTimeRemaining( Guid connectionId )
		{
			if ( !_activeWarrants.TryGetValue( connectionId, out var entry ) )
				return 0f;

			float remaining = BustasConfig.WarrantDuration - entry.TimeSinceIssued;
			return remaining > 0 ? remaining : 0f;
		}

		/// <summary>
		/// Clean up expired warrants.
		/// </summary>
		public static void CleanupExpired()
		{
			var expired = new List<Guid>();
			foreach ( var kvp in _activeWarrants )
			{
				if ( kvp.Value.TimeSinceIssued >= BustasConfig.WarrantDuration )
				{
					expired.Add( kvp.Key );
				}
			}
			foreach ( var id in expired )
			{
				_activeWarrants.Remove( id );
			}
		}

		/// <summary>
		/// Number of currently active warrants.
		/// </summary>
		public static int ActiveCount
		{
			get
			{
				CleanupExpired();
				return _activeWarrants.Count;
			}
		}
	}
}
