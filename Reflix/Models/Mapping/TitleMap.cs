using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace Reflix.Models.Mapping
{
    public class TitleMap : EntityTypeConfiguration<TitleCache>
    {
        public TitleMap()
        {
            // Primary Key
            this.HasKey(t => new { t.TitleID, t.WeekOfDate, t.IsRss });

            // Properties
            this.Property(t => t.TitleID)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            // Table & Column Mappings
            this.ToTable("Title");
            this.Property(t => t.TitleID).HasColumnName("TitleID");
            this.Property(t => t.WeekOfDate).HasColumnName("WeekOfDate");
            this.Property(t => t.ObjectData).HasColumnName("ObjectData");
            this.Property(t => t.IsRss).HasColumnName("IsRss");
        }
    }
}
