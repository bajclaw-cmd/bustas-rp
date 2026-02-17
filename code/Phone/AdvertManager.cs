using System;
using Sandbox.GameSystems;

namespace GameSystems.Phone
{
	/// <summary>
	/// Manages the advertisement system. Players can post ads via /advert or the phone Ads app.
	/// Ads cost $100, have a 30 second cooldown, and broadcast to all players with [AD] prefix.
	/// </summary>
	public static class AdvertManager
	{
		public record Advert( string AuthorName, string Message, RealTimeSince TimeSincePosted );

		private static readonly Dictionary<Guid, RealTimeSince> _cooldowns = new();
		private static readonly List<Advert> _recentAds = new();

		/// <summary>
		/// Check if a player is on advert cooldown.
		/// </summary>
		public static bool IsOnCooldown( Guid connectionId )
		{
			if ( !_cooldowns.TryGetValue( connectionId, out var timeSince ) )
				return false;

			if ( timeSince >= BustasConfig.AdvertCooldown )
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

			float remaining = BustasConfig.AdvertCooldown - timeSince;
			return remaining > 0 ? remaining : 0f;
		}

		/// <summary>
		/// Post an advertisement. Sets the cooldown. Does NOT handle payment or chat broadcast
		/// (those are handled by the caller). Returns false if on cooldown.
		/// </summary>
		public static bool PostAd( Guid connectionId, string authorName, string message )
		{
			if ( IsOnCooldown( connectionId ) )
				return false;

			_cooldowns[connectionId] = 0;
			_recentAds.Add( new Advert( authorName, message, 0 ) );

			// Trim old ads (keep last 20)
			while ( _recentAds.Count > 20 )
				_recentAds.RemoveAt( 0 );

			Log.Info( $"Ad posted by {authorName}: {message}" );
			return true;
		}

		/// <summary>
		/// Get recent ads (for phone Ads app display).
		/// </summary>
		public static List<Advert> GetRecentAds()
		{
			// Keep ads from the last 5 minutes
			_recentAds.RemoveAll( a => a.TimeSincePosted >= 300f );
			return new List<Advert>( _recentAds );
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
