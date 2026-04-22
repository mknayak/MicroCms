using MediatR;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Common.Markers;

/// <summary>Marker interface for commands that return a typed <see cref="Result{T}"/>.</summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;

/// <summary>Marker interface for commands that return a non-generic <see cref="Result"/>.</summary>
public interface ICommand : IRequest<Result>;
