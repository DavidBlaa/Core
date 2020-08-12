
using BExIS.Dlm.Entities.Data;
using BExIS.Security.Entities.Objects;
using BExIS.Security.Services.Objects;
using BExIS.Xml.Helpers;
using System.Linq;
using System.Xml;

namespace BExIS.Modules.Rdb.UI.Helpers
{
    public class RdbSeedDataGenerator
    {
        public static void GenerateSeedData()
        {
            EntityManager entityManager = new EntityManager();
            FeatureManager featureManager = new FeatureManager();
            OperationManager operationManager = new OperationManager();

            try
            {
                #region create entities

                // Entities
                Entity entity = entityManager.Entities.Where(e => e.Name.ToUpperInvariant() == "Sample".ToUpperInvariant()).FirstOrDefault();


                if (entity == null)
                {
                    entity = new Entity();
                    entity.Name = "Sample";
                    entity.EntityType = typeof(Dataset);
                    entity.EntityStoreType = typeof(Xml.Helpers.DatasetStore);
                    entity.UseMetadata = true;
                    entity.Securable = true;

                    //add to Extra

                    XmlDocument xmlDoc = new XmlDocument();
                    XmlDatasetHelper xmlDatasetHelper = new XmlDatasetHelper();
                    xmlDatasetHelper.AddReferenceToXml(xmlDoc, AttributeNames.name.ToString(), "rdb", AttributeType.parameter.ToString(), "extra/modules/module");

                    entity.Extra = xmlDoc;

                    entityManager.Create(entity);
                }


                #endregion

                #region SECURITY
                //workflows = größere sachen, vielen operation
                //operations = einzelne actions

                //1.controller-> 1.Operation




                Feature Sample = featureManager.FeatureRepository.Get().FirstOrDefault(f => f.Name.Equals("Sample"));
                if (Sample == null) Sample = featureManager.Create("Sample", "Sample");

                Feature Search = featureManager.FeatureRepository.Get().FirstOrDefault(f => f.Name.Equals("Search") && f.Parent != null && f.Parent.Id.Equals(Sample.Id));
                if (Search == null) Search = featureManager.Create("Search", "Search", Sample);

                Feature Management = featureManager.FeatureRepository.Get().FirstOrDefault(f => f.Name.Equals("Management") && f.Parent != null && f.Parent.Id.Equals(Sample.Id));
                if (Management == null) Management = featureManager.Create("Management", "Management", Sample);

                Feature Dashboard = featureManager.FeatureRepository.Get().FirstOrDefault(f => f.Name.Equals("Dashboard") && f.Parent != null && f.Parent.Id.Equals(Sample.Id));
                if (Dashboard == null) Dashboard = featureManager.Create("Dashboard", "Dashboard", Sample);

                #region Help Workflow

                //operationManager.Create("DCM", "Help", "*");

                #endregion

                #region Search

                operationManager.Create("RDB", "Sample", "*", Search);


                #endregion

                #region Management

                operationManager.Create("RDB", "CreateSample", "*", Management);
                operationManager.Create("RDB", "RDB", "*", Management);
                operationManager.Create("RDB", "Label", "*", Management);

                #endregion

                #region Dashboard

                operationManager.Create("RDB", "Dashboard", "*", Dashboard);

                #endregion

                #endregion

            }
            finally
            {
                entityManager.Dispose();
                featureManager.Dispose();
                operationManager.Dispose();
            }

        }
    }
}