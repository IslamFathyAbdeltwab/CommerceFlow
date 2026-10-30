using System;
using System.Collections.Generic;
using System.Text;

namespace CommerceFlow.Domain.Common.Entitiy
{
    public abstract class Entity
    {
        public Guid Id { get; protected set; } = Guid.NewGuid();
    }
}
