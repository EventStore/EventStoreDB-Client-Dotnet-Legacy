using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ductus.FluentDocker.Services;
using Xunit;

namespace EventStore.ClientAPI {
	// This will spin up a single node or a cluster
	// todo: perhaps the single/cluster fixtures can be unified behind an interface
	// (or set of interfaces)
	public class EventStoreClientAPIFixture : IAsyncLifetime {
		private readonly EventStoreClientAPIClusterFixture _cluster;
		private readonly EventStoreClientAPISingleNodeFixture _singleNode;

		public EventStoreClientAPIFixture() {
			if (UseCluster) {
				_cluster = new EventStoreClientAPIClusterFixture();
			} else {
				_singleNode = new EventStoreClientAPISingleNodeFixture();
			}
		}

		public IEventStoreConnection Connection =>
			UseCluster
				? _cluster.Connection
				: _singleNode.Connection;

		public IService EventStore =>
			UseCluster
				? _cluster.EventStore
				: _singleNode.EventStore;

		public static bool UseCluster => GlobalEnvironment.UseCluster;

		public Task InitializeAsync() =>
			UseCluster
				? _cluster.InitializeAsync()
				: _singleNode.InitializeAsync();

		public Task DisposeAsync() =>
			UseCluster
				? _cluster.DisposeAsync()
				: _singleNode.DisposeAsync();

		public IEnumerable<EventData> CreateTestEvents(int count = 1, int metadataSize = 1) =>
			TestEventGenerator.CreateTestEvents(count, metadataSize);

		public IEventStoreConnection CreateConnection(
			Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configureSettings,
			bool useStandardPort,
			int clusterMaxDiscoverAttempts = 1) =>

			UseCluster
				? _cluster.CreateConnectionWithGossipSeeds(
					configureSettings,
					useStandardPort ? 2113 : 2118,
					useDnsEndPoint: true,
					maxDiscoverAttempts: clusterMaxDiscoverAttempts)
				: _singleNode.CreateConnection(
					configureSettings,
					useStandardPort ? 1113 : 1114,
					useDnsEndPoint: true);

		public IEventStoreConnection CreateConnectionWithConnectionString(
			string configureSettings,
			bool useStandardPort,
			bool useDnsEndPoint) =>

			UseCluster
				? _cluster.CreateConnectionWithConnectionString(
					useSsl: true,
					configureSettings,
					useStandardPort ? 2113 : 2118,
					useDnsEndPoint)
				: _singleNode.CreateConnectionWithConnectionString(
					configureSettings,
					useStandardPort ? 1113 : 1114,
					useDnsEndPoint);
	}
}
