using Sandbox.GameSystems;

namespace GameSystems.LawOrder
{
	/// <summary>
	/// Manages the city law board. The Mayor can add/remove laws.
	/// When no Mayor is active, default laws apply.
	/// Laws reset when the Mayor dies, disconnects, or changes job.
	/// </summary>
	public static class LawManager
	{
		private static readonly List<string> DefaultLaws = new()
		{
			"No raiding",
			"No mugging",
			"No weapons in public"
		};

		private static List<string> _currentLaws = new( DefaultLaws );
		private static bool _mayorActive = false;

		/// <summary>
		/// Current active laws. Returns a copy to prevent external mutation.
		/// </summary>
		public static IReadOnlyList<string> Laws => _currentLaws.AsReadOnly();

		/// <summary>
		/// Whether a Mayor is currently setting laws.
		/// </summary>
		public static bool IsMayorActive => _mayorActive;

		/// <summary>
		/// Called when a Mayor takes office. Keeps existing laws but marks Mayor as active.
		/// </summary>
		public static void OnMayorActive()
		{
			_mayorActive = true;
			Log.Info( "Mayor is now active. Laws can be modified." );
		}

		/// <summary>
		/// Called when the Mayor leaves office (disconnect, death, job change).
		/// Resets laws to defaults.
		/// </summary>
		public static void OnMayorLeave()
		{
			_mayorActive = false;
			_currentLaws = new List<string>( DefaultLaws );
			Log.Info( "Mayor left office. Laws reset to defaults." );
		}

		/// <summary>
		/// Adds a new law. Returns true on success.
		/// </summary>
		public static bool AddLaw( string law )
		{
			if ( _currentLaws.Count >= BustasConfig.MaxLaws )
				return false;

			if ( string.IsNullOrWhiteSpace( law ) )
				return false;

			if ( law.Length > BustasConfig.MaxLawLength )
				law = law[..BustasConfig.MaxLawLength];

			_currentLaws.Add( law );
			return true;
		}

		/// <summary>
		/// Removes a law by 1-based index. Returns true on success.
		/// </summary>
		public static bool RemoveLaw( int lawNumber )
		{
			int index = lawNumber - 1;
			if ( index < 0 || index >= _currentLaws.Count )
				return false;

			_currentLaws.RemoveAt( index );
			return true;
		}

		/// <summary>
		/// Returns all laws formatted for chat display.
		/// </summary>
		public static List<string> GetFormattedLaws()
		{
			var lines = new List<string>();
			lines.Add( _mayorActive ? "=== City Laws ===" : "=== Default Laws (No Mayor) ===" );
			for ( int i = 0; i < _currentLaws.Count; i++ )
			{
				lines.Add( $"{i + 1}. {_currentLaws[i]}" );
			}
			if ( _currentLaws.Count == 0 )
			{
				lines.Add( "No laws currently set." );
			}
			return lines;
		}
	}
}
