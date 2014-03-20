using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using DataTypes;
using System.Diagnostics;

namespace DataAccess
{
    public class DataConnector
    {
        private const int RMSIS_REQ_ID_LOCATION         = 22;
        private const int RMSIS_REQ_DESC_LOCATION       = 28;
        private const int RMSIS_PROJECT_ID_LOCATION     = 0;
        private const int RMSIS_PROJECT_NAME_LOCATION   = 7;

        public List<Requirement> GetRequirements(string projectID)
        {
            List<Requirement> tempList = new List<Requirement>();
            Requirement req = new Requirement();
       
            using (var sc = new SQLCommand("RMsisDB"))
            {
                sc.CommandText.AppendFormat("SELECT * from {0} where {1} = {2}",
                               sc.WrapObjectName("Requirement"),
                               sc.WrapObjectName("project_id"),
                               projectID);

                using (var dr = sc.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        req = new Requirement();
                        req.ID = dr.GetValue(RMSIS_REQ_ID_LOCATION).ToString();
                        req.Description = dr.GetValue(RMSIS_REQ_DESC_LOCATION).ToString();
                        tempList.Add(req);
                    }
                }
            }
            
            return tempList;
        }

        public List<RequirementHeirachy> GetRequirementHeriachies()
        {
            List<RequirementHeirachy> tempList = new List<RequirementHeirachy>();
            tempList.Add(new RequirementHeirachy { ID = "200", position = 0 });
            tempList.Add(new RequirementHeirachy { ID = "201", position = 1 });
            tempList.Add(new RequirementHeirachy { ID = "202", position = 2 });
            tempList.Add(new RequirementHeirachy { ID = "203", position = 3 });
            tempList.Add(new RequirementHeirachy { ID = "204", position = 4 });
            tempList.Add(new RequirementHeirachy { ID = "205", position = 5 });
            return tempList;
        }

        public List<Project> GetProjectList()
        {
            List<Project> tempList = new List<Project>();
            Project proj = new Project();

            using (var sc = new SQLCommand("RMsisDB"))
            {
                sc.CommandText.AppendFormat("SELECT * from {0}",
                               sc.WrapObjectName("Project"));
                using (var dr = sc.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Object[] val = new Object[dr.FieldCount];
                        dr.GetValues(val);
                        proj = new Project();
                        proj.ID = dr.GetValue(RMSIS_PROJECT_ID_LOCATION).ToString();
                        proj.Name = dr.GetValue(RMSIS_PROJECT_NAME_LOCATION).ToString();
                        tempList.Add(proj);
                    }
                }
            }
            return tempList;
        }

        public List<string> GetBaselineList(Project proj)
        {
            List<string> tempList = new List<string>();
            tempList.Add("Baseline 1");
            tempList.Add("Baseline 2");
            tempList.Add("Baseline 3");
            return tempList;
        }
    }
}
