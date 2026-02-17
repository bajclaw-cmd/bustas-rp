using Sandbox.GameSystems;

namespace Entity.Vehicle
{
	/// <summary>
	/// All available vehicle types in Bustas RP.
	/// </summary>
	public enum VehicleType
	{
		Sedan,
		SUV,
		SportsCar,
		Taxi,
		TowTruck,
		PoliceCruiser,
		SwatVan,
		Ambulance,
		VIPSupercar,
		DeliveryVan
	}

	/// <summary>
	/// Speed category for vehicles.
	/// </summary>
	public enum VehicleSpeed
	{
		Slow,
		Medium,
		Fast,
		VeryFast
	}

	/// <summary>
	/// Configuration for a single vehicle type.
	/// </summary>
	public record VehicleConfig(
		string DisplayName,
		VehicleType Type,
		float Cost,
		VehicleSpeed Speed,
		float MaxHealth,
		string RequiredJob,
		bool RequiresVIP,
		Color DisplayColor
	)
	{
		/// <summary>
		/// Whether this vehicle is free (job-restricted vehicles are free).
		/// </summary>
		public bool IsFree => Cost <= 0f;

		/// <summary>
		/// Get the actual max speed value from BustasConfig.
		/// </summary>
		public float GetMaxSpeed() => Speed switch
		{
			VehicleSpeed.Slow => BustasConfig.VehicleSpeedSlow,
			VehicleSpeed.Medium => BustasConfig.VehicleSpeedMedium,
			VehicleSpeed.Fast => BustasConfig.VehicleSpeedFast,
			VehicleSpeed.VeryFast => BustasConfig.VehicleSpeedVeryFast,
			_ => BustasConfig.VehicleSpeedMedium
		};

		/// <summary>
		/// Get a display string for the speed tier.
		/// </summary>
		public string SpeedLabel => Speed switch
		{
			VehicleSpeed.Slow => "Slow",
			VehicleSpeed.Medium => "Medium",
			VehicleSpeed.Fast => "Fast",
			VehicleSpeed.VeryFast => "Very Fast",
			_ => "Medium"
		};

		/// <summary>
		/// Health as a percentage string (e.g. "120%").
		/// </summary>
		public string HealthLabel => $"{(MaxHealth / 100f * 100):N0}%";
	}

	/// <summary>
	/// Static registry of all vehicle configurations.
	/// </summary>
	public static class VehicleConfigs
	{
		public static readonly Dictionary<VehicleType, VehicleConfig> All = new()
		{
			{ VehicleType.Sedan, new VehicleConfig(
				"Sedan", VehicleType.Sedan,
				2500f, VehicleSpeed.Medium, 100f,
				null, false,
				Color.Parse( "#A0A0A0" ).Value
			)},
			{ VehicleType.SUV, new VehicleConfig(
				"SUV", VehicleType.SUV,
				4000f, VehicleSpeed.Medium, 120f,
				null, false,
				Color.Parse( "#4A4A4A" ).Value
			)},
			{ VehicleType.SportsCar, new VehicleConfig(
				"Sports Car", VehicleType.SportsCar,
				8000f, VehicleSpeed.Fast, 80f,
				null, false,
				Color.Parse( "#E74C3C" ).Value
			)},
			{ VehicleType.Taxi, new VehicleConfig(
				"Taxi", VehicleType.Taxi,
				0f, VehicleSpeed.Medium, 100f,
				"Taxi Driver", false,
				Color.Parse( "#F1C40F" ).Value
			)},
			{ VehicleType.TowTruck, new VehicleConfig(
				"Tow Truck", VehicleType.TowTruck,
				0f, VehicleSpeed.Slow, 150f,
				"Tow Truck Driver", false,
				Color.Parse( "#E67E22" ).Value
			)},
			{ VehicleType.PoliceCruiser, new VehicleConfig(
				"Police Cruiser", VehicleType.PoliceCruiser,
				0f, VehicleSpeed.Fast, 120f,
				"Police", false,
				Color.Parse( "#3498DB" ).Value
			)},
			{ VehicleType.SwatVan, new VehicleConfig(
				"SWAT Van", VehicleType.SwatVan,
				0f, VehicleSpeed.Medium, 200f,
				"S.W.A.T.", false,
				Color.Parse( "#2C3E50" ).Value
			)},
			{ VehicleType.Ambulance, new VehicleConfig(
				"Ambulance", VehicleType.Ambulance,
				0f, VehicleSpeed.Medium, 120f,
				"Medic", false,
				Color.Parse( "#ECF0F1" ).Value
			)},
			{ VehicleType.VIPSupercar, new VehicleConfig(
				"VIP Supercar", VehicleType.VIPSupercar,
				15000f, VehicleSpeed.VeryFast, 80f,
				null, true,
				Color.Parse( "#9B59B6" ).Value
			)},
			{ VehicleType.DeliveryVan, new VehicleConfig(
				"Delivery Van", VehicleType.DeliveryVan,
				3000f, VehicleSpeed.Slow, 130f,
				null, false,
				Color.Parse( "#BDC3C7" ).Value
			)}
		};

		/// <summary>
		/// Get configuration for a specific vehicle type.
		/// </summary>
		public static VehicleConfig Get( VehicleType type )
		{
			return All[type];
		}

		/// <summary>
		/// Check if a player's job allows them to use a specific vehicle.
		/// Police Cruiser is available to all police jobs (Officer, Sergeant, Chief).
		/// </summary>
		public static bool CanPlayerUse( VehicleConfig config, string jobName, bool isVIP )
		{
			// VIP check
			if ( config.RequiresVIP && !isVIP )
				return false;

			// No job restriction
			if ( config.RequiredJob == null )
				return true;

			// Police Cruiser is available to all police jobs
			if ( config.RequiredJob == "Police" )
			{
				return jobName == "Police Officer" || jobName == "Police Sergeant" || jobName == "Police Chief";
			}

			// Exact job match
			return jobName == config.RequiredJob;
		}
	}
}
