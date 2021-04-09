using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.ClientAPI.Exceptions;
using Xunit;

namespace EventStore.ClientAPI {
	public class delete_stream : EventStoreClientAPITest {
		private readonly EventStoreClientAPIFixture _fixture;

		public delete_stream(EventStoreClientAPIFixture fixture) {
			_fixture = fixture;
		}

		private static IEnumerable<bool> HardDelete => new[] {true, false};

		public static IEnumerable<object[]> HardDeleteCases() {
			foreach (var hardDelete in HardDelete) {
				yield return new object[] {hardDelete};
			}
		}

		[Theory, MemberData(nameof(ExpectedVersionTestCases))]
		public async Task that_does_not_exist_with_expected_version_succeeds(long expectedVersion, string displayName) {
			var streamName = $"{GetStreamName()}_{displayName}";
			var connection = _fixture.Connection;
			await connection.DeleteStreamAsync(streamName, expectedVersion).WithTimeout();
		}

		[Theory, MemberData(nameof(HardDeleteCases))]
		public async Task that_does_not_exist_with_wrong_expected_version_fails(bool hardDelete) {
			var streamName = $"{GetStreamName()}_{hardDelete}";
			var connection = _fixture.Connection;
			var ex = await Assert.ThrowsAsync<WrongExpectedVersionException>(
				() => connection.DeleteStreamAsync(streamName, 7, hardDelete).WithTimeout());

			//Assert.Equal(7, ex.ExpectedVersion); TODO JPB looks like a bug
			//Assert.Equal(ExpectedVersion.NoStream, ex.ActualVersion);
		}

		[Theory, MemberData(nameof(HardDeleteCases))]
		public async Task that_does_exist_succeeds(bool hardDelete) {
			var streamName = $"{GetStreamName()}_{hardDelete}";
			var connection = _fixture.Connection;
			var result = await connection
				.AppendToStreamAsync(streamName, ExpectedVersion.NoStream, _fixture.CreateTestEvents()).WithTimeout();

			await connection.DeleteStreamAsync(streamName, result.NextExpectedVersion, hardDelete).WithTimeout();
		}
	}
}
