using System;
using Sandbox.GameSystems;

namespace GameSystems.Phone
{
	/// <summary>
	/// Manages text messages between players. Messages persist during the session only.
	/// Each conversation is keyed by a pair of SteamIds (sorted for consistency).
	/// </summary>
	public static class MessageManager
	{
		public record TextMessage( ulong SenderSteamId, string SenderName, ulong ReceiverSteamId, string Content, DateTime Timestamp );

		/// <summary>
		/// Pending notification for a player who received a new message.
		/// </summary>
		public record MessageNotification( ulong SenderSteamId, string SenderName, string Content, RealTimeSince TimeSinceReceived );

		// Conversations keyed by "smallerId_largerId"
		private static readonly Dictionary<string, List<TextMessage>> _conversations = new();

		// Pending notifications per player (receiver SteamId -> list of notifications)
		private static readonly Dictionary<ulong, List<MessageNotification>> _notifications = new();

		/// <summary>
		/// Send a text message from one player to another.
		/// </summary>
		public static void SendMessage( ulong senderSteamId, string senderName, ulong receiverSteamId, string content )
		{
			if ( string.IsNullOrWhiteSpace( content ) ) return;

			var key = GetConversationKey( senderSteamId, receiverSteamId );
			if ( !_conversations.ContainsKey( key ) )
				_conversations[key] = new List<TextMessage>();

			var msg = new TextMessage( senderSteamId, senderName, receiverSteamId, content, DateTime.Now );
			_conversations[key].Add( msg );

			// Trim if over max history
			while ( _conversations[key].Count > BustasConfig.MaxMessageHistory )
				_conversations[key].RemoveAt( 0 );

			// Add notification for receiver
			if ( !_notifications.ContainsKey( receiverSteamId ) )
				_notifications[receiverSteamId] = new List<MessageNotification>();

			_notifications[receiverSteamId].Add( new MessageNotification( senderSteamId, senderName, content, 0 ) );

			Log.Info( $"Message from {senderName} to {receiverSteamId}: {content}" );
		}

		/// <summary>
		/// Get all messages in a conversation between two players.
		/// </summary>
		public static List<TextMessage> GetConversation( ulong steamId1, ulong steamId2 )
		{
			var key = GetConversationKey( steamId1, steamId2 );
			if ( _conversations.TryGetValue( key, out var messages ) )
				return new List<TextMessage>( messages );
			return new List<TextMessage>();
		}

		/// <summary>
		/// Get and clear pending notifications for a player.
		/// </summary>
		public static List<MessageNotification> PopNotifications( ulong steamId )
		{
			if ( !_notifications.TryGetValue( steamId, out var notifs ) || notifs.Count == 0 )
				return new List<MessageNotification>();

			var result = new List<MessageNotification>( notifs );
			notifs.Clear();
			return result;
		}

		/// <summary>
		/// Check if a player has unread notifications.
		/// </summary>
		public static bool HasNotifications( ulong steamId )
		{
			return _notifications.TryGetValue( steamId, out var notifs ) && notifs.Count > 0;
		}

		/// <summary>
		/// Get the number of pending notifications.
		/// </summary>
		public static int GetNotificationCount( ulong steamId )
		{
			if ( _notifications.TryGetValue( steamId, out var notifs ) )
				return notifs.Count;
			return 0;
		}

		/// <summary>
		/// Get all unique conversation partners for a player.
		/// </summary>
		public static List<ulong> GetConversationPartners( ulong steamId )
		{
			var partners = new HashSet<ulong>();
			foreach ( var kvp in _conversations )
			{
				foreach ( var msg in kvp.Value )
				{
					if ( msg.SenderSteamId == steamId )
						partners.Add( msg.ReceiverSteamId );
					else if ( msg.ReceiverSteamId == steamId )
						partners.Add( msg.SenderSteamId );
				}
			}
			return partners.ToList();
		}

		/// <summary>
		/// Clean up all messages involving a player (on disconnect).
		/// </summary>
		public static void ClearPlayer( ulong steamId )
		{
			_notifications.Remove( steamId );
		}

		private static string GetConversationKey( ulong id1, ulong id2 )
		{
			var small = Math.Min( id1, id2 );
			var large = Math.Max( id1, id2 );
			return $"{small}_{large}";
		}
	}
}
