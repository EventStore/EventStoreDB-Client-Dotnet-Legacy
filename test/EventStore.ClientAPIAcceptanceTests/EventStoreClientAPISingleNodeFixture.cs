using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Builders;
using Ductus.FluentDocker.Services;
using Polly;
using Xunit;

namespace EventStore.ClientAPI {
	public partial class EventStoreClientAPISingleNodeFixture : IAsyncLifetime {
		private static readonly string HostCertificatePath =
			Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..", "..", "certs"));

		private readonly IContainerService _eventStore;

		public IEventStoreConnection Connection { get; }
		public IContainerService EventStore => _eventStore;

		public EventStoreClientAPISingleNodeFixture() {
			ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

			// todo: consider using docker-compose for here as we do with the clustered version
			// this would save us specifying defaults in GlobalEnvironment.cs as well as the .env file
			// might be useful to make memdb configurable too
			_eventStore = new Builder()
				.UseContainer()
				.UseImage($"docker.pkg.github.com/eventstore/eventstore/eventstore:{GlobalEnvironment.ImageTag}")
				.WithEnvironment(
					"EVENTSTORE_DB_LOG_FORMAT=" + GlobalEnvironment.DbLogFormat,
					"EVENTSTORE_MEM_DB=true",
					"EVENTSTORE_ENABLE_EXTERNAL_TCP=true",
					"EVENTSTORE_CERTIFICATE_FILE=/etc/eventstore/certs/node/node.crt",
					"EVENTSTORE_CERTIFICATE_PRIVATE_KEY_FILE=/etc/eventstore/certs/node/node.key",
					"EVENTSTORE_TRUSTED_ROOT_CERTIFICATES_PATH=/etc/eventstore/certs/ca")
				.ExposePort(1113, 1113)
				.ExposePort(2113, 2113)
				.MountVolume(HostCertificatePath, "/etc/eventstore/certs", MountType.ReadOnly)
				.Build();
			Connection = CreateConnection(settings => settings.DisableServerCertificateValidation());
		}

		public async Task InitializeAsync() {
			_eventStore.Start();
			try {
				using var httpClient = new HttpClient(new HttpClientHandler {
					ServerCertificateCustomValidationCallback = delegate {
						return true;
					}
				}) {
					BaseAddress = new UriBuilder {
						Port = 2113,
						Scheme = Uri.UriSchemeHttps
					}.Uri
				};
				await Policy.Handle<Exception>()
					.WaitAndRetryAsync(5, retryCount => TimeSpan.FromSeconds(retryCount * retryCount))
					.ExecuteAsync(async () => {
						using var response = await httpClient.GetAsync("/health/live");
						if (response.StatusCode >= HttpStatusCode.BadRequest) {
							throw new Exception($"Health check failed with status code: {response.StatusCode}.");
						}
					});
			} catch (Exception) {
				_eventStore.Dispose();
				throw;
			}
			await Connection.ConnectAsync();
		}

		public Task DisposeAsync() {
			Connection.Dispose();
			_eventStore.Dispose();
			return Task.CompletedTask;
		}
	}
}
