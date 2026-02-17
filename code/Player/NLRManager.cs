using System;
using Sandbox.GameSystems;

namespace GameSystems.Player
{
	/// <summary>
	/// Tracks NLR (New Life Rule) zones. Players cannot return to their death location
	/// for NLRDuration seconds within NLRRadius units.
	/// </summary>
	public static class NLRManager
	{
		public record NLREntry( Vector3 DeathPosition, RealTimeSince TimeSinceDeath );

		private static readonly Dictionary<Guid, NLREntry> _nlrZones = new();

		/// <summary>
		/// Record a player's death location for NLR tracking.
		/// </summary>
		public static void RecordDeath( Guid connectionId, Vector3 deathPosition )
		{
			_nlrZones[connectionId] = new NLREntry( deathPosition, 0 );
		}

		/// <summary>
		/// Check if a player is violating NLR at the given position.
		/// </summary>
		public static bool IsViolatingNLR( Guid connectionId, Vector3 currentPosition )
		{
			if ( !_nlrZones.TryGetValue( connectionId, out var entry ) )
				return false;

			// NLR expired
			if ( entry.TimeSinceDeath >= BustasConfig.NLRDuration )
			{
				_nlrZones.Remove( connectionId );
				return false;
			}

			// Check distance
			float distance = Vector3.DistanceBetween( currentPosition, entry.DeathPosition );
			return distance <= BustasConfig.NLRRadius;
		}

		/// <summary>
		/// Get time remaining on NLR in seconds, or 0 if not active.
		/// </summary>
		public static float GetTimeRemaining( Guid connectionId )
		{
			if ( !_nlrZones.TryGetValue( connectionId, out var entry ) )
				return 0f;

			float remaining = BustasConfig.NLRDuration - entry.TimeSinceDeath;
			return remaining > 0 ? remaining : 0f;
		}

		/// <summary>
		/// Check if a player has an active NLR zone.
		/// </summary>
		public static bool HasActiveNLR( Guid connectionId )
		{
			if ( !_nlrZones.TryGetValue( connectionId, out var entry ) )
				return false;

			if ( entry.TimeSinceDeath >= BustasConfig.NLRDuration )
			{
				_nlrZones.Remove( connectionId );
				return false;
			}

			return true;
		}

		/// <summary>
		/// Clear NLR for a player (on disconnect).
		/// </summary>
		public static void ClearPlayer( Guid connectionId )
		{
			_nlrZones.Remove( connectionId );
		}
	}
}
