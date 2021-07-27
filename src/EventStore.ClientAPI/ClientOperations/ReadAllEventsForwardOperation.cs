using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI.ClientOperations {
	internal class ReadAllEventsForwardOperation : OperationBase<AllEventsSlice, ClientMessage.ReadAllEventsCompleted> {
		private readonly Position _position;
		private readonly int _maxCount;
		private readonly bool _resolveLinkTos;
		private readonly bool _requireLeader;

		public ReadAllEventsForwardOperation(ILogger log, TaskCompletionSource<AllEventsSlice> source,
			Position position, int maxCount, bool resolveLinkTos, bool requireLeader,
			UserCredentials userCredentials)
			: base(log, source, TcpCommand.ReadAllEventsForward, TcpCommand.ReadAllEventsForwardCompleted,
				userCredentials) {
			_position = position;
			_maxCount = maxCount;
			_resolveLinkTos = resolveLinkTos;
			_requireLeader = requireLeader;
		}

		protected override object CreateRequestDto() =>
			new ClientMessage.ReadAllEvents(_position.CommitPosition, _position.PreparePosition, _maxCount,
				_resolveLinkTos, _requireLeader);

		protected override InspectionResult InspectResponse(ClientMessage.ReadAllEventsCompleted response) {
			switch (response.Result) {
				case ClientMessage.ReadAllEventsCompleted.ReadAllResult.Success:
					Succeed();
					return new InspectionResult(InspectionDecision.EndOperation, "Success");
				case ClientMessage.ReadAllEventsCompleted.ReadAllResult.Error:
					Fail(new ServerErrorException(
						string.IsNullOrEmpty(response.Error) ? "<no message>" : response.Error));
					return new InspectionResult(InspectionDecision.EndOperation, "Error");
				case ClientMessage.ReadAllEventsCompleted.ReadAllResult.AccessDenied:
					Fail(new AccessDeniedException("Read access denied for $all."));
					return new InspectionResult(InspectionDecision.EndOperation, "AccessDenied");
				default:
					throw new Exception($"Unexpected ReadAllResult: {response.Result}.");
			}
		}

		protected override AllEventsSlice TransformResponse(ClientMessage.ReadAllEventsCompleted response) =>
			new(ReadDirection.Forward,
				new Position(response.CommitPosition, response.PreparePosition),
				new Position(response.NextCommitPosition, response.NextPreparePosition),
				Array.ConvertAll(response.Events ?? Array.Empty<ClientMessage.ResolvedEvent>(),
					e => new ResolvedEvent(e)));

		public override string ToString() =>
			$"Position: {_position}, MaxCount: {_maxCount}, ResolveLinkTos: {_resolveLinkTos}, RequireLeader: {_requireLeader}";
	}
}
