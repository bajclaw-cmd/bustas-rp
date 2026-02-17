using System;
using Sandbox.GameSystems;

namespace GameSystems.CriminalEconomy
{
	/// <summary>
	/// Manages the mugging system. Thieves can mug players with a 5-minute cooldown.
	/// Target has 10 seconds to drop money or resist.
	/// Max mug amount: $500 (BustasConfig.MugMaxAmount).
	/// </summary>
	public static class MugManager
	{
		public record MugAttempt( Guid MuggerId, string MuggerName, Guid TargetId, string TargetName, RealTimeSince TimeSinceStarted );

		private static readonly Dictionary<Guid, MugAttempt> _activeMugs = new();
		private static readonly Dictionary<Guid, RealTimeSince> _cooldowns = new();

		/// <summary>
		/// Duration a target has to comply with a mug (seconds).
		/// </summary>
		public const float MugDuration = 10f;

		/// <summary>
		/// Start a mug attempt. Returns true on success.
		/// </summary>
		public static bool StartMug( Guid muggerId, string muggerName, Guid targetId, string targetName )
		{
			CleanupExpired();

			// Check cooldown
			if ( IsOnCooldown( muggerId ) )
				return false;

			// Can't mug yourself
			if ( muggerId == targetId )
				return false;

			// Target already being mugged
			if ( _activeMugs.ContainsKey( targetId ) )
				return false;

			// Mugger already mugging someone
			foreach ( var kvp in _activeMugs )
			{
				if ( kvp.Value.MuggerId == muggerId )
					return false;
			}

			_activeMugs[targetId] = new MugAttempt( muggerId, muggerName, targetId, targetName, 0 );

			// Set cooldown immediately
			_cooldowns[muggerId] = 0;

			Log.Info( $"{muggerName} is mugging {targetName}" );
			return true;
		}

		/// <summary>
		/// Check if a mugger is on cooldown.
		/// </summary>
		public static bool IsOnCooldown( Guid muggerId )
		{
			if ( !_cooldowns.TryGetValue( muggerId, out var timeSince ) )
				return false;

			if ( timeSince >= BustasConfig.MugCooldown )
			{
				_cooldowns.Remove( muggerId );
				return false;
			}

			return true;
		}

		/// <summary>
		/// Get remaining cooldown time in seconds.
		/// </summary>
		public static float GetCooldownRemaining( Guid muggerId )
		{
			if ( !_cooldowns.TryGetValue( muggerId, out var timeSince ) )
				return 0f;

			float remaining = BustasConfig.MugCooldown - timeSince;
			return remaining > 0 ? remaining : 0f;
		}

		/// <summary>
		/// Get the active mug attempt on a target, or null.
		/// </summary>
		public static MugAttempt GetActiveMug( Guid targetId )
		{
			if ( !_activeMugs.TryGetValue( targetId, out var attempt ) )
				return null;

			// Auto-expire after MugDuration
			if ( attempt.TimeSinceStarted >= MugDuration )
			{
				_activeMugs.Remove( targetId );
				return null;
			}

			return attempt;
		}

		/// <summary>
		/// Check if a player is currently being mugged.
		/// </summary>
		public static bool IsBeingMugged( Guid targetId )
		{
			return GetActiveMug( targetId ) != null;
		}

		/// <summary>
		/// Get remaining time for a mug attempt in seconds.
		/// </summary>
		public static float GetMugTimeRemaining( Guid targetId )
		{
			if ( !_activeMugs.TryGetValue( targetId, out var attempt ) )
				return 0f;

			float remaining = MugDuration - attempt.TimeSinceStarted;
			return remaining > 0 ? remaining : 0f;
		}

		/// <summary>
		/// End a mug attempt (target complied or resisted).
		/// </summary>
		public static MugAttempt EndMug( Guid targetId )
		{
			if ( _activeMugs.TryGetValue( targetId, out var attempt ) )
			{
				_activeMugs.Remove( targetId );
				return attempt;
			}
			return null;
		}

		/// <summary>
		/// Clean up expired mug attempts.
		/// </summary>
		public static void CleanupExpired()
		{
			var expired = new List<Guid>();
			foreach ( var kvp in _activeMugs )
			{
				if ( kvp.Value.TimeSinceStarted >= MugDuration )
				{
					expired.Add( kvp.Key );
				}
			}
			foreach ( var id in expired )
			{
				_activeMugs.Remove( id );
			}
		}

		/// <summary>
		/// Remove all mug state for a disconnecting player.
		/// </summary>
		public static void RemovePlayer( Guid connectionId )
		{
			// Remove if they're being mugged
			_activeMugs.Remove( connectionId );

			// Remove if they're mugging someone
			var toRemove = new List<Guid>();
			foreach ( var kvp in _activeMugs )
			{
				if ( kvp.Value.MuggerId == connectionId )
					toRemove.Add( kvp.Key );
			}
			foreach ( var id in toRemove )
			{
				_activeMugs.Remove( id );
			}

			// Remove cooldown
			_cooldowns.Remove( connectionId );
		}
	}
}
