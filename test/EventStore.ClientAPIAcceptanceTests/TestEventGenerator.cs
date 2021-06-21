using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventStore.ClientAPI {
	public static class TestEventGenerator {
		private const string TestEventType = "-";

		public static IEnumerable<EventData> CreateTestEvents(int count = 1)
			=> Enumerable.Range(0, count).Select(CreateTestEvent);

		private static EventData CreateTestEvent(int index) =>
			new EventData(Guid.NewGuid(), TestEventType, true, Encoding.UTF8.GetBytes($@"{{""x"":{index}}}"),
				Array.Empty<byte>());
	}
}
