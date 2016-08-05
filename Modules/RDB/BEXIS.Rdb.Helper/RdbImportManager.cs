using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
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
        public List<TmpBoundingBox> TmpBoundingBoxes;
        public List<TmpMeasurementHeight> TmpMeasurementHeights;

        public RdbImportManager()
        {
            reader = new RdbCsvReader();
        }

        public void Load()
        {

            RdbCsvReader reader = new RdbCsvReader();

            //load emp bounding boxes list
            TmpBoundingBoxes = reader.ReadBoundingBoxesCsv();

            //read extra measurementHeights
            TmpMeasurementHeights = reader.ReadMeasurmentHeightCsv();

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
            XDocument defaultmetadata = writer.CreateMetadataXml(metadataStructure.Id);
            #endregion

            for (int i = 0; i < Trees.Count; i++)
            {
                XDocument metadata = new XDocument(defaultmetadata);
      
                createDsFromTree(Trees.ElementAt(i), metadata, unStructuredDataStructure, metadataStructure);
            }
        }

        private void createDsFromTree(Tree tree, XDocument metadata, UnStructuredDataStructure unStructuredDataStructure, MetadataStructure metadataStructure)
        {

                string destinationXPath = "";
                string sampleName = tree.ShortName;

                #region base

                            Person contributer = Persons.Where(p => p.Id.Equals(Convert.ToInt64(tree.Contributor))).FirstOrDefault();

                            if (contributer != null)
                            {
                                //contact
                                //Metadata/Base/BaseType/Contact/personType/Name/NameType
                                destinationXPath = "Metadata/Base/BaseType/Contact/personType/Name/NameType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = contributer.First_Name + " " +
                                                                                                  contributer.Last_Name;

                                //Metadata/Base/BaseType/Contact/personType/Email/EmailType
                                destinationXPath = "Metadata/Base/BaseType/Contact/personType/Email/EmailType";
                                            XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = contributer.EMail;

                                //Metadata/Base/BaseType/Contact/personType/Phone/PhoneType
                                destinationXPath = "Metadata/Base/BaseType/Contact/personType/Phone/PhoneType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = contributer.Telephone;

                                //Owner
                                //Metadata/Base/BaseType/Owner/personType/Name/NameType
                                destinationXPath = "Metadata/Base/BaseType/Owner/personType/Name/NameType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = contributer.First_Name + " " +
                                                                                                  contributer.Last_Name;

                                //Metadata/Base/BaseType/Owner/personType/Email/EmailType
                                destinationXPath = "Metadata/Base/BaseType/Owner/personType/Email/EmailType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = contributer.EMail;

                                //Metadata/Base/BaseType/Owner/personType/Phone/PhoneType
                                destinationXPath = "Metadata/Base/BaseType/Owner/personType/Phone/PhoneType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = contributer.Telephone;
                            }


                #endregion

                #region organisation

                #endregion

                #region Insitute

                #endregion

                #region Location

                Plot plot = Plots.Where(p => p.Trees.Contains(tree.RefId)).FirstOrDefault();
                if (plot != null)
                {
                    // boundingbox of the Plot
                    TmpBoundingBox plotBB = TmpBoundingBoxes.Where(b => b.Id.Equals(plot.RefId)).FirstOrDefault();
                    
                    //plot
                    destinationXPath = "Metadata/Location/LocationType/Plot/plotType/Name/NameType";
                    XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = plot.Name;

                    //plot abbr
                    destinationXPath = "Metadata/Location/LocationType/Plot/plotType/Abbrevation/AbbrevationType";
                    XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = plot.ShortName;

                    //plot GroundwaterDepth
                    destinationXPath = "Metadata/Location/LocationType/Plot/plotType/GroundwaterDepth/GroundwaterDepthType";
                    XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = plot.GroundwaterDepth;

                    //plot Vegetation
                    destinationXPath = "Metadata/Location/LocationType/Plot/plotType/Vegetation/VegetationType";
                    XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = plot.Vegetation;

                    //coordinates - longtitude
                    destinationXPath = "Metadata/Location/LocationType/Plot/plotType/Coordinates/coordinatesType/Longtitude/LongtitudeType";
                    XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = plot.Coordinates.Longitude;

                    //coordinates - laditude
                    destinationXPath = "Metadata/Location/LocationType/Plot/plotType/Coordinates/coordinatesType/Laditude/LaditudeType";
                    XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = plot.Coordinates.Latitude;

                    if (plotBB != null)
                    {
                        //boundingbox - east long
                        destinationXPath = "Metadata/Location/LocationType/Plot/plotType/BoundingBox/boundingBoxType/EasternLongtiude/EasternLongtiudeType";
                        XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = plotBB.EastLongtitude;

                        //boundingbox - west long
                        destinationXPath = "Metadata/Location/LocationType/Plot/plotType/BoundingBox/boundingBoxType/WesternLongtiude/WesternLongtiudeType";
                        XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = plotBB.WestLongtitude;

                        //boundingbox - north ladtitude
                        destinationXPath = "Metadata/Location/LocationType/Plot/plotType/BoundingBox/boundingBoxType/NothernLaditude/NothernLaditudeType";
                        XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = plotBB.NorthLatitude;

                        //boundingbox - south ladtitude
                        destinationXPath = "Metadata/Location/LocationType/Plot/plotType/BoundingBox/boundingBoxType/SouthernLaditude/SouthernLaditudeType";
                        XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = plotBB.SouthLatitude;
                    }

                    Site site = Sites.Where(p => p.Plots.Contains(plot.RefId)).FirstOrDefault();

                    if (site != null)
                    {
                        TmpBoundingBox siteBB = TmpBoundingBoxes.Where(b => b.Id.Equals(site.RefId)).FirstOrDefault();
                        //site
                        destinationXPath =
                            "Metadata/Location/LocationType/Site/siteType/Name/NameType";
                        XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = site.Name;

                        destinationXPath =
                            "Metadata/Location/LocationType/Site/siteType/Abbrevation/AbbrevationType";
                        XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = site.ShortName;

                        if (siteBB != null)
                        {
                            //boundingbox - east long
                            destinationXPath = "Metadata/Location/LocationType/Site/siteType/BoundingBox/boundingBoxType/EasternLongtiude/EasternLongtiudeType";
                            XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = siteBB.EastLongtitude;

                            //boundingbox - west long
                            destinationXPath = "Metadata/Location/LocationType/Site/siteType/BoundingBox/boundingBoxType/WesternLongtiude/WesternLongtiudeType";
                            XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = siteBB.WestLongtitude;

                            //boundingbox - north ladtitude
                            destinationXPath = "Metadata/Location/LocationType/Site/siteType/BoundingBox/boundingBoxType/NothernLaditude/NothernLaditudeType";
                            XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = siteBB.NorthLatitude;

                            //boundingbox - south ladtitude
                            destinationXPath = "Metadata/Location/LocationType/Site/siteType/BoundingBox/boundingBoxType/SouthernLaditude/SouthernLaditudeType";
                            XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = siteBB.SouthLatitude;
                        }
                    }
                }




                //site
                //"Metadata/researchObjects/researchObjectsType/location/locationType/site/siteType"

                #endregion

                #region Tree Infos

                //SampleId
                destinationXPath = "Metadata/Tree/TreeType/Description/DescriptionType";
                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = "ID from old Database: " +tree.Id.ToString();

                //tree name
                destinationXPath = "Metadata/Tree/TreeType/Name/NameType";
                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = tree.ShortName;

            //tree TreeSpecies
            destinationXPath = "Metadata/Tree/TreeType/TreeSpecies/TreeSpeciesType";
            XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = tree.Treespecies;
            //tree Description
            destinationXPath = "Metadata/Tree/TreeType/Description/DescriptionType";
            //XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = "";
            //tree Age
            destinationXPath = "Metadata/Tree/TreeType/Age/AgeType";
            XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = tree.Age;
            //tree Volume
            destinationXPath = "Metadata/Tree/TreeType/Volume/VolumeType";
            XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = tree.Volume;
            //tree VolumeWithoutBark
            destinationXPath = "Metadata/Tree/TreeType/VolumeWithoutBark/VolumeWithoutBarkType";
            XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = tree.Volumewithoutbark;
            //tree Height
            destinationXPath = "Metadata/Tree/TreeType/Height/HeightType";
            XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = tree.Height;
            //tree MassPerTree
            destinationXPath = "Metadata/Tree/TreeType/MassPerTree/MassPerTreeType";
            XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = tree.MassperTree;
            //tree Firescars
            destinationXPath = "Metadata/Tree/TreeType/FireScars/FireScarsType";
            XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = tree.FireScars;
            //tree Diameter XXX foreach loop

            if (tree.Diameters.Count > 1)
            {
                for(int i=1;i<tree.Diameters.Count;i++)
                {
                    XElement tmp = XmlUtility.GetXElementByXPath("Metadata/Tree/TreeType/Diameter/diameterType", metadata);
                    XElement newTmp = tmp;
                    tmp.AddAfterSelf(newTmp);
                }

                XElement parent = XmlUtility.GetXElementByXPath("Metadata/Tree/TreeType/Diameter", metadata);
                for (int i = 1; i <= XmlUtility.GetChildren(parent).Count(); i++)
                {
                    XElement tmp = XmlUtility.GetXElementByXPath("Metadata/Tree/TreeType/Diameter/diameterType["+i+"]", metadata);
                    tmp.SetAttributeValue("number", i);
                }
            }

            for (int i = 0; i < tree.Diameters.Count; i++)
            {
                DiameterClass d = tree.Diameters.ElementAt(i);
                int index = i + 1;
                //tree MeasurementHeight
                destinationXPath =
                    "Metadata/Tree/TreeType/Diameter/diameterType["+ index + "]/MeasurementHeight/MeasurementHeightType";
                var tmpMeasurementHeight = TmpMeasurementHeights.Where(m=>m.ParentId.Equals(d.Id)).FirstOrDefault();
                if (tmpMeasurementHeight != null)
                    XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = tmpMeasurementHeight.Value;

                //tree MeasurementHeight
                destinationXPath = "Metadata/Tree/TreeType/Diameter/diameterType[" + index + "]/ Diameter/DiameterType";
                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = d.Diameter;
            }

            #endregion

                #region stem slices

            if (tree.TreeStemSlices.Count > 1)
            {
                for (int i = 1; i < tree.TreeStemSlices.Count; i++)
                {
                    XElement tmp = XmlUtility.GetXElementByXPath("Metadata/Compartment/CompartmentType/StemSlice/stemSliceType", metadata);
                    XElement newTmp = tmp;
                    tmp.AddAfterSelf(newTmp);
                }

                XElement parent = XmlUtility.GetXElementByXPath("Metadata/Compartment/CompartmentType/StemSlice", metadata);
                for (int i = 1; i <= XmlUtility.GetChildren(parent).Count(); i++)
                {
                    XElement tmp = XmlUtility.GetXElementByXPath("Metadata/Compartment/CompartmentType/StemSlice/stemSliceType[" + i + "]", metadata);
                    tmp.SetAttributeValue("number", i);
                }
            }

            //sort stemslices
            //tree.TreeStemSlices = tree.TreeStemSlices.OrderBy(t=> Convert.ToInt32(t.Treestemsegment)).ToList();

            for (int i = 0; i < tree.TreeStemSlices.Count; i++)
            {
                TreeStemSlice treeStemSlice = tree.TreeStemSlices.ElementAt(i);
                int index = i + 1;
                //tree StemSlice postion
                destinationXPath =
                    "Metadata/Compartment/CompartmentType/StemSlice/stemSliceType["+index+"]/Position/PositionType";

                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = treeStemSlice.Treestemsegment;

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
