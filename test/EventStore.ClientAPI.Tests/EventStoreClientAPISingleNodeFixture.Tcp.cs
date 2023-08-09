using System;
using System.Net;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI {
	partial class EventStoreClientAPISingleNodeFixture {
		private const bool UseLoggerBridge = true;

		public IEventStoreConnection CreateConnection(
			Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configureSettings = default,
			int? port = default, bool useDnsEndPoint = true) {
			var settings = (configureSettings ?? DefaultConfigureSettings)(DefaultBuilder).SetDefaultUserCredentials(new UserCredentials("admin", "changeit")).Build();
			return EventStoreConnection.Create(
				settings,
				useDnsEndPoint
					? new DnsEndPoint("localhost", port ?? 1113)
					: new IPEndPoint(IPAddress.Loopback, port ?? 1113));
		}

		public IEventStoreConnection CreateConnectionWithConnectionString(string configureSettings = default,
			int? port = default, bool useDnsEndPoint = false) {
			var settings = configureSettings ?? DefaultConfigureSettingsForConnectionString;
			var host = useDnsEndPoint ? "localhost" : IPAddress.Loopback.ToString();
			port ??= 1113;

			settings += "UseSslConnection=true;ValidateServer=false;";

			return EventStoreConnection.Create($"ConnectTo=tcp://admin:changeit@{host}:{port};{settings}");
		}
	}
}
