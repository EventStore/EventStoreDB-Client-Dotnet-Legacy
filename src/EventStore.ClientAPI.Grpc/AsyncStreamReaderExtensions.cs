using System.Collections.Generic;
using System.Threading;
using Grpc.Core;

namespace EventStore.ClientAPI {
	internal static class AsyncStreamReaderExtensions {
#if !NET5_0_OR_GREATER
		public static async IAsyncEnumerable<T> ReadAllAsync<T>(this IAsyncStreamReader<T> source,
			CancellationToken cancellationToken = default) {
			while (await source.MoveNext(cancellationToken).ConfigureAwait(false)) {
				yield return source.Current;
			}
		}
#endif
	}
}
