﻿
using Sandbox.Internal;
using System.Threading.Tasks;
using System.Text.Json;
using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Strafe.Api;

internal class Backend
{

	public static bool Connected => WebSocket?.IsConnected ?? false;

	public static string Endpoint => "https://localhost:7265/api";
	public static string WebSocketEndpoint => "wss://localhost:7265/api/ws";

	//public static string Endpoint => "https://strafedb.com/api";
	//public static string WebSocketEndpoint => "wss://strafedb.com/api/ws";

	private static WebSocket WebSocket;
	private static int MessageIdAccumulator;
	private static List<GameMessage> Responses = new();
	private static JsonSerializerOptions JsonOptions = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };

	public static async Task<T> Get<T>( string controller )
	{
		var http = new Http( new System.Uri( $"{Endpoint}/{controller}" ) );
		var result = await http.GetStringAsync();
		http.Dispose();

		return JsonSerializer.Deserialize<T>( result );
	}

	public static async Task Post( string controller, string jsonData )
	{
		if ( !await EnsureWebSocket() )
		{
			Log.Error( "WebSocket failed to connect" );
			return;
		}

		var msg = new GameMessage()
		{
			Id = ++MessageIdAccumulator,
			Controller = controller,
			Message = jsonData,
		};

		await WebSocket.Send( JsonSerializer.Serialize( msg, JsonOptions ) );
	}

	public static async Task<T> Post<T>( string controller, string jsonData )
	{
		if( !await EnsureWebSocket() )
		{
			Log.Error( "WebSocket failed to connect" );
			return default;
		}

		jsonData ??= string.Empty;

		var msg = new GameMessage()
		{
			Id = ++MessageIdAccumulator,
			Controller = controller,
			Message = jsonData,
		};

		await WebSocket.Send( JsonSerializer.Serialize( msg, JsonOptions ) );
		var response = await WaitForResponse( msg.Id );

		if( response == null )
		{
			Log.Error( $"WebSocket response failed: {controller}" );
			return default;
		}

		try
		{
			return JsonSerializer.Deserialize<T>( response.Message, JsonOptions );
		}
		catch( System.Exception e )
		{
			Log.Warning( "Errored on WebSocket message: " + e.Message );
			return default;
		}
	}

	private static async Task<GameMessage> WaitForResponse( int messageid, float timeout = 7f )
	{
		RealTimeUntil tu = timeout;
		while( tu > 0 )
		{
			var response = Responses.FirstOrDefault( x => x.Id == messageid );
			if ( response != null ) return response;

			await Task.Delay( 100 );
		}
		return null;
	}

	private static bool connectionInProgress;
	private static async Task<bool> EnsureWebSocket()
	{
		Host.AssertServer();

		while ( connectionInProgress ) await Task.Delay( 100 );

		if ( WebSocket?.IsConnected ?? false ) return true;

		connectionInProgress = true;

		WebSocket?.Dispose();
		WebSocket = new();
		WebSocket.OnMessageReceived += WebSocket_OnMessageReceived;
		await WebSocket.Connect( WebSocketEndpoint );

		connectionInProgress = false;

		return WebSocket.IsConnected;
	}

	[Event.Tick]
	public static void OnTick()
	{
		for ( int i = Responses.Count - 1; i >= 0; i-- )
		{
			if ( (Responses[i]?.TimeSinceReceived ?? 0) > 7f )
			{
				Responses.RemoveAt( i );
			}
		}
	}

	private static void WebSocket_OnMessageReceived( string message )
	{
		try
		{
			var msg = JsonSerializer.Deserialize<GameMessage>( message );
			msg.TimeSinceReceived = 0;
			Responses.Add( msg );
		}
		catch( System.Exception e )
		{
			Log.Warning( e.Message );
		}
	}

	public class GameMessage
	{
		public int Id { get; set; }
		public string Controller { get; set; }
		public string Message { get; set; }

		public TimeSince TimeSinceReceived;
	}

}
