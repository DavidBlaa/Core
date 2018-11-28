using BExIS.Rdb.Entities.Barcode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vaiona.Persistence.Api;

namespace BExIS.Rdb.Services
{
    public class BarcodeManager:IDisposable
    {
        private IUnitOfWork guow = null;
        public BarcodeManager()
        {
            guow = this.GetIsolatedUnitOfWork();


        }
        private bool isDisposed = false;
        ~BarcodeManager()
        {
            Dispose(true);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    if (guow != null)
                        guow.Dispose();
                    isDisposed = true;
                }
            }
        }

        public IReadOnlyRepository<BarcodeSequence> BarcodeSequenceRepo { get; private set; }

        public long GetNextBarcodeId()
        {
            BarcodeSequence bs = new BarcodeSequence();

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {

                IRepository<BarcodeSequence> repo = uow.GetRepository<BarcodeSequence>();
                repo.Put(bs);
                uow.Commit();
            }

            return (bs.Id);
        }

    }
}
