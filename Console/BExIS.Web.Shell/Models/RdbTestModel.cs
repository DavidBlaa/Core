using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BEXIS.Rdb.Entities;

namespace BExIS.Web.Shell.Models
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
}