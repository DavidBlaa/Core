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
        public BoundingBox BoundingBox { get; set; }
        public long Contributor { get; set; }
        public List<long> Plots { get; set; }

        public Site()
        {
            Plots = new List<long>();
            Contributor = 0;
            BoundingBox = new BoundingBox();
        }
    }

    public class Plot : Site
    {
        public Coordinate Coordinates { get; set; }
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
            Coordinates = new Coordinate();
            TopographicPosition = "";
            Orientation = "";
            GroundwaterDepth = "";
            GeologicalParentMaterialofEntireSoilProfile = "";
            SoilType = "";
            Vegetation = "";

            Soils = new List<long>();
            Trees = new List<long>();
        }
    }

    public class Coordinate
    {
        public string Longitude { get; set; }
        public string Latitude { get; set; }

        public Coordinate()
        {
            Longitude = "";
            Latitude = "";
        }
    }

    public class BoundingBox
    {
        public string EastLongitude { get; set; }
        public string WestLongitude { get; set; }
        public string NorthLatitude { get; set; }
        public string SouthLatitude { get; set; }

        public BoundingBox()
        {
            EastLongitude = "";
            WestLongitude = "";
            NorthLatitude = "";
            SouthLatitude = "";
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
            Treespecies = "";
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
        public string Barcode { get; set; }

        public TreeStemSlice()
        {
            Barcode = "";
            Treestemsegment = "";
        }
    }

    public class Soil
    {
        public long Id { get; set; }
        public long RefId { get; set; }
        public string Name { get; set; }
        public string SamplingDate { get; set; }
        public string SoilType { get; set; }
        public string Vegetation { get; set; }
        public long Contributor { get; set; }

        //public double TotalDepth { get; set; }
        //public double PitSize { get; set; }
        public ProfilType Profil { get; set; }
        public BohrerType Bohrer { get; set; }

        public Soil()
        {
            Name = "";
            SamplingDate = "";
            SoilType = "";
            Vegetation = "";
            Contributor = 0;
            //TotalDepth = 0;
            //PitSize = 0;
        }

    }

    public class CollectionType
    {
        public long Id { get; set; }
        public long RefId { get; set; }
        public string ShortName { get; set; }
        public List<SoilUnderClass> Soils { get; set; }
        public DepthRange DepthRange { get; set; }

        public CollectionType()
        {
            ShortName = "";
            Soils = new List<SoilUnderClass>();
            DepthRange = new DepthRange();

        }
    }

    public class ProfilType:CollectionType
    {

        public double TotalDepth { get; set; }

        public ProfilType()
        {
            TotalDepth = 0;
        }

        public ProfilType(CollectionType collectionType)
        {
            ShortName = collectionType.ShortName;
            Soils = collectionType.Soils;
            Id = collectionType.Id;
            RefId = collectionType.RefId;
            TotalDepth = 0;
            DepthRange = collectionType.DepthRange;
        }
    }

    public class BohrerType : CollectionType
    {
        public double PitSize { get; set; }
        

        public BohrerType()
        {
            PitSize = 0;
        }

        public BohrerType(CollectionType collectionType)
        {
            ShortName = collectionType.ShortName;
            Soils = collectionType.Soils;
            Id = collectionType.Id;
            RefId = collectionType.RefId;
            PitSize = 0;
            DepthRange = collectionType.DepthRange;
        }
    }

   

    public class SoilUnderClass
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string SamplingDate { get; set; }
        public List<MineralSoil> MineralSoils { get; set; }
        public List<OrganicLayer> OrganicLayers { get; set; }

        public SoilUnderClass()
        {
            Id = 0;
            Name = "";
            MineralSoils = new List<MineralSoil>();
            OrganicLayers = new List<OrganicLayer>();
        }

    }

    public class OrganicLayer
    {
        public long Id { get; set; }
        public string Name { get; set; }
        //public DepthRange DepthRange { get; set; }
        //public double DepthInterval { get; set; }
        public double Density { get; set; }

        public OrganicLayer()
        {
            Id = 0;
            Name = "";
            //DepthRange = new DepthRange();
            Density = 0;
        }

    }

    public class MineralSoil
    {
        public long Id { get; set; }
        public string Name { get; set; }
        //public DepthRange DepthRange { get; set; }
        public double DepthInterval { get; set; }

        public List<Layer> Layers { get; set; }

        public MineralSoil()
        {
            Id = 0;
            Name = "";
            //DepthRange = new DepthRange();
            Layers = new List<Layer>();
        }

    }

    public class Layer
    {
        public long Id { get; set; }
        public string Horizon { get; set; }
        public double Volume { get; set; }

        public Layer()
        {
            Id = 0;
            Horizon = "";
            Volume = 0;
        }
    }

    public class DepthRange
    {
        public double Min { get; set; }
        public double Max { get; set; }

        public DepthRange()
        {
            Min = 0;
            Max = 0;
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

    public class TmpBoundingBox
    {
        public long Id { get; set; }
        public string EastLongtitude { get; set; }
        public string WestLongtitude { get; set; }
        public string NorthLatitude { get; set; }
        public string SouthLatitude { get; set; }

        public TmpBoundingBox()
        {
            Id = 0;
            EastLongtitude = "";
            WestLongtitude = "";
            NorthLatitude = "";
            SouthLatitude = "";
        }
    }

    public class TmpMeasurementHeight
    {
        public long Id { get; set; }
        public long ParentId { get; set; }
        public string Value { get; set; }
    }

    public class TmpSampleId:BaseRdbEntity
    {
        public string Value { get; set; }
    }
}
