namespace GameSystems.Jobs
{
	public static class JobProvider
	{
		public static Dictionary<string, JobResource> Jobs { get; private set; } = new();
		public static Dictionary<string, JobGroupResource> JobGroups { get; private set; } = new();

		// On Start load all jobs
		static JobProvider()
		{
			Log.Info( "Loading groups..." );
			// Get all JobGroup resources from data files
			foreach ( var group in ResourceLibrary.GetAll<JobGroupResource>( "data/jobs/groups" ) )
			{
				Log.Info( $"Loading group: {group.Name}" );
				JobGroups[group.Name] = group;
			}

			Log.Info( "Loading jobs..." );
			// Get all Job resources from data files
			foreach ( var job in ResourceLibrary.GetAll<JobResource>( "data/jobs" ) )
			{
				Log.Info( $"Loading job: {job.Name}" );
				Jobs[job.Name] = job;
			}

			// Register code-defined Bustas RP jobs (won't overwrite file-defined jobs)
			foreach ( var job in BustasJobs.All )
			{
				if ( !Jobs.ContainsKey( job.Name ) )
				{
					Log.Info( $"Registering Bustas job: {job.Name}" );
					Jobs[job.Name] = job;
				}
			}

			Log.Info( $"Total jobs loaded: {Jobs.Count}" );
		}

		// Get default job when player spawns
		public static JobResource GetDefault()
		{
			return BustasJobs.GetDefault()
				?? ResourceLibrary.Get<JobResource>( "data/jobs/citizen.job" );
		}
	}
}
