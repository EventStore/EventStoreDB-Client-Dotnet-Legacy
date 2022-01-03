using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace secure_with_tls 
{
	class Program 
	{
		static async Task Main(string[] args) 
		{
			// take the address from environment variable (when run with Docker) or use localhost by default 
			var connectionString = Environment.GetEnvironmentVariable("ESDB_CONNECTION_STRING") ?? "ConnectTo=tcp://localhost:1113;DefaultCredentials=admin:changeit";

			Console.WriteLine($"Connecting to EventStoreDB at: `{connectionString}`");
			
			using var connection = EventStoreConnection.Create(connectionString);
			await connection.ConnectAsync();
			
			var eventData = new EventData(
				Guid.NewGuid(), 
				"some-event",
				true,
				Encoding.UTF8.GetBytes("{\"id\": \"1\" \"value\": \"some value\"}"),
				null
			);


			try {
				var appendResult = await connection.AppendToStreamAsync(
					"some-stream",
					ExpectedVersion.Any,
					new List<EventData> {
						eventData
					});
				Console.WriteLine($"SUCCESS! Append result: {appendResult.LogPosition}");
			} 
			catch (Exception exception) 
			{
				Console.WriteLine($"FAILED! {exception}");
			}
		}
	}
}
