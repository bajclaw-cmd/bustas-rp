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

		// VIP
		public const int VIPExtraDoors = 2;
		public const int VIPExtraProps = 20;
		public const float VIPSalaryMultiplier = 1.5f;
	}
}
