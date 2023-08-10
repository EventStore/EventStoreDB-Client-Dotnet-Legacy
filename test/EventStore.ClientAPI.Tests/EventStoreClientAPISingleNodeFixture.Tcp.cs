using System;
using System.Net;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI {
	partial class EventStoreClientAPISingleNodeFixture {
		private const bool UseLoggerBridge = true;

		public IEventStoreConnection CreateConnection(
			Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configureSettings = default,
			int? port = default, bool useDnsEndPoint = true, bool authenticated = true) {

			UserCredentials user = authenticated ? DefaultUserCredentials.Admin : null;
			var settings = (configureSettings ?? DefaultConfigureSettings)(DefaultBuilder).SetDefaultUserCredentials(user).Build();
			return EventStoreConnection.Create(
				settings,
				useDnsEndPoint
					? new DnsEndPoint("localhost", port ?? 1113)
					: new IPEndPoint(IPAddress.Loopback, port ?? 1113));
		}

		public IEventStoreConnection CreateConnectionWithConnectionString(string configureSettings = default,
			int? port = default, bool useDnsEndPoint = false, bool authenticated = true) {
			var settings = configureSettings ?? DefaultConfigureSettingsForConnectionString;
			var host = useDnsEndPoint ? "localhost" : IPAddress.Loopback.ToString();
			port ??= 1113;

			settings += "UseSslConnection=true;ValidateServer=false;";

			var auth = authenticated ? "admin:changeit@" : "";

			return EventStoreConnection.Create($"ConnectTo=tcp://{auth}{host}:{port};{settings}");
		}
	}
}
