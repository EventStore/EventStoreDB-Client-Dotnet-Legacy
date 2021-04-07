using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using Polly;
using Xunit;

namespace EventStore.ClientAPI {
	public partial class EventStoreClientAPIClusterFixture : IAsyncLifetime {
		private readonly ICompositeService _eventStoreCluster;

		public EventStoreClientAPIClusterFixture() {
			_eventStoreCluster = new Builder()
				.UseContainer()
				.UseCompose()
				.FromFile("docker-compose.yml")
				.ForceRecreate()
				.RemoveOrphans()
				.Build();
		}

		public async Task InitializeAsync() {
			_eventStoreCluster.Start();
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
				_eventStoreCluster.Dispose();
				throw;
			}
		}

		public Task DisposeAsync() {
			_eventStoreCluster.Stop();
			_eventStoreCluster.Dispose();
			return Task.CompletedTask;
		}
	}
}
