using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace EventStore.ClientAPI {
	[Collection(nameof(EventStoreClientAPICollection))]
	public abstract class EventStoreClientAPITest : IClassFixture<EventStoreClientAPIFixture> {
		protected string GetStreamName([CallerMemberName] string testMethod = default)
			=> $"{GetType().Name}_{testMethod ?? "unknown"}";

		protected static IEnumerable<bool> UseSsl => new[] {true};

		protected static IEnumerable<(long expectedVersion, string displayName)> ExpectedVersions
			=> new[] {
				((long)ExpectedVersion.Any, nameof(ExpectedVersion.Any)),
				(ExpectedVersion.NoStream, nameof(ExpectedVersion.NoStream))
			};

		public static IEnumerable<object[]> ExpectedVersionTestCases() {
			foreach (var (expectedVersion, displayName) in ExpectedVersions) {
				yield return new object[] {expectedVersion, displayName};
			}
		}
	}
}
