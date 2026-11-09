using System;
using System.Collections.Generic;
using System.Text;

namespace CommerceFlow.Domain.Enums
{
    public enum StockMovementType
    {
        StockReceived,
        Shipment,
        ReturnRestock,
        ManualAdjustment,
        DamagedWriteOff
    }
}
