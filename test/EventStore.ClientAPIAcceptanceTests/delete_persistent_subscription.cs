using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.Exceptions;
using Xunit;

namespace EventStore.ClientAPI {
	public class delete_persistent_subscription : EventStoreClientAPITest {
		private const string Group = nameof(delete_persistent_subscription);
		private readonly EventStoreClientAPIFixture _fixture;

		public delete_persistent_subscription(EventStoreClientAPIFixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task without_credentials_fails() {
			var streamName = GetStreamName();
			var connection = _fixture.AnonymousConnection;

			await connection.CreatePersistentSubscriptionAsync(streamName, Group,
				PersistentSubscriptionSettings.Create(), DefaultUserCredentials.Admin).WithTimeout();

			await Assert.ThrowsAsync<AccessDeniedException>(
				() => connection.DeletePersistentSubscriptionAsync(streamName, Group).WithTimeout());
		}

		[Fact]
		public async Task that_does_not_exist_fails() {
			var streamName = GetStreamName();
			var connection = _fixture.Connection;

			await Assert.ThrowsAsync<InvalidOperationException>(
				() => connection.DeletePersistentSubscriptionAsync(streamName, Group, DefaultUserCredentials.Admin)
					.WithTimeout());
		}

		[Fact]
		public async Task with_credentials_succeeds() {
			var streamName = GetStreamName();
			var connection = _fixture.Connection;

			await connection.CreatePersistentSubscriptionAsync(streamName, Group,
				PersistentSubscriptionSettings.Create(), DefaultUserCredentials.Admin).WithTimeout();

			await connection.DeletePersistentSubscriptionAsync(streamName, Group, DefaultUserCredentials.Admin)
				.WithTimeout();
		}
	}
}
