using GameSystems.Player;

namespace GameSystems.Jobs
{
	[GameResource( "Job Definition", "job", "" )]
	public class JobResource : GameResource
	{
		[Category( "Description" )] public string Name { get; set; }
		[Category( "Description" )] public string Description { get; set; }
		[Category( "Description" )] public Color Color { get; set; }
		[Category( "Description" )] public JobGroupResource Group { get; set; }
		[Category( "Description" )] public PermissionLevel PermissionLevel { get; set; }
		[Category( "Description" )] public int SortingLevel { get; set; }
		[Category( "Description" )] public string Model { get; set; }
		[Category( "Gameplay" )] public float Salary { get; set; }
		[Category( "Gameplay" )] public int MaxWorkers { get; set; }
		[Category( "Gameplay" )] public List<string> Weapons { get; set; }
		[Category( "Gameplay" )] public Dictionary<string, int> WeaponsAmmo { get; set; }
		[Category( "Gameplay" )] public bool Vote { get; set; }
		[Category( "Gameplay" )] public bool CanOwnDoors { get; set; } = true;
		[Category( "Gameplay" )] public bool IsGovernment { get; set; }
		[Category( "Gameplay" )] public bool IsCriminal { get; set; }
		[Category( "Gameplay" )] public BustasTeam Team { get; set; } = BustasTeam.Civilians;
		[Category( "Gameplay" )] public Color TeamColor { get; set; } = BustasTeamColors.Civilians;
		[Category( "Gameplay" )] public string Category { get; set; }
		[Category( "Gameplay" )] public bool RequiresVIP { get; set; }
	}
}
