using System;
using Sandbox.GameSystems;

namespace GameSystems.Phone
{
	/// <summary>
	/// Manages 911 emergency calls. Any player can call 911 from the phone.
	/// Alert goes to all online police with caller location.
	/// 60 second cooldown between calls.
	/// </summary>
	public static class EmergencyManager
	{
		public record EmergencyCall( Guid CallerConnectionId, string CallerName, Vector3 CallerPosition, RealTimeSince TimeSinceCall );

		private static readonly List<EmergencyCall> _recentCalls = new();
		private static readonly Dictionary<Guid, RealTimeSince> _cooldowns = new();

		/// <summary>
		/// Place a 911 call. Returns true on success.
		/// </summary>
		public static bool Call911( Guid callerConnectionId, string callerName, Vector3 callerPosition )
		{
			// Check cooldown
			if ( IsOnCooldown( callerConnectionId ) )
				return false;

			var call = new EmergencyCall( callerConnectionId, callerName, callerPosition, 0 );
			_recentCalls.Add( call );
			_cooldowns[callerConnectionId] = 0;

			// Trim old calls (keep last 20)
			while ( _recentCalls.Count > 20 )
				_recentCalls.RemoveAt( 0 );

			Log.Info( $"911 call from {callerName} at {callerPosition}" );
			return true;
		}

		/// <summary>
		/// Check if a player is on 911 cooldown.
		/// </summary>
		public static bool IsOnCooldown( Guid connectionId )
		{
			if ( !_cooldowns.TryGetValue( connectionId, out var timeSince ) )
				return false;

			if ( timeSince >= BustasConfig.EmergencyCooldown )
			{
				_cooldowns.Remove( connectionId );
				return false;
			}

			return true;
		}

		/// <summary>
		/// Get remaining cooldown time in seconds.
		/// </summary>
		public static float GetCooldownRemaining( Guid connectionId )
		{
			if ( !_cooldowns.TryGetValue( connectionId, out var timeSince ) )
				return 0f;

			float remaining = BustasConfig.EmergencyCooldown - timeSince;
			return remaining > 0 ? remaining : 0f;
		}

		/// <summary>
		/// Get recent emergency calls (for police HUD display).
		/// Returns calls from the last 60 seconds.
		/// </summary>
		public static List<EmergencyCall> GetRecentCalls()
		{
			_recentCalls.RemoveAll( c => c.TimeSinceCall >= BustasConfig.EmergencyCooldown );
			return new List<EmergencyCall>( _recentCalls );
		}

		/// <summary>
		/// Clean up on player disconnect.
		/// </summary>
		public static void ClearPlayer( Guid connectionId )
		{
			_cooldowns.Remove( connectionId );
		}
	}
}
