using System;

namespace GameSystems.Admin
{
	/// <summary>
	/// Manages VIP status with expiration dates. VIP data persists between sessions.
	/// VIP is separate from admin ranks — it's a purchasable/grantable perk.
	/// </summary>
	public static class VIPManager
	{
		public record VIPData( ulong SteamId, DateTime ExpiresAt, string GrantedBy, DateTime GrantedAt );

		private static readonly Dictionary<ulong, VIPData> _vipPlayers = new();
		private const string VIPDir = "playersdata/vip";

		/// <summary>
		/// Grant VIP to a player for a number of days. If already VIP, extends the duration.
		/// </summary>
		public static void GrantVIP( ulong steamId, int days, string grantedBy )
		{
			var now = DateTime.UtcNow;
			DateTime expiresAt;

			if ( _vipPlayers.TryGetValue( steamId, out var existing ) && existing.ExpiresAt > now )
			{
				// Extend existing VIP
				expiresAt = existing.ExpiresAt.AddDays( days );
			}
			else
			{
				expiresAt = now.AddDays( days );
			}

			_vipPlayers[steamId] = new VIPData( steamId, expiresAt, grantedBy, now );
			SaveVIP( steamId );
			Log.Info( $"VIP granted to {steamId} until {expiresAt} by {grantedBy}" );
		}

		/// <summary>
		/// Remove VIP from a player immediately.
		/// </summary>
		public static void RemoveVIP( ulong steamId )
		{
			_vipPlayers.Remove( steamId );

			try
			{
				var path = $"{VIPDir}/{steamId}.json";
				if ( FileSystem.Data.FileExists( path ) )
					FileSystem.Data.DeleteFile( path );
			}
			catch ( Exception e )
			{
				Log.Warning( $"Failed to delete VIP file for {steamId}: {e.Message}" );
			}

			Log.Info( $"VIP removed from {steamId}" );
		}

		/// <summary>
		/// Check if a player currently has VIP status.
		/// </summary>
		public static bool IsVIP( ulong steamId )
		{
			if ( !_vipPlayers.TryGetValue( steamId, out var data ) )
			{
				// Try loading from disk
				data = LoadVIP( steamId );
				if ( data == null ) return false;
				_vipPlayers[steamId] = data;
			}

			if ( data.ExpiresAt <= DateTime.UtcNow )
			{
				_vipPlayers.Remove( steamId );
				return false;
			}

			return true;
		}

		/// <summary>
		/// Get VIP data for a player, or null if not VIP.
		/// </summary>
		public static VIPData GetVIPData( ulong steamId )
		{
			if ( !IsVIP( steamId ) ) return null;
			return _vipPlayers.GetValueOrDefault( steamId );
		}

		/// <summary>
		/// Get remaining VIP time as a formatted string.
		/// </summary>
		public static string GetTimeRemaining( ulong steamId )
		{
			var data = GetVIPData( steamId );
			if ( data == null ) return "No VIP";

			var remaining = data.ExpiresAt - DateTime.UtcNow;
			if ( remaining.TotalDays >= 1 )
				return $"{remaining.Days}d {remaining.Hours}h";
			if ( remaining.TotalHours >= 1 )
				return $"{remaining.Hours}h {remaining.Minutes}m";
			return $"{remaining.Minutes}m";
		}

		/// <summary>
		/// Load VIP status when a player connects.
		/// </summary>
		public static void LoadPlayer( ulong steamId )
		{
			var data = LoadVIP( steamId );
			if ( data != null && data.ExpiresAt > DateTime.UtcNow )
			{
				_vipPlayers[steamId] = data;
			}
		}

		private static void SaveVIP( ulong steamId )
		{
			try
			{
				if ( !FileSystem.Data.DirectoryExists( VIPDir ) )
					FileSystem.Data.CreateDirectory( VIPDir );

				if ( _vipPlayers.TryGetValue( steamId, out var data ) )
				{
					var json = Json.Serialize( data );
					FileSystem.Data.WriteAllText( $"{VIPDir}/{steamId}.json", json );
				}
			}
			catch ( Exception e )
			{
				Log.Warning( $"Failed to save VIP for {steamId}: {e.Message}" );
			}
		}

		private static VIPData LoadVIP( ulong steamId )
		{
			try
			{
				var path = $"{VIPDir}/{steamId}.json";
				if ( !FileSystem.Data.FileExists( path ) )
					return null;

				var json = FileSystem.Data.ReadAllText( path );
				return Json.Deserialize<VIPData>( json );
			}
			catch ( Exception e )
			{
				Log.Warning( $"Failed to load VIP for {steamId}: {e.Message}" );
				return null;
			}
		}
	}
}
