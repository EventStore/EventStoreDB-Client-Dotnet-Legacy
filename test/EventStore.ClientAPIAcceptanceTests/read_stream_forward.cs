using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.ClientAPI {
	public class read_stream_forward : EventStoreClientAPITest {
		private readonly EventStoreClientAPIFixture _fixture;

		public read_stream_forward(EventStoreClientAPIFixture fixture) {
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

			var result = await connection.ReadStreamEventsForwardAsync(streamName, 0, testEvents.Length, false)
				.WithTimeout();

			Assert.Equal(SliceReadStatus.Success, result.Status);
			Assert.True(result.IsEndOfStream);
			Assert.Equal(ReadDirection.Forward, result.ReadDirection);
			Assert.Equal(testEvents.Select(x => x.EventId), result.Events.Select(x => x.OriginalEvent.EventId));
		}

		[Fact]
		public async Task when_the_stream_does_not_exist() {
			var streamName = GetStreamName();
			var connection = _fixture.Connection;

			var result = await connection.ReadStreamEventsForwardAsync(streamName, 0, 5, false)
				.WithTimeout();

			Assert.Equal(SliceReadStatus.StreamNotFound, result.Status);
			Assert.True(result.IsEndOfStream);
			Assert.Equal(ReadDirection.Forward, result.ReadDirection);
		}

		[Fact]
		public async Task when_the_stream_is_deleted() {
			var streamName = GetStreamName();
			var connection = _fixture.Connection;

			await connection.AppendToStreamAsync(streamName, ExpectedVersion.NoStream, _fixture.CreateTestEvents(3))
				.WithTimeout();
			await connection.DeleteStreamAsync(streamName, ExpectedVersion.Any).WithTimeout();

			var result = await connection.ReadStreamEventsForwardAsync(streamName, 0, 5, false)
				.WithTimeout();

			Assert.Equal(SliceReadStatus.StreamNotFound, result.Status);
			Assert.True(result.IsEndOfStream);
			Assert.Equal(ReadDirection.Forward, result.ReadDirection);
		}
	}
}
