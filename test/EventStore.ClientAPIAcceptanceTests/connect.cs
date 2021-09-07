using System;
using System.Threading.Tasks;
using Polly;
using Xunit;

namespace EventStore.ClientAPI {
	public class connect : EventStoreClientAPITest {
		private readonly EventStoreClientAPIFixture _fixture;

		public connect(EventStoreClientAPIFixture fixture) {
			_fixture = fixture;
		}


		[Fact]
		public async Task does_not_throw_when_server_is_down() {
			if (GlobalEnvironment.UseCluster) {
				// suspicious that behaviour is different for cluster, but likely
				// always been this way. the discovery is part of the connectasync
				// call and throws if the cluster is not accessible
				return;
			}

			using var connection = _fixture.CreateConnection(
				configureSettings: null,
				useStandardPort: false);
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
				useStandardPort: true);

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
				useStandardPort: true);
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

			_fixture.EventStore.Stop();

			await closedSource.Task.WithTimeout(TimeSpan.FromSeconds(120));

			await Assert.ThrowsAsync<ObjectDisposedException>(() => connection.AppendToStreamAsync(
				nameof(closes_after_configured_amount_of_failed_reconnections),
				ExpectedVersion.NoStream,
				_fixture.CreateTestEvents()).WithTimeout());

			_fixture.EventStore.Start();

			// wait for the cluster to come back
			await Policy.Handle<Exception>()
				.WaitAndRetryAsync(5, retryCount => TimeSpan.FromSeconds(retryCount * 2))
				.ExecuteAsync(async () => {
					using var newConnection =
						_fixture.CreateConnection(
							builder => builder
								.UseSsl(true)
								.DisableServerCertificateValidation()
								.WithConnectionTimeoutOf(TimeSpan.FromSeconds(10))
								.SetReconnectionDelayTo(TimeSpan.Zero)
								.FailOnNoServerResponse(),
							useStandardPort: true,
							clusterMaxDiscoverAttempts: -1);
					await newConnection.ConnectAsync().WithTimeout();
					await newConnection.AppendToStreamAsync(GetStreamName(), ExpectedVersion.Any, _fixture.CreateTestEvents()).WithTimeout();
				});
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
					.FailOnNoServerResponse(),
				useStandardPort: true);
			await connection.ConnectAsync().WithTimeout();
			var writeResult =
				await connection.AppendToStreamAsync(streamName, ExpectedVersion.Any, _fixture.CreateTestEvents());
			Assert.True(writeResult.LogPosition.PreparePosition > 0);
		}


		[Fact]
		public async Task can_connect_to_ip_endpoint_with_connection_string() {
			var streamName = GetStreamName();
			using var connection = _fixture.CreateConnectionWithConnectionString(
				configureSettings: null,
				useStandardPort: true,
				useDnsEndPoint: false);
			await connection.ConnectAsync().WithTimeout();
			var writeResult =
				await connection.AppendToStreamAsync(streamName, ExpectedVersion.Any, _fixture.CreateTestEvents());
			Assert.True(writeResult.LogPosition.PreparePosition > 0);
		}

#if NETCOREAPP3_1
		[Fact(Skip = "Skip on netcoreapp3.1 due to test instability")]
#else
		[Fact]
#endif

		public async Task can_reconnect_and_retry() {
			var streamName = GetStreamName();
			var disconnectedSource = new TaskCompletionSource<bool>();
			using var connection = _fixture.CreateConnection(
				builder => builder.UseSsl(true)
					.DisableServerCertificateValidation()
					.WithConnectionTimeoutOf(TimeSpan.FromSeconds(10))
					.SetReconnectionDelayTo(TimeSpan.FromMilliseconds(20))
					.KeepReconnecting()
					.KeepRetrying()
					.FailOnNoServerResponse(),
				useStandardPort: true,
				clusterMaxDiscoverAttempts: -1);

			connection.Disconnected += (_, _) => disconnectedSource.TrySetResult(true);

			await connection.ConnectAsync().WithTimeout();

			// can definitely write without throwing
			await WriteAnEventAsync().WithTimeout();

			_fixture.EventStore.Stop();

			await disconnectedSource.Task.WithTimeout(TimeSpan.FromMinutes(1));

			var writeTask = WriteAnEventAsync();

			// writeTask continuation cannot complete and will timeout because ES is stopped
			await Assert.ThrowsAsync<TimeoutException>(() => writeTask.WithTimeout());

			_fixture.EventStore.Start();

			// same writeTask can complete now by reconnecting and retrying
			var writeResult = await writeTask.WithTimeout(TimeSpan.FromMilliseconds(100 * 1000));

			Assert.True(writeResult.LogPosition.PreparePosition > 0);

			Task<WriteResult> WriteAnEventAsync() => connection
				.AppendToStreamAsync(streamName, ExpectedVersion.Any, _fixture.CreateTestEvents());
		}

		[Fact]
		public async Task can_connect_to_dns_endpoint_with_connection_string() {
			var streamName = GetStreamName();
			using var connection = _fixture.CreateConnectionWithConnectionString(
				configureSettings: null,
				useStandardPort: true,
				useDnsEndPoint: true);
			await connection.ConnectAsync().WithTimeout();
			var writeResult =
				await connection.AppendToStreamAsync(streamName, ExpectedVersion.Any, _fixture.CreateTestEvents());
			Assert.True(writeResult.LogPosition.PreparePosition > 0);
		}
	}
}
