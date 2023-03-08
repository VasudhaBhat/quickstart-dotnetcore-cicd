using Databases.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Databases.Identity.Mappings
{
    public class OrganisationMap : IEntityTypeConfiguration<Organisation>
    {
        public void Configure(EntityTypeBuilder<Organisation> builder) {

            // Primary key
            builder.HasKey(p => p.Id);

            // Table & Column Mappings
            builder.ToTable("Organisation");

            builder.Property(t => t.Id).HasMaxLength(100);
            builder.Property(t => t.Name).HasColumnName("Name").HasMaxLength(100);
            builder.Property(t => t.Region).HasColumnName("Region").HasMaxLength(10);
            builder.Property(t => t.Data).HasColumnName("Data");
            builder.Property(t => t.IsDeleted).HasColumnName("IsDeleted");
            builder.Property(t => t.CreatedDate).HasColumnType("datetime2");
            builder.Property(t => t.ModifiedDate).HasColumnType("datetime2");

        }
    }
}
