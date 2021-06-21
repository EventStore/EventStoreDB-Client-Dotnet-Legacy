using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.ClientAPI {
	public class read_stream_backward : EventStoreClientAPITest {
		private readonly EventStoreClientAPIFixture _fixture;

		public read_stream_backward(EventStoreClientAPIFixture fixture) {
			_fixture = fixture;
		}

		[Theory]
		[InlineData("single", 1, 1)]
		[InlineData("multiple", 3, 1)]
		[InlineData("large", 2, 6_000_000)]
		public async Task when_the_stream_exists(string suffix, int count, int metadataSize) {
			var streamName = $"{GetStreamName()}_{suffix}";
			var connection = _fixture.Connection;

			var testEvents = _fixture.CreateTestEvents(count, metadataSize).ToArray();

			await connection.AppendToStreamAsync(streamName, ExpectedVersion.NoStream, testEvents).WithTimeout();

			var result = await connection.ReadStreamEventsBackwardAsync(streamName, -1, testEvents.Length, false)
				.WithTimeout();

			Assert.Equal(SliceReadStatus.Success, result.Status);
			Assert.True(result.IsEndOfStream);
			Assert.Equal(ReadDirection.Backward, result.ReadDirection);
			Assert.Equal(testEvents.Reverse().Select(x => x.EventId),
				result.Events.Select(x => x.OriginalEvent.EventId));
		}

		[Fact]
		public async Task when_the_stream_does_not_exist() {
			var streamName = GetStreamName();
			var connection = _fixture.Connection;

			var result = await connection.ReadStreamEventsBackwardAsync(streamName, -1, 5, false).WithTimeout();

			Assert.Equal(SliceReadStatus.StreamNotFound, result.Status);
			Assert.True(result.IsEndOfStream);
			Assert.Equal(ReadDirection.Backward, result.ReadDirection);
		}

		[Fact]
		public async Task when_the_stream_is_deleted() {
			var streamName = GetStreamName();
			var connection = _fixture.Connection;

			await connection.AppendToStreamAsync(streamName, ExpectedVersion.NoStream, _fixture.CreateTestEvents(3));
			await connection.DeleteStreamAsync(streamName, ExpectedVersion.Any).WithTimeout();

			var result = await connection.ReadStreamEventsBackwardAsync(streamName, -1, 5, false).WithTimeout();

			Assert.Equal(SliceReadStatus.StreamNotFound, result.Status);
			Assert.True(result.IsEndOfStream);
			Assert.Equal(ReadDirection.Backward, result.ReadDirection);
		}
	}
}
