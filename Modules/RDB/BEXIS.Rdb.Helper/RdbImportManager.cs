using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public List<Soil> Soils;
        public List<Plot> Plots;
        public List<Project> Projects;
        public List<Site> Sites;
        public List<Person> Persons;
        public List<TmpBoundingBox> TmpBoundingBoxes;
        public List<TmpMeasurementHeight> TmpMeasurementHeights;
        public List<TmpSampleId> TmpSampleIds;

        public RdbImportManager()
        {
            reader = new RdbCsvReader();
        }

        public void Load()
        {
            try
            {
                RdbCsvReader reader = new RdbCsvReader();

                //load emp bounding boxes list
                TmpBoundingBoxes = reader.ReadBoundingBoxesCsv();

                //read extra measurementHeights
                TmpMeasurementHeights = reader.ReadMeasurmentHeightCsv();

                //sampleids
                TmpSampleIds = reader.ReadSampleIds();

                //sites
                Sites = reader.ReadSiteCsv();

                //person
                Persons = reader.ReadPersonCsv();

                //plots
                Plots = reader.ReadPlotCsv();

                //projects
                Projects = reader.ReadProjectCsv();

                //soils

                Soils = reader.ReadSoilCsv();

                //trees

                Trees = reader.ReadTreeCsv();

                //SetStemSliceBarcode
                foreach (Tree t in Trees)
                {
                    foreach (var treestemslice in t.TreeStemSlices)
                    {
                        treestemslice.Barcode = GetBarcodeForStemSlice(treestemslice.ParentId).ToString();
                    }
                }

            }
            catch (Exception exception)
            {
                throw exception;
            }
            
        }

        public void ConvertAll()
        {
            //ConvertTreesToADataset();
            ConvertSoilsToADataset();
        }

        public void ConvertTreesToADataset()
        {
            #region tree
            //create a temp unstructed datastructure
            DataStructureManager dsm = new DataStructureManager();
            UnStructuredDataStructure unStructuredDataStructure = dsm.CreateUnStructuredDataStructure("TreeSampleFiles", "...");

            long metadataStructureId = 3;

            MetadataStructureManager msm = new MetadataStructureManager();
            MetadataStructure metadataStructure = msm.Repo.Get().Where(m => m.Name.Equals("Tree")).FirstOrDefault();

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

                //Metadata/Tree/TreeType/Barcode/BarcodeType
                destinationXPath = "Metadata/Tree/TreeType/Barcode/BarcodeType";
                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = tree.Id.ToString();

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

                //id
                destinationXPath =
                    "Metadata/Compartment/CompartmentType/StemSlice/stemSliceType[" + index + "]/Barcode/BarcodeType";
                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = treeStemSlice.Barcode;
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

                    string title = XmlDatasetHelper.GetInformation(workingCopy, NameAttributeValues.title);

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


        public void ConvertSoilsToADataset()
        {
            #region soil
            //create a temp unstructed datastructure
            DataStructureManager dsm = new DataStructureManager();
            UnStructuredDataStructure unStructuredDataStructure = dsm.CreateUnStructuredDataStructure("SoilSampleFiles", "..."
                );

            long metadataStructureId = 3;

            MetadataStructureManager msm = new MetadataStructureManager();
            MetadataStructure metadataStructure = msm.Repo.Get().Where(m => m.Name.Equals("Soil")).FirstOrDefault();

            XmlMetadataWriter writer = new XmlMetadataWriter(XmlNodeMode.xPath);
            XDocument defaultmetadata = writer.CreateMetadataXml(metadataStructure.Id);
            #endregion

            for (int i = 0; i < Soils.Count; i++)
            {
                XDocument metadata = new XDocument(defaultmetadata);

                createDsFromSoil(Soils.ElementAt(i), metadata, unStructuredDataStructure, metadataStructure);
            }
        }

        private void createDsFromSoil(Soil soil, XDocument metadata, UnStructuredDataStructure unStructuredDataStructure, MetadataStructure metadataStructure)
        {
            Debug.WriteLine("-----------------------------------------------");
            Debug.WriteLine("start create soil");

            string destinationXPath = "";
            string sampleName = soil.Name;

            #region base

            //Person contributer = Persons.Where(p => p.Id.Equals(Convert.ToInt64(Soil.Contributor))).FirstOrDefault();

            //if (contributer != null)
            //{
            //    //contact
            //    //Metadata/Base/BaseType/Contact/personType/Name/NameType
            //    destinationXPath = "Metadata/Base/BaseType/Contact/personType/Name/NameType";
            //    XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = contributer.First_Name + " " +
            //                                                                      contributer.Last_Name;

            //    //Metadata/Base/BaseType/Contact/personType/Email/EmailType
            //    destinationXPath = "Metadata/Base/BaseType/Contact/personType/Email/EmailType";
            //    XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = contributer.EMail;

            //    //Metadata/Base/BaseType/Contact/personType/Phone/PhoneType
            //    destinationXPath = "Metadata/Base/BaseType/Contact/personType/Phone/PhoneType";
            //    XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = contributer.Telephone;

            //    //Owner
            //    //Metadata/Base/BaseType/Owner/personType/Name/NameType
            //    destinationXPath = "Metadata/Base/BaseType/Owner/personType/Name/NameType";
            //    XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = contributer.First_Name + " " +
            //                                                                      contributer.Last_Name;

            //    //Metadata/Base/BaseType/Owner/personType/Email/EmailType
            //    destinationXPath = "Metadata/Base/BaseType/Owner/personType/Email/EmailType";
            //    XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = contributer.EMail;

            //    //Metadata/Base/BaseType/Owner/personType/Phone/PhoneType
            //    destinationXPath = "Metadata/Base/BaseType/Owner/personType/Phone/PhoneType";
            //    XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = contributer.Telephone;
            //}


            #endregion

            #region organisation

            #endregion

            #region Insitute

            #endregion

            #region Location

            Plot plot = null;
            if (soil.Profil == null && soil.Bohrer == null)
            {
                plot = Plots.Where(p => p.Soils.Contains(soil.RefId)).FirstOrDefault();
            }
            else
            {
                if (soil.Profil != null)
                {
                    plot = Plots.Where(p => p.Plots.Contains(soil.Profil.RefId)).FirstOrDefault();
                }
                else if (soil.Bohrer != null)
                {
                    plot = Plots.Where(p => p.Plots.Contains(soil.Bohrer.RefId)).FirstOrDefault();
                }

            }

            
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

            Debug.WriteLine("start Soil Infos");

            #region Soil Infos
            //Soil id
            destinationXPath = "Metadata/Soil/SoilType/Barcode/BarcodeType";
            XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = soil.Id.ToString();

            destinationXPath = "Metadata/Soil/SoilType/Description/DescriptionType";
            XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = "ID from old Database: " + soil.Id.ToString();

            //Soil name
            destinationXPath = "Metadata/Soil/SoilType/Name/NameType";
            XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = soil.Name;

            //Soil Vegetation
            destinationXPath = "Metadata/Soil/SoilType/Vegetation/VegetationType";
            XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = soil.Vegetation;

            //Soil TotalDepth
            destinationXPath = "Metadata/Soil/SoilType/TotalDepth/TotalDepthType";
            XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = soil.TotalDepth.ToString();

            //Soil PitSize
            destinationXPath = "Metadata/Soil/SoilType/PitSize/PitSizeType";
            XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = soil.PitSize.ToString();


            #region Soil Profil
            if (soil.Profil != null)
            {
                Debug.WriteLine("start Profil Infos");

                //Soil profil id
                destinationXPath = "Metadata/HowTo/HowToType/Profil/profileType/Barcode/BarcodeType";
                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = soil.Profil.Id.ToString();

                //Soil profil name
                destinationXPath = "Metadata/HowTo/HowToType/Profil/profileType/Name/NameType";
                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = soil.Profil.ShortName;

                //Soil profil total depth
                destinationXPath = "Metadata/HowTo/HowToType/Profil/profileType/TotalDepth/TotalDepthType";
                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = soil.Profil.TotalDepth.ToString();

                //soilunderclass
                if (soil.Profil.Soils.Count > 0)
                {
                    Debug.WriteLine("start soilunderclass");

                    SoilUnderClass suc = soil.Profil.Soils.Where(s => s.Id.Equals(soil.Id)).FirstOrDefault();
                    Debug.WriteLine("start soilunderclass ");

                    if (suc != null)
                    {

                        SoilUnderClass soilunderclass = suc;
                        int index = 1;
                        //soil Barcode
                        destinationXPath = "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                           "]/Barcode/BarcodeType";
                        XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = soilunderclass.Id.ToString();
                        //soil name
                        destinationXPath = "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                           "]/Name/NameType";
                        XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = soilunderclass.Name;

                        //soil sampletype
                        destinationXPath = "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                           "]/SampleType/SampleTypeType";
                        XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = soil.SoilType;

                        //soil sampleDate
                        destinationXPath = "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                           "]/SampleDate/SampleDateType";
                        XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = soil.SamplingDate;

                        #region mineralsoils

                        if (soilunderclass.MineralSoils.Count > 0)
                        {
                            Debug.WriteLine("start mineralsoil");

                            for (int j = 1; j < soilunderclass.MineralSoils.Count; j++)
                            {
                                XElement tmp =
                                    XmlUtility.GetXElementByXPath(
                                        "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                        "]/MineralSoil/mineralSoilType",
                                        metadata);
                                XElement newTmp = tmp;
                                tmp.AddAfterSelf(newTmp);
                            }

                            XElement parentMS =
                                XmlUtility.GetXElementByXPath(
                                    "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                    "]/MineralSoil", metadata);
                            for (int j = 1; j <= XmlUtility.GetChildren(parentMS).Count(); j++)
                            {
                                XElement tmp =
                                    XmlUtility.GetXElementByXPath(
                                        "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                        "]/MineralSoil/mineralSoilType[" +
                                        j + "]", metadata);
                                tmp.SetAttributeValue("number", j);
                            }

                            for (int j = 0; j < soilunderclass.MineralSoils.Count; j++)
                            {
                                MineralSoil mineralsoil = soilunderclass.MineralSoils.ElementAt(j);
                                int indexMS = j + 1;
                                //mineralsoil Barcode
                                destinationXPath =
                                    "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                    "]/MineralSoil/mineralSoilType[" +
                                    indexMS + "]/Barcode/BarcodeType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                    mineralsoil.Id.ToString();
                                //mineralsoil name
                                destinationXPath =
                                    "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                    "]/MineralSoil/mineralSoilType[" +
                                    indexMS + "]/Name/NameType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = mineralsoil.Name;
                                //mineralsoil DepthInterval min
                                destinationXPath =
                                    "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                    "]/MineralSoil/mineralSoilType[" +
                                    indexMS + "]/DepthRange/depthRangeType/Min/MinType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                    mineralsoil.DepthRange.Min.ToString();
                                //mineralsoil DepthInterval max
                                destinationXPath =
                                    "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                    "]/MineralSoil/mineralSoilType[" +
                                    indexMS + "]/DepthInterval/DepthIntervalType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                    mineralsoil.DepthInterval.ToString();

                                #region layer

                                if (mineralsoil.Layers.Count > 0)
                                {
                                    for (int k = 1; k < mineralsoil.Layers.Count; k++)
                                    {
                                        Debug.WriteLine("start layer");

                                        XElement tmp =
                                            XmlUtility.GetXElementByXPath(
                                                "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                                "]/MineralSoil/mineralSoilType[" + indexMS + "]/Layer/mineralLayerType",
                                                metadata);
                                        XElement newTmp = tmp;
                                        tmp.AddAfterSelf(newTmp);
                                    }

                                    XElement parentLa =
                                        XmlUtility.GetXElementByXPath(
                                            "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                            "]/MineralSoil/mineralSoilType[" + indexMS + "]/Layer",
                                            metadata);
                                    for (int k = 1; k <= XmlUtility.GetChildren(parentLa).Count(); k++)
                                    {
                                        XElement tmp =
                                            XmlUtility.GetXElementByXPath(
                                                "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                                "]/MineralSoil/mineralSoilType[" + indexMS + "]/Layer/mineralLayerType[" +
                                                k + "]", metadata);
                                        tmp.SetAttributeValue("number", k);
                                    }

                                    for (int k = 0; k < mineralsoil.Layers.Count; k++)
                                    {
                                        Layer layer = mineralsoil.Layers.ElementAt(k);
                                        int indexLa = k + 1;
                                        //layer Barcode
                                        destinationXPath =
                                            "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                            "]/MineralSoil/mineralSoilType[" + indexMS + "]/Layer/mineralLayerType[" +
                                            indexLa + "]/Barcode/BarcodeType";
                                        XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                            layer.Id.ToString();
                                        //layer horizon
                                        destinationXPath =
                                            "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                            "]/MineralSoil/mineralSoilType[" + indexMS + "]/Layer/mineralLayerType[" +
                                            indexLa + "]/Horizon/HorizonType";
                                        XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = layer.Horizon;

                                        //layer Volume
                                        destinationXPath =
                                            "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                            "]/MineralSoil/mineralSoilType[" + indexMS + "]/Layer/mineralLayerType[" +
                                            indexLa + "]/Volume/VolumeType";
                                        XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                            layer.Volume.ToString();
                                    }
                                }


                                #endregion
                            }
                        }

                        #endregion

                        #region organic layer

                        if (soilunderclass.OrganicLayers.Count > 0)
                        {
                            Debug.WriteLine("start organic layer");

                            for (int j = 1; j < soilunderclass.OrganicLayers.Count; j++)
                            {
                                XElement tmp =
                                    XmlUtility.GetXElementByXPath(
                                        "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                        "]/OrganicLayer/organicLayerType",
                                        metadata);
                                XElement newTmp = tmp;
                                tmp.AddAfterSelf(newTmp);
                            }

                            XElement parentMS =
                                XmlUtility.GetXElementByXPath(
                                    "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                    "]/OrganicLayer", metadata);
                            for (int j = 1; j <= XmlUtility.GetChildren(parentMS).Count(); j++)
                            {
                                XElement tmp =
                                    XmlUtility.GetXElementByXPath(
                                        "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                        "]/OrganicLayer/organicLayerType[" +
                                        j + "]", metadata);
                                tmp.SetAttributeValue("number", j);
                            }

                            for (int j = 0; j < soilunderclass.OrganicLayers.Count; j++)
                            {
                                OrganicLayer organicLayer = soilunderclass.OrganicLayers.ElementAt(j);
                                int indexOL = j + 1;
                                //organiclayer Barcode
                                destinationXPath =
                                    "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                    "]/OrganicLayer/organicLayerType[" +
                                    indexOL + "]/Barcode/BarcodeType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                    organicLayer.Id.ToString();
                                //organiclayer name
                                destinationXPath =
                                    "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                    "]/OrganicLayer/organicLayerType[" +
                                    indexOL + "]/Name/NameType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = organicLayer.Name;

                                //organiclayer DepthRange min
                                destinationXPath =
                                    "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                    "]/OrganicLayer/organicLayerType[" +
                                    indexOL + "]/DepthRange/depthRangeType/Min/MinType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                    organicLayer.DepthRange.Min.ToString();
                                //organiclayer DepthRange max
                                destinationXPath =
                                    "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                    "]/OrganicLayer/organicLayerType[" +
                                    indexOL + "]/DepthRange/depthRangeType/Max/MaxType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                    organicLayer.DepthRange.Max.ToString();

                                //organiclayer DepthInterval mx
                                destinationXPath =
                                    "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                    "]/OrganicLayer/organicLayerType[" +
                                    indexOL + "]/DepthInterval/DepthIntervalType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                    organicLayer.DepthRange.Max.ToString();

                                //organiclayer density
                                destinationXPath =
                                    "Metadata/HowTo/HowToType/Profil/profileType/Soil/soilType2[" + index +
                                    "]/OrganicLayer/organicLayerType[" +
                                    indexOL + "]/Density/DensityType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                    organicLayer.Density.ToString();
                            }


                        }

                        #endregion
                    }
                }

            }
            #endregion

            #region Soil Bohrer
            if (soil.Bohrer != null)
            {
                Debug.WriteLine("start borher layer");

                //Soil profil id
                destinationXPath = "Metadata/HowTo/HowToType/Bohrer/bohrerType/Barcode/BarcodeType";
                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = soil.Bohrer.Id.ToString();

                //Soil profil name
                destinationXPath = "Metadata/HowTo/HowToType/Bohrer/bohrerType/Name/NameType";
                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = soil.Bohrer.ShortName;

                //Soil profil total depth / length
                destinationXPath = "Metadata/HowTo/HowToType/Bohrer/bohrerType/Length/LengthType";
                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = soil.Bohrer.TotalDepth.ToString();

                ////Soil profil total depth
                //destinationXPath = "Metadata/HowTo/HowToType/Profil/profileType/TotalDepth/TotalDepthType";
                //XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = soil.Profil.TotalDepth.ToString();

                //get soilunderclass
                if (soil.Bohrer.Soils.Count > 0)
                {
                    SoilUnderClass suc = soil.Bohrer.Soils.Where(s => s.Id.Equals(soil.Id)).FirstOrDefault();
                    Debug.WriteLine("start soilunderclass ");

                    if (suc != null)
                    {
                        SoilUnderClass soilunderclass = suc;
                        int index = 1;
                        //soil Barcode
                        destinationXPath = "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                            "]/Barcode/BarcodeType";
                        XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                            soilunderclass.Id.ToString();
                        //soil name
                        destinationXPath = "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                            "]/Name/NameType";
                        XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = soilunderclass.Name;

                        //soil sampletype
                        destinationXPath = "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                            "]/SampleType/SampleTypeType";
                        XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = soil.SoilType;

                        //soil sampleDate
                        destinationXPath = "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                            "]/SampleDate/SampleDateType";
                        XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = soil.SamplingDate;

                        #region mineralsoils

                        if (soilunderclass.MineralSoils.Count > 0)
                        {
                            Debug.WriteLine("start MineralSoils ");

                            for (int j = 1; j < soilunderclass.MineralSoils.Count; j++)
                            {
                                XElement tmp =
                                    XmlUtility.GetXElementByXPath(
                                        "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                        "]/MineralSoil/mineralSoilType",
                                        metadata);
                                XElement newTmp = tmp;
                                tmp.AddAfterSelf(newTmp);
                            }

                            XElement parentMS =
                                XmlUtility.GetXElementByXPath(
                                    "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                    "]/MineralSoil", metadata);
                            for (int j = 1; j <= XmlUtility.GetChildren(parentMS).Count(); j++)
                            {
                                XElement tmp =
                                    XmlUtility.GetXElementByXPath(
                                        "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                        "]/MineralSoil/mineralSoilType[" +
                                        j + "]", metadata);
                                tmp.SetAttributeValue("number", j);
                            }

                            for (int j = 0; j < soilunderclass.MineralSoils.Count; j++)
                            {
                                MineralSoil mineralsoil = soilunderclass.MineralSoils.ElementAt(j);
                                int indexMS = j + 1;
                                //mineralsoil Barcode
                                destinationXPath =
                                    "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                    "]/MineralSoil/mineralSoilType[" + indexMS + "]/Barcode/BarcodeType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                    mineralsoil.Id.ToString();
                                //mineralsoil name
                                destinationXPath =
                                    "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                    "]/MineralSoil/mineralSoilType[" + indexMS + "]/Name/NameType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = mineralsoil.Name;
                                //mineralsoil DepthInterval min
                                destinationXPath =
                                    "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                    "]/MineralSoil/mineralSoilType[" + indexMS +
                                    "]/DepthRange/depthRangeType/Min/MinType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                    mineralsoil.DepthRange.Min.ToString();
                                //mineralsoil DepthInterval max
                                destinationXPath =
                                    "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                    "]/MineralSoil/mineralSoilType[" + indexMS +
                                    "]/DepthRange/depthRangeType/Max/MaxType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                    mineralsoil.DepthRange.Max.ToString();

                                //organiclayer DepthInterval mx
                                destinationXPath =
                                    "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                    "]/MineralSoil/mineralSoilType[" +
                                    indexMS + "]/DepthInterval/DepthIntervalType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                    mineralsoil.DepthInterval.ToString();

                                #region layer

                                if (mineralsoil.Layers.Count > 0)
                                {
                                    for (int k = 1; k < mineralsoil.Layers.Count; k++)
                                    {
                                        Debug.WriteLine("start layers ");

                                        XElement tmp =
                                            XmlUtility.GetXElementByXPath(
                                                "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                                "]/MineralSoil/mineralSoilType/Layer/mineralLayerType",
                                                metadata);
                                        XElement newTmp = tmp;
                                        tmp.AddAfterSelf(newTmp);
                                    }

                                    XElement parentLa =
                                        XmlUtility.GetXElementByXPath(
                                            "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                            "]/MineralSoil/mineralSoilType/Layer",
                                            metadata);
                                    for (int k = 1; k <= XmlUtility.GetChildren(parentLa).Count(); k++)
                                    {
                                        XElement tmp =
                                            XmlUtility.GetXElementByXPath(
                                                "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                                "]/MineralSoil/mineralSoilType/Layer/mineralLayerType[" +
                                                k + "]", metadata);
                                        tmp.SetAttributeValue("number", k);
                                    }

                                    for (int k = 0; k < mineralsoil.Layers.Count; k++)
                                    {
                                        Layer layer = mineralsoil.Layers.ElementAt(k);
                                        int indexLa = k + 1;
                                        //layer Barcode
                                        destinationXPath =
                                            "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                            "]/MineralSoil/mineralSoilType/Layer/mineralLayerType[" +
                                            indexLa + "]/Barcode/BarcodeType";
                                        XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                            layer.Id.ToString();
                                        //layer horizon
                                        destinationXPath =
                                            "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                            "]/MineralSoil/mineralSoilType/Layer/mineralLayerType[" +
                                            indexLa + "]/Horizon/HorizonType";
                                        XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                            layer.Horizon;

                                        //layer Volume
                                        destinationXPath =
                                            "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                            "]/MineralSoil/mineralSoilType/Layer/mineralLayerType[" +
                                            indexLa + "]/Volume/VolumeType";
                                        XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                            layer.Volume.ToString();
                                    }
                                }


                                #endregion
                            }
                        }

                        #endregion

                        #region organic layer

                        if (soilunderclass.OrganicLayers.Count > 0)
                        {
                            Debug.WriteLine("start OrganicLayers ");

                            for (int j = 1; j < soilunderclass.OrganicLayers.Count; j++)
                            {
                                XElement tmp =
                                    XmlUtility.GetXElementByXPath(
                                        "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                        "]/OrganicLayer/organicLayerType",
                                        metadata);
                                XElement newTmp = tmp;
                                tmp.AddAfterSelf(newTmp);
                            }

                            XElement parentMS =
                                XmlUtility.GetXElementByXPath(
                                    "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                    "]/OrganicLayer", metadata);
                            for (int j = 1; j <= XmlUtility.GetChildren(parentMS).Count(); j++)
                            {
                                XElement tmp =
                                    XmlUtility.GetXElementByXPath(
                                        "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                        "]/OrganicLayer/organicLayerType[" +
                                        j + "]", metadata);
                                tmp.SetAttributeValue("number", j);
                            }

                            for (int j = 0; j < soilunderclass.OrganicLayers.Count; j++)
                            {
                                OrganicLayer organicLayer = soilunderclass.OrganicLayers.ElementAt(j);
                                int indexOL = j + 1;
                                //organiclayer Barcode
                                destinationXPath =
                                    "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                    "]/OrganicLayer/organicLayerType[" + indexOL + "]/Barcode/BarcodeType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                    organicLayer.Id.ToString();
                                //organiclayer name
                                destinationXPath =
                                    "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                    "]/OrganicLayer/organicLayerType[" + indexOL + "]/Name/NameType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value = organicLayer.Name;

                                //organiclayer DepthInterval min
                                destinationXPath =
                                    "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                    "]/OrganicLayer/organicLayerType[" + indexOL +
                                    "]/DepthRange/depthRangeType/Min/MinType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                    organicLayer.DepthRange.Min.ToString();
                                //organiclayer DepthInterval max
                                destinationXPath =
                                    "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                    "]/OrganicLayer/organicLayerType[" + indexOL +
                                    "]/DepthRange/depthRangeType/Max/MaxType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                    organicLayer.DepthRange.Max.ToString();

                                destinationXPath =
                                    "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                    "]/OrganicLayer/organicLayerType[" + indexOL +
                                    "]/DepthInterval/DepthIntervalType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                    organicLayer.DepthInterval.ToString();

                                //organiclayer density
                                destinationXPath =
                                    "Metadata/HowTo/HowToType/Bohrer/bohrerType/Soil/soilType2[" + index +
                                    "]/OrganicLayer/organicLayerType[" + indexOL + "]/Density/DensityType";
                                XmlUtility.GetXElementByXPath(destinationXPath, metadata).Value =
                                    organicLayer.Density.ToString();
                            }


                        }

                        #endregion
                        
                    }
                }
            }
            #endregion

            #endregion

            #region create dataset
            Debug.WriteLine("start create ");

            ResearchPlanManager researchPlanManager = new ResearchPlanManager();
            ResearchPlan researchPlan = researchPlanManager.Repo.Get(1);

            DatasetManager datasetManager = new DatasetManager();
            Dataset dataset = datasetManager.CreateEmptyDataset(unStructuredDataStructure, researchPlan,
                metadataStructure);

            Debug.WriteLine("add security ");

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
                Debug.WriteLine("IsDatasetCheckedOutFor ");

                DatasetVersion workingCopy = datasetManager.GetDatasetWorkingCopy(dataset.Id);

                workingCopy.Metadata = XmlMetadataWriter.ToXmlDocument(metadata);

                string title = XmlDatasetHelper.GetInformation(workingCopy, NameAttributeValues.title);

                Debug.WriteLine("store ");

                datasetManager.EditDatasetVersion(workingCopy, null, null, null);
                datasetManager.CheckInDataset(dataset.Id, "Metadata was submited.", GetUsernameOrDefault());
                Debug.WriteLine("stored ");

                ////add to index
                //// ToDo check which SearchProvider it is, default luceneprovider
                //ISearchProvider provider =
                //    IoCFactory.Container.ResolveForSession<ISearchProvider>() as ISearchProvider;
                //provider?.UpdateSingleDatasetIndex(dataset.Id, IndexingAction.CREATE);

            }

            Debug.WriteLine("FINISHeD ");
            Debug.WriteLine("******************* ");
            #endregion


        }


        #region helpers

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

        private long GetBarcodeForStemSlice(long id)
        {
            long tmp = -1;

            if (TmpSampleIds.Any(s => s.ParentId.Equals(id)))
            {
                tmp = Convert.ToInt64(
                    TmpSampleIds.Where(s => s.ParentId.Equals(id)).FirstOrDefault().Value);
            }

            return tmp;
        }

        #endregion

    }
}
