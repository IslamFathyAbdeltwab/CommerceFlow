using CommerceFlow.Application.Addresses.Interfaces;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Addresses;
using MediatR;

namespace CommerceFlow.Application.Addresses.Commands.CreateAddress;

public sealed class CreateAddressCommandHandler(
    ICustomerAddressRepository addresses,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateAddressCommand, Guid>
{
    public async Task<Guid> Handle(CreateAddressCommand request, CancellationToken cancellationToken)
    {
        // The first saved address always becomes the default; an explicitly
        // requested default replaces the previous one (one default per customer).
        var isFirstAddress = await addresses.CountAsync(request.CustomerId, cancellationToken) == 0;
        var isDefault = request.IsDefault || isFirstAddress;

        var address = CustomerAddress.Create(
            request.CustomerId,
            request.FullName,
            request.Line1,
            request.City,
            request.PostalCode,
            request.Country,
            line2: request.Line2,
            state: request.State,
            phoneNumber: request.PhoneNumber,
            isDefault: isDefault);

        if (isDefault)
            await AddressDefaults.ClearPreviousAsync(
                addresses, request.CustomerId, Guid.Empty, cancellationToken);

        await addresses.AddAsync(address, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return address.Id;
    }
}
