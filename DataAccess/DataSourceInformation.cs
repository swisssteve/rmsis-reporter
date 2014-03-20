using System;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DataAccess
{
    public class DataSourceInformation
    {
        private static readonly Type _Type = typeof(DataSourceInformation);
        private static readonly Type _IdentifierCaseType =
           Enum.GetUnderlyingType(typeof(IdentifierCase));
        private static readonly Type _GroupByBehaviorType =
           Enum.GetUnderlyingType(typeof(GroupByBehavior));

        private static readonly Type _SupportedJoinOperatorsType =
            Enum.GetUnderlyingType(typeof(SupportedJoinOperators));

        //These are filled within the "switch/case"
        //statement, either directly, or thru reflection.
        //since Resharper can't tell they are being filled 
        //thru reflection, it suggests to convert them to
        //constants. DO NOT do that!!!!!

        // ReSharper disable ConvertToConstant.Local
        private readonly string _compositeIdentifierSeparatorPattern = string.Empty;
        private readonly string _dataSourceProductName = string.Empty;
        private readonly string _dataSourceProductVersion = string.Empty;
        private readonly string _dataSourceProductVersionNormalized = string.Empty;
        private readonly GroupByBehavior _groupByBehavior;
        private readonly string _identifierPattern = string.Empty;
        private readonly IdentifierCase _identifierCase;
        private readonly bool _orderByColumnsInSelect = false;
        private readonly string _parameterMarkerFormat = string.Empty;
        private readonly string _parameterMarkerPattern = string.Empty;
        private readonly Int32 _parameterNameMaxLength = 0;
        private readonly string _parameterNamePattern = string.Empty;
        private readonly string _quotedIdentifierPattern = string.Empty;
        private readonly Regex _quotedIdentifierCase;
        private readonly string _statementSeparatorPattern = string.Empty;
        private readonly Regex _stringLiteralPattern;
        private readonly SupportedJoinOperators _supportedJoinOperators;
        // ReSharper restore ConvertToConstant.Local
        private Regex _parameterNamePatternRegex;
        private string _parameterPrefix;

        public DataSourceInformation(DataTable dt)
        {
            //DataTable dt = Connection.GetSchema(
            //   DbMetaDataCollectionNames.DataSourceInformation);
            foreach (DataRow r in dt.Rows)
            {
                foreach (DataColumn c in dt.Columns)
                {
                    string s = c.ColumnName;
                    object o = r[c.ColumnName];
                    //just for safety
                    if (o == DBNull.Value)
                    {
                        o = null;
                    }
                    if (!string.IsNullOrEmpty(s) && o != null)
                    {
                        switch (s)
                        {
                            case "QuotedIdentifierCase":
                                _quotedIdentifierCase = new Regex(o.ToString());
                                break;
                            case "StringLiteralPattern":
                                _stringLiteralPattern = new Regex(o.ToString());
                                break;
                            case "GroupByBehavior":
                                o = Convert.ChangeType(o, _GroupByBehaviorType);
                                _groupByBehavior = (GroupByBehavior)o;
                                break;
                            case "IdentifierCase":
                                o = Convert.ChangeType(o, _IdentifierCaseType);
                                _identifierCase = (IdentifierCase)o;
                                break;
                            case "SupportedJoinOperators":
                                o = Convert.ChangeType(o, _SupportedJoinOperatorsType);
                                _supportedJoinOperators = (SupportedJoinOperators)o;
                                // (o as SupportedJoinOperators?) ??
                                //    SupportedJoinOperators.None;
                                break;
                            default:
                                FieldInfo fi = _Type.GetField("_" + s,
                                  BindingFlags.IgnoreCase | BindingFlags.NonPublic |
                                  BindingFlags.Instance);
                                if (fi != null)
                                {
                                    fi.SetValue(this, o);
                                }
                                break;
                        }
                    }
                }
                //there should only ever be a single row.
                break;
            }
        }

        public string CompositeIdentifierSeparatorPattern
        {
            get { return _compositeIdentifierSeparatorPattern; }
        }

        public string DataSourceProductName
        {
            get { return _dataSourceProductName; }
        }

        public string DataSourceProductVersion
        {
            get { return _dataSourceProductVersion; }
        }

        public string DataSourceProductVersionNormalized
        {
            get { return _dataSourceProductVersionNormalized; }
        }

        public GroupByBehavior GroupByBehavior
        {
            get { return _groupByBehavior; }
        }

        public string IdentifierPattern
        {
            get { return _identifierPattern; }
        }

        public IdentifierCase IdentifierCase
        {
            get { return _identifierCase; }
        }

        public bool OrderByColumnsInSelect
        {
            get { return _orderByColumnsInSelect; }
        }

        public string ParameterMarkerFormat
        {
            get { return _parameterMarkerFormat; }
        }

        public string ParameterMarkerPattern
        {
            get { return _parameterMarkerPattern; }
        }

        public int ParameterNameMaxLength
        {
            get { return _parameterNameMaxLength; }
        }

        public string ParameterNamePattern
        {
            get { return _parameterNamePattern; }
        }

        public string QuotedIdentifierPattern
        {
            get { return _quotedIdentifierPattern; }
        }

        public Regex QuotedIdentifierCase
        {
            get { return _quotedIdentifierCase; }
        }

        public string StatementSeparatorPattern
        {
            get { return _statementSeparatorPattern; }
        }

        public Regex StringLiteralPattern
        {
            get { return _stringLiteralPattern; }
        }

        public SupportedJoinOperators SupportedJoinOperators
        {
            get { return _supportedJoinOperators; }
        }

        public Regex ParameterNamePatternRegex
        {
            get
            {
                return _parameterNamePatternRegex ??
                    (_parameterNamePatternRegex = new Regex(ParameterNamePattern));
            }

        }

        public string ParameterMarker
        {
            get
            {
                if (string.IsNullOrEmpty(_parameterPrefix))
                {
                    _parameterPrefix = _parameterNameMaxLength != 0
                                        ? ParameterMarkerPattern.Substring(0, 1)
                                        : ParameterMarkerFormat;
                }
                return _parameterPrefix;
            }
        }
    }
} 
