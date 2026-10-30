using System;
using System.Collections.Generic;
using System.Text;

namespace CommerceFlow.Domain.Common.Contracts
{
    public interface IDomainEvent
    {
        DateTimeOffset OccurredOnUtc { get; }
    }
}
