using System;

namespace GameSystems.Admin
{
	/// <summary>
	/// Logs all admin actions to a persistent file.
	/// Each entry includes timestamp, admin name, target, action, and reason.
	/// </summary>
	public static class AdminLogger
	{
		public record LogEntry( DateTime Timestamp, string AdminName, ulong AdminSteamId, string Action, string Target, string Reason );

		private static readonly List<LogEntry> _logs = new();
		private const string LogFile = "playersdata/admin_log.json";
		private static bool _loaded = false;

		/// <summary>
		/// Log an admin action.
		/// </summary>
		public static void LogAction( string adminName, ulong adminSteamId, string action, string target, string reason = "" )
		{
			EnsureLoaded();

			var entry = new LogEntry( DateTime.UtcNow, adminName, adminSteamId, action, target, reason );
			_logs.Add( entry );

			Log.Info( $"[ADMIN] {adminName} ({adminSteamId}): {action} -> {target} | {reason}" );

			Save();
		}

		/// <summary>
		/// Get recent log entries (most recent first).
		/// </summary>
		public static List<LogEntry> GetRecentLogs( int count = 20 )
		{
			EnsureLoaded();

			var result = new List<LogEntry>();
			for ( int i = Math.Max( 0, _logs.Count - count ); i < _logs.Count; i++ )
			{
				result.Add( _logs[i] );
			}
			result.Reverse();
			return result;
		}

		/// <summary>
		/// Get formatted log strings for display.
		/// </summary>
		public static List<string> GetFormattedLogs( int count = 10 )
		{
			var logs = GetRecentLogs( count );
			var formatted = new List<string>();
			foreach ( var log in logs )
			{
				var time = log.Timestamp.ToString( "MM/dd HH:mm" );
				var reason = string.IsNullOrEmpty( log.Reason ) ? "" : $" ({log.Reason})";
				formatted.Add( $"[{time}] {log.AdminName}: {log.Action} -> {log.Target}{reason}" );
			}
			return formatted;
		}

		/// <summary>
		/// Total number of log entries.
		/// </summary>
		public static int Count
		{
			get
			{
				EnsureLoaded();
				return _logs.Count;
			}
		}

		private static void Save()
		{
			try
			{
				// Keep last 500 entries max
				while ( _logs.Count > 500 )
					_logs.RemoveAt( 0 );

				var json = Json.Serialize( _logs );
				FileSystem.Data.WriteAllText( LogFile, json );
			}
			catch ( Exception e )
			{
				Log.Warning( $"Failed to save admin logs: {e.Message}" );
			}
		}

		private static void EnsureLoaded()
		{
			if ( _loaded ) return;
			_loaded = true;

			try
			{
				if ( !FileSystem.Data.FileExists( LogFile ) ) return;

				var json = FileSystem.Data.ReadAllText( LogFile );
				var loaded = Json.Deserialize<List<LogEntry>>( json );
				if ( loaded != null )
				{
					_logs.Clear();
					_logs.AddRange( loaded );
				}
			}
			catch ( Exception e )
			{
				Log.Warning( $"Failed to load admin logs: {e.Message}" );
			}
		}
	}
}
