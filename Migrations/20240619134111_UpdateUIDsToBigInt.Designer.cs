﻿// <auto-generated />
using AspnetWeb.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace AspnetWeb.Migrations
{
    [DbContext(typeof(AspnetNoteDbContext))]
    [Migration("20240619134111_UpdateUIDsToBigInt")]
    partial class UpdateUIDsToBigInt
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("AspnetWeb.Models.AspnetUser", b =>
                {
                    b.Property<long>("UID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("UID"));

                    b.Property<long>("MUID")
                        .HasColumnType("bigint");

                    b.Property<string>("Salt")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserPassword")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UID");

                    b.ToTable("AspnetUsers");
                });

            modelBuilder.Entity("AspnetWeb.Models.OAuthUser", b =>
                {
                    b.Property<long>("UID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("UID"));

                    b.Property<string>("GoogleEmail")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("GoogleUID")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("MUID")
                        .HasColumnType("bigint");

                    b.HasKey("UID");

                    b.ToTable("OAuthUsers");
                });

            modelBuilder.Entity("AspnetWeb.Models.User", b =>
                {
                    b.Property<long>("UID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("UID"));

                    b.Property<int>("LoginType")
                        .HasColumnType("int");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UID");

                    b.ToTable("Users");
                });
#pragma warning restore 612, 618
        }
    }
}
