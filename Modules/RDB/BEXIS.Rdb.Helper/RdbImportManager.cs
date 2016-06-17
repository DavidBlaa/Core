using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using BExIS.Dlm.Entities.Administration;
using BExIS.Dlm.Entities.Data;
using BExIS.Dlm.Entities.DataStructure;
using BExIS.Dlm.Entities.MetadataStructure;
using BExIS.Dlm.Services.Administration;
using BExIS.Dlm.Services.Data;
using BExIS.Dlm.Services.DataStructure;
using BExIS.Dlm.Services.MetadataStructure;
using BExIS.Security.Entities.Objects;
using BExIS.Security.Services.Authorization;
using BExIS.Security.Services.Subjects;
using BExIS.Xml.Helpers;
using BExIS.Xml.Services;
using BEXIS.Rdb.Entities;
using BExIS.Ddm.Api;
using Vaiona.IoC;

namespace BEXIS.Rdb.Helper
{
    public class RdbImportManager
    {
        public RdbCsvReader reader;

        public List<Tree> Trees;
        public List<Plot> Plots;
        public List<Project> Projects;
        public List<Site> Sites;
        public List<Person> Persons;

        public RdbImportManager()
        {
            reader = new RdbCsvReader();
        }

        public void Load()
        {
            RdbCsvReader reader = new RdbCsvReader();

            //sites
            Sites = reader.ReadSiteCsv();

            //person
            Persons = reader.ReadPersonCsv();

            //plots
            Plots = reader.ReadPlotCsv();

            //projects
            Projects = reader.ReadProjectCsv();

            //trees
            Trees = reader.ReadTreeCsv();
        }

        public void ConvertAll()
        {
            ConvertTreesToADataset();
        }

        public void ConvertTreesToADataset()
        {
            #region tree
            //create a temp unstructed datastructure
            DataStructureManager dsm = new DataStructureManager();
            UnStructuredDataStructure unStructuredDataStructure = dsm.CreateUnStructuredDataStructure("TreeSampleFiles", "..."
                );

            long metadataStructureId = 3;

            MetadataStructureManager msm = new MetadataStructureManager();
            MetadataStructure metadataStructure = msm.Repo.Get().Where(m => m.Id.Equals(metadataStructureId)).FirstOrDefault();

            XmlMetadataWriter writer = new XmlMetadataWriter(XmlNodeMode.xPath);
            XDocument metadata = writer.CreateMetadataXml(metadataStructure.Id);
            #endregion

            foreach (var tree in Trees)
            {
                createDsFromTreeStemSlices(tree, metadata, unStructuredDataStructure, metadataStructure);
            }
        }

        private void createDsFromTreeStemSlices(Tree tree, XDocument metadata, UnStructuredDataStructure unStructuredDataStructure, MetadataStructure metadataStructure)
        {

            foreach (var treestemslice in tree.TreeStemSlices)
            {

                string destinationXPath = "";
                string sampleName = tree.ShortName + " - Slice Segment " + treestemslice.Treestemsegment; 

                #region Sample Infos

                //SampleId
                //Metadata/general/generalType/sampleID/sampleIDType
                destinationXPath = "Metadata/general/generalType/sampleID/sampleIDType";
                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = tree.Id.ToString();

                //Samplename
                //Metadata/researchObjects/researchObjectsType/sampleName/sampleNameType
                destinationXPath = "Metadata/researchObjects/researchObjectsType/sampleName/sampleNameType";
                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = sampleName;

                #endregion

                #region Location

                //site
                //"Metadata/researchObjects/researchObjectsType/location/locationType/site/siteType"

                //plot
                //"Metadata/researchObjects/researchObjectsType/location/locationType/plot/plotType"

                //plot abbr
                //Metadata/researchObjects/researchObjectsType/location/locationType/abbrPlot/abbrPlotType


                #endregion

                #region contributer

                Person contributer = Persons.Where(p => p.Id.Equals(Convert.ToInt64(tree.Contributor))).FirstOrDefault();

                if (contributer != null)
                {
                    //owner
                    //Metadata/general/generalType/owners/ownerType/owner/ownerType
                    destinationXPath = "Metadata/general/generalType/owners/ownerType/owner/ownerType";
                    XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = contributer.First_Name + " " +
                                                                                      contributer.Second_Name + " " +
                                                                                      contributer.Last_Name;

                    //Contact name
                    //Metadata/general/generalType/contact/contactType/userName/userNameType
                    destinationXPath = "Metadata/general/generalType/contact/contactType/userName/userNameType";
                    XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = contributer.First_Name + " " +
                                                                                      contributer.Second_Name + " " +
                                                                                      contributer.Last_Name;
                    //contact group
                    //Metadata/general/generalType/contact/contactType/group/groupType

                    //contact project
                    //Metadata/general/generalType/contact/contactType/project/projectType

                    //contact institute
                    //Metadata/general/generalType/contact/contactType/institute/instituteType

                    //contact email
                    //Metadata/general/generalType/contact/contactType/email/emailType

                    //contact phone
                    //Metadata/general/generalType/contact/contactType/phone/phoneType


                }

                #endregion

                #region create dataset

                ResearchPlanManager researchPlanManager = new ResearchPlanManager();
                ResearchPlan researchPlan = researchPlanManager.Repo.Get(1);

                DatasetManager datasetManager = new DatasetManager();
                Dataset dataset = datasetManager.CreateEmptyDataset(unStructuredDataStructure, researchPlan,
                    metadataStructure);


                // add security
                if (GetUsernameOrDefault() != "DEFAULT")
                {
                    PermissionManager pm = new PermissionManager();
                    SubjectManager sm = new SubjectManager();

                    BExIS.Security.Entities.Subjects.User user = sm.GetUserByName(GetUsernameOrDefault());

                    foreach (RightType rightType in Enum.GetValues(typeof(RightType)).Cast<RightType>())
                    {
                        pm.CreateDataPermission(user.Id, 1, dataset.Id, rightType);
                    }
                }

                if (datasetManager.IsDatasetCheckedOutFor(dataset.Id, GetUsernameOrDefault()) ||
                    datasetManager.CheckOutDataset(dataset.Id, GetUsernameOrDefault()))
                {
                    DatasetVersion workingCopy = datasetManager.GetDatasetWorkingCopy(dataset.Id);

                    workingCopy.Metadata = XmlMetadataWriter.ToXmlDocument(metadata);

                    string title = XmlDatasetHelper.GetInformation(workingCopy, AttributeNames.title);

                    datasetManager.EditDatasetVersion(workingCopy, null, null, null);
                    datasetManager.CheckInDataset(dataset.Id, "Metadata was submited.", GetUsernameOrDefault());

                    ////add to index
                    //// ToDo check which SearchProvider it is, default luceneprovider
                    //ISearchProvider provider =
                    //    IoCFactory.Container.ResolveForSession<ISearchProvider>() as ISearchProvider;
                    //provider?.UpdateSingleDatasetIndex(dataset.Id, IndexingAction.CREATE);

                }


                #endregion

            }
        }

        public string GetUsernameOrDefault()
        {
            string username = string.Empty;
            try
            {
                username = HttpContext.Current.User.Identity.Name;
            }
            catch { }

            return !string.IsNullOrWhiteSpace(username) ? username : "DEFAULT";
        }

    }
}
