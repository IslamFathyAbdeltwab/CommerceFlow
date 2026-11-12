using CommerceFlow.Application.Addresses.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Addresses;
using MediatR;

namespace CommerceFlow.Application.Addresses.Commands.UpdateAddress;

public sealed class UpdateAddressCommandHandler(
    ICustomerAddressRepository addresses,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateAddressCommand>
{
    public async Task Handle(UpdateAddressCommand request, CancellationToken cancellationToken)
    {
        var address = await addresses.GetByIdAsync(request.AddressId, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerAddress), request.AddressId);

        // Edits rewrite the saved address in place; Order keeps its own
        // AddressSnapshot copies, so old orders are unaffected by design.
        address.Update(
            request.FullName,
            request.Line1,
            request.City,
            request.PostalCode,
            request.Country,
            line2: request.Line2,
            state: request.State,
            phoneNumber: request.PhoneNumber);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
