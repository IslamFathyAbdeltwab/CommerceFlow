using CommerceFlow.Application.Addresses.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Addresses;
using MediatR;

namespace CommerceFlow.Application.Addresses.Commands.DeleteAddress;

public sealed class DeleteAddressCommandHandler(
    ICustomerAddressRepository addresses,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteAddressCommand>
{
    public async Task Handle(DeleteAddressCommand request, CancellationToken cancellationToken)
    {
        var address = await addresses.GetByIdAsync(request.AddressId, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerAddress), request.AddressId);

        // Deleting the default is allowed and simply leaves the customer with
        // no default until another address is set (no auto-promotion).
        addresses.Remove(address);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
