using GroupDocs.Viewer.MVC.Products.Common.Config;
using GroupDocs.Viewer.MVC.Products.Common.Entity.Web;
using GroupDocs.Viewer.MVC.Products.Common.Resources;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Util
{
    public class DbInputHandler : IInputHandler
    {
        private readonly GlobalConfiguration globalConfiguration;

        public DbInputHandler(GlobalConfiguration globalConfiguration)
        {
            this.globalConfiguration = globalConfiguration;
        }

        public string GetFileName(string guid)
        {
            string ext;
            using (SqlConnection cn = new SqlConnection(@"Data Source =.\SQLEXPRESS; Initial Catalog = tempdb; Integrated Security = True"))
            using (SqlCommand cm = cn.CreateCommand())
            {
                cm.CommandText = @"
                    SELECT Ext
                    FROM   tempdb.dbo.Files
                    WHERE  Id = @Id";
                cm.Parameters.AddWithValue("@Id", guid);
                cn.Open();
                ext = (cm.ExecuteScalar() as string).TrimEnd();
            }

            return guid + "." + ext;
        }

        public List<FileDescriptionEntity> GetFilesList()
        {
            var filesList = new List<FileDescriptionEntity>();

            SqlConnection conn = new SqlConnection(@"Data Source =.\SQLEXPRESS; Initial Catalog = tempdb; Integrated Security = True");
            conn.Open();

            SqlCommand command = new SqlCommand("select Id, Ext from tempdb.dbo.Files", conn);
            // int result = command.ExecuteNonQuery();
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Console.WriteLine(String.Format("{0}", reader["Id"]));

                    FileDescriptionEntity fileDescription = new FileDescriptionEntity
                    {
                        guid = reader["Id"].ToString(),
                        name = reader["Id"].ToString() + "." + reader["Ext"].ToString().TrimEnd(),

                        // set is directory true/false
                        isDirectory = false,
                    };

                    filesList.Add(fileDescription);
                }
            }

            conn.Close();

            return filesList;
        }

        public Stream GetFile(string guid)
        {
            using (SqlConnection cn = new SqlConnection(@"Data Source =.\SQLEXPRESS; Initial Catalog = tempdb; Integrated Security = True"))
            using (SqlCommand cm = cn.CreateCommand())
            {
                cm.CommandText = @"
                    SELECT Content
                    FROM   tempdb.dbo.Files
                    WHERE  Id = @Id";
                cm.Parameters.AddWithValue("@Id", guid);
                cn.Open();
                return new MemoryStream(cm.ExecuteScalar() as byte[]);
            }
        }

        public string StoreFile(Stream inputStream, string fileName, bool rewrite)
        {
            string fileSavePath;

            if (rewrite)
            {
                // Get the complete file path.
                fileSavePath = Path.Combine(globalConfiguration.Viewer.GetFilesDirectory(), fileName);
            }
            else
            {
                fileSavePath = Resources.GetFreeFileName(globalConfiguration.Viewer.GetFilesDirectory(), fileName);
            }

            using (var fileStream = File.Create(fileSavePath))
            {
                inputStream.Seek(0, SeekOrigin.Begin);
                inputStream.CopyTo(fileStream);
            }

            return fileSavePath;
        }
    }
}