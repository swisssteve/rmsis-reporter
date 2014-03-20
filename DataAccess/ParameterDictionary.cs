using System.Collections.Generic;
using System.Data.Common;

    namespace DataAccess
    {
        public class ParameterDictionary : Dictionary<string, DbParameter>
        {
            public void Add(DbParameter item)
            {
                Add(item.ParameterName, item);
            }
        }
    } 

