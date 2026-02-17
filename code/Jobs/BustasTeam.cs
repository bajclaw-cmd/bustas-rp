namespace GameSystems.Jobs
{
	public enum BustasTeam
	{
		Civilians,
		Government,
		Criminals
	}

	public static class BustasTeamColors
	{
		public static readonly Color Government = Color.Parse( "#3498DB" ).Value; // Blue
		public static readonly Color Civilians = Color.Parse( "#2ECC71" ).Value;  // Green
		public static readonly Color Criminals = Color.Parse( "#E74C3C" ).Value;  // Red

		public static Color GetTeamColor( BustasTeam team )
		{
			return team switch
			{
				BustasTeam.Government => Government,
				BustasTeam.Criminals => Criminals,
				_ => Civilians
			};
		}
	}
}
