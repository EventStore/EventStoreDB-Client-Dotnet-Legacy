using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.Exceptions;
using Xunit;

namespace EventStore.ClientAPI {
	public class connect_to_persistent_subscription
		: EventStoreClientAPITest {
		private const string Group = nameof(connect_to_persistent_subscription);
		private readonly EventStoreClientAPIFixture _fixture;

		public connect_to_persistent_subscription(EventStoreClientAPIFixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task that_does_not_exist_throws() {
			var streamName = GetStreamName();
			var connection = _fixture.AnonymousConnection;

			var ex = await Record.ExceptionAsync(() => connection.ConnectToPersistentSubscriptionAsync(
				streamName, Group,
				delegate { return Task.CompletedTask; })).WithTimeout();
			if (ex is AggregateException agg) {
				agg = agg.Flatten();
				ex = Assert.Single(agg.InnerExceptions);
			}

			Assert.IsType<AccessDeniedException>(ex);

		}
	}
}
