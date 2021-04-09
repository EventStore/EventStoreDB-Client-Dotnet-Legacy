using System.Threading.Tasks;
using Xunit;

namespace EventStore.ClientAPI {
	public class conditional_append_to_stream : EventStoreClientAPITest {
		private readonly EventStoreClientAPIFixture _fixture;

		public conditional_append_to_stream(EventStoreClientAPIFixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task returns_version_mismatch_with_wrong_expected_version() {
			var stream = GetStreamName();
			var connection = _fixture.Connection;

			var result = await connection.ConditionalAppendToStreamAsync(stream, 7, _fixture.CreateTestEvents())
				.WithTimeout();
			Assert.Equal(ConditionalWriteStatus.VersionMismatch, result.Status);
		}

		[Theory, MemberData(nameof(ExpectedVersionTestCases))]
		public async Task returns_succeeded_with_correct_expected_version(long expectedVersion, string displayName) {
			var stream = $"{GetStreamName()}_{displayName}";
			var connection = _fixture.Connection;

			var result = await connection
				.ConditionalAppendToStreamAsync(stream, expectedVersion, _fixture.CreateTestEvents()).WithTimeout();
			Assert.Equal(ConditionalWriteStatus.Succeeded, result.Status);
		}

		[Fact]
		public async Task returns_succeeded_when_stream_deleted() {
			var stream = GetStreamName();
			var connection = _fixture.Connection;

			await connection.AppendToStreamAsync(stream, ExpectedVersion.Any, _fixture.CreateTestEvents())
				.WithTimeout();

			await connection.DeleteStreamAsync(stream, ExpectedVersion.Any, true).WithTimeout();

			var result = await connection
				.ConditionalAppendToStreamAsync(stream, ExpectedVersion.Any, _fixture.CreateTestEvents()).WithTimeout();
			Assert.Equal(ConditionalWriteStatus.StreamDeleted, result.Status);
		}
	}
}
