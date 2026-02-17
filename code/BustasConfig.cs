namespace Sandbox.GameSystems
{
	public static class BustasConfig
	{
		// Server
		public const string ServerName = "Bustas RP";
		public const int MaxPlayers = 64;

		// Economy
		public const int StartingMoney = 500;
		public const int SalaryInterval = 300; // seconds (5 min)
		public const int MaxWalletMoney = 1000000;
		public const float PrinterBaseRate = 50f; // $/cycle
		public const int PrinterCycleTime = 120; // seconds

		// Gameplay
		public const int MaxPropsPerPlayer = 40;
		public const int MaxDoorsPerPlayer = 3;
		public const int DoorPrice = 100;
		public const float MugMaxAmount = 500f;
		public const float MugCooldown = 300f; // seconds
		public const float HitMinPrice = 1000f;
		public const float HitExpireDuration = 600f; // seconds (10 min)

		// VIP
		public const int VIPExtraDoors = 2;
		public const int VIPExtraProps = 20;
		public const float VIPSalaryMultiplier = 1.5f;

		// Printer Tiers - Costs
		public const float PrinterBronzeCost = 1000f;
		public const float PrinterSilverCost = 3000f;
		public const float PrinterGoldCost = 6000f;
		public const float PrinterDiamondCost = 12000f;
		public const float PrinterEmeraldCost = 15000f; // VIP only

		// Printer Tiers - Generation rates ($/cycle)
		public const float PrinterBronzeRate = 50f;
		public const float PrinterSilverRate = 125f;
		public const float PrinterGoldRate = 250f;
		public const float PrinterDiamondRate = 500f;
		public const float PrinterEmeraldRate = 600f;

		// Printer Tiers - Overheat chance per cycle (0.0 - 1.0)
		public const float OverheatBronze = 0.05f;
		public const float OverheatSilver = 0.08f;
		public const float OverheatGold = 0.12f;
		public const float OverheatDiamond = 0.18f;
		public const float OverheatEmerald = 0.25f;

		// Printer - Overheat
		public const float PrinterExplosionTimer = 30f; // seconds before overheated printer explodes
		public const int MaxPrintersPerPlayer = 5;

		// Vehicles
		public const int MaxVehiclesPerPlayer = 2;
		public const float VehicleRepairCostMin = 200f;
		public const float VehicleRepairCostMax = 500f;
		public const float VehicleSpawnCooldown = 30f; // seconds
		public const float VehicleSpeedSlow = 400f;
		public const float VehicleSpeedMedium = 600f;
		public const float VehicleSpeedFast = 900f;
		public const float VehicleSpeedVeryFast = 1200f;
		public const float VehicleDamageCollisionMultiplier = 0.5f;
		public const float VehicleDamageWeaponMultiplier = 1.0f;
		public const float VehicleDamageExplosionMultiplier = 3.0f;

		// Phone & Communication
		public const float EmergencyCooldown = 60f; // seconds
		public const float AdvertCost = 100f;
		public const float AdvertCooldown = 30f; // seconds
		public const int MaxMessageHistory = 50;
		public const float GPSUpdateInterval = 2f; // seconds
		public const float GPSRange = 2000f; // units

		// Law & Order
		public const int MaxLaws = 10;
		public const int MaxLawLength = 100;
		public const float WantedDuration = 300f; // seconds (5 min)
		public const float WarrantDuration = 180f; // seconds (3 min)
		public const int MaxActiveWarrants = 2;
	}
}
