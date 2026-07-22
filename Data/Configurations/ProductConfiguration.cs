// Data - Configuration - Product Configuration
namespace CafeManagement.Data.Configurations
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class ProductConfiguration : IEntityTypeConfiguration<Models.Entities.BaseEntity>
    {
        public void Configure(EntityTypeBuilder<Models.Entities.BaseEntity> builder)
        {
            // Product configuration will go here
        }
    }
}
