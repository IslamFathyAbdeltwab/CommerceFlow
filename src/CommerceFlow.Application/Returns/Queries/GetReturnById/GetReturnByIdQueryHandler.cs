using CommerceFlow.Application.Returns.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Returns.Queries.GetReturnById;

public sealed class GetReturnByIdQueryHandler(IReturnRequestRepository returns)
    : IRequestHandler<GetReturnByIdQuery, ReturnRequestDto?>
{
    public async Task<ReturnRequestDto?> Handle(GetReturnByIdQuery request, CancellationToken cancellationToken)
    {
        var returnRequest = await returns.GetByIdAsync(request.ReturnId, cancellationToken);

        return returnRequest is null ? null : ReturnRequestMappings.ToDto(returnRequest);
    }
}
