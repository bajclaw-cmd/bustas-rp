using System;
using System.Linq;

namespace Sandbox.GameResources;

[GameResource( "DarkRp/Weapon", "weapon", "A basic weapon definition", IconBgColor = "#5877E0", Icon = "track_changes" )]
public partial class WeaponResource : GameResource
{
	public static HashSet<WeaponResource> All { get; set; } = new();

	[Group( "General" )] public string Name { get; set; } = "Weapon";
	[Group( "General" )] public string Description { get; set; } = "A basic weapon";
	[Group( "General" )] public int Slot { get; set; } = 1;
	[Group( "General" )] public string IconCharacter { get; set; } = "u";
	[Group( "General" ), ImageAssetPath] public string Icon { get; set; }
	[Group( "Ammo" )] public bool HasAmmo { get; set; } = true;
	[Group( "Ammo" )] public int ClipSize { get; set; } = 16;
	[Group( "Ammo" )] public int StartingReserve { get; set; } = 32;
	[Group( "Stats" )] public float Damage { get; set; } = 15f;
	[Group( "Stats" )] public float FireRate { get; set; } = 0.15f;
	[Group( "Stats" )] public float Range { get; set; } = 5000f;
	[Group( "Stats" )] public float Spread { get; set; } = 0.02f;
	[Group( "Stats" )] public float ReloadTime { get; set; } = 2.0f;
	[Group( "Prefabs" )] public GameObject MainPrefab { get; set; }
	[Group( "Prefabs" )] public GameObject ViewModelPrefab { get; set; }
	[Group( "Information" )] public Model WorldModel { get; set; }

	public static WeaponResource FindByName( string name )
	{
		return All.FirstOrDefault( w => w.Name.Equals( name, StringComparison.OrdinalIgnoreCase ) );
	}

	protected override void PostLoad()
	{
		if ( All.Contains( this ) )
		{
			Log.Warning( "Tried to add two of the same weapon (?)" );
			return;
		}

		All.Add( this );
	}
}
