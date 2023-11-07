using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.Exceptions;
using Xunit;

namespace EventStore.ClientAPI {
	public class update_persistent_subscription : EventStoreClientAPITest {
		private const string Group = nameof(update_persistent_subscription);
		private readonly EventStoreClientAPIFixture _fixture;

		public update_persistent_subscription(EventStoreClientAPIFixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task without_credentials_throws() {
			var streamName = GetStreamName();
			var connection = _fixture.AnonymousConnection;

			await connection.CreatePersistentSubscriptionAsync(streamName, Group,
				PersistentSubscriptionSettings.Create(), DefaultUserCredentials.Admin).WithTimeout();

			await Assert.ThrowsAsync<AccessDeniedException>(() => connection.UpdatePersistentSubscriptionAsync(
				streamName, Group,
				PersistentSubscriptionSettings.Create(), null)).WithTimeout();
		}

		[Fact]
		public async Task with_credentials() {
			var streamName = GetStreamName();
			var connection = _fixture.Connection;

			await connection.CreatePersistentSubscriptionAsync(streamName, Group,
				PersistentSubscriptionSettings.Create(), DefaultUserCredentials.Admin).WithTimeout();

			await connection.UpdatePersistentSubscriptionAsync(streamName, Group,
				PersistentSubscriptionSettings.Create(), DefaultUserCredentials.Admin).WithTimeout();
		}

		[Fact]
		public async Task when_they_do_not_exist_throws() {
			var streamName = GetStreamName();
			var connection = _fixture.Connection;

			await Assert.ThrowsAsync<InvalidOperationException>(() => connection.UpdatePersistentSubscriptionAsync(
					streamName, Group, PersistentSubscriptionSettings.Create(), DefaultUserCredentials.Admin))
				.WithTimeout();
		}
	}
}
