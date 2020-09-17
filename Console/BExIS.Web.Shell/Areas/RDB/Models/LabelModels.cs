using BExIS.Dim.Helpers.Mapping;
using System.Collections.Generic;
using System.Drawing;

namespace BExIS.Modules.Rdb.UI.Models
{

    public enum LabelFormat
    {
        small = 40,
        big = 4
    }
    public class LabelManagerModel
    {
        public LabelFormat Format;
        public List<SampleModel> Samples { get; set; }
    }

    public class SampleModel
    {
        public long Id;
        public string Input;
        public string Owner;
        public string Plot;
        public string Site;
        public string Date;
        public string Type;

        public List<string> Barcodes { get; set; }

        public SampleModel()
        {
            Barcodes = new List<string>();
        }
    }

    public class LabelModel
    {
        public long Id;
        public string Input;
        public Image BarCode;
        public Image QRCode;
        public string BarCodeText;
        public string Owner;
        public string Plot;
        public string Site;
        public string Date;
        public string Type;
    }

}