namespace Fcmb.Assessment.CSharp.Common.Application.Messaging;

public interface ICommand : IRequest;

public interface ICommand<TResponse> : IRequest<TResponse>;
