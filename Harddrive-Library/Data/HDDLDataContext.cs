using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.Data
{
    /// <summary>
    /// Handles reading and writing to and from the database
    /// </summary>
    class HDDLDataContext : DbContext
    {
        public DbSet<DiskItem> DiskItems { get; set; }

        /// <summary>
        /// The database file's location
        /// </summary>
        private string _dbFilePath;

        /// <summary>
        /// Creates a HDDLDataContext for the given database
        /// </summary>
        /// <param name="dbPath">The database file's path</param>
        public HDDLDataContext(string dbPath)
        {
            _dbFilePath = dbPath;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={_dbFilePath}");
        }

        /// <summary>
        /// Ensures that the file exists.  If it doesn't, creates it and builds the table structure
        /// </summary>
        /// <param name="dbPath">The database file's path</param>
        public static void EnsureDatabase(string dbPath)
        {
            if (!File.Exists(dbPath))
            {
                using (SqliteConnection conn = new SqliteConnection($"Data Source={dbPath};foreign keys=true;"))
                {
                    conn.Open();

                    // Build the DiskItem table
                    var sql =
                        "CREATE TABLE \"main\".\"DiskItem\" (" +
                            "\"Id\"    BLOB, " +
                            "\"ParentId\"  BLOB, " +
                            "\"FirstScanned\"  TEXT NOT NULL, " +
                            "\"LastScanned\"   TEXT NOT NULL, " +
                            "\"Path\"  TEXT NOT NULL, " +
                            "\"ItemName\"  TEXT NOT NULL, " +
                            "\"Extension\"  TEXT, " +
                            "\"IsFile\"    INTEGER NOT NULL, " +
                            "\"SizeInBytes\"    INTEGER, " +
                            "\"LastWritten\"   TEXT NOT NULL, " +
                            "\"LastAccessed\"   TEXT NOT NULL, " +
                            "\"CreationDate\"   TEXT NOT NULL, " +
                            "PRIMARY KEY(\"Id\"), " +
                            "FOREIGN KEY(\"ParentId\") REFERENCES DiskItem(\"Id\") " +
                        "); ";
                    var cmd = new SqliteCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
