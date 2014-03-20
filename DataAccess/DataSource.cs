using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace DataAccess
{
    public class DataSource
    {
        private static readonly RandomNumberGenerator
                _random = RandomNumberGenerator.Create();
        private string _name;
        private DataSourceInformation _information;
        private DbCommandBuilder _commandBuilder;
        private DbProviderFactory _factory;
        private DbConnectionStringBuilder _connectionStringBuilder;
        private char _compositeIdentifierSeparatorPattern = ' ';
        private bool _trackOpenConnections;
        private string _seperator;
        private string _quoteSuffix;
        private string _quotePrefix;
        private int _openConnections;

        public DataSource(string name)
        {
            // this will throw if it doesn't exist
            var css = ConfigurationManager.ConnectionStrings[name];

            Initialize(name, css.ConnectionString, css.ProviderName);
        }

        public DataSource(string name, string connectionString,
                          string providerName)
        {
            Initialize(name, connectionString, providerName);
        }

        public string Name
        {
            get { return _name; }
        }

        public DataSourceInformation Information
        {
            get { return _information; }
        }

        public DbProviderFactory Factory
        {
            get { return _factory; }
        }

        public DbConnectionStringBuilder ConnectionStringBuilder
        {
            get { return _connectionStringBuilder; }
        }

        public string ConnectionString
        {
            get { return _connectionStringBuilder.ConnectionString; }
        }

        private DbCommandBuilder CommandBuilder
        {
            get
            {
                return _commandBuilder ??
                    (_commandBuilder = Factory.CreateCommandBuilder());
            }
        }

        private char CompositeIdentifierSeparatorPattern
        {
            get
            {
                if (_compositeIdentifierSeparatorPattern == ' ')
                {
                    var seperator = '.';
                    var s = _information.CompositeIdentifierSeparatorPattern;
                    if (!string.IsNullOrEmpty(s))
                    {
                        seperator = s.Replace("\\", string.Empty)[0];
                    }
                    _compositeIdentifierSeparatorPattern = seperator;
                }
                return _compositeIdentifierSeparatorPattern;
            }
        }

        private string JoinSeperator
        {
            get
            {
                if (string.IsNullOrEmpty(_seperator))
                {
                    _seperator = string.Concat(QuoteSuffix,
                         CompositeIdentifierSeparatorPattern, QuotePrefix);
                }

                return _seperator;
            }
        }

        private string QuoteSuffix
        {
            get
            {
                if (string.IsNullOrEmpty(_quoteSuffix))
                {
                    _quoteSuffix = CommandBuilder.QuoteSuffix;
                    if (string.IsNullOrEmpty(_quoteSuffix))
                    {
                        _quoteSuffix = "\"";
                    }
                    _quoteSuffix = _quoteSuffix.Trim();
                }
                return _quoteSuffix;
            }
        }

        private string QuotePrefix
        {
            get
            {
                if (string.IsNullOrEmpty(_quotePrefix))
                {
                    _quotePrefix = CommandBuilder.QuotePrefix;
                    if (string.IsNullOrEmpty(_quotePrefix))
                    {
                        _quotePrefix = "\"";
                    }
                    _quotePrefix = _quotePrefix.Trim();
                }
                return _quotePrefix;
            }
        }

        public string GenerateNewParameterName()
        {
            var len = Information.ParameterNameMaxLength;
            return GenerateNewParameterName(len);
        }

        public string GenerateNewParameterName(int length)
        {
            if (length == 0 || length > 8)
            {
                length = 8;
            }
            var buffer = new byte[length];
            _random.GetBytes(buffer);
            var sb = new StringBuilder();
            var i = 0;
            foreach (var b in buffer)
            {
                var valid = b > 64 && b < 91; // A-Z are valid
                valid |= b > 96 && b < 123;   // a-z are also valid
                if (i > 0)
                {
                    valid |= b > 47 && b < 58;
                    // 0-9 are only valid if not the first char
                }
                // if the byte is a valid char use it,
                // otherwise, use modulo divide and addition
                // to make it an a-z value
                var c = !valid ? (char)((b % 26) + 'a') : (char)b;

                sb.Append(c);
                i++;
            }
            return sb.ToString();
        }

        public string WrapObjectName(string objectName)
        {
            if (!string.IsNullOrEmpty(objectName))
            {
                var quoteSuffix = QuoteSuffix;
                var quotePrefix = QuotePrefix;
                if (objectName.Contains(quotePrefix) ||
                    objectName.Contains(quoteSuffix))
                {
                    objectName = UnwrapObjectName(objectName);
                }
                var ss = objectName.Split(CompositeIdentifierSeparatorPattern);
                if (ss.Length > 1)
                {
                    objectName = string.Join(JoinSeperator, ss);
                }

                objectName =
                  string.Concat(quotePrefix, objectName, quoteSuffix);
            }
            return objectName;
        }

        public string UnwrapObjectName(string objectName)
        {
            if (!string.IsNullOrEmpty(objectName))
            {
                var ss = objectName.Split(CompositeIdentifierSeparatorPattern);
                var quotePrefix = QuotePrefix;
                var quoteSuffix = QuoteSuffix;
                if (ss.Length > 1 && quoteSuffix.Length > 0 &&
                    quotePrefix.Length > 0)
                {
                    var list = new List<string>();
                    foreach (var s in ss)
                    {
                        var tmp = s;
                        var len = tmp.Length;
                        if (len > 2)
                        {
                            if (tmp.Substring(0, 1) == quotePrefix &&
                                tmp.Substring(len - 1, 1) == quoteSuffix)
                            {
                                tmp = tmp.Substring(1, len - 2);
                            }
                        }
                        list.Add(tmp);
                    }
                    list.CopyTo(ss);
                }
                objectName = string.Join(
                      CompositeIdentifierSeparatorPattern.ToString(), ss);
            }
            return objectName;
        }

        public DbConnection GetNewConnection()
        {
            var conn = Factory.CreateConnection();
            conn.ConnectionString = _connectionStringBuilder.ConnectionString;
            if (_trackOpenConnections)
            {
                //Add connection state change events if the
                //provider supports it
                conn.StateChange += StateChange;
            }
            conn.Disposed += ConnDisposed;
            conn.Open();
            return conn;
        }

        public string GetParameterName(string parameterName)
        {
            var s = parameterName;
            var l = Information.ParameterNameMaxLength;
            if (l < 1)
            {
                return Information.ParameterMarker;
            }
            if (l < s.Length)
            {
                s = s.Substring(0, l);
            }
            var reg = Information.ParameterNamePatternRegex;
            if (!reg.IsMatch(s))
            {
                s = GenerateNewParameterName();
            }
            return string.Concat(Information.ParameterMarker, s);
        }

        private void Initialize(string name,
                string connectionString, string providerName)
        {
            _name = name;

            // get the provider and then get the Factory Singleton
            _factory = DbProviderFactories.GetFactory(providerName);

            //some providers, don't provide an inherited DbConnectionStringBuilder
            //so if the factory call returns null, use the default.
            _connectionStringBuilder = Factory.CreateConnectionStringBuilder() ??
                                       new DbConnectionStringBuilder(true);
            _connectionStringBuilder.ConnectionString = connectionString;
            TestConnectionStringForMicrosoftExcelOrAccess();
            using (var conn = Factory.CreateConnection())
            {
                conn.ConnectionString = ConnectionString;

                //add a state change event, if this one is called, it will 
                //set up the events later, so we can keep track of how many are open
                conn.StateChange += ConnStateChange;
                conn.Open();
                _information = new DataSourceInformation(
                    conn.GetSchema(DbMetaDataCollectionNames.DataSourceInformation));
            }
       }

        private void TestConnectionStringForMicrosoftExcelOrAccess()
        {
            var useSquareBrackets = false;
            var name = _connectionStringBuilder.GetType().FullName ?? string.Empty;
            if (name.StartsWith("System.Data.OleDb"))
            {
                //this is a OleDb connection
                var s = _connectionStringBuilder["Extended Properties"] as string;
                if (!string.IsNullOrEmpty(s) && s.ToLower().Contains("excel"))
                {
                    //we found MS Excel
                    useSquareBrackets = true;
                }
                else
                {
                    //check for MS Acess
                    s = _connectionStringBuilder["Provider"] as string ?? string.Empty;
                    useSquareBrackets = s.Contains("MS Remote");
                    if (!useSquareBrackets)
                    {
                        s = (_connectionStringBuilder["Data Source"]
                              as string ?? string.Empty).ToLower();
                        useSquareBrackets = s.EndsWith(".accdb") || s.EndsWith(".mdb");
                    }
                }
            }
            else
            {
                if (name.StartsWith("System.Data.Odbc"))
                {
                    //this is an Odbc Connection
                    var s = _connectionStringBuilder["driver"] as string;
                    if (!string.IsNullOrEmpty(s))
                    {
                        s = s.ToLower();
                        //test for either excel or access
                        useSquareBrackets =
                          s.Contains("*.xls") || s.Contains("*.mdb");
                    }
                }
            }
            if (useSquareBrackets)
            {
                _quotePrefix = "[";
                _quoteSuffix = "]";
            }
        }

        private void ConnStateChange(object sender, StateChangeEventArgs e)
        {
            _trackOpenConnections = true;
        }

        private void ConnDisposed(object sender, EventArgs e)
        {
            //Debug.WriteLine("Connection Disposed");
        }

        private void StateChange(object sender, StateChangeEventArgs e)
        {
            var connectionState = e.CurrentState;
            // Debug.WriteLine(Enum.GetName(typeof(ConnectionState), 
            //                              connectionState));
            switch (connectionState)
            {
                case ConnectionState.Open:
                    Interlocked.Increment(ref _openConnections);
                    break;
                case ConnectionState.Closed:
                case ConnectionState.Broken:
                    Interlocked.Decrement(ref _openConnections);
                    break;
                default:
                    //case ConnectionState.Connecting:
                    //case ConnectionState.Executing:
                    //case ConnectionState.Fetching:
                    break;
            }
            //  Debug.WriteLine("Open Connections :" + 
            //        Interlocked.Add(ref _openConnections, 0));
        }
    }
} 
