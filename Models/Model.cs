using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VideoChat.Models
{
    public class Dao
    {
        VideoChatDBContext db = new VideoChatDBContext();
        public bool Login(string username, string password)
        {
            List<IdentityUser> users = db.IdentityUser.ToList();
            foreach (IdentityUser u in users)
            {
                if (u.UserName == username)
                {
                    if (u.Passwd == password)
                        return true;
                    else return false;
                }
            }
            return false;
        }

        public IdentityUser getByName(string username)
        {
            return db.IdentityUser.Where(u => u.UserName == username).FirstOrDefault();
        }
    }
    public class ClassRoom
    {
        public string ClassID { get; set; }
        public string ClassName { get; set; }
        public string Topic { get; set; }
    }
    public class Room
    {
        public ClassRoom RoomIF {get;set;}
        public List<IdentityUser> UserCall { get; set; }
    }
    public class IdentityUserLogin
    {
        public string LoginProvider { get; set; }
        public string ProviderKey { get; set; }
        public string ProviderDisplayName { get; set; }
        public string UserID { get; set; }
    }
    public class IdentityUser
    {
        public string ID { get; set; }
        public string UserName { get; set; }
        public string ConnectionID { get; set; }
        public string Passwd { get; set; }
        public bool InCall { get; set; }
        public bool IsCaller { get; set; }
    }
    public class VideoChatDBContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<IdentityUserLogin>()
                .HasKey(t => new { t.LoginProvider, t.UserID });
            modelBuilder.Entity<IdentityUser>()
                .HasKey(t => new { t.ID });
            modelBuilder.Entity<ClassRoom>()
                .HasKey(t => t.ClassID);
        }
        public VideoChatDBContext() { }
        public VideoChatDBContext(DbContextOptions<VideoChatDBContext> options) : base(options)
        { }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
    => options.UseSqlServer("Data Source=.\\SQLEXPRESS;Initial Catalog=HocTrucTuyen;Integrated Security=True");
        public DbSet<IdentityUserLogin> IdentityUserLogin { get; set; }
        public DbSet<IdentityUser> IdentityUser { get; set; }
        public DbSet<ClassRoom> ClassRoom { get; set; }
    }
}
