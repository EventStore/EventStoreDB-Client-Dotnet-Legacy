using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI {
	partial class EventStoreClientAPIClusterFixture {
		private EndPoint[] GetGossipSeedEndPointsExceptFor(int nodeIndex, int port, bool dnsEndpoint) {
			List<EndPoint> endPoints = new List<EndPoint>();
			for (var i = 0; i < 3; i++) {
				if (i != nodeIndex) {
					if (dnsEndpoint) {
						endPoints.Add(new DnsEndPoint("localhost", port - i));
					} else {
						endPoints.Add(new IPEndPoint(IPAddress.Loopback, port - i));
					}
				}
			}
			return endPoints.ToArray();
		}
		private const bool UseLoggerBridge = true;

		public IEventStoreConnection CreateConnectionWithGossipSeeds(
			Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configureSettings = default,
			int port = 2113,
			bool useDnsEndPoint = false,
			int maxDiscoverAttempts = 1) {

			var settings = (configureSettings ?? DefaultConfigureSettings)(DefaultBuilder).SetDefaultUserCredentials(new UserCredentials("admin", "changeit"))
			.Build();
			var gossipSeeds = GetGossipSeedEndPointsExceptFor(-1, port, useDnsEndPoint);
			var clusterSettings = new ClusterSettingsBuilder()
				.DiscoverClusterViaGossipSeeds()
				.SetGossipSeedEndPoints(true, gossipSeeds);

			if (maxDiscoverAttempts == -1) {
				clusterSettings = clusterSettings.KeepDiscovering();
			} else {
				clusterSettings = clusterSettings.SetMaxDiscoverAttempts(maxDiscoverAttempts);
			}

			return EventStoreConnection.Create(settings, clusterSettings.Build());
		}

		public IEventStoreConnection CreateConnectionWithConnectionString(
			bool useSsl,
			string configureSettings = default,
			int port = 2113,
			bool useDnsEndPoint = false) {

			var settings = configureSettings ?? DefaultConfigureSettingsForConnectionString;
			var host = useDnsEndPoint ? "localhost" : IPAddress.Loopback.ToString();

			if (useSsl) settings += "UseSslConnection=true;ValidateServer=false;";
			else settings += "UseSslConnection=false;";

			var gossipSeeds = GetGossipSeedEndPointsExceptFor(-1, port, useDnsEndPoint);
			var gossipSeedsString = "";
			for(var i=0;i<gossipSeeds.Length;i++) {
				if (i > 0) gossipSeedsString += ",";
				gossipSeedsString += host + ":" + gossipSeeds[i].GetPort();
			}

			settings += "CustomHttpMessageHandler=SkipCertificateValidation;";

			var connectionString = $"GossipSeeds={gossipSeedsString};{settings}";
			var builder = ConnectionSettings.Create()
				.SetDefaultUserCredentials(new UserCredentials("admin", "changeit"));
			return EventStoreConnection.Create(connectionString, builder);
		}
	}
}
