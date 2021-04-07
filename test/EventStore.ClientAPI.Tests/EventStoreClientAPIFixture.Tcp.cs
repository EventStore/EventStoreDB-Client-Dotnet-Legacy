using System;
using System.Net;

namespace EventStore.ClientAPI {
	partial class EventStoreClientAPIFixture {
		private const bool UseLoggerBridge = true;

		public IEventStoreConnection CreateConnection(
			Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configureSettings = default,
			int? port = default, bool useDnsEndPoint = true) {
			var settings = (configureSettings ?? DefaultConfigureSettings)(DefaultBuilder).Build();
			return EventStoreConnection.Create(
				settings,
				useDnsEndPoint
					? new DnsEndPoint("localhost", port ?? 1113)
					: new IPEndPoint(IPAddress.Loopback, port ?? 1113));
		}

		public static IEventStoreConnection CreateConnectionWithConnectionString(bool useSsl,
			string configureSettings = default, int? port = default, bool useDnsEndPoint = false) {
			var settings = configureSettings ?? DefaultConfigureSettingsForConnectionString;
			var host = useDnsEndPoint ? "localhost" : IPAddress.Loopback.ToString();
			port ??= 1113;

			settings += useSsl ? "UseSslConnection=true;ValidateServer=false;" : "UseSslConnection=false;";

			return EventStoreConnection.Create($"ConnectTo=tcp://{host}:{port};{settings}");
		}
	}
}
