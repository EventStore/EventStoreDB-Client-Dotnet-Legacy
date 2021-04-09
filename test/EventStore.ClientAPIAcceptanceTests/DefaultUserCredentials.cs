using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI {
	internal static class DefaultUserCredentials {
		public static readonly UserCredentials Admin = new UserCredentials("admin", "changeit");
	}
}
