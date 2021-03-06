using System.Threading.Tasks;
using EventStore.ClientAPI.Exceptions;
using Xunit;

namespace EventStore.ClientAPI {
	public class append_to_stream : EventStoreClientAPITest {
		private readonly EventStoreClientAPIFixture _fixture;

		public append_to_stream(EventStoreClientAPIFixture fixture) {
			_fixture = fixture;
		}

		[Theory, MemberData(nameof(ExpectedVersionTestCases))]
		public async Task expected_version(long expectedVersion, string displayName) {
			var streamName = $"{GetStreamName()}_{displayName}";
			var connection = _fixture.Connection;

			var result = await connection.AppendToStreamAsync(streamName, expectedVersion, _fixture.CreateTestEvents())
				.WithTimeout();
			Assert.Equal(0, result.NextExpectedVersion);
			await connection.AppendToStreamAsync(streamName, result.NextExpectedVersion, _fixture.CreateTestEvents())
				.WithTimeout();
		}

		[Fact]
		public async Task wrong_expected_version() {
			var streamName = GetStreamName();
			var connection = _fixture.Connection;

			var ex = await Assert.ThrowsAsync<WrongExpectedVersionException>(() =>
					connection.AppendToStreamAsync(streamName, ExpectedVersion.StreamExists,
						_fixture.CreateTestEvents()))
				.WithTimeout();

			Assert.Equal(ExpectedVersion.StreamExists, ex.ExpectedVersion);
			Assert.Equal(ExpectedVersion.NoStream, ex.ActualVersion);
		}
	}
}
