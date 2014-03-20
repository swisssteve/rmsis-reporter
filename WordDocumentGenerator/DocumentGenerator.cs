using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DataTypes;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WordDocumentGenerator
{
    public class DocumentGenerator 
    {    
        // Struct to hold the return value and possible exception        
        public struct RETURN_VAL {
            public bool Value;
            public string Exception;
        }

        private const string FieldDelimeter = " MERGEFIELD ";

        private string _templateFileName;
        private string _targetFileName;
        private List<Requirement> _requirements;

        /// <summary>
        /// Constructor for the DocumentGeneration Class
        /// </summary>
        /// <param name="templateFileName">File to base the document off of</param>
        /// <param name="targetFileName">End File to write the Mail Merge to</param>
        public DocumentGenerator(string templateFileName, string targetFileName, List<Requirement> reqs) 
        {
            _templateFileName = templateFileName;
            _targetFileName = targetFileName;
            _requirements = reqs;
        }

        /// <summary>
        /// Gets the Mail Merge Value
        /// </summary>
        /// <param name="FieldName">Field Name from the Word Template/Document</param>
        /// <returns>The Mail Merge value, but if the Field could not be found, throw an exception</returns>
        private string GetMergeValue(string FieldName) {
            switch (FieldName) {
                case "Requirement":
                    {
                        StringBuilder builder = new StringBuilder();
                        foreach (Requirement req in _requirements)
                        {
                            builder.AppendLine(req.Description);
                        }
                        return builder.ToString();
                    }
                default:
                    throw new Exception(message: "FieldName (" + FieldName + ") was not found");
            }
        }

        /// <summary>
        /// Generates the Word Document and performs the Mail Merge
        /// </summary>
        /// <returns>True or false(with exception) if the generation was successful or not</returns>
        public string GenerateDocument() 
        {
            try {
                // Don't continue if the template file name is not found
                if (!File.Exists(_templateFileName)) {
                    throw new Exception(message: "TemplateFileName (" + _templateFileName + ") does not exist");
                }

                // Make a copy of the Word Document to the targetFileName
                if(File.Exists(_targetFileName))
                {
                     File.Delete(_targetFileName);
                }
                File.Copy(_templateFileName, _targetFileName);
               
                using (WordprocessingDocument docGenerated = WordprocessingDocument.Open(_targetFileName, true)) {
                    docGenerated.ChangeDocumentType(WordprocessingDocumentType.Document);

                    foreach (FieldCode field in docGenerated.MainDocumentPart.RootElement.Descendants<FieldCode>()) 
                    {
                        try
                        {
                            var fieldNameStart = field.Text.LastIndexOf(FieldDelimeter, System.StringComparison.Ordinal);
                            var fieldname = field.Text.Substring(fieldNameStart + FieldDelimeter.Length).Trim();
                            var fieldValue = GetMergeValue(FieldName: fieldname);

                            // Go through all of the Run elements and replace the Text Elements Text Property
                            foreach (Run run in docGenerated.MainDocumentPart.Document.Descendants<Run>())
                            {
                                foreach (Text txtFromRun in run.Descendants<Text>().Where(a => a.Text == "«" + fieldname + "»"))
                                {
                                    txtFromRun.Text = fieldValue;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            int a = 1;
                        }
                    }

                    // If the Document has settings remove them so the end user doesn't get prompted to use the data source
                    DocumentSettingsPart settingsPart = docGenerated.MainDocumentPart.GetPartsOfType<DocumentSettingsPart>().First();

                    var oxeSettings = settingsPart.Settings.Where(a => a.LocalName == "mailMerge").FirstOrDefault();

                    if (oxeSettings != null) {
                        settingsPart.Settings.RemoveChild(oxeSettings);

                        settingsPart.Settings.Save();
                    }

                    docGenerated.MainDocumentPart.Document.Save();
                }

                FileInfo fi = new FileInfo(_targetFileName);
                return fi.FullName;
   
            } catch (Exception ex) {
                return null;
            }
        }
    }
}

