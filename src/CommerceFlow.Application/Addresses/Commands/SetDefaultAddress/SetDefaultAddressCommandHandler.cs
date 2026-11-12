using CommerceFlow.Application.Addresses.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Addresses;
using MediatR;

namespace CommerceFlow.Application.Addresses.Commands.SetDefaultAddress;

public sealed class SetDefaultAddressCommandHandler(
    ICustomerAddressRepository addresses,
    IUnitOfWork unitOfWork) : IRequestHandler<SetDefaultAddressCommand>
{
    public async Task Handle(SetDefaultAddressCommand request, CancellationToken cancellationToken)
    {
        var address = await addresses.GetByIdAsync(request.AddressId, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerAddress), request.AddressId);

        if (!address.IsDefault)
            await AddressDefaults.ClearPreviousAsync(
                addresses, address.CustomerId, address.Id, cancellationToken);

        address.SetDefault(); // idempotent in the domain

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
