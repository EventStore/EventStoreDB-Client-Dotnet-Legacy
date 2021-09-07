using System.Reflection;
using System.Threading.Tasks;

namespace EventStore.ClientAPI.Common.Utils.Threading {
	internal static class TaskCompletionSourceFactory {
		public static TaskCompletionSource<T> Create<T>(TaskCreationOptions options = TaskCreationOptions.None) {
			return new TaskCompletionSource<T>(options | TaskCreationOptions.RunContinuationsAsynchronously);
		}
	}
}
