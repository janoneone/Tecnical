using Oracle.ManagedDataAccess.Client;

namespace PruebaTecnica.Infrastructure
{
    public class OracleConnectionFactory
    {
        private readonly string _connectionString;

        public OracleConnectionFactory(IConfiguration configuration)
        {
            var host = configuration["Oracle:Host"];
            var port = configuration["Oracle:Port"];
            var serviceName = configuration["Oracle:ServiceName"];
            var userName = configuration["Oracle:UserName"];
            var password = configuration["Oracle:Password"];

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(port) ||
                string.IsNullOrWhiteSpace(serviceName) ||
                string.IsNullOrWhiteSpace(userName) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "La configuración de Oracle está incompleta.");
            }

            var dataSource =
                $"(DESCRIPTION=" +
                    $"(ADDRESS=" +
                        $"(PROTOCOL=TCP)" +
                        $"(HOST={host})" +
                        $"(PORT={port})" +
                    $")" +
                    $"(CONNECT_DATA=" +
                        $"(SERVICE_NAME={serviceName})" +
                    $")" +
                $")";

            var builder = new OracleConnectionStringBuilder
            {
                UserID = userName,
                Password = password,
                DataSource = dataSource
            };

            _connectionString = builder.ConnectionString;
        }

        public OracleConnection CreateConnection()
        {
            return new OracleConnection(_connectionString);
        }
    }
}