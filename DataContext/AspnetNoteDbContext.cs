using AspnetWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace AspnetWeb.DataContext
{
    // 테이블을 생성할 수 있는 코드를 작성
    public class AspnetNoteDbContext : DbContext
    {
        // code first로 테이블 생성
        public DbSet<User> Users { get; set; }
        public DbSet<AspnetUser> AspnetUsers { get; set; }
        public DbSet<OAuthUser> OAuthUsers { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<User>()
				.Property(u => u.UID)
				.HasColumnType("bigint");

			modelBuilder.Entity<AspnetUser>()
				.Property(u => u.MUID)
				.HasColumnType("bigint");

			modelBuilder.Entity<OAuthUser>()
				.Property(u => u.MUID)
				.HasColumnType("bigint");

			modelBuilder.Entity<AspnetUser>()
				.Property(u => u.UID)
				.HasColumnType("bigint");

			modelBuilder.Entity<OAuthUser>()
				.Property(u => u.UID)
				.HasColumnType("bigint");
		}


		// migration 후 DB를 생성하는 UpdateDatabase 명령을 수행하면 DB가 생성된다. 
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=localhost\SQLEXPRESS;Database=AspnetNoteDb;User Id=sa;Password=test1234;Encrypt=false;");
        }
	}
}
