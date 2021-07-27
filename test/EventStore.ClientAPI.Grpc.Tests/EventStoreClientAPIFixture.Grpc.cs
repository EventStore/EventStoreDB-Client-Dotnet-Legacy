using System;
using System.Net;
using System.Net.Http;

namespace EventStore.ClientAPI {
	partial class EventStoreClientAPIFixture {
		private const bool UseLoggerBridge = true;

		public IEventStoreConnection CreateConnection(
			Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configureSettings = default,
			int? port = default, bool useDnsEndPoint = true) {
			var settings = (configureSettings ?? DefaultConfigureSettings)(DefaultBuilder)
				.UseCustomHttpMessageHandler(new HttpClientHandler {
					ServerCertificateCustomValidationCallback = delegate { return true; }
				})
				.Build();
			return EventStoreGrpcConnection.Create(
				settings,
				useDnsEndPoint
					? new DnsEndPoint("localhost", port ?? 2113)
					: new IPEndPoint(IPAddress.Loopback, port ?? 2113));
		}

		public static IEventStoreConnection CreateConnectionWithConnectionString(string configureSettings = default,
			int? port = default, bool useDnsEndPoint = false) {
			var settings = configureSettings ?? DefaultConfigureSettingsForConnectionString;
			var host = useDnsEndPoint ? "localhost" : IPAddress.Loopback.ToString();
			port ??= 2113;

			settings += "UseSslConnection=true;ValidateServer=false;";

			return EventStoreConnection.Create($"ConnectTo=tcp://{host}:{port};{settings}");
		}
	}
}
