using System;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.ClientAPI {
	public class get_stream_metadata_as_raw_bytes : EventStoreClientAPITest {
		private readonly EventStoreClientAPIFixture _fixture;

		public get_stream_metadata_as_raw_bytes(EventStoreClientAPIFixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task for_non_existing_stream_returns_default() {
			var streamName = GetStreamName();
			var connection = _fixture.Connection;
			var meta = await connection.GetStreamMetadataAsRawBytesAsync(streamName).WithTimeout();
			Assert.Equal(streamName, meta.Stream);
			Assert.False(meta.IsStreamDeleted);
			Assert.Equal(-1, meta.MetastreamVersion);
			Assert.Equal(Array.Empty<byte>(), meta.StreamMetadata);
		}
	}
}
