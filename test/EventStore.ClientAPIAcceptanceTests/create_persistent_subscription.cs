using System.Threading.Tasks;
using EventStore.ClientAPI.Exceptions;
using Xunit;

namespace EventStore.ClientAPI {
	public class create_persistent_subscription : EventStoreClientAPITest {
		private const string Group = nameof(create_persistent_subscription);
		private readonly EventStoreClientAPIFixture _fixture;

		public create_persistent_subscription(EventStoreClientAPIFixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task without_credentials_throws() {
			var streamName = GetStreamName();
			var connection = _fixture.Connection;

			await Assert.ThrowsAsync<AccessDeniedException>(() => connection.CreatePersistentSubscriptionAsync(
				streamName, Group,
				PersistentSubscriptionSettings.Create(), null).WithTimeout());
		}

		[Fact]
		public async Task with_credentials() {
			var streamName = GetStreamName();
			var connection = _fixture.Connection;

			await connection.CreatePersistentSubscriptionAsync(streamName, Group,
				PersistentSubscriptionSettings.Create(), DefaultUserCredentials.Admin).WithTimeout();
		}
	}
}
