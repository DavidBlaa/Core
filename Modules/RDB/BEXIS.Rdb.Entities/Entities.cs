using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BEXIS.Rdb.Entities
{
    public class CsvFileEntity
    {
        //ID	Name	Category	VarId	VarCat	VarName	VarValue	TypeID	SuperID
        public long ID { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public long VarId { get; set; }
        public string VarCat { get; set; }
        public string VarName { get; set; }
        public string VarValue { get; set; }
        public long TypeID { get; set; }
        public long SuperID { get; set; }
    }

    public class BaseRdbEntity
    {
        public long Id { get; set; }
        public long RefId { get; set; }
        public long ParentId { get; set; }

        public string Name { get; set; }
        public string ShortName { get; set; }

        public BaseRdbEntity()
        {
            RefId = 0;
            ParentId = 0;
            Id = 0;
            Name = "";
            ShortName = "";
        }
    }

    public class Project: BaseRdbEntity
    {
        public List<long> Campaigns { get; set; }
    }

    public class ProjectCluster : BaseRdbEntity
    {
        public List<long> Projects { get; set; }
    }

    public class Site: BaseRdbEntity
    {
        public string BoundingBox { get; set; }
        public long Contributor { get; set; }
        public List<long> Plots { get; set; }

        public Site()
        {
            Plots = new List<long>();
            Contributor = 0;
            BoundingBox = "";
        }
    }

    public class Plot : Site
    {
        public string Coordinates { get; set; }
        public string TopographicPosition { get; set; }
        public string Orientation { get; set; }
        public string GroundwaterDepth { get; set; }
        public string GeologicalParentMaterialofEntireSoilProfile { get; set; }
        public string SoilType { get; set; }
        public string Vegetation { get; set; }
        public Double PitSize { get; set; }
        public Double TotalDepth { get; set; }


        public List<long> Soils { get; set; }
        public List<long> Trees { get; set; }

        public Plot()
        {
            Coordinates = "";
            TopographicPosition = "";
            Orientation = "";
            GeologicalParentMaterialofEntireSoilProfile = "";
            SoilType = "";
            Vegetation = "";

            Soils = new List<long>();
            Trees = new List<long>();
        }
    }

    public class Tree:BaseRdbEntity
    {
        public string Age { get; set; }
        public string Volume { get; set; }
        public string Height { get; set; }
        public string MassperTree { get; set; }
        public long Contributor { get; set; }
        public string Treespecies { get; set; }
        public string Volumewithoutbark { get; set; }
        public string FireScars { get; set; }

        public string SamplingDate { get; set; }

        public List<TreeStemSlice> TreeStemSlices;
        public List<DiameterClass> Diameters;

        public Tree()
        {
            Age = "";
            Volume = "";
            Height = "";
            MassperTree = "";
            Contributor = 0;
            SamplingDate = "";
            TreeStemSlices = new List<TreeStemSlice>();
            Volumewithoutbark = "";
            FireScars = "";

            Diameters= new List<DiameterClass>();
            TreeStemSlices = new List<TreeStemSlice>();
        }
    }

    public class DiameterClass:BaseRdbEntity
    {
        public string Diameter { get; set; }
        public string MeasurementHeight { get; set; }

        public DiameterClass()
        {
            Diameter = "";
            MeasurementHeight = "";
        }
    }

    public class TreeStemSlice:BaseRdbEntity
    {
        public string Treestemsegment { get; set; }

        public TreeStemSlice()
        {
            Treestemsegment = "";
        }
    }

    public class Person
    {
        public long Id { get; set; }
        public string First_Name { get; set; }
        public string Last_Name { get; set; }
        public string Second_Name { get; set; }
        public string Title { get; set; }
        public string Institute { get; set; }
        public string Telephone { get; set; }
        public string Fax { get; set; }
        public string EMail { get; set; }
        public string Url { get; set; }
        

        public Person()
        {
            Id = 0;
            First_Name  = "";
            Last_Name = "";
            Second_Name = "";
            Title = "";
            Institute = "";
            EMail = "";
            Telephone = "";
            Fax = "";
            Url = "";
        }
    }
}
