using Sandbox.GameSystems;

namespace GameSystems.Jobs
{
	/// <summary>
	/// Defines all 11 standard Bustas RP jobs in code.
	/// These are registered into JobProvider at startup.
	/// </summary>
	public static class BustasJobs
	{
		public static readonly List<JobResource> All = new();

		static BustasJobs()
		{
			// ── Civilians ──────────────────────────────────────────────

			Register( new JobResource
			{
				Name = "Citizen",
				Description = "Default job. Can own doors, build bases, work odd jobs.",
				Team = BustasTeam.Civilians,
				TeamColor = BustasTeamColors.Civilians,
				Color = BustasTeamColors.Civilians,
				Category = "Civilians",
				Salary = BustasConfig.StartingMoney > 0 ? 25f : 0f,
				MaxWorkers = 0, // Unlimited
				Weapons = new List<string>(),
				CanOwnDoors = true,
				IsGovernment = false,
				IsCriminal = false,
				SortingLevel = 0,
				Vote = false,
			} );

			Register( new JobResource
			{
				Name = "Medic",
				Description = "Heal players for money. Cannot refuse emergency treatment.",
				Team = BustasTeam.Civilians,
				TeamColor = BustasTeamColors.Civilians,
				Color = BustasTeamColors.Civilians,
				Category = "Civilians",
				Salary = 50f,
				MaxWorkers = 3,
				Weapons = new List<string> { "medkit" },
				CanOwnDoors = true,
				IsGovernment = false,
				IsCriminal = false,
				SortingLevel = 1,
				Vote = false,
			} );

			Register( new JobResource
			{
				Name = "Gun Dealer",
				Description = "Sell legal weapons. Must have a shop.",
				Team = BustasTeam.Civilians,
				TeamColor = BustasTeamColors.Civilians,
				Color = BustasTeamColors.Civilians,
				Category = "Civilians",
				Salary = 45f,
				MaxWorkers = 3,
				Weapons = new List<string> { "pistol" },
				CanOwnDoors = true,
				IsGovernment = false,
				IsCriminal = false,
				SortingLevel = 2,
				Vote = false,
			} );

			Register( new JobResource
			{
				Name = "Heavy Gun Dealer",
				Description = "Sells military-grade weapons. Requires a hidden shop.",
				Team = BustasTeam.Civilians,
				TeamColor = BustasTeamColors.Civilians,
				Color = BustasTeamColors.Civilians,
				Category = "Civilians",
				Salary = 45f,
				MaxWorkers = 2,
				Weapons = new List<string> { "pistol" },
				CanOwnDoors = true,
				IsGovernment = false,
				IsCriminal = false,
				SortingLevel = 3,
				Vote = false,
			} );

			Register( new JobResource
			{
				Name = "Hobo",
				Description = "Beg for money, build on sidewalks. No salary, no doors.",
				Team = BustasTeam.Civilians,
				TeamColor = BustasTeamColors.Civilians,
				Color = BustasTeamColors.Civilians,
				Category = "Civilians",
				Salary = 0f,
				MaxWorkers = 5,
				Weapons = new List<string> { "bugbait" },
				CanOwnDoors = false,
				IsGovernment = false,
				IsCriminal = false,
				SortingLevel = 4,
				Vote = false,
			} );

			// ── Government ─────────────────────────────────────────────

			Register( new JobResource
			{
				Name = "Police Officer",
				Description = "Enforce laws, arrest criminals, respond to 911 calls.",
				Team = BustasTeam.Government,
				TeamColor = BustasTeamColors.Government,
				Color = BustasTeamColors.Government,
				Category = "Government",
				Salary = 65f,
				MaxWorkers = 6,
				Weapons = new List<string> { "pistol", "taser", "baton" },
				CanOwnDoors = false,
				IsGovernment = true,
				IsCriminal = false,
				SortingLevel = 10,
				Vote = false,
			} );

			Register( new JobResource
			{
				Name = "Police Sergeant",
				Description = "Senior officer. Can authorize warrants when no Chief.",
				Team = BustasTeam.Government,
				TeamColor = BustasTeamColors.Government,
				Color = BustasTeamColors.Government,
				Category = "Government",
				Salary = 80f,
				MaxWorkers = 2,
				Weapons = new List<string> { "pistol", "taser", "baton", "shotgun" },
				CanOwnDoors = false,
				IsGovernment = true,
				IsCriminal = false,
				SortingLevel = 11,
				Vote = false,
			} );

			Register( new JobResource
			{
				Name = "Police Chief",
				Description = "Manage PD, issue warrants, set department policies.",
				Team = BustasTeam.Government,
				TeamColor = BustasTeamColors.Government,
				Color = BustasTeamColors.Government,
				Category = "Government",
				Salary = 100f,
				MaxWorkers = 1,
				Weapons = new List<string> { "pistol", "taser", "baton" },
				CanOwnDoors = false,
				IsGovernment = true,
				IsCriminal = false,
				SortingLevel = 12,
				Vote = true,
			} );

			Register( new JobResource
			{
				Name = "S.W.A.T.",
				Description = "Heavy response unit. Raids, hostage situations.",
				Team = BustasTeam.Government,
				TeamColor = BustasTeamColors.Government,
				Color = BustasTeamColors.Government,
				Category = "Government",
				Salary = 85f,
				MaxWorkers = 4,
				Weapons = new List<string> { "m4a1", "pistol", "flashbang" },
				CanOwnDoors = false,
				IsGovernment = true,
				IsCriminal = false,
				SortingLevel = 13,
				Vote = false,
			} );

			Register( new JobResource
			{
				Name = "Mayor",
				Description = "Set laws, manage city budget, announce lockdowns.",
				Team = BustasTeam.Government,
				TeamColor = BustasTeamColors.Government,
				Color = BustasTeamColors.Government,
				Category = "Government",
				Salary = 120f,
				MaxWorkers = 1,
				Weapons = new List<string>(),
				CanOwnDoors = true,
				IsGovernment = true,
				IsCriminal = false,
				SortingLevel = 14,
				Vote = true,
			} );

			Register( new JobResource
			{
				Name = "Mayor's Bodyguard",
				Description = "Protect the Mayor at all costs.",
				Team = BustasTeam.Government,
				TeamColor = BustasTeamColors.Government,
				Color = BustasTeamColors.Government,
				Category = "Government",
				Salary = 70f,
				MaxWorkers = 2,
				Weapons = new List<string> { "pistol", "smg" },
				CanOwnDoors = false,
				IsGovernment = true,
				IsCriminal = false,
				SortingLevel = 15,
				Vote = false,
			} );
		}

		private static void Register( JobResource job )
		{
			All.Add( job );
		}

		/// <summary>
		/// Returns the default job (Citizen).
		/// </summary>
		public static JobResource GetDefault()
		{
			return All.FirstOrDefault( j => j.Name == "Citizen" );
		}

		/// <summary>
		/// Returns a job by name, or null if not found.
		/// </summary>
		public static JobResource GetByName( string name )
		{
			return All.FirstOrDefault( j => j.Name.Equals( name, System.StringComparison.OrdinalIgnoreCase ) );
		}

		/// <summary>
		/// Returns all jobs belonging to a specific team.
		/// </summary>
		public static List<JobResource> GetByTeam( BustasTeam team )
		{
			return All.Where( j => j.Team == team ).ToList();
		}
	}
}
