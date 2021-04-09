using System.Threading.Tasks;
using EventStore.ClientAPI.Exceptions;
using Xunit;

namespace EventStore.ClientAPI {
	public class transaction : EventStoreClientAPITest {
		private readonly EventStoreClientAPIFixture _fixture;

		public transaction(EventStoreClientAPIFixture fixture) {
			_fixture = fixture;
		}

		[Theory, MemberData(nameof(ExpectedVersionTestCases))]
		public async Task expected_version(long expectedVersion, string displayName) {
			var streamName = $"{GetStreamName()}_{displayName}";
			var connection = _fixture.Connection;

			using var transaction = await connection.StartTransactionAsync(streamName, expectedVersion).WithTimeout();

			await transaction.WriteAsync(_fixture.CreateTestEvents()).WithTimeout();
			var result = await transaction.CommitAsync().WithTimeout();
			Assert.Equal(0, result.NextExpectedVersion);
		}

		[Fact]
		public async Task wrong_expected_version() {
			var streamName = GetStreamName();
			var connection = _fixture.Connection;

			using var transaction = await connection.StartTransactionAsync(streamName, 1).WithTimeout();
			await transaction.WriteAsync(_fixture.CreateTestEvents()).WithTimeout();
			var ex = await Assert.ThrowsAsync<WrongExpectedVersionException>(() =>
				transaction.CommitAsync().WithTimeout());
			Assert.False(ex.ExpectedVersion.HasValue);
			Assert.False(ex.ActualVersion.HasValue);
			//Assert.Equal(ExpectedVersion.StreamExists, ex.ExpectedVersion); TODO JPB seems like a bug?
			//Assert.Equal(ExpectedVersion.NoStream, ex.ActualVersion);
		}
	}
}
