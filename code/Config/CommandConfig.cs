using System;
using Entity.Vehicle;
using GameSystems.CriminalEconomy;
using GameSystems.LawOrder;
using GameSystems.Player;
using GameSystems.UI;
using Sandbox.GameSystems.Config;

namespace GameSystems.Config
{
	/// <summary>
	/// Command configuration.
	/// </summary>
	public class CommandConfig : Component
	{
		private readonly Dictionary<string, ICommandConfig> _commands = new()
		{
			{ "clear", new Command(
						name: "clear",
						description: "Clears the chat",
						permissionLevel: PermissionLevel.User,
						commandFunction: (player, scene, args) =>
						{
							var playerStats = player.Components.Get<Sandbox.GameSystems.Player.Player>();
							if (playerStats == null) return false;
							
							// Get the chat
							var chat = scene.Directory.FindByName("Screen")?.First()?.Components.Get<Chat>();
							if (chat == null) return false;

							using ( Rpc.FilterInclude( c => c.Id == playerStats.GetNetworkPlayer()?.Connection.Id) )
							{
								chat.ClearChat();
							}

							playerStats.SendMessage("Chat has been cleared");
							return true;
						}
				)},
			{ "lorem", new Command(
						name: "lorem",
						description: "Spams the chat with lorem ipsum X times.",
						permissionLevel: PermissionLevel.User,
						commandFunction: (player, scene, args) =>
						{
								// Get the player stats
								var playerStats = player.Components.Get<Sandbox.GameSystems.Player.Player>();
								if (playerStats == null) return false;

								playerStats.SendMessage("Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.");
								return true;
						}
				)},
				{ "givemoney", new Command(
						name: "givemoney",
						description: "Gives the player money",
						permissionLevel: PermissionLevel.Admin,
						commandFunction: (player, scene, args) =>
						{
								// Get the player stats
								var playerStats = player.Components.Get<Sandbox.GameSystems.Player.Player>();
								if (playerStats == null) return false;

								// Get the 2nd parameter for player
								if (args.Length < 2)
								{
									playerStats.SendMessage("Usage: /givemoney <player> <amount>");
									return false;
								}

								var amount = 0;
								if (!int.TryParse(args[1], out amount))
								{
									playerStats.SendMessage("Invalid amount");
									return false;
								}

								var GameController = GameSystems.GameController.Instance;
								if (GameController == null) return false;

								var foundPlayer = GameController.PlayerLookup(args[0]);

								if (foundPlayer == null)
								{
									playerStats.SendMessage($"Player {args[0]} not found");
									return false;
								}

								foundPlayer.GameObject.Components.Get<Sandbox.GameSystems.Player.Player>()?.UpdateBalance(amount);

								if ( foundPlayer.GameObject != player ) foundPlayer.GameObject.Components.Get<Sandbox.GameSystems.Player.Player>()?.SendMessage($"You were given ${amount.ToString("N0")} money.");
								playerStats.SendMessage($"Gave {foundPlayer.Connection.DisplayName} ${amount.ToString("N0")} money");
								return true;
						}
				)},
				{ "setmoney", new Command(
						name: "setmoney",
						description: "Set a player's money",
						permissionLevel: PermissionLevel.Admin,
						commandFunction: (player, scene, args) =>
						{
								// Get the player stats
								var playerStats = player.Components.Get<Sandbox.GameSystems.Player.Player>();
								if (playerStats == null) return false;

								// Get the 2nd parameter for player
								if (args.Length < 2)
								{
									playerStats.SendMessage("Usage: /setmoney <player> <amount>");
									return false;
								}

								var amount = 0;
								if (!int.TryParse(args[1], out amount))
								{
									playerStats.SendMessage("Invalid amount");
									return false;
								}

								var GameController = GameSystems.GameController.Instance;
								if (GameController == null) return false;

								var foundPlayer = GameController.PlayerLookup(args[0]);

								if (foundPlayer == null)
								{
									playerStats.SendMessage($"Player {args[0]} not found");
									return false;
								}

								foundPlayer.GameObject.Components.Get<Sandbox.GameSystems.Player.Player>()?.SetBalance(amount);

								if ( foundPlayer.GameObject != player ) foundPlayer.GameObject.Components.Get<Sandbox.GameSystems.Player.Player>()?.SendMessage($"Your money has been set to ${amount.ToString("N0")}.");
								playerStats.SendMessage($"Set {foundPlayer.Connection.DisplayName} money to ${amount.ToString("N0")}");
								return true;
						}
				)},
				{ "setrank", new Command(
						name: "setrank",
						description: "Set a player's rank",
						permissionLevel: PermissionLevel.SuperAdmin,
						commandFunction: (player, scene, args) =>
						{
								// Get the player stats
								var playerStats = player.Components.Get<Sandbox.GameSystems.Player.Player>();
								if (playerStats == null) return false;

								// Get the 2nd parameter for player
								if (args.Length < 2)
								{
									playerStats.SendMessage("Usage: /setrank <player> <rank>");
									return false;
								}

								var GameController = GameSystems.GameController.Instance;
								if (GameController == null) return false;

								var foundPlayer = GameController.PlayerLookup(args[0]);

								if (foundPlayer == null)
								{
									playerStats.SendMessage($"Player {args[0]} not found");
									return false;
								}

								// Get the rank
								var rank = GameController.GetUserGroup(args[1]);
								if (rank == null)
								{
									playerStats.SendMessage("Invalid rank");
									return false;
								}

								// Check if the player has permission to set the rank
								if ( playerStats.GetNetworkPlayer()?.CheckPermission(rank.PermissionLevel) == false )
								{
									playerStats.SendMessage("You do not have permission to set this rank.");
									return false;
								}

								// Set the rank
								foundPlayer.SetRank(rank);

								if ( foundPlayer.GameObject != player ) foundPlayer.GameObject.Components.Get<Sandbox.GameSystems.Player.Player>()?.SendMessage($"Your rank has been set to {rank.DisplayName}.");
								playerStats.SendMessage($"Set {foundPlayer.Connection.DisplayName} rank to {rank.DisplayName}");
								return true;
						}
				)},
				{ "noclip", new Command(
						name: "noclip",
						description: "Enable noclip on a player",
						permissionLevel: PermissionLevel.Admin,
						commandFunction: (player, scene, args) =>
						{
							
							var targetPlayer = player;
								// Get the player stats
								var GameController = GameSystems.GameController.Instance;
								if (GameController == null) return false;
								if (args.Length > 0)
								{
									var foundPlayer = GameController.PlayerLookup(args[0]);
									if ( foundPlayer is not null ) targetPlayer = foundPlayer.GameObject;
								}
								

								// Get the player controller
								var controller = targetPlayer.Components.Get<Sandbox.GameSystems.Player.Player>();
								if (controller == null) return false;

								controller.ToggleNoClip(!controller.IsNoClip);
								if ( targetPlayer.Id == player.Id )
								{
									targetPlayer.Components.Get<Sandbox.GameSystems.Player.Player>()?.SendMessage($"Noclip {(controller.IsNoClip ? "enabled" : "disabled")}.");
								}else
								{
									player.Components.Get<Sandbox.GameSystems.Player.Player>()?.SendMessage($"Noclip {(controller.IsNoClip ? "enabled" : "disabled")} for {targetPlayer.Name}.");
								}
								return true;
						}
				)},
				{ "dropmoney", new Command(
						name: "dropmoney",
						description: "Drops the specified amount of money.",
						permissionLevel: PermissionLevel.User,
						commandFunction: (player, scene, args) =>
						{
								// Get the player stats
								var playerStats = player.Components.Get<Sandbox.GameSystems.Player.Player>();
								if (playerStats == null)
								{
									Log.Error("Player stats not found.");
									return false;
								}

								// Validate the command arguments
								if (args.Length < 1)
								{
									playerStats.SendMessage("Usage: /dropmoney <amount>");
									return false;
								}

								if (!int.TryParse(args[0], out int amount) || amount <= 0)
								{
									playerStats.SendMessage("Invalid amount specified.");
									return false;
								}

								// Check if the player has enough money to drop
								if (!playerStats.UpdateBalance(-amount))
								{
									playerStats.SendMessage("You do not have enough money to drop that amount.");
									return false;
								}

								try
								{
									// Get the ConfigManager to access the MoneyPrefab
									var configManager = ConfigManager.Instance;
									if (configManager == null || configManager.MoneyPrefab == null)
									{
										Log.Error("Money prefab is not set in the ConfigManager.");
										return false;
									}

									// Clone the MoneyPrefab and position it
									var moneyObject = configManager.MoneyPrefab.Clone(player.Transform.Position);
									if (moneyObject == null)
									{
										Log.Error("Failed to clone MoneyPrefab.");
										return false;
									}

									// Attach the Money component to the GameObject
									var moneyComponent = moneyObject.Components.Get<Money>();
									if (moneyComponent == null)
									{
										Log.Error("Money component is missing on the prefab.");
										return false;
									}

									// Set the amount and owner
									moneyComponent.Amount = amount;
									moneyComponent.Owner = playerStats.GetNetworkPlayer();

									// Network the spawned GameObject
									moneyObject.NetworkSpawn();

									// Notify the player
									playerStats.SendMessage($"You have dropped ${amount:N0}.");

									return true;
								}
								catch (Exception e)
								{
									Log.Error($"Error in /dropmoney command: {e.Message}");
									return false;
								}
						}
				)},
				{ "tp", new Command(
						name: "tp",
						description: "Teleports a player to where you are looking.",
						permissionLevel: PermissionLevel.Admin,
						commandFunction: (player, scene, args) =>
						{
								// Get the player stats
								var playerStats = player.Components.Get<Sandbox.GameSystems.Player.Player>();
								if (playerStats == null) return false;

								// Check if a username was provided
								if (args.Length < 1)
								{
									playerStats.SendMessage("Usage: /tp <player>");
									return false;
								}

								// Get the GameController instance
								var gameController = GameSystems.GameController.Instance;
								if (gameController == null) return false;

								// Find the target player by username
								var targetPlayer = gameController.PlayerLookup(args[0]);
								if (targetPlayer == null)
								{
									playerStats.SendMessage($"Player {args[0]} not found.");
									return false;
								}

								// Get the player's transform component
								var playerTransform = player.Transform;
								if (playerTransform == null) return false;

								// Calculate the forward direction based on the player's rotation
								var forwardDirection = playerTransform.Rotation * Vector3.Forward;

								// Calculate the teleport position (100 units in front of the player)
								var position = playerTransform.Position + forwardDirection * 100;

								// Set the target player's position to the calculated position
								targetPlayer.GameObject.Transform.Position = position;

								// Notify both players
								targetPlayer.GameObject.Components.Get<Sandbox.GameSystems.Player.Player>()?.SendMessage($"You have been teleported by {playerStats.GetNetworkPlayer().Name}.");
								playerStats.SendMessage($"You have teleported {targetPlayer.Connection.DisplayName} to your location.");

								return true;
						}
				)},
				// ── Law & Order Commands ──────────────────────────────────
				{ "laws", new Command(
						name: "laws",
						description: "Shows all current city laws.",
						permissionLevel: PermissionLevel.User,
						commandFunction: (player, scene, args) =>
						{
								var playerStats = player.Components.Get<Sandbox.GameSystems.Player.Player>();
								if (playerStats == null) return false;

								foreach (var line in LawManager.GetFormattedLaws())
								{
									playerStats.SendMessage(line);
								}
								return true;
						}
				)},
				{ "addlaw", new Command(
						name: "addlaw",
						description: "Mayor adds a new law. Usage: /addlaw <law text>",
						permissionLevel: PermissionLevel.User,
						commandFunction: (player, scene, args) =>
						{
								var playerStats = player.Components.Get<Sandbox.GameSystems.Player.Player>();
								if (playerStats == null) return false;

								// Check if player is Mayor
								var networkPlayer = playerStats.GetNetworkPlayer();
								if (networkPlayer?.Job?.Name != "Mayor")
								{
									playerStats.SendMessage("Only the Mayor can add laws.");
									return false;
								}

								if (args.Length < 1)
								{
									playerStats.SendMessage("Usage: /addlaw <law text>");
									return false;
								}

								string lawText = string.Join(" ", args);
								if (!LawManager.AddLaw(lawText))
								{
									playerStats.SendMessage($"Cannot add law. Maximum {Sandbox.GameSystems.BustasConfig.MaxLaws} laws allowed.");
									return false;
								}

								playerStats.SendMessage($"Law added: {lawText}");
								return true;
						}
				)},
				{ "removelaw", new Command(
						name: "removelaw",
						description: "Mayor removes a law by number. Usage: /removelaw <number>",
						permissionLevel: PermissionLevel.User,
						commandFunction: (player, scene, args) =>
						{
								var playerStats = player.Components.Get<Sandbox.GameSystems.Player.Player>();
								if (playerStats == null) return false;

								var networkPlayer = playerStats.GetNetworkPlayer();
								if (networkPlayer?.Job?.Name != "Mayor")
								{
									playerStats.SendMessage("Only the Mayor can remove laws.");
									return false;
								}

								if (args.Length < 1 || !int.TryParse(args[0], out int lawNum))
								{
									playerStats.SendMessage("Usage: /removelaw <number>");
									return false;
								}

								if (!LawManager.RemoveLaw(lawNum))
								{
									playerStats.SendMessage($"Invalid law number. Use /laws to see current laws.");
									return false;
								}

								playerStats.SendMessage($"Law #{lawNum} removed.");
								return true;
						}
				)},
				{ "wanted", new Command(
						name: "wanted",
						description: "Police: Set a player as wanted. Usage: /wanted <player> <reason>",
						permissionLevel: PermissionLevel.User,
						commandFunction: (player, scene, args) =>
						{
								var playerStats = player.Components.Get<Sandbox.GameSystems.Player.Player>();
								if (playerStats == null) return false;

								var networkPlayer = playerStats.GetNetworkPlayer();
								if (networkPlayer?.Job == null || !networkPlayer.Job.IsGovernment)
								{
									playerStats.SendMessage("Only government officials can set players as wanted.");
									return false;
								}

								if (args.Length < 2)
								{
									playerStats.SendMessage("Usage: /wanted <player> <reason>");
									return false;
								}

								var gameController = GameSystems.GameController.Instance;
								var target = gameController?.PlayerLookup(args[0]);
								if (target == null)
								{
									playerStats.SendMessage($"Player {args[0]} not found.");
									return false;
								}

								string reason = string.Join(" ", args.Skip(1));
								WantedManager.SetWanted(target.Connection.Id, reason);

								// Notify the target
								target.GameObject.Components.Get<Sandbox.GameSystems.Player.Player>()?.SendMessage($"You are now WANTED: {reason}");
								playerStats.SendMessage($"{target.Name} is now wanted: {reason}");
								return true;
						}
				)},
				{ "unwanted", new Command(
						name: "unwanted",
						description: "Police: Remove wanted status. Usage: /unwanted <player>",
						permissionLevel: PermissionLevel.User,
						commandFunction: (player, scene, args) =>
						{
								var playerStats = player.Components.Get<Sandbox.GameSystems.Player.Player>();
								if (playerStats == null) return false;

								var networkPlayer = playerStats.GetNetworkPlayer();
								if (networkPlayer?.Job == null || !networkPlayer.Job.IsGovernment)
								{
									playerStats.SendMessage("Only government officials can remove wanted status.");
									return false;
								}

								if (args.Length < 1)
								{
									playerStats.SendMessage("Usage: /unwanted <player>");
									return false;
								}

								var gameController = GameSystems.GameController.Instance;
								var target = gameController?.PlayerLookup(args[0]);
								if (target == null)
								{
									playerStats.SendMessage($"Player {args[0]} not found.");
									return false;
								}

								if (!WantedManager.IsWanted(target.Connection.Id))
								{
									playerStats.SendMessage($"{target.Name} is not wanted.");
									return false;
								}

								WantedManager.RemoveWanted(target.Connection.Id);
								target.GameObject.Components.Get<Sandbox.GameSystems.Player.Player>()?.SendMessage("You are no longer wanted.");
								playerStats.SendMessage($"{target.Name} is no longer wanted.");
								return true;
						}
				)},
				{ "warrant", new Command(
						name: "warrant",
						description: "Chief/Mayor: Issue a search warrant. Usage: /warrant <player> <reason>",
						permissionLevel: PermissionLevel.User,
						commandFunction: (player, scene, args) =>
						{
								var playerStats = player.Components.Get<Sandbox.GameSystems.Player.Player>();
								if (playerStats == null) return false;

								var networkPlayer = playerStats.GetNetworkPlayer();
								var jobName = networkPlayer?.Job?.Name ?? "";
								if (jobName != "Police Chief" && jobName != "Mayor" && jobName != "Police Sergeant")
								{
									playerStats.SendMessage("Only the Police Chief, Police Sergeant, or Mayor can issue warrants.");
									return false;
								}

								if (args.Length < 2)
								{
									playerStats.SendMessage("Usage: /warrant <player> <reason>");
									return false;
								}

								var gameController = GameSystems.GameController.Instance;
								var target = gameController?.PlayerLookup(args[0]);
								if (target == null)
								{
									playerStats.SendMessage($"Player {args[0]} not found.");
									return false;
								}

								string reason = string.Join(" ", args.Skip(1));
								if (!WarrantManager.IssueWarrant(target.Connection.Id, target.Name, reason, networkPlayer.Connection.Id))
								{
									playerStats.SendMessage($"Cannot issue warrant. Max {Sandbox.GameSystems.BustasConfig.MaxActiveWarrants} active warrants, or player already has a warrant.");
									return false;
								}

								// Notify the target and all government
								target.GameObject.Components.Get<Sandbox.GameSystems.Player.Player>()?.SendMessage($"A search warrant has been issued against you: {reason}");
								playerStats.SendMessage($"Warrant issued for {target.Name}: {reason}");
								return true;
						}
				)},
				// ── Criminal Economy Commands ─────────────────────────────
				{ "hit", new Command(
						name: "hit",
						description: "Place a hit or accept one. Usage: /hit <player> <amount> or /hit accept",
						permissionLevel: PermissionLevel.User,
						commandFunction: (player, scene, args) =>
						{
								var playerStats = player.Components.Get<Sandbox.GameSystems.Player.Player>();
								if (playerStats == null) return false;

								var networkPlayer = playerStats.GetNetworkPlayer();
								if (networkPlayer == null) return false;

								if (args.Length < 1)
								{
									playerStats.SendMessage("Usage: /hit <player> <amount> or /hit accept");
									return false;
								}

								// /hit accept - Hitman accepts the first available hit
								if (args[0].Equals("accept", StringComparison.OrdinalIgnoreCase))
								{
									if (networkPlayer.Job?.Name != "Hitman")
									{
										playerStats.SendMessage("Only Hitmen can accept hits.");
										return false;
									}

									var contract = HitManager.AcceptFirstAvailable(networkPlayer.Connection.Id);
									if (contract == null)
									{
										playerStats.SendMessage("No available hits to accept.");
										return false;
									}

									playerStats.SendMessage($"Hit accepted: Kill {contract.TargetName} for ${contract.Amount:N0}.");
									return true;
								}

								// /hit <player> <amount> - Place a hit
								if (args.Length < 2)
								{
									playerStats.SendMessage("Usage: /hit <player> <amount>");
									return false;
								}

								if (!float.TryParse(args[1], out float amount) || amount <= 0)
								{
									playerStats.SendMessage("Invalid amount.");
									return false;
								}

								if (amount < Sandbox.GameSystems.BustasConfig.HitMinPrice)
								{
									playerStats.SendMessage($"Minimum hit price is ${Sandbox.GameSystems.BustasConfig.HitMinPrice:N0}.");
									return false;
								}

								var gameController = GameSystems.GameController.Instance;
								var target = gameController?.PlayerLookup(args[0]);
								if (target == null)
								{
									playerStats.SendMessage($"Player {args[0]} not found.");
									return false;
								}

								// Check if the player can afford it
								if (!playerStats.UpdateBalance(-amount))
								{
									playerStats.SendMessage("You don't have enough money.");
									return false;
								}

								if (!HitManager.PlaceHit(networkPlayer.Connection.Id, networkPlayer.Name, target.Connection.Id, target.Name, amount))
								{
									// Refund if hit placement failed
									playerStats.UpdateBalance(amount);
									playerStats.SendMessage("Cannot place hit. Target may already have an active hit.");
									return false;
								}

								playerStats.SendMessage($"Hit placed on {target.Name} for ${amount:N0}.");

								// Notify all online Hitmen
								foreach (var p in gameController.GetAllPlayers())
								{
									if (p.Value.Job?.Name == "Hitman")
									{
										p.Value.GameObject.Components.Get<Sandbox.GameSystems.Player.Player>()?.SendMessage($"New hit available: {target.Name} for ${amount:N0}. Use /hit accept.");
									}
								}

								return true;
						}
				)},
				{ "mug", new Command(
						name: "mug",
						description: "Thief: Mug a nearby player. Usage: /mug <player>",
						permissionLevel: PermissionLevel.User,
						commandFunction: (player, scene, args) =>
						{
								var playerStats = player.Components.Get<Sandbox.GameSystems.Player.Player>();
								if (playerStats == null) return false;

								var networkPlayer = playerStats.GetNetworkPlayer();
								if (networkPlayer == null) return false;

								// Only Thieves can mug
								if (networkPlayer.Job?.Name != "Thief")
								{
									playerStats.SendMessage("Only Thieves can mug players.");
									return false;
								}

								if (args.Length < 1)
								{
									playerStats.SendMessage("Usage: /mug <player>");
									return false;
								}

								// Check cooldown
								if (MugManager.IsOnCooldown(networkPlayer.Connection.Id))
								{
									float remaining = MugManager.GetCooldownRemaining(networkPlayer.Connection.Id);
									playerStats.SendMessage($"Mug on cooldown. {remaining:F0}s remaining.");
									return false;
								}

								var gameController = GameSystems.GameController.Instance;
								var target = gameController?.PlayerLookup(args[0]);
								if (target == null)
								{
									playerStats.SendMessage($"Player {args[0]} not found.");
									return false;
								}

								// Can't mug government
								if (target.Job?.IsGovernment == true)
								{
									playerStats.SendMessage("You cannot mug government officials.");
									return false;
								}

								if (!MugManager.StartMug(networkPlayer.Connection.Id, networkPlayer.Name, target.Connection.Id, target.Name))
								{
									playerStats.SendMessage("Cannot mug this player right now.");
									return false;
								}

								playerStats.SendMessage($"You are mugging {target.Name}! They have 10 seconds to drop up to ${Sandbox.GameSystems.BustasConfig.MugMaxAmount:N0}.");
								target.GameObject.Components.Get<Sandbox.GameSystems.Player.Player>()?.SendMessage($"You are being mugged by {networkPlayer.Name}! Drop up to ${Sandbox.GameSystems.BustasConfig.MugMaxAmount:N0} or resist! (10 seconds)");

								return true;
						}
				)},
				// ── Vehicle Commands ──────────────────────────────────────
				{ "repair", new Command(
						name: "repair",
						description: "Mechanic: Repair a vehicle you're looking at. Usage: /repair",
						permissionLevel: PermissionLevel.User,
						commandFunction: (player, scene, args) =>
						{
								var playerStats = player.Components.Get<Sandbox.GameSystems.Player.Player>();
								if (playerStats == null) return false;

								var networkPlayer = playerStats.GetNetworkPlayer();
								if (networkPlayer == null) return false;

								// Only Mechanics can use /repair
								if (networkPlayer.Job?.Name != "Mechanic")
								{
									playerStats.SendMessage("Only Mechanics can repair vehicles.");
									return false;
								}

								// Trace to find a vehicle the player is looking at
								var camera = scene.GetAllComponents<CameraComponent>().FirstOrDefault(x => x.IsMainCamera);
								if (camera == null) return false;

								var start = camera.Transform.Position;
								var direction = camera.Transform.World.Forward;
								var end = start + direction * 200f;
								var tr = scene.Trace.IgnoreGameObject(player).Ray(start, end).Run();

								var vehicleLogic = tr.GameObject?.Components.Get<VehicleLogic>();
								if (vehicleLogic == null)
								{
									playerStats.SendMessage("No vehicle found. Look at a vehicle to repair it.");
									return false;
								}

								// Check if vehicle needs repair
								if (vehicleLogic.CurrentHealth >= vehicleLogic.MaxHealth)
								{
									playerStats.SendMessage("This vehicle doesn't need repairs.");
									return false;
								}

								float repairCost = vehicleLogic.GetRepairCost();

								// Find the vehicle owner to charge them
								var gameController = GameSystems.GameController.Instance;
								var ownerPlayer = gameController?.GetPlayerByConnectionId(vehicleLogic.OwnerConnectionId);
								if (ownerPlayer == null)
								{
									playerStats.SendMessage("Vehicle owner not found.");
									return false;
								}

								var ownerStats = ownerPlayer.GameObject.Components.Get<Sandbox.GameSystems.Player.Player>();
								if (ownerStats == null) return false;

								// Charge the vehicle owner
								if (!ownerStats.UpdateBalance(-repairCost))
								{
									playerStats.SendMessage($"Vehicle owner can't afford the repair (${repairCost:N0}).");
									return false;
								}

								// Pay the mechanic
								playerStats.UpdateBalance(repairCost);

								// Repair the vehicle
								vehicleLogic.FullRepair();

								playerStats.SendMessage($"Vehicle repaired! You earned ${repairCost:N0}.");
								ownerStats.SendMessage($"Your {vehicleLogic.EntityName} was repaired for ${repairCost:N0}.");
								return true;
						}
				)}
		};

		public IReadOnlyCollection<ICommandConfig> Commands => _commands.Values;
		/// <summary>
		/// Registers a command.
		/// </summary>
		/// <param name="command"></param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		public void RegisterCommand(ICommandConfig command)
		{
			if (command == null)
				throw new ArgumentNullException(nameof(command));

			var commandNameLower = command.Name.ToLowerInvariant();
			if (_commands.ContainsKey(commandNameLower)) Log.Warning($"Command with name \"{commandNameLower}\" already exists.");

			_commands[commandNameLower] = command;
		}

		public void UnregisterCommand(string commandName)
		{
			commandName = commandName.ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(commandName))
				throw new ArgumentException("Command name cannot be null or whitespace.", nameof(commandName));

			if (!_commands.Remove(commandName)) Log.Warning($"Command with name \"{commandName}\" does not exist.");
		}

		public ICommandConfig GetCommand(string commandName)
		{
			// Lowercase the command name to make it case-insensitive.
			commandName = commandName.ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(commandName))
				throw new ArgumentException("Command name cannot be null or whitespace.", nameof(commandName));

			if (_commands.TryGetValue(commandName, out var command))
				return command;

			throw new KeyNotFoundException($"Command with name {commandName} does not exist.");
		}

		public string[] GetCommandNames()
		{
			var commandNames = _commands.Keys.ToList();
			commandNames.Add("help");
			return commandNames.ToArray();
		}

		[Broadcast( NetPermission.HostOnly )]
		public void ExecuteCommand(string commandName, GameObject player, Scene scene, string[] args)
		{
			// Get the PlayerStats component. This is required for all players. Verifies the player is a player.
			var playerStats = player.Components.Get<Sandbox.GameSystems.Player.Player>();
			if ( playerStats == null ) return;
			try
			{

				// Check if its the default "help" command
				if ( commandName == "help" )
				{
					var commandNames = string.Join( ", ", GetCommandNames().Select( name => "/" + name ) );

					playerStats.SendMessage( $"Available commands: {commandNames}" );
					return;
				}

				// Get the player details
				var details = playerStats.GetNetworkPlayer();
				if ( details == null ) return;

				var command = GetCommand( commandName );

				if ( !details.CheckPermission(command.PermissionLevel) )
				{
					playerStats.SendMessage( "You do not have permission to execute this command." );
					return;
				}

				Log.Info( $"Executing command \"{commandName}\"." );
				if ( command.CommandFunction( player, scene, args ) == false )
				{
					return;
				}
				return;
			}
			catch ( Exception e )
			{
				Log.Error( $"Failed to execute command \"{commandName}\": {e.Message}" );
				playerStats.SendMessage( $"Failed to execute command \"{commandName}\"." );
				return;
			}
		}
	}
}


