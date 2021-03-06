using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.ClientAPI {
	public class connect_to_cluster : EventStoreClientAPIClusterTest {
		private readonly EventStoreClientAPIClusterFixture _fixture;

		public connect_to_cluster(EventStoreClientAPIClusterFixture fixture) {
			_fixture = fixture;
		}

		private string GetStreamName([CallerMemberName] string testMethod = default)
			=> $"{GetType().Name}_{testMethod ?? "unknown"}";

		/*
		 * NOTE: We currently only support TLS tests here since the gossip protocol now only contains either the TLS TCP endpoint or the insecure TCP endpoint but not both
		 * and XUnit does not support parametrized test fixtures so that we can start up the server with only insecure TCP
		 */

		[Fact]
		public async Task can_connect_to_tls_ip_endpoint_gossip_seed() {
			var streamName = GetStreamName();
			using var connection = _fixture.CreateConnectionWithGossipSeeds(
				builder => builder.UseSsl(true));
			await connection.ConnectAsync().WithTimeout();
			var writeResult =
				await connection.AppendToStreamAsync(streamName, ExpectedVersion.Any, TestEventGenerator.CreateTestEvents());
			Assert.True(writeResult.LogPosition.PreparePosition > 0);
		}

		[Fact]
		public async Task can_connect_to_tls_dns_endpoint_gossip_seed() {
			var streamName = GetStreamName();
			using var connection = _fixture.CreateConnectionWithGossipSeeds(
				builder => builder.UseSsl(true),
				useDnsEndPoint: true);
			await connection.ConnectAsync().WithTimeout();
			var writeResult =
				await connection.AppendToStreamAsync(streamName, ExpectedVersion.Any, TestEventGenerator.CreateTestEvents());
			Assert.True(writeResult.LogPosition.PreparePosition > 0);
		}


		[Fact]
		public async Task can_connect_to_tls_ip_endpoint_gossip_seed_with_connection_string() {
			var streamName = GetStreamName();
			using var connection = _fixture.CreateConnectionWithConnectionString(useSsl: true);
			await connection.ConnectAsync().WithTimeout();
			var writeResult =
				await connection.AppendToStreamAsync(streamName, ExpectedVersion.Any, TestEventGenerator.CreateTestEvents());
			Assert.True(writeResult.LogPosition.PreparePosition > 0);
		}

		[Fact]
		public async Task can_connect_to_tls_dns_endpoint_gossip_seed_with_connection_string() {
			var streamName = GetStreamName();
			using var connection = _fixture.CreateConnectionWithConnectionString(useSsl: true, useDnsEndPoint: true);
			await connection.ConnectAsync().WithTimeout();
			var writeResult =
				await connection.AppendToStreamAsync(streamName, ExpectedVersion.Any, TestEventGenerator.CreateTestEvents());
			Assert.True(writeResult.LogPosition.PreparePosition > 0);
		}
	}
}
