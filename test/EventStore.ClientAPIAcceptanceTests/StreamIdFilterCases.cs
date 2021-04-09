using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EventStore.ClientAPI {
	public class StreamIdFilterCases : IEnumerable<object[]> {
		public IEnumerator<object[]> GetEnumerator() {
			yield return new object[] {StreamIdPrefix, nameof(StreamIdPrefix)};
			yield return new object[] {StreamIdRegex, nameof(StreamIdRegex)};
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		private static readonly Func<string, Filter> StreamIdPrefix = prefix => Filter.StreamId.Prefix(prefix);

		private static readonly Func<string, Filter> StreamIdRegex = prefix =>
			Filter.StreamId.Regex(new Regex($"^{prefix}"));

		public enum FilterType {
			Prefix,
			Regex
		}

		public class StreamIdFilterCase {
			public StreamIdFilterCase(FilterType filterType) {
				FilterType = filterType;
			}

			public FilterType FilterType { get; }

			public Filter CreateFilter(string prefix) => FilterType switch {
				FilterType.Prefix => StreamIdPrefix(prefix),
				FilterType.Regex => StreamIdRegex(prefix),
				_ => throw new NotImplementedException(),
			};
		}
	}
}
