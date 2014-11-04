using System;
using System.Data.SqlServerCe;
using EPiServerChannelLib.RecordManagers;

namespace ImportFromDatabase.RecordManagers
{
    public class SqlCeRecordManager : IRecordManager
    {
        private SqlCeConnection connection;

        public SqlCeRecordManager(string path = null)
        {
            DatabasePath = path;
        }

        public string DatabasePath { get; set; }

        public void Init()
        {
            connection = new SqlCeConnection("Data Source=" + DatabasePath + ";Persist Security Info=False;");
            connection.Open();
        }

        public Guid GetEPiServerGuid(string key)
        {
            var command = new SqlCeCommand();
            command.Connection = connection;
            command.CommandText = "SELECT EPiServerGuid FROM Mappings WHERE ExternalId = @ExternalId";
            command.Parameters.AddWithValue("ExternalId", key);

            object value = command.ExecuteScalar();

            if (value == null)
            {
                return Guid.Empty;
            }

            return Guid.Parse((string) value);
        }

        public void AddEPiServerGuid(string key, Guid pageGuid)
        {
            var command = new SqlCeCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("ExternalId", key);
            command.Parameters.AddWithValue("EPiServerGuid", pageGuid.ToString());

            command.CommandText = "DELETE FROM Mappings WHERE ExternalId = @ExternalId";
            command.ExecuteNonQuery();

            command.CommandText = "INSERT INTO Mappings (ExternalId, EPiServerGuid) VALUES (@ExternalId, @EPiServerGuid)";
            command.ExecuteNonQuery();
        }

        public void Close()
        {
        }
    }
}