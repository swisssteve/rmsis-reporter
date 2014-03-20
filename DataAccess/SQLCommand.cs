
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Text;
    using System.Threading;

namespace DataAccess
{
        public sealed class SQLCommand : IDisposable
        {
            private static readonly Dictionary<string, DataSource>
                    _dataSourceDictionary = new Dictionary<string, DataSource>();
            private static readonly object _syncObject = new object();
            private readonly ParameterDictionary _parameters =
                    new ParameterDictionary();
            private readonly StringBuilder _commandText = new StringBuilder();
            private DataSource _dataSource;
            private int _disposed;
            private DbConnection _connection;

            public SQLCommand(string connectionName)
            {
                Initialize(connectionName);
            }

            public SQLCommand(string connectionName, string connectionString,
                              string providerName)
            {
                DataSource dataSource = null;
                lock (_syncObject)
                {
                    if (!_dataSourceDictionary.TryGetValue(
                              connectionName, out dataSource))
                    {
                        dataSource = new DataSource(connectionName,
                                         connectionString, providerName);
                        _dataSourceDictionary.Add(connectionName, dataSource);
                    }
                }

                _dataSource = dataSource;
            }

            ~SQLCommand()
            {
                Dispose(false);
            }

            public StringBuilder CommandText
            {
                get { return _commandText; }
            }

            public bool InTransaction
            {
                get { return false; }
            }

            public ParameterDictionary Parameters
            {
                get { return _parameters; }
            }

            public void Dispose()
            {
                Dispose(true);
            }

            public void Initialize(string connectionName)
            {
                DataSource dataSource = null;
                lock (_syncObject)
                {
                    if (!_dataSourceDictionary.TryGetValue(connectionName,
                                                           out dataSource))
                    {
                        dataSource = new DataSource(connectionName);
                        _dataSourceDictionary.Add(connectionName, dataSource);
                    }
                }

                _dataSource = dataSource;
            }

            public DbDataReader ExecuteReader()
            {
                var behavior = InTransaction ?
                    CommandBehavior.Default : CommandBehavior.CloseConnection;

                return ExecuteReader(behavior, CommandType.Text, 30);
            }

            public DbDataReader ExecuteReader(CommandBehavior commandBehavior,
                   CommandType commandType, int? commandTimeOut)
            {
                var conn = GetConnection();
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = _commandText.ToString();
                        cmd.CommandType = commandType;
                        cmd.CommandTimeout = commandTimeOut ?? cmd.CommandTimeout;
                        try
                        {
                            foreach (var parameter in Parameters.Values)
                            {
                                cmd.Parameters.Add(parameter);
                            }

                            return cmd.ExecuteReader(commandBehavior);
                        }
                        finally
                        {
                            cmd.Parameters.Clear();
                        }
                    }
                }
                finally
                {
                    // this is a special case even if this object
                    // is NOT part of a transaction so handle it 
                    // differently than other cases
                    if ((commandBehavior & CommandBehavior.CloseConnection) ==
                         CommandBehavior.CloseConnection)
                    {
                        // get rid of the connection
                        // so the connection won't be reused
                        // if not in a transaction
                        // and the SQLCommand is reused.
                        _connection = null;
                    }
                }
            }

            public object ExecuteScalar()
            {
                return ExecuteScalar(CommandType.Text, null);
            }

            public object ExecuteScalar(CommandType commandType)
            {
                return ExecuteScalar(commandType, null);
            }

            public object ExecuteScalar(CommandType commandType,
                                        int? commandTimeout)
            {
                try
                {
                    var behavior = InTransaction ? CommandBehavior.Default :
                                   CommandBehavior.CloseConnection;
                    behavior |= CommandBehavior.SingleRow |
                                CommandBehavior.SingleResult;

                    using (var dr = ExecuteReader(behavior,
                                                  commandType, commandTimeout))
                    {
                        dr.Read();
                        return dr.GetValue(0);
                    }
                }
                finally
                {
                    DisposeConnection();
                }
            }

            public T ExecuteScalar<T>()
            {
                return (T)ExecuteScalar(CommandType.Text, null);
            }

            public T ExecuteScalar<T>(CommandType commandType)
            {
                return (T)ExecuteScalar(commandType, null);
            }

            public T ExecuteScalar<T>(CommandType commandType, int? commandTimeout)
            {
                return (T)ExecuteScalar(commandType, commandTimeout);
            }

            public int ExecuteNonQuery(CommandType commandType)
            {
                return ExecuteNonQuery(commandType, null);
            }

            public int ExecuteNonQuery(CommandType commandType, int? commandTimeout)
            {
                var conn = GetConnection();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = _commandText.ToString();
                    cmd.CommandTimeout = commandTimeout ?? cmd.CommandTimeout;
                    cmd.CommandType = commandType;
                    try
                    {
                        foreach (var parameter in Parameters.Values)
                        {
                            cmd.Parameters.Add(parameter);
                        }

                        return cmd.ExecuteNonQuery();
                    }
                    finally
                    {
                        cmd.Parameters.Clear();
                    }
                }
            }

            public DataSet ExecuteDataSet()
            {
                var conn = GetConnection();
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        try
                        {
                            cmd.CommandText = _commandText.ToString();
                            // cmd.CommandTimeout = 0;
                            cmd.CommandType = CommandType.Text;
                            using (var da = _dataSource.Factory.CreateDataAdapter())
                            {
                                da.SelectCommand = cmd;
                                var dt = new DataSet();
                                da.Fill(dt);
                                return dt;
                            }
                        }
                        finally
                        {
                            cmd.Parameters.Clear();
                        }
                    }
                }
                finally
                {
                    DisposeConnection();
                }
            }

            public DataTable ExecuteDataTable()
            {
                var conn = GetConnection();
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        try
                        {
                            cmd.CommandText = _commandText.ToString();
                            // cmd.CommandTimeout = 0;
                            cmd.CommandType = CommandType.Text;
                            using (var da = _dataSource.Factory.CreateDataAdapter())
                            {
                                da.SelectCommand = cmd;
                                var dt = new DataTable();
                                da.Fill(dt);
                                return dt;
                            }
                        }
                        finally
                        {
                            cmd.Parameters.Clear();
                        }
                    }
                }
                finally
                {
                    DisposeConnection();
                }
            }

            public void BeginTransaction()
            {
                throw new NotImplementedException();
            }

            public void CommitTransaction()
            {
                throw new NotImplementedException();
            }

            public void RollbackTransaction()
            {
                throw new NotImplementedException();
            }

            public string WrapObjectName(string objectName)
            {
                return _dataSource.WrapObjectName(objectName);
            }

            public DbParameter CreateParameter(DbType dbType,
                               string name, object value)
            {
                var p = _dataSource.Factory.CreateParameter();
                p.ParameterName = name;
                p.Value = value;
                p.DbType = dbType;
                return p;
            }

            public string GenerateNewParameterName()
            {
                return _dataSource.GenerateNewParameterName();
            }

            public string GetParameterName(DbParameter dbParameter)
            {
                return _dataSource.GetParameterName(dbParameter.ParameterName);
            }

            public string GetParameterName(string parameterName)
            {
                return _dataSource.GetParameterName(parameterName);
            }

            public DbParameter CreateParameter(DbType dbType, object value)
            {
                return CreateParameter(dbType,
                       _dataSource.GenerateNewParameterName(), value);
            }

            private DbConnection GetConnection()
            {
                // While I am not going to cover it here,
                // you would get the transactions existing connection 
                // from that transaction, or if need be,
                // get a new connection for a seperate database
                if (_connection != null && _connection.State ==
                     ConnectionState.Closed)
                {
                    DisposeConnection();
                }
                _connection = _connection ?? GetNewConnection();
                return _connection;
            }

            private void DisposeConnection()
            {
                if (!InTransaction && _connection != null)
                {
                    _connection.Dispose();
                    _connection = null;
                }
            }

            private void Dispose(bool disposing)
            {
                if (Interlocked.Increment(ref _disposed) == 1)
                {
                    if (disposing)
                    {
                        GC.SuppressFinalize(this);
                    }
                    if (_connection != null)
                    {
                        if (InTransaction)
                        {
                            //rollback 
                        }
                        DisposeConnection();

                    }
                    _dataSource = null;
                }
                Interlocked.Exchange(ref _disposed, 1);
            }

            private DbConnection GetNewConnection()
            {
                return _dataSource.GetNewConnection();
            }
        }
    } 
