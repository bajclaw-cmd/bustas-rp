using System;
using GameSystems;
using Sandbox.GameResources;

namespace Scenebox;

public class Weapon : Component
{
	[Property] public WeaponResource Resource { get; set; }

	// Stats (initialized from Resource on start, can be overridden per-prefab)
	[Property, Group( "Stats" )] public float Damage { get; set; } = 15f;
	[Property, Group( "Stats" )] public float FireRate { get; set; } = 0.15f;
	[Property, Group( "Stats" )] public float Range { get; set; } = 5000f;
	[Property, Group( "Stats" )] public float Spread { get; set; } = 0.02f;
	[Property, Group( "Stats" )] public float ReloadTime { get; set; } = 2.0f;

	// Synced ammo state
	[Sync] public int CurrentClip { get; set; }
	[Sync] public int ReserveAmmo { get; set; }
	[Sync] public bool IsReloading { get; set; }

	private TimeSince _timeSinceLastShot;
	private TimeSince _timeSinceReloadStart;
	private CameraComponent _camera;

	protected override void OnStart()
	{
		if ( IsProxy )
			return;

		_camera = Scene.Camera;

		if ( Resource != null )
		{
			Damage = Resource.Damage;
			FireRate = Resource.FireRate;
			Range = Resource.Range;
			Spread = Resource.Spread;
			ReloadTime = Resource.ReloadTime;

			if ( Resource.HasAmmo )
			{
				CurrentClip = Resource.ClipSize;
				ReserveAmmo = Resource.StartingReserve;
			}
		}
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		// Handle reload completion
		if ( IsReloading && _timeSinceReloadStart >= ReloadTime )
		{
			FinishReload();
		}

		if ( IsReloading ) return;

		// Fire input
		if ( Input.Down( "attack1" ) && _timeSinceLastShot >= FireRate )
		{
			if ( Resource == null || !Resource.HasAmmo )
			{
				// Unlimited ammo weapon (melee/special)
				PrimaryAttack();
			}
			else if ( CurrentClip > 0 )
			{
				PrimaryAttack();
			}
			else if ( ReserveAmmo > 0 )
			{
				StartReload();
			}
			else
			{
				// Empty click
				_timeSinceLastShot = 0;
			}
		}

		// Manual reload
		if ( Input.Pressed( "reload" ) && !IsReloading )
		{
			if ( Resource != null && Resource.HasAmmo && CurrentClip < Resource.ClipSize && ReserveAmmo > 0 )
			{
				StartReload();
			}
		}
	}

	protected virtual void PrimaryAttack()
	{
		_timeSinceLastShot = 0;

		if ( Resource != null && Resource.HasAmmo )
		{
			CurrentClip--;
		}

		ShootHitscan();
	}

	private void ShootHitscan()
	{
		if ( _camera == null ) return;

		var start = _camera.Transform.Position;
		var forward = _camera.Transform.World.Forward;

		// Apply spread
		var spread = (Vector3.Random * Spread);
		var direction = (forward + spread).Normal;
		var end = start + direction * Range;

		var tr = Scene.Trace.Ray( start, end )
			.UseHitboxes()
			.IgnoreGameObject( GameObject.Root )
			.Run();

		// Broadcast fire effects for all clients
		BroadcastFireEffects( start, tr.Hit ? tr.EndPosition : end );

		if ( tr.Hit && tr.GameObject != null )
		{
			DealDamage( tr );
		}
	}

	[Broadcast]
	private void BroadcastFireEffects( Vector3 start, Vector3 end )
	{
		// TODO: Add weapon-specific fire sound
		// Sound.Play( "sounds/weapon/fire.sound", Transform.Position );

		// TODO: Muzzle flash particle
		// TODO: Bullet tracer from start to end
		// TODO: Impact effect at end position
	}

	private void DealDamage( SceneTraceResult tr )
	{
		// Check for player
		var targetPlayer = tr.GameObject.Root.Components.Get<Sandbox.GameSystems.Player.Player>();
		if ( targetPlayer != null )
		{
			// Get attacker name for kill attribution
			var attackerPlayer = GameObject.Root.Components.Get<Sandbox.GameSystems.Player.Player>();
			var attackerName = attackerPlayer?.Name ?? "Unknown";
			ApplyDamageToPlayer( targetPlayer.GameObject.Id, Damage, attackerName );
			return;
		}

		// Check for BaseEntity (printers, props, etc.)
		var targetEntity = tr.GameObject.Root.Components.Get<Sandbox.Entity.BaseEntity>();
		if ( targetEntity != null )
		{
			targetEntity.TakeDamage( Damage );
		}
	}

	[Broadcast]
	private void ApplyDamageToPlayer( Guid targetGameObjectId, float damage, string attackerName )
	{
		if ( !Networking.IsHost ) return;

		var networkPlayer = GameController.Instance?.GetPlayerByGameObjectId( targetGameObjectId );
		if ( networkPlayer == null ) return;

		var target = networkPlayer.GameObject.Components.Get<Sandbox.GameSystems.Player.Player>();
		if ( target == null ) return;

		// Set last attacker for kill credit
		target.LastAttacker = attackerName;
		target.SetHealth( target.Health - damage );
	}

	private void StartReload()
	{
		IsReloading = true;
		_timeSinceReloadStart = 0;
	}

	private void FinishReload()
	{
		IsReloading = false;

		if ( Resource == null ) return;

		int needed = Resource.ClipSize - CurrentClip;
		int toLoad = Math.Min( needed, ReserveAmmo );
		CurrentClip += toLoad;
		ReserveAmmo -= toLoad;
	}
}
