using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess;
using DataTypes;
using WordDocumentGenerator;

namespace BusinessLogic
{
    public class RMSISBusinessLogic
    {
        private ObservableCollection<Requirement> requirementData;
        private ObservableCollection<Project> projectData;
        private ObservableCollection<string> baselineData;

        public RMSISBusinessLogic()
        {
            var fred = ProjectData;
        }

        public string GenerateWordDocument(string wordTemplate, string BaselineID, string ProjectName, string projectLocation, int type)
        {

            //Get all Requirements for the project
            string projectID = GetProjectID(ProjectName);
            List<Requirement> requirements = OrderedRequirementsByProject(projectID);

            DocumentGenerator gen = new DocumentGenerator(wordTemplate, projectLocation, requirements);
            return gen.GenerateDocument();

        }

        public string GetProjectID(string ProjectName)
        {
            ObservableCollection<Project> projectData = ProjectData;
            string projectID = null;
 
            foreach (Project p in projectData)
            {
                if (p.Name.Equals(ProjectName))
                {
                    projectID = p.ID;
                    break;
                }
            }
            return projectID;
        }
  

        public ObservableCollection<Project> ProjectData
        {
            get
            {
                if (projectData == null)
                    projectData = GetRMSISProjects();
                return projectData;
            }
            set
            {
                if (value != projectData)
                    projectData = GetRMSISProjects();
            }
        }

        public ObservableCollection<Project> GetRMSISProjects()
        {
            DataConnector dc = new DataConnector();
            List<Project> project = dc.GetProjectList();

            if (projectData == null)
            {
                projectData = new ObservableCollection<Project>();
            }
            project.ForEach(x => projectData.Add(x));
            return projectData;
        }

        public ObservableCollection<string> GetBaselines(Project proj)
        {
            return null;
        }

        public ObservableCollection<string> GetRMSIS() 
        {
            return null;
        }

        public List<Requirement> OrderedRequirementsByProject(string projectID)
        {
            DataConnector dc = new DataConnector();
            return dc.GetRequirements(projectID);
        }

        public List<Requirement> OrderedRequirementsByProjectBaseline(string project, string baseline)
        {
            return null;
        }
    }
}
