
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

            }
            finally
            {
                entityManager.Dispose();
            }

        }
    }
}