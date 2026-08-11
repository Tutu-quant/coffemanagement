using System.Collections.Generic;

namespace Quản_lý_quán_cafe.Models.ViewModels.TableGroup
{
    public class CreateTableGroupInput
    {
        public int PrimaryTableId { get; set; }
        public List<int> SecondaryTableIds { get; set; } = new List<int>();
    }
}
