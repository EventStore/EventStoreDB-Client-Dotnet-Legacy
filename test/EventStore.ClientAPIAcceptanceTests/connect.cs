using System;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.ClientAPI {
	public class connect : EventStoreClientAPITest {
		private readonly EventStoreClientAPIFixture _fixture;

		public connect(EventStoreClientAPIFixture fixture) {
			_fixture = fixture;
		}


		[Fact]
		public async Task does_not_throw_when_server_is_down() {
			using var connection = _fixture.CreateConnection(port: 1114);
			await connection.ConnectAsync().WithTimeout();
		}

		[Fact]
		public async Task reopening_a_closed_connection_throws() {
			var closedSource = new TaskCompletionSource<bool>();
			using var connection = _fixture.CreateConnection(builder => builder
					.UseSsl(true)
					.DisableServerCertificateValidation()
					.LimitReconnectionsTo(0)
					.WithConnectionTimeoutOf(TimeSpan.FromSeconds(10))
					.SetReconnectionDelayTo(TimeSpan.Zero)
					.FailOnNoServerResponse(),
				1114);
			connection.Closed += delegate { closedSource.TrySetResult(true); };
			await connection.ConnectAsync().WithTimeout();

			connection.Close();

			await closedSource.Task.WithTimeout(TimeSpan.FromSeconds(120));

			await Assert.ThrowsAsync<ObjectDisposedException>(() => connection.ConnectAsync().WithTimeout());
		}

		[Fact]
		public async Task closes_after_configured_amount_of_failed_reconnections() {
			var closedSource = new TaskCompletionSource<bool>();
			using var connection = _fixture.CreateConnection(
				builder => builder.UseSsl(true)
					.DisableServerCertificateValidation()
					.LimitReconnectionsTo(1)
					.WithConnectionTimeoutOf(TimeSpan.FromSeconds(10))
					.SetReconnectionDelayTo(TimeSpan.Zero)
					.FailOnNoServerResponse(),
				1114);
			connection.Closed += delegate { closedSource.TrySetResult(true); };
			connection.Connected += (s, e) => Console.WriteLine(
				"EventStoreConnection '{0}': connected to [{1}]...", e.Connection.ConnectionName, e.RemoteEndPoint);
			connection.Reconnecting += (s, e) =>
				Console.WriteLine("EventStoreConnection '{0}': reconnecting...", e.Connection.ConnectionName);
			connection.Disconnected += (s, e) =>
				Console.WriteLine("EventStoreConnection '{0}': disconnected from [{1}]...",
					e.Connection.ConnectionName, e.RemoteEndPoint);
			connection.ErrorOccurred += (s, e) => Console.WriteLine("EventStoreConnection '{0}': error = {1}",
				e.Connection.ConnectionName, e.Exception);

			await connection.ConnectAsync().WithTimeout();

			await closedSource.Task.WithTimeout(TimeSpan.FromSeconds(120));

			await Assert.ThrowsAsync<ObjectDisposedException>(() => connection.AppendToStreamAsync(
				nameof(closes_after_configured_amount_of_failed_reconnections),
				ExpectedVersion.NoStream,
				_fixture.CreateTestEvents()).WithTimeout());
		}

		[Fact]
		public async Task can_connect_to_dns_endpoint() {
			var streamName = GetStreamName();
			using var connection = _fixture.CreateConnection(
				builder => builder.UseSsl(true)
					.DisableServerCertificateValidation()
					.LimitReconnectionsTo(1)
					.WithConnectionTimeoutOf(TimeSpan.FromSeconds(10))
					.SetReconnectionDelayTo(TimeSpan.Zero)
					.FailOnNoServerResponse(), 1113, true);
			await connection.ConnectAsync().WithTimeout();
			var writeResult =
				await connection.AppendToStreamAsync(streamName, ExpectedVersion.Any, _fixture.CreateTestEvents());
			Assert.True(writeResult.LogPosition.PreparePosition > 0);
		}


		[Fact]
		public async Task can_connect_to_ip_endpoint_with_connection_string() {
			var streamName = GetStreamName();
			using var connection = EventStoreClientAPIFixture.CreateConnectionWithConnectionString();
			await connection.ConnectAsync().WithTimeout();
			var writeResult =
				await connection.AppendToStreamAsync(streamName, ExpectedVersion.Any, _fixture.CreateTestEvents());
			Assert.True(writeResult.LogPosition.PreparePosition > 0);
		}

		[Fact]
		public async Task can_connect_to_dns_endpoint_with_connection_string() {
			var streamName = GetStreamName();
			using var connection = EventStoreClientAPIFixture.CreateConnectionWithConnectionString(null, null, true);
			await connection.ConnectAsync().WithTimeout();
			var writeResult =
				await connection.AppendToStreamAsync(streamName, ExpectedVersion.Any, _fixture.CreateTestEvents());
			Assert.True(writeResult.LogPosition.PreparePosition > 0);
		}
	}
}
