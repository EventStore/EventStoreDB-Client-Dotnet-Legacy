using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.Common;
using EventStore.ClientAPI.Exceptions;
using Xunit;

namespace EventStore.ClientAPI {
	public class set_system_settings : EventStoreClientAPITest, IAsyncLifetime {
		private readonly EventStoreClientAPIFixture _fixture;

		public set_system_settings(EventStoreClientAPIFixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task writes_to_the_correct_stream() {
			var connection = _fixture.Connection;
			var expected = new SystemSettings(
				new StreamAcl(
					Guid.NewGuid().ToString(),
					Guid.NewGuid().ToString(),
					Guid.NewGuid().ToString(),
					Guid.NewGuid().ToString(),
					Guid.NewGuid().ToString()),
				new StreamAcl(
					Guid.NewGuid().ToString(),
					Guid.NewGuid().ToString(),
					Guid.NewGuid().ToString(),
					Guid.NewGuid().ToString(),
					Guid.NewGuid().ToString()));

			await connection.SetSystemSettingsAsync(expected, DefaultUserCredentials.Admin).WithTimeout();

			var result = await connection.ReadStreamEventsBackwardAsync(SystemStreams.SettingsStream, -1, 1,
				false, DefaultUserCredentials.Admin).WithTimeout();

			Assert.Equal(SliceReadStatus.Success, result.Status);

			Assert.Equal(expected.ToJsonBytes(), result.Events[0].OriginalEvent.Data);
		}

		[Fact]
		public async Task without_permission_throws() {
			var connection = _fixture.AnonymousConnection;
			await Assert.ThrowsAsync<AccessDeniedException>(() => connection.SetSystemSettingsAsync(new SystemSettings(
				new StreamAcl(
					Guid.NewGuid().ToString(),
					Guid.NewGuid().ToString(),
					Guid.NewGuid().ToString(),
					Guid.NewGuid().ToString(),
					Guid.NewGuid().ToString()),
				new StreamAcl(
					Guid.NewGuid().ToString(),
					Guid.NewGuid().ToString(),
					Guid.NewGuid().ToString(),
					Guid.NewGuid().ToString(),
					Guid.NewGuid().ToString()))).WithTimeout());
		}

		public Task InitializeAsync() => Task.CompletedTask;

		public async Task DisposeAsync() {
			var connection = _fixture.Connection;;

			await connection.SetSystemSettingsAsync(new SystemSettings(null, null), DefaultUserCredentials.Admin)
				.WithTimeout();
		}
	}
}
