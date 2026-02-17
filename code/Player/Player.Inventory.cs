using System.Linq;
using Sandbox.GameResources;
using Scenebox;

namespace Sandbox.GameSystems.Player;

public partial class Player
{
	// define the default Items related to all players
	[Property, Group("Inventory")] public List<WeaponResource> DefaultItems;
	[Property, Group("Inventory")] public float InventoryVisibilityDelay { get; set; } = 3f;
	public bool IsInventoryVisible;
	public const int MaxSlots = 9;
	public int CurrentSelectedSlot;
	// Slots for storing weapon resources
	public WeaponResource[] InventorySlots;
	private TimeSince _timeSinceLastVisible = 0;
	private bool _inputDetected;

	// Currently equipped weapon instance
	private GameObject _equippedWeaponObject;
	[Sync] public int ActiveWeaponSlot { get; set; } = 1;

	private void OnStartInventory()
	{
		// Destroy any existing equipped weapon
		if ( _equippedWeaponObject != null && _equippedWeaponObject.IsValid )
		{
			_equippedWeaponObject.Destroy();
			_equippedWeaponObject = null;
		}

		// Initialize the inventory slots
		InventorySlots = new WeaponResource[MaxSlots];

		// Equip all the defaults Items
		foreach ( var weaponResource in DefaultItems )
		{
			AddItem( weaponResource );
		}

		// Equip slot 1 (hands) by default
		CurrentSelectedSlot = 1;
		EquipItem( 1 );
	}

	protected void OnFixedUpdateInventory()
	{
		CheckForInputs();
	}

	// Add the desired item to the inventory
	public void AddItem( WeaponResource resource )
	{
		int slotIndex = resource.Slot - 1;

		if ( slotIndex >= 0 && slotIndex < MaxSlots )
		{
			InventorySlots[slotIndex] = resource;
			Log.Info( $"Weapon {resource.Name} added to slot {slotIndex + 1}" );
		}
		else
		{
			Log.Warning( "Invalid slot selected!" );
		}
	}

	// Equip the desired Item from the slot
	public void EquipItem( int slot )
	{
		if ( slot < 1 || slot > MaxSlots )
		{
			Log.Warning( "Invalid slot selected!" );
			return;
		}

		ActiveWeaponSlot = slot;
		var resource = InventorySlots[slot - 1];

		// Destroy the previously equipped weapon
		if ( _equippedWeaponObject != null && _equippedWeaponObject.IsValid )
		{
			_equippedWeaponObject.Destroy();
			_equippedWeaponObject = null;
		}

		if ( resource == null )
		{
			Log.Info( $"No weapon in slot {slot}" );
			return;
		}

		if ( resource.MainPrefab == null )
		{
			Log.Warning( $"Weapon {resource.Name} has no MainPrefab assigned" );
			return;
		}

		// Instantiate the weapon prefab as a child of this player
		_equippedWeaponObject = resource.MainPrefab.Clone( Transform.Position );
		_equippedWeaponObject.SetParent( GameObject );

		// Configure the Weapon component with the resource data
		var weapon = _equippedWeaponObject.Components.Get<Weapon>();
		if ( weapon != null )
		{
			weapon.Resource = resource;
		}

		Log.Info( $"Equipped weapon: {resource.Name} from slot {slot}" );
	}

	// Remove the desired item from the inventory
	public void RemoveItem( int slot )
	{
		if ( slot < 1 || slot > MaxSlots ) return;

		var resource = InventorySlots[slot - 1];
		if ( resource == null ) return;

		InventorySlots[slot - 1] = null;

		// If this was the active weapon, destroy it
		if ( ActiveWeaponSlot == slot && _equippedWeaponObject != null && _equippedWeaponObject.IsValid )
		{
			_equippedWeaponObject.Destroy();
			_equippedWeaponObject = null;
		}

		Log.Info( $"Removed {resource.Name} from slot {slot}" );
	}

	// Check if the inventory has a specific item
	public bool HasItem( WeaponResource resource )
	{
		return InventorySlots != null && InventorySlots.Any( slot => slot != null && slot == resource );
	}

	// Drop the currently equipped item
	[Broadcast]
	public void DropItem()
	{
		if ( ActiveWeaponSlot < 1 || ActiveWeaponSlot > MaxSlots ) return;

		var resource = InventorySlots[ActiveWeaponSlot - 1];
		if ( resource == null ) return;

		// Don't allow dropping slot 1 (hands)
		if ( ActiveWeaponSlot == 1 ) return;

		RemoveItem( ActiveWeaponSlot );
		Log.Info( $"Dropped {resource.Name}" );
	}

	/// <summary>
	/// Clears all weapons and destroys the equipped weapon object.
	/// Called on death to clean up weapon state.
	/// </summary>
	public void ClearInventory()
	{
		if ( _equippedWeaponObject != null && _equippedWeaponObject.IsValid )
		{
			_equippedWeaponObject.Destroy();
			_equippedWeaponObject = null;
		}

		if ( InventorySlots != null )
		{
			for ( int i = 0; i < InventorySlots.Length; i++ )
			{
				InventorySlots[i] = null;
			}
		}
	}

	private void CheckForInputs()
	{
		// Reset input detection at the beginning of each check
		_inputDetected = false;
		var wheel = -Input.MouseWheel.y;

		if ( wheel > 0 || Input.Pressed( "SlotNext" ) )
		{
			CurrentSelectedSlot++;
			SlotLogicCheck();
		}

		if ( wheel < 0 || Input.Pressed( "SlotPrev" ))
		{
			CurrentSelectedSlot--;
			SlotLogicCheck();
		}

		// Check input for slots 0 to 9
		for ( int i = 0; i < 10; i++ )
		{
			if ( Input.Pressed( $"Slot{i}" ) )
			{
				// Show inventory and play sound when a slot key is pressed
				IsInventoryVisible = true;
				CurrentSelectedSlot = i;
				PlayInventoryOpenSound();
				SlotLogicCheck();
				_inputDetected = true;
				break; // Exit loop once an input is detected
			}

		}

		// Check input for SlotNext and SlotPrev
		if ( !_inputDetected && (Input.Pressed( "SlotNext" ) || Input.Pressed( "SlotPrev" )) )
		{
			IsInventoryVisible = true;
			PlayInventoryOpenSound();
			SlotLogicCheck();
			_inputDetected = true;
		}

		// Check for mouse wheel input
		if ( !_inputDetected && wheel != 0 )
		{
			IsInventoryVisible = true;
			PlayInventoryOpenSound();
			_inputDetected = true;
		}

		// Hide inventory if the delay has passed and no input was detected
		if ( _timeSinceLastVisible >= InventoryVisibilityDelay && !_inputDetected )
		{
			IsInventoryVisible = false;
			_timeSinceLastVisible = 0;
		}

		// Reset the timer if an input was detected
		if ( _inputDetected )
		{
			_timeSinceLastVisible = 0;
		}

	}

	private void SlotLogicCheck()
	{
		if (CurrentSelectedSlot < 1) { CurrentSelectedSlot = MaxSlots; }
		if (CurrentSelectedSlot > MaxSlots ) { CurrentSelectedSlot = 1; }
		EquipItem( CurrentSelectedSlot );
	}

	private void PlayInventoryOpenSound()
	{
		Sound.Play( "audio/select.sound" );
	}

}
