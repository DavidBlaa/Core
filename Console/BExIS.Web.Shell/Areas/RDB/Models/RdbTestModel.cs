using BExIS.Rdb.Entities;
using BExIS.Web.Shell.Models;
using System.Collections.Generic;
using System.Drawing;

namespace BExIS.Modules.Rdb.UI.Models
{
    public class RdbTestModel
    {
        public Dictionary<string, EntitySelectorModel> ListOfEntites;
        public List<Tree> Trees;
        public List<Soil> Soils;

        public RdbTestModel()
        {
            ListOfEntites = new Dictionary<string, EntitySelectorModel>();
        }
    }

    public class BarCodeModel
    {
        public string Input;
        public Image Image;

    }

}