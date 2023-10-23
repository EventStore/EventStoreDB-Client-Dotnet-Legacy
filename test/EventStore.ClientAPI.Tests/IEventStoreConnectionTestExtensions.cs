using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.Exceptions;

namespace EventStore.ClientAPI;

public static class EventStoreConnectionTestExtensions {
	public static async Task WaitForUsers(this IEventStoreConnection connection) {
		int attempts = 0;
		while (true) {
			try {
				await connection.ReadStreamEventsForwardAsync("$users", 0, 100, false, DefaultUserCredentials.Admin);
				return;
			} catch (NotAuthenticatedException) {
				if (attempts++ > 500)
					throw;
				await Task.Delay(TimeSpan.FromMilliseconds(100));
			}
		}
	}
}
