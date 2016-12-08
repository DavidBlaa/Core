using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using BExIS.Dlm.Entities.DataStructure;
using BExIS.IO;
using BExIS.IO.Transform.Input;
using BEXIS.Rdb.Entities;
using Vaiona.Utils.Cfg;

namespace BEXIS.Rdb.Helper
{
    public class RdbCsvReader
    {
        private string PROJECT_CSV = "projects.csv";
        private string SITE_CSV = "sites.csv";
        private string PLOT_CSV = "plots.csv";
        private string TREE_CSV = "0trees.csv";
        private string PERSON_CSV = "Person.csv";
        private string LOCATION_COORDIANTE_CSV = "locationsWithCoordinates2.csv";
        private string MEASUREMENT_HEIGHT_CSV = "MeasrurmentHeight_For_Csv.csv";
        private string SAMPLE_IDS_TXT = "sampleIds.txt"; 

        private string ALL = "QualifierHierarchy_Query_mod.csv";

        private string AREA = "RDB";

        private int ID_INDEX = 0;
        private int NAME_INDEX = 1;
        private int CATEGORY_INDEX = 2;
        private int VARID_INDEX = 3;
        private int VAR_CAT_INDEX = 4;
        private int VAR_NAME_INDEX = 5;
        private int VAR_VALUE_INDEX = 6;
        private int TYPE_ID_INDEX = 7;
        private int SUPERID_INDEX = 8;

        private List<CsvFileEntity> rowList;

        #region tmp object

        public List<TmpMeasurementHeight> ReadMeasurmentHeightCsv()
        {
            List<TmpMeasurementHeight> tmp = new List<TmpMeasurementHeight>();

            try
            {
                string path = Path.Combine(AppConfiguration.GetModuleWorkspacePath("RDB"), MEASUREMENT_HEIGHT_CSV);
                AsciiReader reader = new AsciiReader();

                if (File.Exists(path))
                {
                    FileStream stream = reader.Open(path);
                    AsciiFileReaderInfo afri = new AsciiFileReaderInfo();
                    afri.Seperator = TextSeperator.semicolon;
                    afri.Variables = 1;
                    afri.Data = 2;

                    List<List<string>> rowsOfBB = reader.ReadFile(stream, LOCATION_COORDIANTE_CSV, afri);

                    foreach (var bb in rowsOfBB)
                    {
                        tmp.Add(CreateMeasurementHeightFromCsvRows(bb));
                    }
                    

                }

                return tmp;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public List<TmpBoundingBox> ReadBoundingBoxesCsv()
        {
            List<TmpBoundingBox> tmp = new List<TmpBoundingBox>();

            try
            {
                string path = Path.Combine(AppConfiguration.GetModuleWorkspacePath("RDB"), LOCATION_COORDIANTE_CSV);
                AsciiReader reader = new AsciiReader();

                if (File.Exists(path))
                {
                    FileStream stream = reader.Open(path);
                    AsciiFileReaderInfo afri = new AsciiFileReaderInfo();
                    afri.Seperator = TextSeperator.semicolon;
                    afri.Variables = 1;
                    afri.Data = 2;

                    List<List<string>> rowsOfBB = reader.ReadFile(stream, LOCATION_COORDIANTE_CSV, afri);

                    foreach (var bb in rowsOfBB)
                    {
                        tmp.Add(CreateBoundingBoxFromCsvRows(bb));
                    }


                }

                return tmp;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public List<TmpSampleId> ReadSampleIds()
        {
            List<TmpSampleId> tmp = new List<TmpSampleId>();

            try
            {
                string path = Path.Combine(AppConfiguration.GetModuleWorkspacePath("RDB"), SAMPLE_IDS_TXT);
                AsciiReader reader = new AsciiReader();

                if (File.Exists(path))
                {
                    FileStream stream = reader.Open(path);
                    AsciiFileReaderInfo afri = new AsciiFileReaderInfo();
                    afri.Seperator = TextSeperator.semicolon;
                    afri.Variables = 1;
                    afri.Data = 2;

                    List<List<string>> rowsOfSampleIds = reader.ReadFile(stream, SAMPLE_IDS_TXT, afri);

                    foreach (var bb in rowsOfSampleIds)
                    {
                        tmp.Add(CreateSampleIdFromCsvRows(bb));
                    }


                }

                return tmp;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        #endregion

        public List<Soil> ReadSoilCsv()
        {
            List<Soil> tmp = new List<Soil>();
            try
            {
               

                string path = Path.Combine(AppConfiguration.GetModuleWorkspacePath("RDB"), ALL);
                AsciiReader reader = new AsciiReader();

                if (File.Exists(path))
                {
                    FileStream stream = reader.Open(path);
                    AsciiFileReaderInfo afri = new AsciiFileReaderInfo();
                    afri.Seperator = TextSeperator.semicolon;
                    afri.Variables = 1;
                    afri.Data = 2;

                    List<List<string>> rowsOfSites = reader.ReadFile(stream, SITE_CSV, afri);
                    rowList = new List<CsvFileEntity>();
                    foreach (List<string> row in rowsOfSites)
                    {
                        rowList.Add(RowToAccessDBEntities(row));
                    }

                    var ids = rowList.Where(e => e.VarName == "Soil").Select(e => e.VarId).Distinct();

                    foreach (long id in ids)
                    {
                        if (id > 0)
                            tmp.Add(CreateSoilFromCsvRows(rowList.Where(e => e.ID.Equals(id)).ToList(), id));
                    }
                }

                return tmp;

            }
            catch (Exception ex)
            {

                throw ex;
            }


            return tmp;
        }

        public List<Site> ReadSiteCsv()
        {
            List<Site> tmp = new List<Site>();

            try
            {
                string path = Path.Combine(AppConfiguration.GetModuleWorkspacePath("RDB"), SITE_CSV);
                AsciiReader reader = new AsciiReader();

                if (File.Exists(path))
                {
                    FileStream stream = reader.Open(path);
                    AsciiFileReaderInfo afri = new AsciiFileReaderInfo();
                    afri.Seperator = TextSeperator.semicolon;
                    afri.Variables = 1;
                    afri.Data = 2;

                    List<List<string>> rowsOfSites = reader.ReadFile(stream, SITE_CSV, afri);
                    rowList = new List<CsvFileEntity>();
                    foreach (List<string> row in rowsOfSites)
                    {
                        rowList.Add(RowToFileEntities(row));
                    }

                    var ids = rowList.Select(e => e.ID).Distinct();

                    foreach (long id in ids)
                    {
                        long parentid = rowList.Where(e => e.ID.Equals(id)).FirstOrDefault().SuperID;
                        tmp.Add(CreateSiteFromCsvRows(rowList.Where(e => e.ID.Equals(id)).ToList(),id, parentid));
                    }

                }

                return tmp;
            }
            catch (Exception ex)
            {

                throw ex;
            }

            return tmp;
        }

        public List<Plot> ReadPlotCsv()
        {
            List<Plot> tmp = new List<Plot>();

            try
            {
                string path = Path.Combine(AppConfiguration.GetModuleWorkspacePath("RDB"), PLOT_CSV);
                AsciiReader reader = new AsciiReader();

                if (File.Exists(path))
                {
                    FileStream stream = reader.Open(path);
                    AsciiFileReaderInfo afri = new AsciiFileReaderInfo();
                    afri.Seperator = TextSeperator.semicolon;
                    afri.Variables = 1;
                    afri.Data = 2;

                    List<List<string>> rowsOfSites = reader.ReadFile(stream, SITE_CSV, afri);
                    rowList = new List<CsvFileEntity>();
                    foreach (List<string> row in rowsOfSites)
                    {
                        rowList.Add(RowToFileEntities(row));
                    }

                    var ids = rowList.Select(e => e.ID).Distinct();

                    foreach (long id in ids)
                    {
                        long parentid = rowList.Where(e => e.ID.Equals(id)).FirstOrDefault().SuperID;
                        tmp.Add(CreatePlotFromCsvRows(rowList.Where(e => e.ID.Equals(id)).ToList(), id, parentid));
                    }

                }

                return tmp;
            }
            catch (Exception ex)
            {

                throw ex;
            }

            return tmp;
        }

        public List<Project> ReadProjectCsv()
        {
            List<Project> tmp = new List<Project>();

            try
            {
                string path = Path.Combine(AppConfiguration.GetModuleWorkspacePath("RDB"), PROJECT_CSV);
                AsciiReader reader = new AsciiReader();

                if (File.Exists(path))
                {
                    FileStream stream = reader.Open(path);
                    AsciiFileReaderInfo afri = new AsciiFileReaderInfo();
                    afri.Seperator = TextSeperator.semicolon;
                    afri.Variables = 1;
                    afri.Data = 2;

                    List<List<string>> rowsOfSites = reader.ReadFile(stream, SITE_CSV, afri);
                    rowList = new List<CsvFileEntity>();
                    foreach (List<string> row in rowsOfSites)
                    {
                        rowList.Add(RowToFileEntities(row));
                    }

                    var ids = rowList.Select(e => e.ID).Distinct();

                    foreach (long id in ids)
                    {
                        long parentid = rowList.Where(e => e.ID.Equals(id)).FirstOrDefault().SuperID;
                        tmp.Add(CreateProjectFromCsvRows(rowList.Where(e => e.ID.Equals(id)).ToList(), id, parentid));
                    }

                }

                return tmp;
            }
            catch (Exception ex)
            {

                throw ex;
            }

            return tmp;
        }

        public List<Tree> ReadTreeCsv()
        {
            List<Tree> tmp = new List<Tree>();

            try
            {
                string path = Path.Combine(AppConfiguration.GetModuleWorkspacePath("RDB"), TREE_CSV);
                AsciiReader reader = new AsciiReader();

                if (File.Exists(path))
                {
                    FileStream stream = reader.Open(path);
                    AsciiFileReaderInfo afri = new AsciiFileReaderInfo();
                    afri.Seperator = TextSeperator.semicolon;
                    afri.Decimal = DecimalCharacter.point;
                    afri.Variables = 1;
                    afri.Data = 2;

                    List<List<string>> rowsOfSites = reader.ReadFile(stream, SITE_CSV, afri);
                    rowList = new List<CsvFileEntity>();
                    foreach (List<string> row in rowsOfSites)
                    {
                        rowList.Add(RowToTreeFileEntities(row));
                    }

                    var ids = rowList.Where(e => e.Name == "Tree" && e.ID != 0).Select(e=>e.ID).Distinct();

                    foreach (long id in ids)
                    {
                        if(id>0)
                        //long parentid = rowList.Where(e => e.ID.Equals(id)).FirstOrDefault().SuperID;
                        tmp.Add(CreateTreeFromCsvRows(rowList.Where(e => e.ID.Equals(id) && e.Name.Equals("Tree")).ToList(), id));
                    }

                }

                return tmp;
            }
            catch (Exception ex)
            {

                throw ex;
            }

            return tmp;
        }

        public List<Person> ReadPersonCsv()
        {
            List<Person> tmp = new List<Person>();

            try
            {
                string path = Path.Combine(AppConfiguration.GetModuleWorkspacePath("RDB"), PERSON_CSV);
                AsciiReader reader = new AsciiReader();

                if (File.Exists(path))
                {
                    FileStream stream = reader.Open(path);
                    AsciiFileReaderInfo afri = new AsciiFileReaderInfo();
                    afri.Seperator = TextSeperator.semicolon;
                    afri.Decimal = DecimalCharacter.point;
                    afri.Variables = 1;
                    afri.Data = 2;

        
                    List<List<string>> rowsOfPerson = reader.ReadFile(stream, PERSON_CSV, afri);
                    foreach (List<string> row in rowsOfPerson)
                    {
                        tmp.Add(CreatePerson(row));
                    }

                }

                return tmp;
            }
            catch (Exception ex)
            {

                throw ex;
            }

            return tmp;
        }

        private CsvFileEntity RowToFileEntities(List<string> row)
        {
            CsvFileEntity tmp = new CsvFileEntity();

            tmp.ID = Convert.ToInt64(row[0]);
            tmp.Name = row[1];
            tmp.Category = row[2];
            tmp.VarId = Convert.ToInt64(row[3]);
            tmp.VarCat = row[4];
            tmp.VarName = row[5];
            tmp.VarValue = row[6];
            tmp.TypeID = Convert.ToInt64(row[7]);
            tmp.SuperID = Convert.ToInt64(row[8]);

            return tmp;
        }

        private CsvFileEntity RowToAccessDBEntities(List<string> row)
        {
            CsvFileEntity tmp = new CsvFileEntity();

            if(!string.IsNullOrEmpty(row[0]))tmp.ID = Convert.ToInt64(row[0]);
            if (!string.IsNullOrEmpty(row[1])) tmp.VarId = Convert.ToInt64(row[1]);
            tmp.VarCat = row[4];
            tmp.VarName = row[5];
        
            tmp.VarValue = row[9];

            return tmp;
        }

        private CsvFileEntity RowToTreeFileEntities(List<string> row)
        {
            CsvFileEntity tmp = new CsvFileEntity();

            tmp.ID = Convert.ToInt64(row[0]);
            tmp.Name = row[1];
            tmp.VarId = Convert.ToInt64(row[2]);
            tmp.VarCat = row[3];
            tmp.VarName = row[4];
            tmp.VarValue = row[5];

            return tmp;
        }

        private TmpBoundingBox CreateBoundingBoxFromCsvRows(List<string> row)
        {
            TmpBoundingBox tmp = new TmpBoundingBox();
            if (!String.IsNullOrEmpty(row.ElementAt(1)))
            {
                tmp.Id = Convert.ToInt64(row.ElementAt(1));
                tmp.EastLongtitude = row.ElementAt(9);
                tmp.WestLongtitude = row.ElementAt(10);
                tmp.NorthLatitude = row.ElementAt(11);
                tmp.SouthLatitude = row.ElementAt(12);
            }
            return tmp;
        }

        private TmpMeasurementHeight CreateMeasurementHeightFromCsvRows(List<string> row)
        {
            TmpMeasurementHeight tmp = new TmpMeasurementHeight();
            if (!String.IsNullOrEmpty(row.ElementAt(1)))
            {
                tmp.Id = Convert.ToInt64(row.ElementAt(0));
                tmp.ParentId = Convert.ToInt64(row.ElementAt(1));
                tmp.Value = row.ElementAt(3);
            }
            return tmp;
        }

        private TmpSampleId CreateSampleIdFromCsvRows(List<string> row)
        {
            TmpSampleId tmp = new TmpSampleId();
            if (!String.IsNullOrEmpty(row.ElementAt(1)))
            {
                tmp.Id = Convert.ToInt64(row.ElementAt(0));
                tmp.ParentId = Convert.ToInt64(row.ElementAt(1));
                tmp.Value = row.ElementAt(3).Replace("\"","");
            }
            return tmp;
        }

        private Site CreateSiteFromCsvRows(List<CsvFileEntity> rows, long refId,long parentId)
        {
            Site tmp = new Site();
            tmp.RefId = refId;
            tmp.ParentId = parentId;
            Type type = typeof(Site);

            foreach (var x in rows)
            {
                    //set properties
                    if (x.VarCat.Equals("V"))
                    {
                        PropertyInfo propertyInfo = tmp.GetType().GetProperty(x.VarName.Replace(" ", ""));
                        if (!String.IsNullOrEmpty(x.VarValue))
                        {
                            propertyInfo.SetValue(tmp, Convert.ChangeType(x.VarValue, propertyInfo.PropertyType), null);
                        }
                    }

                    if (x.VarCat.Equals("P"))
                    {
                        if (x.VarName.Equals("Plot"))
                        {
                            tmp.Plots.Add(x.VarId);
                        }
                    }
            }

            return tmp;
        }

        private Plot CreatePlotFromCsvRows(List<CsvFileEntity> rows, long refId, long parentId)
        {
            Plot tmp = new Plot();
            tmp.RefId = refId;
            tmp.ParentId = parentId;
            Type type = typeof(Plot);

            foreach (var x in rows)
            {
                try
                {
                    //set properties
                    if (x.VarCat.Equals("V"))
                    {
                        PropertyInfo propertyInfo = tmp.GetType().GetProperty(x.VarName.Replace(" ", ""));
                        if (!String.IsNullOrEmpty(x.VarValue))
                        {
                            propertyInfo.SetValue(tmp, Convert.ChangeType(x.VarValue, propertyInfo.PropertyType), null);
                        }
                    }

                    if (x.VarCat.Equals("S"))
                    {
                        if (x.VarName.Equals("Soil"))
                        {
                            tmp.Soils.Add(x.VarId);
                        }

                        if (x.VarName.Equals("Tree"))
                        {
                            tmp.Trees.Add(x.VarId);
                        }

                        if (x.VarName.Equals("Sub-Plot"))
                        {
                            tmp.Plots.Add(x.VarId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    
                    throw ex;
                }
                
            }

            return tmp;
        }

        private Project CreateProjectFromCsvRows(List<CsvFileEntity> rows, long refId, long parentId)
        {
            Project tmp = new Project();
            tmp.RefId = refId;
            tmp.ParentId = parentId;
            Type type = typeof(Project);

            foreach (var x in rows)
            {
                try
                {
                    //set properties
                    if (x.VarCat.Equals("V"))
                    {
                        PropertyInfo propertyInfo = tmp.GetType().GetProperty(x.VarName.Replace(" ", ""));
                        if (!String.IsNullOrEmpty(x.VarValue))
                        {
                            propertyInfo.SetValue(tmp, Convert.ChangeType(x.VarValue, propertyInfo.PropertyType), null);
                        }
                    }

                    //if (x.VarCat.Equals("S"))
                    //{
                    //    if (x.VarName.Equals("Soil"))
                    //    {
                    //        tmp.Soils.Add(x.VarId);
                    //    }

                    //    if (x.VarName.Equals("Tree"))
                    //    {
                    //        tmp.Trees.Add(x.VarId);
                    //    }

                    //    if (x.VarName.Equals("Sub-Plot"))
                    //    {
                    //        tmp.Plots.Add(x.VarId);
                    //    }
                    //}
                }
                catch (Exception ex)
                {

                    throw ex;
                }

            }

            return tmp;
        }

        private Tree CreateTreeFromCsvRows(List<CsvFileEntity> rows, long refId)
        {
            Tree tmp = new Tree();
            tmp.RefId = refId;
            Type type = typeof(Tree);

            foreach (var x in rows)
            {
                try
                {
                    //set properties
                    if (x.VarCat.Equals("V"))
                    {
                        PropertyInfo propertyInfo = tmp.GetType().GetProperty(x.VarName.Replace(" ", ""));
                        if (!String.IsNullOrEmpty(x.VarValue))
                        {
                            if (propertyInfo.PropertyType.Name.Equals("DateTime"))
                            {
                                #region datetime not supported
                                //try
                                //{
                                //    DateTime td = System.DateTime.Parse(x.VarValue, new CultureInfo("de-DE"));
                                //    propertyInfo.SetValue(tmp, td, null);
                                //}
                                //catch (Exception)
                                //{

                                //    try
                                //    {
                                //        DateTime td = System.DateTime.ParseExact(x.VarValue,"yyyy", new CultureInfo("de-DE"));
                                //        propertyInfo.SetValue(tmp, td, null);

                                //    }
                                //    catch (Exception)
                                //    {

                                //        throw;
                                //    }
                                //}
                                #endregion

                            }
                            else
                            {
                                propertyInfo.SetValue(tmp, Convert.ChangeType(x.VarValue, propertyInfo.PropertyType), null);
                            }
                            
                        }
                    }

                    if (x.VarCat.Equals("S"))
                    {
                        if (x.VarName.Equals("Tree stem slice"))
                        {
                            List<CsvFileEntity> treestemsliceRows = rowList.Where(e => e.ID.Equals(x.VarId)).ToList();
                            TreeStemSlice tss = CreateTreeStemSlice(treestemsliceRows);

                            tmp.TreeStemSlices.Add(tss);
                        }
                    }

                    if (x.VarCat.Equals("D"))
                    {
                        List<CsvFileEntity> diameterRows = rowList.Where(e => e.ID.Equals(x.VarId)).ToList();
                        DiameterClass d = CreateDiameter(diameterRows, x.VarId);
                        tmp.Diameters.Add(d);
                    }
                }
                catch (Exception ex)
                {

                    throw ex;
                }

            }

            return tmp;
        }

        private Soil CreateSoilFromCsvRows(List<CsvFileEntity> rows, long refId)
        {
            Soil tmp = new Soil();

            foreach (var x in rows)
            {
                try
                {
                    setPropertyToSoil(tmp, x);

                    if (x.VarCat.Equals("Sample"))
                    {

                        Console.WriteLine(x.VarName);

                    }
                }
                catch (Exception ex)
                {

                    throw ex;
                }

            }

            #region get soilType

            //get SoilType Place = Plot
            long placeId = rowList.Where(e => e.VarId.Equals(refId)).FirstOrDefault().ID;
            var placeObj = rowList.Where(e => e.VarId.Equals(placeId)).FirstOrDefault();
            IEnumerable<CsvFileEntity> plotRows = new List<CsvFileEntity>();
            //sub-plot
            if (placeObj!=null && placeObj.VarName.ToLower().Equals("sub-plot"))
            {
                long plotId = rowList.Where(e => e.VarId.Equals(placeId)).FirstOrDefault().ID;
                plotRows = rowList.Where(e => e.ID.Equals(plotId));
                Debug.WriteLine("--------------------------------");
                foreach (var row in plotRows)
                {
                    setPropertyToSoil(tmp, row);
                    Debug.WriteLine(row.VarName + " : " + row.VarValue);

                }

            }
            else
            {
                plotRows = rowList.Where(e => e.ID.Equals(placeId));
                Debug.WriteLine("--------------------------------");
                foreach (var row in plotRows)
                {
                    setPropertyToSoil(tmp, row);
                    Debug.WriteLine(row.VarName + " : " + row.VarValue);

                }
            }
            

            #endregion




            return tmp;
        }

        private Soil setPropertyToSoil(Soil soil, CsvFileEntity row)
        {
            if (row.VarCat.Equals("Value"))
            {
                PropertyInfo propertyInfo = soil.GetType().GetProperty(row.VarName.Replace(" ", ""));
                if (!String.IsNullOrEmpty(row.VarValue))
                {
                    if (propertyInfo != null)
                    {
                        if (propertyInfo.PropertyType.Name.Equals("DateTime"))
                        {
                            #region datetime not supported

                            DateTime dt;
                            if (!DateTime.TryParse(row.VarValue, out dt))
                            {
                                propertyInfo.SetValue(soil, dt, null);
                            }

                            #endregion

                        }
                        else
                        {
                            propertyInfo.SetValue(soil, Convert.ChangeType(row.VarValue, propertyInfo.PropertyType),
                                null);
                        }
                    }

                }
            }

            return soil;
        }

        private TreeStemSlice CreateTreeStemSlice(List<CsvFileEntity> rows)
        {
            TreeStemSlice tmp = new TreeStemSlice();
           
            foreach (var x in rows)
            {
                try
                {
                    //set properties

                    PropertyInfo propertyInfo = tmp.GetType().GetProperty(x.VarName.Replace(" ", ""));
                    if (!String.IsNullOrEmpty(x.VarValue))
                    {
                        if (propertyInfo.PropertyType.Name.Equals("DateTime"))
                        {

                        }
                        else
                        {
                            propertyInfo.SetValue(tmp, Convert.ChangeType(x.VarValue, propertyInfo.PropertyType), null);
                        }

                    }

                    //set stem slice systemid
                    tmp.ParentId = x.ID;

                }
                catch (Exception ex)
                {

                    throw ex;
                }

            }

            return tmp;
        }

        private DiameterClass CreateDiameter(List<CsvFileEntity> rows, long id)
        {
            DiameterClass tmp = new DiameterClass();
            tmp.Id = id;
            foreach (var x in rows)
            {
                try
                {
                    //set properties

                    PropertyInfo propertyInfo = tmp.GetType().GetProperty(x.VarName.Replace(" ", ""));
                    if (!String.IsNullOrEmpty(x.VarValue))
                    {
                        if (propertyInfo.PropertyType.Name.Equals("DateTime"))
                        {

                        }
                        else
                        {
                            propertyInfo.SetValue(tmp, Convert.ChangeType(x.VarValue, propertyInfo.PropertyType), null);
                        }

                    }


                }
                catch (Exception ex)
                {

                    throw ex;
                }

            }

            return tmp;
        }

        private Person CreatePerson(List<string> row)
        {
            Person tmp = new Person();
            tmp.Last_Name = row.ElementAt(0);
            tmp.First_Name = row.ElementAt(1);
            tmp.Second_Name = row.ElementAt(2);
            tmp.Title = row.ElementAt(3);
            tmp.Institute = row.ElementAt(4);
            tmp.Telephone = row.ElementAt(5);
            tmp.Fax = row.ElementAt(6);
            tmp.EMail = row.ElementAt(7);
            tmp.Url = row.ElementAt(8);
            tmp.Id = Convert.ToInt64(row.ElementAt(9));

            return tmp;
        }

    }
}
