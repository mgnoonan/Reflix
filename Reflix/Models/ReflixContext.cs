using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using Reflix.Models.Mapping;

namespace Reflix.Models
{
    public class ReflixContext : DbContext
    {
        static ReflixContext()
        {
            Database.SetInitializer<ReflixContext>(null);
        }

		public ReflixContext()
			: base("Name=ReflixContext")
		{
		}

        public DbSet<TitleCache> Titles { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new TitleMap());
        }
    }
}
