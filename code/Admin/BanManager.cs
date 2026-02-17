using System;

namespace GameSystems.Admin
{
	/// <summary>
	/// Manages persistent player bans. Bans are stored to disk and checked on connect.
	/// Supports permanent and timed bans.
	/// </summary>
	public static class BanManager
	{
		public record BanEntry( ulong SteamId, string PlayerName, string Reason, string BannedBy, DateTime BannedAt, DateTime? ExpiresAt );

		private static readonly Dictionary<ulong, BanEntry> _bans = new();
		private const string BanFile = "playersdata/bans.json";
		private static bool _loaded = false;

		/// <summary>
		/// Ban a player. Duration in minutes. 0 = permanent.
		/// </summary>
		public static void Ban( ulong steamId, string playerName, int durationMinutes, string reason, string bannedBy )
		{
			EnsureLoaded();

			DateTime? expiresAt = durationMinutes > 0
				? DateTime.UtcNow.AddMinutes( durationMinutes )
				: null;

			_bans[steamId] = new BanEntry( steamId, playerName, reason, bannedBy, DateTime.UtcNow, expiresAt );
			Save();

			Log.Info( $"[BAN] {playerName} ({steamId}) banned by {bannedBy} for {(durationMinutes > 0 ? $"{durationMinutes}m" : "permanent")}: {reason}" );
		}

		/// <summary>
		/// Unban a player by SteamID.
		/// </summary>
		public static bool Unban( ulong steamId )
		{
			EnsureLoaded();

			if ( !_bans.Remove( steamId ) )
				return false;

			Save();
			Log.Info( $"[UNBAN] {steamId} has been unbanned." );
			return true;
		}

		/// <summary>
		/// Check if a player is currently banned. Automatically removes expired bans.
		/// </summary>
		public static bool IsBanned( ulong steamId )
		{
			EnsureLoaded();

			if ( !_bans.TryGetValue( steamId, out var ban ) )
				return false;

			// Check if ban has expired
			if ( ban.ExpiresAt.HasValue && ban.ExpiresAt.Value <= DateTime.UtcNow )
			{
				_bans.Remove( steamId );
				Save();
				return false;
			}

			return true;
		}

		/// <summary>
		/// Get ban data for a player, or null if not banned.
		/// </summary>
		public static BanEntry GetBan( ulong steamId )
		{
			if ( !IsBanned( steamId ) ) return null;
			return _bans.GetValueOrDefault( steamId );
		}

		/// <summary>
		/// Get a human-readable string of the remaining ban duration.
		/// </summary>
		public static string GetBanTimeRemaining( ulong steamId )
		{
			var ban = GetBan( steamId );
			if ( ban == null ) return "Not banned";
			if ( !ban.ExpiresAt.HasValue ) return "Permanent";

			var remaining = ban.ExpiresAt.Value - DateTime.UtcNow;
			if ( remaining.TotalDays >= 1 )
				return $"{remaining.Days}d {remaining.Hours}h";
			if ( remaining.TotalHours >= 1 )
				return $"{remaining.Hours}h {remaining.Minutes}m";
			return $"{remaining.Minutes}m";
		}

		private static void Save()
		{
			try
			{
				var list = new List<BanEntry>( _bans.Values );
				var json = Json.Serialize( list );
				FileSystem.Data.WriteAllText( BanFile, json );
			}
			catch ( Exception e )
			{
				Log.Warning( $"Failed to save bans: {e.Message}" );
			}
		}

		private static void EnsureLoaded()
		{
			if ( _loaded ) return;
			_loaded = true;

			try
			{
				if ( !FileSystem.Data.FileExists( BanFile ) ) return;

				var json = FileSystem.Data.ReadAllText( BanFile );
				var loaded = Json.Deserialize<List<BanEntry>>( json );
				if ( loaded != null )
				{
					foreach ( var ban in loaded )
					{
						_bans[ban.SteamId] = ban;
					}
				}
			}
			catch ( Exception e )
			{
				Log.Warning( $"Failed to load bans: {e.Message}" );
			}
		}
	}
}
