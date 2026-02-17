using System;
using Sandbox.GameSystems;

namespace GameSystems.Phone
{
	/// <summary>
	/// Manages player contacts. Contacts persist between sessions via filesystem.
	/// Each player has their own contact list keyed by their SteamId.
	/// </summary>
	public static class ContactManager
	{
		public record Contact( ulong SteamId, string DisplayName, string JobName, DateTime AddedAt );

		private static readonly Dictionary<ulong, List<Contact>> _contacts = new();
		private const string ContactsDir = "playersdata/contacts";

		/// <summary>
		/// Add a contact for a player. Returns false if already exists.
		/// </summary>
		public static bool AddContact( ulong ownerSteamId, ulong contactSteamId, string displayName, string jobName )
		{
			if ( ownerSteamId == contactSteamId )
				return false;

			var contacts = GetContacts( ownerSteamId );

			// Check if contact already exists
			if ( contacts.Any( c => c.SteamId == contactSteamId ) )
				return false;

			contacts.Add( new Contact( contactSteamId, displayName, jobName, DateTime.Now ) );
			SaveContacts( ownerSteamId );
			return true;
		}

		/// <summary>
		/// Remove a contact by SteamId.
		/// </summary>
		public static bool RemoveContact( ulong ownerSteamId, ulong contactSteamId )
		{
			var contacts = GetContacts( ownerSteamId );
			var removed = contacts.RemoveAll( c => c.SteamId == contactSteamId ) > 0;
			if ( removed ) SaveContacts( ownerSteamId );
			return removed;
		}

		/// <summary>
		/// Get all contacts for a player. Loads from disk if not cached.
		/// </summary>
		public static List<Contact> GetContacts( ulong ownerSteamId )
		{
			if ( !_contacts.ContainsKey( ownerSteamId ) )
			{
				_contacts[ownerSteamId] = LoadContacts( ownerSteamId );
			}
			return _contacts[ownerSteamId];
		}

		/// <summary>
		/// Check if a player has a specific contact.
		/// </summary>
		public static bool HasContact( ulong ownerSteamId, ulong contactSteamId )
		{
			return GetContacts( ownerSteamId ).Any( c => c.SteamId == contactSteamId );
		}

		/// <summary>
		/// Update a contact's display name and job (e.g., when they change jobs).
		/// </summary>
		public static void UpdateContact( ulong ownerSteamId, ulong contactSteamId, string newName, string newJob )
		{
			var contacts = GetContacts( ownerSteamId );
			var index = contacts.FindIndex( c => c.SteamId == contactSteamId );
			if ( index >= 0 )
			{
				var old = contacts[index];
				contacts[index] = old with { DisplayName = newName, JobName = newJob };
			}
		}

		/// <summary>
		/// Save contacts to filesystem.
		/// </summary>
		private static void SaveContacts( ulong ownerSteamId )
		{
			try
			{
				if ( !FileSystem.Data.DirectoryExists( ContactsDir ) )
					FileSystem.Data.CreateDirectory( ContactsDir );

				var contacts = GetContacts( ownerSteamId );
				var json = Json.Serialize( contacts );
				FileSystem.Data.WriteAllText( $"{ContactsDir}/{ownerSteamId}.json", json );
			}
			catch ( Exception e )
			{
				Log.Warning( $"Failed to save contacts for {ownerSteamId}: {e.Message}" );
			}
		}

		/// <summary>
		/// Load contacts from filesystem.
		/// </summary>
		private static List<Contact> LoadContacts( ulong ownerSteamId )
		{
			try
			{
				var path = $"{ContactsDir}/{ownerSteamId}.json";
				if ( !FileSystem.Data.FileExists( path ) )
					return new List<Contact>();

				var json = FileSystem.Data.ReadAllText( path );
				return Json.Deserialize<List<Contact>>( json ) ?? new List<Contact>();
			}
			catch ( Exception e )
			{
				Log.Warning( $"Failed to load contacts for {ownerSteamId}: {e.Message}" );
				return new List<Contact>();
			}
		}

		/// <summary>
		/// Clear cached contacts for a player (on disconnect).
		/// </summary>
		public static void ClearCache( ulong steamId )
		{
			if ( _contacts.ContainsKey( steamId ) )
			{
				SaveContacts( steamId );
				_contacts.Remove( steamId );
			}
		}
	}
}
