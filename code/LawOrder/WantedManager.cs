using System;
using Sandbox.GameSystems;

namespace GameSystems.LawOrder
{
	/// <summary>
	/// Manages the wanted system. Police can mark players as wanted.
	/// Wanted status lasts 5 minutes then auto-expires.
	/// </summary>
	public static class WantedManager
	{
		public record WantedEntry( string Reason, RealTimeSince TimeSinceSet );

		private static readonly Dictionary<Guid, WantedEntry> _wantedPlayers = new();

		/// <summary>
		/// Set a player as wanted. ConnectionId is used as key.
		/// </summary>
		public static void SetWanted( Guid connectionId, string reason )
		{
			_wantedPlayers[connectionId] = new WantedEntry( reason, 0 );
			Log.Info( $"Player {connectionId} is now wanted: {reason}" );
		}

		/// <summary>
		/// Remove wanted status from a player.
		/// </summary>
		public static void RemoveWanted( Guid connectionId )
		{
			_wantedPlayers.Remove( connectionId );
			Log.Info( $"Player {connectionId} is no longer wanted." );
		}

		/// <summary>
		/// Check if a player is currently wanted (accounting for expiry).
		/// </summary>
		public static bool IsWanted( Guid connectionId )
		{
			if ( !_wantedPlayers.TryGetValue( connectionId, out var entry ) )
				return false;

			if ( entry.TimeSinceSet >= BustasConfig.WantedDuration )
			{
				_wantedPlayers.Remove( connectionId );
				return false;
			}

			return true;
		}

		/// <summary>
		/// Get the wanted reason for a player, or null if not wanted.
		/// </summary>
		public static string GetWantedReason( Guid connectionId )
		{
			if ( !IsWanted( connectionId ) )
				return null;

			return _wantedPlayers[connectionId].Reason;
		}

		/// <summary>
		/// Get remaining wanted time in seconds, or 0 if not wanted.
		/// </summary>
		public static float GetTimeRemaining( Guid connectionId )
		{
			if ( !_wantedPlayers.TryGetValue( connectionId, out var entry ) )
				return 0f;

			float remaining = BustasConfig.WantedDuration - entry.TimeSinceSet;
			return remaining > 0 ? remaining : 0f;
		}

		/// <summary>
		/// Clean up expired entries. Call periodically.
		/// </summary>
		public static void CleanupExpired()
		{
			var expired = new List<Guid>();
			foreach ( var kvp in _wantedPlayers )
			{
				if ( kvp.Value.TimeSinceSet >= BustasConfig.WantedDuration )
				{
					expired.Add( kvp.Key );
				}
			}
			foreach ( var id in expired )
			{
				_wantedPlayers.Remove( id );
			}
		}

		/// <summary>
		/// Get all currently wanted player connection IDs.
		/// </summary>
		public static IEnumerable<Guid> GetAllWanted()
		{
			CleanupExpired();
			return _wantedPlayers.Keys;
		}
	}
}
