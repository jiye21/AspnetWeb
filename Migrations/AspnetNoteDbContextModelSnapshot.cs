﻿// <auto-generated />
using System;
using AspnetWeb.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace AspnetWeb.Migrations
{
    [DbContext(typeof(AspnetNoteDbContext))]
    partial class AspnetNoteDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
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

            modelBuilder.Entity("AspnetWeb.Models.FriendList", b =>
                {
                    b.Property<long>("UID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("UID"));

                    b.Property<long>("FriendMUID")
                        .HasColumnType("bigint");

                    b.Property<string>("FriendName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("HeartCount")
                        .HasColumnType("int");

                    b.Property<long>("MUID")
                        .HasColumnType("bigint");

                    b.HasKey("UID");

                    b.ToTable("FriendList");
                });

            modelBuilder.Entity("AspnetWeb.Models.Note", b =>
                {
                    b.Property<int>("NoteNo")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("NoteNo"));

                    b.Property<string>("NoteContents")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("NoteTitle")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("UID")
                        .HasColumnType("bigint");

                    b.HasKey("NoteNo");

                    b.ToTable("Notes");
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

            modelBuilder.Entity("AspnetWeb.Models.ShoppingList", b =>
                {
                    b.Property<long>("UID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("UID"));

                    b.Property<int>("Count")
                        .HasColumnType("int");

                    b.Property<long>("MUID")
                        .HasColumnType("bigint");

                    b.Property<int>("Price")
                        .HasColumnType("int");

                    b.Property<string>("Product")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("PurchaseDate")
                        .HasColumnType("datetimeoffset");

                    b.HasKey("UID");

                    b.ToTable("ShoppingList");
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
