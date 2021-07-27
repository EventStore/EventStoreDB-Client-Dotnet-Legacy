using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using EventStore.ClientAPI.Internal;

namespace EventStore.ClientAPI {
	partial class EventStoreClientAPIClusterFixture {
		private EndPoint[] GetGossipSeedEndPointsExceptFor(int nodeIndex, bool dnsEndpoint) {
			List<EndPoint> endPoints = new List<EndPoint>();
			for (var i = 0; i < 3; i++) {
				if (i != nodeIndex) {
					if (dnsEndpoint) {
						endPoints.Add(new DnsEndPoint("localhost", 2113 - i));
					} else {
						endPoints.Add(new IPEndPoint(IPAddress.Loopback, 2113 - i));
					}
				}
			}
			return endPoints.ToArray();
		}
		private const bool UseLoggerBridge = true;

		public IEventStoreConnection CreateConnectionWithGossipSeeds(
			Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configureSettings = default,
			bool useDnsEndPoint = false) {
			var settings = (configureSettings ?? DefaultConfigureSettings)(DefaultBuilder)
			.Build();
			var gossipSeeds = GetGossipSeedEndPointsExceptFor(-1, useDnsEndPoint);
			var clusterSettings = new ClusterSettingsBuilder()
				.DiscoverClusterViaGossipSeeds()
				.SetGossipSeedEndPoints(true, gossipSeeds)
				.SetMaxDiscoverAttempts(1)
				.Build();
			return EventStoreConnection.Create(settings, clusterSettings);
		}

		public IEventStoreConnection CreateConnectionWithConnectionString(
			bool useSsl,
			string configureSettings = default,
			bool useDnsEndPoint = false) {
			var settings = configureSettings ?? DefaultConfigureSettingsForConnectionString;
			var host = useDnsEndPoint ? "localhost" : IPAddress.Loopback.ToString();

			if (useSsl) settings += "UseSslConnection=true;ValidateServer=false;";
			else settings += "UseSslConnection=false;";

			var gossipSeeds = GetGossipSeedEndPointsExceptFor(-1, useDnsEndPoint);
			var gossipSeedsString = "";
			for(var i=0;i<gossipSeeds.Length;i++) {
				if (i > 0) gossipSeedsString += ",";
				gossipSeedsString += host + ":" + 2113;
			}

			settings += "CustomHttpMessageHandler=SkipCertificateValidation;";

			var connectionString = $"GossipSeeds={gossipSeedsString};{settings}";
			return EventStoreConnection.Create(connectionString);
		}
	}
}
