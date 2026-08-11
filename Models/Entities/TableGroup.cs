using System.Collections.Generic;

namespace Quản_lý_quán_cafe.Models.Entities
{
    public class TableGroup
    {
        public int TableGroupID { get; set; }

        // The main (primary) table for the group that owns the Order
        public int PrimaryTableID { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<RestaurantTable> Tables { get; set; } = new List<RestaurantTable>();
    }
}
