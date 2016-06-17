using System.Collections.Generic;
using System.Drawing;
using BExIS.Web.Shell.Models;
using BEXIS.Rdb.Entities;

namespace BExIS.Web.Shell.Areas.RDB.Models
{
    public class RdbTestModel
    {
        public Dictionary<string, EntitySelectorModel> ListOfEntites;
        public List<Tree> Trees;

        public RdbTestModel()
        {
            ListOfEntites = new Dictionary<string, EntitySelectorModel>();
        }
    }

    public class BarCodeModel
    {
        public Image Image;
    }

}