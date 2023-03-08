using Databases.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Databases.Identity.Mappings
{
    public class ApplicationUserMap : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {

            // Primary key
            builder.HasKey(p => p.Id);

            // Table & Column Mappings
            
            builder.ToTable("AspNetUsers").Property(p => p.Id).HasColumnName("Id");

            builder.Property(t => t.Id).HasMaxLength(100);
            builder.Property(t => t.OrganisationId).HasMaxLength(100);
            //builder.Property(t => t.Civility).HasColumnName("Civility").HasMaxLength(100);
            //builder.Property(t => t.FirstName).HasColumnName("FirstName").HasMaxLength(100);
            //builder.Property(t => t.LastName).HasColumnName("LastName").HasMaxLength(100);
            builder.Property(t => t.IsActive).HasColumnName("IsActive");
            builder.Property(t => t.LastLoggedOn).HasColumnType("datetime2");
            builder.Property(t => t.DateAdded).HasColumnType("datetime2");
            builder.Property(t => t.DateModified).HasColumnType("datetime2");

            builder.Property(t => t.AddedBy).HasColumnName("AddedBy").HasMaxLength(100);
            builder.Property(t => t.IsRootUser).HasColumnName("IsRootUser");
            builder.Property(t => t.IsPasswordTemporary).HasColumnName("IsPasswordTemporary");

            //One to one relationship
            builder.HasOne(u => u.Organisation)
                .WithMany()
                .HasPrincipalKey(u => u.Id)
                .HasForeignKey(u => u.OrganisationId);
        }
    }   
}
