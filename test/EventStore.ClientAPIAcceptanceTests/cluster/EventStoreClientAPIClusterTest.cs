using Xunit;

namespace EventStore.ClientAPI {
	[Collection(nameof(EventStoreClientAPICollection))]
	public abstract class EventStoreClientAPIClusterTest : IClassFixture<EventStoreClientAPIClusterFixture> {
	}
}
