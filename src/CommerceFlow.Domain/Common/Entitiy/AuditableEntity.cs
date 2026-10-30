using System;
using System.Collections.Generic;
using System.Text;

namespace CommerceFlow.Domain.Common.Entitiy
{
    public abstract class AuditableEntity : Entity
    {
        public DateTimeOffset CreatedAtUtc { get; protected set; }
        public DateTimeOffset UpdatedAtUtc { get; protected set; }
    }
}
