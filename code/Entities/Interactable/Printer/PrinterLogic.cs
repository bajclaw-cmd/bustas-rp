using System;
using Sandbox.Entity;
using Sandbox.GameSystems;
using Sandbox.GameSystems.Player;

namespace Entity.Interactable.Printer
{
	public sealed class PrinterConfiguration
	{
		public Color Color { get; set; }
		public Material Material { get; set; }
		public float Cost { get; set; }
		public float Rate { get; set; }
		public float OverheatChance { get; set; }
		/// <summary>
		/// The timer for the printer to generate money in seconds
		/// </summary>
		public float Timer { get; set; }
		public bool RequiresVIP { get; set; }
	}

	public sealed class PrinterLogic : BaseEntity
	{
		// Define the different types of printers
		public enum PrinterType { Bronze, Silver, Gold, Diamond, Emerald };

		[Property] public GameObject PrinterFan { get; set; }
		[Property] public float PrinterFanSpeed { get; set; } = 1000f;
		[Property] public Dictionary<PrinterType, PrinterConfiguration> PrinterConfig = new();
		[Property, Sync] public float PrinterCurrentMoney { get; set; } = 0f;
		[Property] public float PrinterMaxMoney { get; set; } = 8000f;

		[Sync] public bool IsOverheating { get; set; } = false;
		[Sync] public float OverheatTimeRemaining { get; set; } = 0f;
		[Sync] public PrinterType CurrentPrinterType { get; set; } = PrinterType.Bronze;

		private TimeSince _lastCycle = 0;
		private TimeSince _overheatStarted = 0;
		private bool _exploded = false;

		/// <summary>
		/// Returns the configuration for the current printer type with BustasConfig values.
		/// </summary>
		public static PrinterConfiguration GetTierConfig( PrinterType type )
		{
			return type switch
			{
				PrinterType.Bronze => new PrinterConfiguration
				{
					Cost = BustasConfig.PrinterBronzeCost,
					Rate = BustasConfig.PrinterBronzeRate,
					OverheatChance = BustasConfig.OverheatBronze,
					Timer = BustasConfig.PrinterCycleTime,
					Color = Color.Parse( "#CD7F32" ).Value,
					RequiresVIP = false,
				},
				PrinterType.Silver => new PrinterConfiguration
				{
					Cost = BustasConfig.PrinterSilverCost,
					Rate = BustasConfig.PrinterSilverRate,
					OverheatChance = BustasConfig.OverheatSilver,
					Timer = BustasConfig.PrinterCycleTime,
					Color = Color.Parse( "#C0C0C0" ).Value,
					RequiresVIP = false,
				},
				PrinterType.Gold => new PrinterConfiguration
				{
					Cost = BustasConfig.PrinterGoldCost,
					Rate = BustasConfig.PrinterGoldRate,
					OverheatChance = BustasConfig.OverheatGold,
					Timer = BustasConfig.PrinterCycleTime,
					Color = Color.Parse( "#FFD700" ).Value,
					RequiresVIP = false,
				},
				PrinterType.Diamond => new PrinterConfiguration
				{
					Cost = BustasConfig.PrinterDiamondCost,
					Rate = BustasConfig.PrinterDiamondRate,
					OverheatChance = BustasConfig.OverheatDiamond,
					Timer = BustasConfig.PrinterCycleTime,
					Color = Color.Parse( "#40E0D0" ).Value,
					RequiresVIP = false,
				},
				PrinterType.Emerald => new PrinterConfiguration
				{
					Cost = BustasConfig.PrinterEmeraldCost,
					Rate = BustasConfig.PrinterEmeraldRate,
					OverheatChance = BustasConfig.OverheatEmerald,
					Timer = BustasConfig.PrinterCycleTime,
					Color = Color.Parse( "#50C878" ).Value,
					RequiresVIP = true,
				},
				_ => new PrinterConfiguration
				{
					Cost = BustasConfig.PrinterBronzeCost,
					Rate = BustasConfig.PrinterBronzeRate,
					OverheatChance = BustasConfig.OverheatBronze,
					Timer = BustasConfig.PrinterCycleTime,
					Color = Color.Parse( "#CD7F32" ).Value,
					RequiresVIP = false,
				}
			};
		}

		/// <summary>
		/// Interact with the printer: collect money or cool down if overheating.
		/// </summary>
		public override void InteractUse( SceneTraceResult tr, GameObject player )
		{
			// If overheating, E key cools it down
			if ( IsOverheating )
			{
				CoolDown();
				var playerStats = player.Components.Get<Player>();
				playerStats?.SendMessage( "You cooled down the printer!" );
				return;
			}

			// Normal collect
			if ( PrinterCurrentMoney > 0 )
			{
				var playerStats = player.Components.Get<Player>();
				if ( playerStats == null ) return;

				playerStats.UpdateBalance( PrinterCurrentMoney );
				ResetPrinterMoney();
				Sound.Play( "audio/money.sound", Transform.World.Position );
			}
		}

		protected override void OnFixedUpdate()
		{
			if ( _exploded ) return;

			// Handle overheating countdown
			if ( IsOverheating )
			{
				OverheatTimeRemaining = BustasConfig.PrinterExplosionTimer - _overheatStarted;

				// Blink red when overheating
				var renderer = GameObject.Components.Get<ModelRenderer>();
				if ( renderer != null )
				{
					float pulse = MathF.Sin( Time.Now * 6f ) * 0.5f + 0.5f;
					renderer.Tint = Color.Lerp( Color.Red, Color.Orange, pulse );
				}

				// Fan slows down when overheating
				SpinFan( 0.2f );

				if ( _overheatStarted >= BustasConfig.PrinterExplosionTimer )
				{
					Explode();
					return;
				}
				return;
			}

			// Normal operation: generate money on cycle
			var config = GetTierConfig( CurrentPrinterType );
			float cycleTime = config.Timer;

			if ( _lastCycle >= cycleTime )
			{
				if ( PrinterCurrentMoney < PrinterMaxMoney )
				{
					PrinterCurrentMoney += config.Rate;

					// Roll for overheat
					if ( Random.Shared.NextSingle() < config.OverheatChance )
					{
						StartOverheat();
					}
				}
				_lastCycle = 0;
			}

			SpinFan( 1f );
		}

		private void SpinFan( float speedMultiplier = 1f )
		{
			if ( PrinterFan == null ) return;
			var rotationAmount = PrinterFanSpeed * speedMultiplier * Time.Delta;
			PrinterFan.Transform.Rotation *= Rotation.FromAxis( Vector3.Left, -rotationAmount );
		}

		public void SetPrinterType( PrinterType type )
		{
			CurrentPrinterType = type;
			UpdatePrinterColor();
		}

		[Broadcast]
		public void ResetPrinterMoney()
		{
			PrinterCurrentMoney = 0f;
		}

		[Broadcast]
		private void StartOverheat()
		{
			IsOverheating = true;
			_overheatStarted = 0;
			OverheatTimeRemaining = BustasConfig.PrinterExplosionTimer;
			Log.Warning( $"{EntityName} is overheating! Cool it down within {BustasConfig.PrinterExplosionTimer}s!" );
		}

		[Broadcast]
		private void CoolDown()
		{
			IsOverheating = false;
			OverheatTimeRemaining = 0f;
			Sound.Play( "audio/select.sound", Transform.World.Position );
			UpdatePrinterColor();
		}

		[Broadcast]
		private void Explode()
		{
			_exploded = true;
			IsOverheating = false;
			PrinterCurrentMoney = 0f;

			Log.Warning( $"{EntityName} exploded!" );
			Sound.Play( "audio/error.sound", Transform.World.Position );

			// Destroy the printer after a short delay
			GameObject.Destroy();
		}

		private void UpdatePrinterColor()
		{
			var config = GetTierConfig( CurrentPrinterType );
			var renderer = GameObject.Components.Get<ModelRenderer>();
			if ( renderer is null )
			{
				Log.Warning( "ModelRenderer component not found" );
				return;
			}

			renderer.Tint = config.Color;

			// Apply material override from editor-configured PrinterConfig if available
			if ( PrinterConfig.TryGetValue( CurrentPrinterType, out var editorConfig ) )
			{
				renderer.MaterialOverride = editorConfig.Material;
			}
		}
	}
}
