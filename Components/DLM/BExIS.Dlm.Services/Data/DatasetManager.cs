﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Xml;
using BExIS.Dlm.Entities.Administration;
using BExIS.Dlm.Entities.Data;
using BExIS.Dlm.Entities.DataStructure;
using Vaiona.Persistence.Api;
using Vaiona.Utils.Xml;
using MDS = BExIS.Dlm.Entities.MetadataStructure;
using System.Linq.Expressions;
using Vaiona.Logging.Aspects;
using System.Threading.Tasks;

namespace BExIS.Dlm.Services.Data
{
    /// <summary>
    /// Contains methods for accessing and manipulating datasets, dataset versions, data tuples, the values of the tuples' variables, and other associated entities.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item><description>Effective use of this class needs solid knowledge of the conceptual model and the versioning method used.Effective use of this class needs solid knowledge of the conceptual model and the versioning method used.</description></item>
    ///         <item><description>There is an automatic and transparent authorization based result set trimming in place, that may reduce the matching entities based on the current user access rights.</description></item>
    ///     </list>
    /// </remarks>
    public class DatasetManager
    {
        public int PreferedBatchSize { get; set; }
        public DatasetManager()
        {
            IUnitOfWork uow = this.GetUnitOfWork();
            this.PreferedBatchSize = uow.PersistenceManager.PreferredPushSize;
            this.DatasetRepo = uow.GetReadOnlyRepository<Dataset>();
            this.DatasetVersionRepo = uow.GetReadOnlyRepository<DatasetVersion>();
            this.DataTupleRepo = uow.GetReadOnlyRepository<DataTuple>();
            this.DataTupleVerionRepo = uow.GetReadOnlyRepository<DataTupleVersion>();
            this.ExtendedPropertyValueRepo = uow.GetReadOnlyRepository<ExtendedPropertyValue>();
            this.VariableValueRepo = uow.GetReadOnlyRepository<VariableValue>();
            this.ParameterValueRepo = uow.GetReadOnlyRepository<ParameterValue>();
            this.AmendmentRepo = uow.GetReadOnlyRepository<Amendment>();
        }

        #region Data Readers

        // provide read only repos for the whole aggregate area

        /// <summary>
        /// Provides read-only querying and access to datasets
        /// </summary>
        public IReadOnlyRepository<Dataset> DatasetRepo { get; private set; }
        
        /// <summary>
        /// Provides read-only querying and access to dataset versions
        /// </summary>
        public IReadOnlyRepository<DatasetVersion> DatasetVersionRepo { get; private set; }

        /// <summary>
        /// Provides read-only querying and access to the tuples of dataset versions
        /// </summary>
        public IReadOnlyRepository<DataTuple> DataTupleRepo { get; private set; }

        /// <summary>
        /// Provides read-only querying and access to the previously archived versions of data tuples
        /// </summary>
        public IReadOnlyRepository<DataTupleVersion> DataTupleVerionRepo { get; private set; }

        /// <summary>
        /// Provides read-only querying and access to the values of extended properties associated to datasets
        /// </summary>
        public IReadOnlyRepository<ExtendedPropertyValue> ExtendedPropertyValueRepo { get; private set; }

        /// <summary>
        /// Provides read-only querying and access to the values of variables
        /// </summary>
        public IReadOnlyRepository<VariableValue> VariableValueRepo { get; private set; }

        /// <summary>
        /// Provides read-only querying and access to the values of parameters
        /// </summary>
        public IReadOnlyRepository<ParameterValue> ParameterValueRepo { get; private set; }

        /// <summary>
        /// Provides read-only querying and access to the amendments of the data tuples
        /// </summary>
        public IReadOnlyRepository<Amendment> AmendmentRepo { get; private set; }

        #endregion

        #region Dataset

        /// <summary>
        /// Determines whether the dataset <paramref name="datasetId"/> is checked out by the user <paramref name="username"/>.
        /// </summary>
        /// <param name="datasetId">The identifier of the dataset.</param>
        /// <param name="username">the username of the user that may have checked the dataset out.</param>
        /// <returns>True if the dataset is checked out by the passed username, False otherwise.</returns>
        /// <remarks>
        /// Returning false does not mean the dataset is not checked out or not by the designated user, it may imply that the dataset does not exist, deleted, or purged.
        /// So do NOT rely on the false return value and use the method when exclusively interested in knowing whether the user <paramref name="username"/> has checked out the dataset <paramref name="datasetId"/>.
        /// </remarks>
        public bool IsDatasetCheckedOutFor(Int64 datasetId, string username)
        {
            return (isDatasetCheckedOutFor(datasetId, username));
        }

        /// <summary>
        /// Determines whether the dataset <paramref name="datasetId"/> is in checked-in state.
        /// </summary>
        /// <param name="datasetId">The identifier of the dataset.</param>
        /// <returns>True if dataset exists and is in checked-in state, False otherwise.</returns>
        /// <remarks>Do NOT rely on False return value to conclude the dataset is not checked in, it may imply that the dataset does not exist.</remarks>
        public bool IsDatasetCheckedIn(Int64 datasetId)
        {
            return (DatasetRepo.Query(p => p.Status == DatasetStatus.CheckedIn && p.Id == datasetId).Count() == 1);
        }
        
        /// <summary>
        /// Retrieves the dataset object having identifier <paramref name="datasetId"/> from the database.
        /// </summary>
        /// <param name="datasetId">The identifier of the dataset.</param>
        /// <returns>The semi-populated dataset entity if exists, or null.</returns>
        /// <remarks>The object based attributes of the entity that are persisted as XML are not populated by default. In order to fully populate the entity, call the <see cref="Materialize"/> method.</remarks>
        public Dataset GetDataset(Int64 datasetId)
        {
            Dataset ds = DatasetRepo.Get(datasetId);
            //if(ds != null)
            //    ds.Materialize();
            return (ds);
        }

        /// <summary>
        /// Creates an empty dataset that has no data tuple, puts it into the checked-in state, and persists it in the database. At the time of creation a valid data structure, research plan, and metadata structure must be available.
        /// </summary>
        /// <param name="dataStructure">A valid and persisted data structure entity.</param>
        /// <param name="researchPlan">A valid and persisted research plan entity.</param>
        /// <param name="metadataStructure">A valid and persisted metadata structure entity.</param>
        /// <returns>A dataset associated to the <paramref name="dataStructure"/>, <paramref name="researchPlan"/>, and <paramref name="metadataStructure"/> entities.</returns>
        //[MeasurePerformance]
        public Dataset CreateEmptyDataset(Entities.DataStructure.DataStructure dataStructure, ResearchPlan researchPlan, MDS.MetadataStructure metadataStructure)
        {
            Contract.Requires(dataStructure != null && dataStructure.Id >= 0);
            Contract.Requires(researchPlan != null && researchPlan.Id >= 0);
            Contract.Requires(metadataStructure != null && metadataStructure.Id >= 0);
            
            Contract.Ensures(Contract.Result<Dataset>() != null && Contract.Result<Dataset>().Id >= 0);
            
            Dataset dataset = new Dataset(dataStructure);
            
            dataset.ResearchPlan = researchPlan;
            researchPlan.Datasets.Add(dataset); 
            
            dataset.MetadataStructure = metadataStructure;
            metadataStructure.Datasets.Add(dataset);            

            dataset.Status = DatasetStatus.CheckedIn;
            dataset.CheckOutUser = string.Empty;
            dataset.LastCheckIOTimestamp = DateTime.UtcNow;

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<Dataset> repo = uow.GetRepository<Dataset>();
                repo.Put(dataset);
                uow.Commit();
            }
            return (dataset);
        }

        /// <summary>
        /// In cases that the dataset's attributes are changed, data set is bound to a research plan or other attributes of the dataset entity are changed, this method persists the changes.
        /// </summary>
        /// <param name="dataset">A dataset instance containing the changes</param>
        /// <returns>The dataset instance with the changes applied</returns>
        /// <remarks>
        /// Do NOT use this method to change the status of the dataset
        /// </remarks>
        public Dataset UpdateDataset(Dataset dataset)
        {
            Contract.Requires(dataset != null);
            Contract.Requires(dataset.Id >= 0);

            Contract.Ensures(Contract.Result<Dataset>() != null && Contract.Result<Dataset>().Id >= 0);

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<Dataset> repo = uow.GetRepository<Dataset>();
                repo.Put(dataset);
                uow.Commit();
            }
            return (dataset);
        }

        /// <summary>
        /// Checks out the dataset for the specified user, in order to make it available for editing.
        /// dataset must be in CheckedIn state.
        /// </summary>
        /// <param name="datasetId">The identifier of the dataset</param>
        /// <returns>True if the dataset is checked out, False otherwise</returns>
        //[Diagnose]
        public bool CheckOutDataset(Int64 datasetId, string username)
        {
            return(checkOutDataset(datasetId, username, DateTime.UtcNow));
        }

        /// <summary>
        /// Checks out the dataset for the specified user, if it is not already checked out.        
        /// dataset must be in CheckedIn state, in order to be checked out.
        /// </summary>
        /// <param name="datasetId">The identifier of the dataset</param>
        /// <returns>True if the dataset is checked out, False otherwise</returns>
        public bool CheckOutDatasetIfNot(Int64 datasetId, string username)
        {
            if (isDatasetCheckedOutFor(datasetId, username))
                return true;
            return (checkOutDataset(datasetId, username, DateTime.UtcNow));
        }
        /// <summary>
        /// This version of the checlout accpes a timestamp, which is likely a past time. The prpuse is to support dataset migarations by preserving their original sumission date.        
        /// </summary>
        /// <param name="datasetId"></param>
        /// <param name="username"></param>
        /// <param name="timestamp">The timestamp of the migrated dataset.</param>
        /// <remarks>The timestamp MUST be greater than the timestamp of the current checked in version, if exist.</remarks>
        /// <returns></returns>
        //[Diagnose]
        public bool CheckOutDataset(Int64 datasetId, string username, DateTime timestamp)
        {
            return (checkOutDataset(datasetId, username, timestamp));
        }

        /// <summary>
        /// approves the working copy version as a new version and changes the status of the dataset to CheckedIn.
        /// The status must be in CheckedOut and the user must be similar to the checkout user.        
        /// </summary>
        /// <param name="datasetId">The identifier of the dataset to be checked-in</param>
        /// <param name="comment">A free form text to describe what has changed with this check-in</param>
        /// <param name="username">The username that performs the check-in, which should be the same as the check-out username</param>
        /// <remarks>Does not support simultaneous check-ins</remarks>
        
        //[Diagnose]
        public void CheckInDataset(Int64 datasetId, string comment, string username)
        {
            checkInDataset(datasetId, comment, username, false);
        }

        /// <summary>
        /// Rolls back all the non checked-in changes done to the latest version (deletes the working copy changes) and takes the dataset back to the latest CheckedIn version.
        /// The dataset must be in CheckedOut state and the performing user should be the check out user.
        /// It does not check-in the dataset so the caller should <see cref="CheckInDataset"/> afterward, if needed.
        /// </summary>
        /// <param name="datasetId">The identifier of the dataset to be checked-in</param>
        /// <param name="username">The username that performs the check-in, which should be the same as the check-out username</param>        
        public void UndoCheckoutDataset(Int64 datasetId, string username)
        {
            undoCheckout(datasetId, username, false);
        }

        /// <summary>
        /// Marks the dataset as deleted but does not physically deletes it from the database. If the dataset is checked out and the <paramref name="rollbackCheckout"/> is
        /// True, the dataset's changes will be roll-backed and then the delete operation takes place, but if the <paramref name="rollbackCheckout"/> is false, 
        /// The changes will be checked in as a new version and then the deletion operation is executed.
        /// </summary>
        /// <param name="datasetId">The identifier of the dataset to be checked-in.</param>
        /// <param name="username">The username that performs the check-in, which should be the same as the check-out username.</param>        
        /// <param name="rollbackCheckout">Determines whether latest uncommitted changes should be rolled back or checked in before marking the dataset as deleted.</param>
        /// <returns>True if the dataset is deleted, False otherwise.</returns>
        public bool DeleteDataset(Int64 datasetId, string username, bool rollbackCheckout)
        {
            Contract.Requires(datasetId >= 0);

            // do not move them to editDatasetVersion function
            this.DatasetRepo.Evict();
            this.DatasetVersionRepo.Evict();
            this.DataTupleRepo.Evict();
            this.DataTupleVerionRepo.Evict();

            Dataset entity = this.DatasetRepo.Get(datasetId);
            if (entity.Status == DatasetStatus.Deleted)
                return false;
            /// the dataset must be in CheckedIn state to be deleted
            /// so if it is checked out, the checkout version (working copy) is removed first
            if (entity.Status == DatasetStatus.CheckedOut)
            {
                if (rollbackCheckout == true)
                {
                    this.undoCheckout(entity.Id, username, false);
                }
                else
                {
                    throw new Exception(string.Format("Dataset {0} is in check out state, which prevents it from being deleted. Rollback the changes or check them in and try again", entity.Id));
                }
            }

            // Make an artificial check-out / edit/ check in so that all the data tuples move to the history
            // this movement reduces the amount of tuples in the active tuples table and also marks the dataset as archived upon delete
            checkOutDataset(entity.Id, username, DateTime.UtcNow);
            var workingCopy = getDatasetWorkingCopy(entity.Id);
            //This fetch and insert will be problematic on bigger datasets! try implement the logic without loading the tuples
            var tuples = getWorkingCopyTuples(workingCopy);
            workingCopy = editDatasetVersion(workingCopy, null, null, tuples, null); // deletes all the tuples from the active list and moves them to the history table
            checkInDataset(entity.Id, "Dataset is deleted", username, false);

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<Dataset> repo = uow.GetRepository<Dataset>();
                entity = repo.Get(datasetId);
                entity.Status = DatasetStatus.Deleted;
                repo.Put(entity);
                uow.Commit();
            }
            // if any problem was detected during the commit, an exception will be thrown!
            return (true);
        }

        /// <summary>
        /// Physically deletes the whole dataset, including its versions and data tuples, from the database.
        /// </summary>
        /// <param name="datasetId">The identifier of the dataset to be checked-in.</param>
        /// <returns>True if the dataset is purged, False otherwise.</returns>
        /// <remarks>There is no way to recover the dataset after this method has successfully purged it.</remarks>
        public bool PurgeDataset(Int64 datasetId)
        {
            Contract.Requires(datasetId >= 0);

            // Attention: when create and purge or delete tuple are called in one run (one http request) the is a problem with removal of tuples/ version/ dataset because having some references!!!
            // but if they are called on a single dataset in 2 different http requests, there is no problem!?
            // perhaps the NH session is not flushed completely or has some references to the objects in the caches, as the session end function is not called yet! this is why an Evict before purge is required!

            this.DatasetRepo.Evict();
            this.DatasetVersionRepo.Evict();
            this.DataTupleRepo.Evict();
            this.DataTupleVerionRepo.Evict();

            Dataset entity = this.DatasetRepo.Get(datasetId);

            if (entity == null)
                return false;

            IList<Int64> versionIds = entity.Versions
                           .Select(p => p.Id)
                           .ToList();
            IReadOnlyRepository<ContentDescriptor> ContentDescriptorRepoReadOnly = DatasetRepo.UnitOfWork.GetReadOnlyRepository<ContentDescriptor>();
            IList<Int64> contentDescriptorIds = (versionIds == null || versionIds.Count() <= 0) ? null :
                ContentDescriptorRepoReadOnly.Query(p => versionIds.Contains(p.DatasetVersion.Id)).Select(p => p.Id).ToList();

            IList<Int64> tupleIds = (versionIds == null || versionIds.Count() <= 0) ? null : DataTupleRepo.Query(p => versionIds.Contains(p.DatasetVersion.Id)).Select(p=>p.Id).ToList();
            IList<Int64> tupleVersionIds = (versionIds == null || versionIds.Count() <= 0) ? null : DataTupleVerionRepo.Query(p => versionIds.Contains(p.DatasetVersion.Id)).Select(p => p.Id).ToList();
            string queryStr = "DELETE FROM {0} e WHERE e.Id IN (:idsList)";
            //queryStr = "DELETE FROM {0} e WHERE e.Id IN (23, 24, 25)";

            using (IUnitOfWork uow = this.GetBulkUnitOfWork())
            {
                IRepository<Dataset> repo = uow.GetRepository<Dataset>();
                IRepository<DataTupleVersion> tupleVersionRepo = uow.GetRepository<DataTupleVersion>();
                IRepository<DatasetVersion> versionRepo = uow.GetRepository<DatasetVersion>();
                IRepository<DataTuple> tuplesRepo = uow.GetRepository<DataTuple>();
                IRepository<ContentDescriptor> ContentDescriptorRepo = uow.GetRepository<ContentDescriptor>();

                if (tupleVersionIds != null && tupleVersionIds.Count > 0)
                {
                    long iternations = tupleVersionIds.Count / PreferedBatchSize;
                    // when the number of columns is not a an exact multiply of the batch size, an additional iteration is needed to purge the last batch of the tuples.
                    if (iternations * PreferedBatchSize < tupleVersionIds.Count)
                        iternations++;

                    for (int round = 0; round < iternations; round++)
                    {
                        // Guards the call to the Execute funtion in cases that there is no more record to purge.
                        // An unusual but possible case is when the number of tuples is an exact multiply of the PreferredBatchSize.
                        // In this case, the last round's Take function takes no Id and the idsList parameter is empty, which causes the ORM
                        // to generate an invalid DB query.
                        if (tupleVersionIds.Skip(round * PreferedBatchSize).Take(PreferedBatchSize).Count() > 0)
                        {
                            Dictionary<string, object> parameters = new Dictionary<string, object>();
                            parameters.Add("idsList", tupleVersionIds.Skip(round * PreferedBatchSize).Take(PreferedBatchSize).ToList());
                            tupleVersionRepo.Execute(string.Format(queryStr, "DataTupleVersion"), parameters);
                        }
                    }
                }
                if (tupleIds != null && tupleIds.Count > 0)
                {
                    long iternations = tupleIds.Count / PreferedBatchSize;
                    if (iternations * PreferedBatchSize < tupleIds.Count)
                        iternations++;

                    for (int round = 0; round < iternations; round++)
                    {
                        if (tupleIds.Skip(round * PreferedBatchSize).Take(PreferedBatchSize).Count() > 0)
                        {
                            Dictionary<string, object> parameters = new Dictionary<string, object>();
                            parameters.Add("idsList", tupleIds.Skip(round * PreferedBatchSize).Take(PreferedBatchSize).ToList());
                            tuplesRepo.Execute(string.Format(queryStr, "DataTuple"), parameters);
                        }
                    }
                }
                if (contentDescriptorIds != null && contentDescriptorIds.Count > 0)
                {
                    long iternations = contentDescriptorIds.Count / PreferedBatchSize;
                    if (iternations * PreferedBatchSize < contentDescriptorIds.Count)
                        iternations++;

                    for (int round = 0; round < iternations; round++)
                    {
                        if (contentDescriptorIds.Skip(round * PreferedBatchSize).Take(PreferedBatchSize).Count() > 0)
                        {
                            Dictionary<string, object> parameters = new Dictionary<string, object>();
                            parameters.Add("idsList", contentDescriptorIds.Skip(round * PreferedBatchSize).Take(PreferedBatchSize).ToList());
                            tuplesRepo.Execute(string.Format(queryStr, "ContentDescriptor"), parameters);
                        }
                    }
                }
                if (versionIds != null && versionIds.Count > 0)
                {
                    long iternations = versionIds.Count / PreferedBatchSize;
                    if (iternations * PreferedBatchSize < versionIds.Count)
                        iternations++;
                    for (int round = 0; round < iternations; round++)
                    {
                        if (versionIds.Skip(round * PreferedBatchSize).Take(PreferedBatchSize).Count() > 0)
                        {
                            Dictionary<string, object> parameters = new Dictionary<string, object>();
                            parameters.Add("idsList", versionIds.Skip(round * PreferedBatchSize).Take(PreferedBatchSize).ToList());
                            versionRepo.Execute(string.Format(queryStr, "DatasetVersion"), parameters);
                        }
                    }
                }
                {
                    Dictionary<string, object> parameters = new Dictionary<string, object>();
                    parameters.Add("idsList", new List<Int64>() { entity.Id });
                    repo.Execute(string.Format(queryStr, "Dataset"), parameters);
                    //repo.Delete(entity);
                }
                uow.Commit();
            }
            // if any problem was detected during the commit, an exception will be thrown!
            return (true);
        }

        #endregion

        #region DatasetVersion

        /// <summary>
        /// Each dataset may have more than one versions, each having their own data tuples. The data tuples of the latest version are kept in a separate collection,
        /// but the previous versions are scattered among the data tuple and historical tuple collections. The later is the place that acts as the place to keep record of
        /// all the previous actions done on the dataset and its their results.
        /// This method may be called to get the tuples of the version 2 while the current version is i.e. 10. 
        /// Based on the status of the requested version, the method may use the tuple collection alone or in combination with the history records to rebuild the version as it was at its check-in time.
        /// The versions are stored in the tuple collection in a differential way, so that the version 3 computes the differences to the version 2 and applies the difference only.
        /// So retrieving algorithm in this method rebuilds the requested version from its own and previous versions' tuples.
        /// If the latest version is requested and the dataset is checked in, the algorithm retrieves all tuples in the tuple collection associated with the current and all previous versions.
        /// If the latest version is requested and the dataset is in checked-out state, the method retrieves the working copy tuples.
        /// </summary>
        /// <param name="datasetVersion">The object representing the data set version requested.</param>
        /// <returns>A list of data tuples representing the associated data of the version requested.</returns>
        /// <remarks>The returned list may contain normal and historic tuples, if the requested version is not the latest. All the tuples are <b>materialized</b>.</remarks>
        public List<AbstractTuple> GetDatasetVersionEffectiveTuples(DatasetVersion datasetVersion)
        {
            return getDatasetVersionEffectiveTuples(datasetVersion);
        }

        /// <summary>
        /// Returns one page of the data tuples of the dataset version requested. See <see cref="GetDatasetVersionEffectiveTuples"/> for more details about the effective tuples of a dataset.
        /// </summary>
        /// <param name="datasetVersion"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns>A list containing one page of maximum length of <paramref name="pageSize"/> from the effective tuples of the <paramref name="datasetVersion"/>.</returns>
        /// <remarks>The actual returned tuples depend on the status of the dataset and whether the requested version is the latest.</remarks>
        //[MeasurePerformance]
        public List<AbstractTuple> GetDatasetVersionEffectiveTuples(DatasetVersion datasetVersion, int pageNumber, int pageSize)
        {
            return getDatasetVersionEffectiveTuples(datasetVersion, pageNumber, pageSize);
        }

        /// <summary>
        /// Returns a list of <b>identifiers</b> of the effective tuples of the dataset version requested. See <see cref="GetDatasetVersionEffectiveTuples"/> for more details about the effective tuples of a dataset.
        /// </summary>
        /// <param name="datasetVersion">The object representing the data set version requested</param>
        /// <returns>The list of identifiers of the specified version</returns>
        public List<Int64> GetDatasetVersionEffectiveTupleIds(DatasetVersion datasetVersion)
        {
            return getDatasetVersionEffectiveTupleIds(datasetVersion);
        }


        /// <summary>
        /// Returns the number of the effective tuples of the dataset version requested.
        /// See <see cref="GetDatasetVersionEffectiveTuples"/> for more details about the effective tuples of a dataset.
        /// </summary>
        /// <param name="datasetVersion">The object representing the data set version requested</param>
        /// <returns>The number of the specified version</returns>
        public Int64 GetDatasetVersionEffectiveTupleCount(DatasetVersion datasetVersion)
        {
            return getDatasetVersionEffectiveTupleCount(datasetVersion);
        }

        /// <summary>
        /// Returns all checked-in versions of the dataset <paramref name="datasetId"/>. 
        /// </summary>
        /// <param name="datasetId">The identifier of the dataset.</param>
        /// <returns>The list of checked-in versions of the dataset requested.</returns>
        /// <remarks>The checked-out version, if exists, is not included in the return list.</remarks>
        public List<DatasetVersion> GetDatasettVersions(Int64 datasetId)
        {
            List<DatasetVersion> dsVersions = DatasetVersionRepo.Query(p => 
                p.Dataset.Id == datasetId 
                && p.Dataset.Status == DatasetStatus.CheckedIn)
                .OrderByDescending(p => p.Timestamp).ToList();
            if (dsVersions != null)
            {
                //dsVersions.ForEach(p=> p.Materialize());
                return (dsVersions);
            }
            try
            {
                Dataset dataset = DatasetRepo.Get(datasetId);
                if (dataset == null)
                    throw new Exception(string.Format("Dataset {0} does not exist!", datasetId));
                if (dataset.Status == DatasetStatus.Deleted)
                    throw new Exception(string.Format("Dataset {0} is deleted", datasetId));
                if (dataset.Status == DatasetStatus.CheckedOut)
                {
                    dsVersions = dataset.Versions.Where(p => p.Status == DatasetVersionStatus.Old || p.Status == DatasetVersionStatus.CheckedIn).ToList(); //dataset.Versions.OrderByDescending(p => p.Timestamp).Skip(1).ToList(); // the first version in the list is the working copy
                    if (dsVersions != null)
                    {
                        //dsVersions.ForEach(p => p.Materialize());
                        return (dsVersions);
                    }
                }
            }
            catch
            {
                throw new Exception(string.Format("Dataset {0} does not exist or an  error occurred!", datasetId));
            }
            return (null);
        }

        /// <summary>
        /// Returns the dataset version specified by the version identifier <paramref name="versionId"/>.
        /// If the requested version is the latest but the dataset is checked-out an exception is thrown.
        /// </summary>
        /// <param name="versionId">The identifier of the dataset version requested.</param>
        /// <returns>The retrieved dataset version</returns>
        /// <exception cref="Exception">The method throws an exception in the following cases:
        ///     <list type="bullet">
        ///         <item><description>The provided version is is greater than the latest version identifier</description></item>
        ///         <item><description>The provided version identifier is not associated with any version.</description></item>
        ///         <item><description>The identifier is pointing to a version, but the version is not associated to any dataset (orphan version).</description></item>
        ///         <item><description>The version is associated to a dataset which is marked as deleted.</description></item>
        ///         <item><description>The identifier is pointing to a working copy, which means the dataset is checked out and the identifier is pointing to the latest (not committed working copy).</description></item>
        ///     </list>
        /// </exception>
        public DatasetVersion GetDatasetVersion(Int64 versionId)
        {
            /// check whether the version id is in fact the latest? the latest checked in version should be returned. if dataset is checked out, the latest stored version is hidden yet.
            /// If the dataset is marked as deleted its like that it is not there at all
            /// get the latest version from the Versions property, or run a direct query on the db
            /// get the latest version by querying Tuples table for records with version <= latest version

            // the requested version is earlier than the latest regardless of check-in/ out status or its the latest version and the dataset is checked in.
            DatasetVersion dsVersion = DatasetVersionRepo.Query(p =>
                                        p.Id == versionId
                                        && (
                                                    (p.Dataset.Status == DatasetStatus.CheckedIn && p.Status == DatasetVersionStatus.CheckedIn)
                                                || (p.Dataset.Status != DatasetStatus.Deleted && p.Status == DatasetVersionStatus.Old)
                                            )
                                        )
                                      .FirstOrDefault();
            if (dsVersion != null)
                return (dsVersion);

            // else there is a problem, try to find and report it
            Dataset dataset = DatasetVersionRepo.Get(versionId).Dataset; // it would be nice to not fetch the dataset!

            if (dataset.Status == DatasetStatus.Deleted)
                throw new Exception(string.Format("Dataset version {0} is not associated with any dataset.", versionId));
            if (dataset.Status == DatasetStatus.Deleted)
                throw new Exception(string.Format("Dataset {0} is deleted", dataset.Id));
            Int64 latestVersionId = dataset.Versions.Where(p => p.Status == DatasetVersionStatus.CheckedIn).Select(p => p.Id).First();// .OrderByDescending(t => t.Timestamp).First().Id;
            if (versionId > latestVersionId)
                throw new Exception(string.Format("Invalid version id. The version id {0} is greater than the latest version number!", versionId));

            if (latestVersionId.Equals(versionId) && dataset.Status == DatasetStatus.CheckedOut) // its a request for the working copy which is hidden
                throw new Exception(string.Format("Invalid version is requested. The version {0} points to the working copy!", versionId));
            return null;
        }

        /// <summary>
        /// Returns the latest version of the dataset <paramref name="datasetId"/> if the dataset is in checked-in state, 
        /// otherwise it throws an exception.
        /// </summary>
        /// <param name="datasetId">The identifier of the dataset</param>
        /// <returns>The latest dataset version</returns>
        /// <exception cref="Exception">The method throws an exception if the dataset does not exist, is deleted, or is checked out.</exception>
        public DatasetVersion GetDatasetLatestVersion(Int64 datasetId)
        {
            return getDatasetLatestVersion(datasetId);
        }

        /// <summary>
        /// Returns the latest version of the dataset <paramref name="dataset"/> if the dataset is in checked-in state, 
        /// otherwise it throws an exception.
        /// </summary>
        /// <param name="dataset">The dataset instance</param>
        /// <returns>The latest version of the dataset</returns>
        /// <exception cref="Exception">The method throws an exception if the dataset is null, deleted, or checked out.</exception>
        public DatasetVersion GetDatasetLatestVersion(Dataset dataset)
        {
            /// the latest checked in version should be returned.
            /// if dataset is checked out, exception
            /// If the dataset is marked as deleted its like that it is not there at all

            return getDatasetLatestVersion(dataset);
        }

        /// <summary>
        /// Returns a list of the latest versions of the provided <paramref name="datasetIds"/> including/ excluding the checked out versions.
        /// </summary>
        /// <param name="datasetIds">The list of identifiers of the datasets whose their latest versions is requested</param>
        /// <param name="includeCheckouts">Determines whether the checked out versions should be included in the result.</param>
        /// <returns>The list of the latest versions of the provided datasets</returns>
        public List<DatasetVersion> GetDatasetLatestVersions(List<Int64> datasetIds, bool includeCheckouts = false)
        {
            if (includeCheckouts) // the working copy versions of checked out datasets are also included
            {
                var q1 = DatasetVersionRepo.Query(p => 
                        datasetIds.Contains(p.Dataset.Id) 
                        && (p.Dataset.Status == DatasetStatus.CheckedIn || p.Dataset.Status == DatasetStatus.CheckedOut) 
                        && (p.Status == DatasetVersionStatus.CheckedIn || p.Status == DatasetVersionStatus.CheckedOut)
                    );
                return (q1.ToList());
            }
            else //just latest checked in versions or checked in datasets 
            {
                var q1 = DatasetVersionRepo.Query(p =>
                        datasetIds.Contains(p.Dataset.Id)
                        && (p.Dataset.Status == DatasetStatus.CheckedIn)
                        && (p.Status == DatasetVersionStatus.CheckedIn)
                    );
                return (q1.ToList());
            }
            // also works, but uses the time stamps instead of STATUS info
           // var qu = (from dsv in DatasetVersionRepo.Get(p => datasetIds.Contains(p.Dataset.Id) && p.Dataset.Status != DatasetStatus.Deleted)
           //           group dsv by dsv.Dataset.Id into grp
           //           let maxTimestamp = grp.Max(p => p.Timestamp)
           //           select grp.Single(p => p.Timestamp >= maxTimestamp)); 
            
           //return (qu.ToList());
        }

        /// <summary>
        /// Returns a list of the latest versions of all datasets including/ excluding the checked out versions.
        /// </summary>
        /// <param name="includeCheckouts">Determines whether the checked out versions should be included in the result.</param>
        /// <returns>The list of the latest versions of all datasets</returns>
        public List<DatasetVersion> GetDatasetLatestVersions(bool includeCheckouts = false)
        {
            if (includeCheckouts) // the working copy versions of checked out datasets are also included
            {
                var q1 = DatasetVersionRepo.Query(p =>                        
                        (p.Dataset.Status == DatasetStatus.CheckedIn || p.Dataset.Status == DatasetStatus.CheckedOut)
                        && (p.Status == DatasetVersionStatus.CheckedIn || p.Status == DatasetVersionStatus.CheckedOut)
                    );
                return (q1.ToList());
            }
            else //just latest checked in versions or checked in datasets 
            {
                var q1 = DatasetVersionRepo.Query(p =>
                        (p.Dataset.Status == DatasetStatus.CheckedIn)
                        && (p.Status == DatasetVersionStatus.CheckedIn)
                    );
                return (q1.ToList());
            }

            //// its a mixed query that happens partially in the database. The grouping is happening in the memory which is BAD. JAVAD. 
            //// I have tested a full DB version but does not work.  needs more investigation

            //var qu = (from dsv in DatasetVersionRepo.Get(p => p.Dataset.Status != DatasetStatus.Deleted)
            //          group dsv by dsv.Dataset.Id into grp
            //          let maxTimestamp = grp.Max(p => p.Timestamp)
            //          select grp.Single(p => p.Timestamp >= maxTimestamp));

            //return (qu.ToList());
        }


        /// <summary>
        /// Returns a list of the latest versions of all datasets associated to a data structure, including/ excluding the checked out versions.
        /// </summary>
        /// <param name="structureId">The data structure that its associated datasets are searched.</param>
        /// <param name="includeCheckouts">Determines whether the checked out versions should be included in the result.</param>
        /// <returns>The list of the latest versions of the matching datasets alongside with thier dataset identifiers.</returns>
        /// <remarks>identifiers are returned to reduce the number of database roundtrips!</remarks>
        public Dictionary<Int64, DatasetVersion> GetDatasetLatestVersions(Int64 structureId, bool includeCheckouts = false)
        {
            if (includeCheckouts) // the working copy versions of checked out datasets are also included
            {
                var q1 = DatasetVersionRepo.Query(p =>
                        (p.Dataset.DataStructure.Id == structureId)
                        && (p.Dataset.Status == DatasetStatus.CheckedIn || p.Dataset.Status == DatasetStatus.CheckedOut)
                        && (p.Status == DatasetVersionStatus.CheckedIn || p.Status == DatasetVersionStatus.CheckedOut)
                    ).Select(p => new KeyValuePair<Int64, DatasetVersion>(p.Dataset.Id, p));
                return (q1.ToList().ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            }
            else //just latest checked in versions or checked in datasets 
            {
                var q1 = DatasetVersionRepo.Query(p =>
                        (p.Dataset.DataStructure.Id == structureId)
                        && (p.Dataset.Status == DatasetStatus.CheckedIn)
                        && (p.Status == DatasetVersionStatus.CheckedIn)
                    ).Select(p => new KeyValuePair<Int64, DatasetVersion>(p.Dataset.Id, p));
                return (q1.ToList().ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            }
        }

        public DatasetVersion GetDatasetWorkingCopy(Int64 datasetId)
        {
            return getDatasetWorkingCopy(datasetId);
        }

        /// <summary>
        /// Returns the metadata of the latest versions of all datasets, alongside with their identifiers including/ excluding the checked out versions.
        /// </summary>
        /// <param name="includeCheckouts">Determines whether the checked out versions should be included in the result.</param>
        /// <returns>The Dictionary of the identifier/ metadata pairs of the latest versions of all datasets</returns>
        public Dictionary<Int64, XmlDocument> GetDatasetLatestMetadataVersions(bool includeCheckouts = false)
        {
            if (includeCheckouts) // the working copy versions of checked out datasets are also included
            {
                var q1 = DatasetVersionRepo.Query(p =>
                        (p.Dataset.Status == DatasetStatus.CheckedIn || p.Dataset.Status == DatasetStatus.CheckedOut)
                        && (p.Status == DatasetVersionStatus.CheckedIn || p.Status == DatasetVersionStatus.CheckedOut)
                    ).Select(p => new KeyValuePair<Int64, XmlDocument>(p.Dataset.Id, p.Metadata));
                return (q1.ToList().ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            }
            else //just latest checked in versions or checked in datasets 
            {
                var q1 = DatasetVersionRepo.Query(p =>
                        (p.Dataset.Status == DatasetStatus.CheckedIn || p.Dataset.Status == DatasetStatus.CheckedOut) // include checked in (latest) versions of currently checked out datasets
                        && (p.Status == DatasetVersionStatus.CheckedIn)
                    ).Select(p => new KeyValuePair<Int64, XmlDocument>(p.Dataset.Id, p.Metadata));
                return (q1.ToList().ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            }

            //// it works using the timestamp technique
            //var qu = (from dsv in DatasetVersionRepo.Get(p => p.Dataset.Status != DatasetStatus.Deleted)
            //         group dsv by dsv.Dataset.Id into grp
            //         let maxTimestamp = grp.Max(p => p.Timestamp)
            //         select grp.Single(p => p.Timestamp >= maxTimestamp).Metadata);

            //return (qu.ToList());
        }

        /// <summary>
        /// Returns the metadata of the latest versions of all datasets associated to a data structure, alongside with their identifiers including/ excluding the checked out versions.
        /// </summary>
        /// <param name="structureId">The data structure that its associated datasets are searched.</param>
        /// <param name="includeCheckouts">Determines whether the checked out versions should be included in the result.</param>
        /// <returns>The Dictionary of the identifier/ metadata pairs of the latest versions of all datasets</returns>
        public Dictionary<Int64, XmlDocument> GetDatasetLatestMetadataVersions(Int64 structureId, bool includeCheckouts = false)
        {
            if (includeCheckouts) // the working copy versions of checked out datasets are also included
            {
                var q1 = DatasetVersionRepo.Query(p =>
                        (p.Dataset.DataStructure.Id == structureId)
                        && (p.Dataset.Status == DatasetStatus.CheckedIn || p.Dataset.Status == DatasetStatus.CheckedOut)
                        && (p.Status == DatasetVersionStatus.CheckedIn || p.Status == DatasetVersionStatus.CheckedOut)
                    ).Select(p => new KeyValuePair<Int64, XmlDocument>(p.Dataset.Id, p.Metadata));
                return (q1.ToList().ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            }
            else //just latest checked in versions or checked in datasets 
            {
                var q1 = DatasetVersionRepo.Query(p =>
                        (p.Dataset.DataStructure.Id == structureId)
                        && (p.Dataset.Status == DatasetStatus.CheckedIn || p.Dataset.Status == DatasetStatus.CheckedOut) // include checked in (latest) versions of currently checked out datasets
                        && (p.Status == DatasetVersionStatus.CheckedIn)
                    ).Select(p => new KeyValuePair<Int64, XmlDocument>(p.Dataset.Id, p.Metadata));
                return (q1.ToList().ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            }

            //// it works using the timestamp technique
            //var qu = (from dsv in DatasetVersionRepo.Get(p => p.Dataset.Status != DatasetStatus.Deleted)
            //         group dsv by dsv.Dataset.Id into grp
            //         let maxTimestamp = grp.Max(p => p.Timestamp)
            //         select grp.Single(p => p.Timestamp >= maxTimestamp).Metadata);

            //return (qu.ToList());
        }

        /// <summary>
        /// Returns the list of identifiers of all the matching datasets.
        /// If <paramref name="includeCheckouts"/> is false, just the datasets having the latest version checked-in are included
        /// , otherwise the datasets with versions either checked-in or checked-out will be included.
        /// </summary>
        /// <param name="includeCheckouts">Determines whether the checked out versions should be included in the result.</param>
        /// <returns>The list of the identifiers of all the matching datasets</returns>
        public List<Int64> GetDatasetLatestIds(bool includeCheckouts = false)
        {
            if (includeCheckouts) // the datasets that their latest version is checked-in or checked-out
            {
                var q1 = DatasetVersionRepo.Query(p =>
                        (p.Dataset.Status == DatasetStatus.CheckedIn || p.Dataset.Status == DatasetStatus.CheckedOut)
                        && (p.Status == DatasetVersionStatus.CheckedIn || p.Status == DatasetVersionStatus.CheckedOut)
                    )
                    .Select(p => p.Dataset.Id)
                    .OrderBy(p=>p)
                    .Distinct();
                return (q1.ToList());
            }
            else //just the datasets that their latest version is checked-in
            {
                var q1 = DatasetVersionRepo.Query(p =>
                        (p.Dataset.Status == DatasetStatus.CheckedIn || p.Dataset.Status == DatasetStatus.CheckedOut) // include checked in (latest) versions of currently checked out datasets
                        && (p.Status == DatasetVersionStatus.CheckedIn)
                    )
                    .Select(p => p.Dataset.Id)
                    .OrderBy(p => p)
                    .Distinct();
                return (q1.ToList());
            }

            //// it works using the timestamp technique
            //var qu = (from dsv in DatasetVersionRepo.Get(p => p.Dataset.Status != DatasetStatus.Deleted)
            //         group dsv by dsv.Dataset.Id into grp
            //         let maxTimestamp = grp.Max(p => p.Timestamp)
            //         select grp.Single(p => p.Timestamp >= maxTimestamp).Metadata);

            //return (qu.ToList());
        }

        /// <summary>
        /// Returns the list of identifiers of the latest version of of the datasets.
        /// </summary>
        /// <param name="includeCheckouts">Determines whether the checked out versions should be included in the result.</param>
        /// <returns>The list of the identifiers of all the matching dataset versions</returns>
        public List<Int64> GetDatasetVersionLatestIds(bool includeCheckouts = false)
        {
            if (includeCheckouts) // the working copy versions of checked out datasets are also included
            {
                var q1 = DatasetVersionRepo.Query(p =>
                        (p.Dataset.Status == DatasetStatus.CheckedIn || p.Dataset.Status == DatasetStatus.CheckedOut)
                        && (p.Status == DatasetVersionStatus.CheckedIn || p.Status == DatasetVersionStatus.CheckedOut)
                    )
                    .Select(p => p.Id)
                    .OrderBy(p=>p)
                    .Distinct();
                return (q1.ToList());
            }
            else //just latest checked in versions
            {
                var q1 = DatasetVersionRepo.Query(p =>
                        (p.Dataset.Status == DatasetStatus.CheckedIn || p.Dataset.Status == DatasetStatus.CheckedOut)
                        && (p.Status == DatasetVersionStatus.CheckedIn)
                    )
                    .Select(p => p.Id)
                    .OrderBy(p => p)
                    .Distinct();
                return (q1.ToList());
            }
        }

        /// <summary>
        /// Returns the metadata of the latest versions of the dataset <param name="datasetId"></param>.
        /// </summary>
        /// <param name="datasetId">The dataset whose latest metadata version is returned.</param>
        /// <param name="includeCheckouts">Determines whether the method should return the metadata if the dataset is checked-out.</param>
        /// <returns>The metadata of the latest version of the specified dataset as an <typeparamref name="XmlDocument"/>.</returns>
        public XmlDocument GetDatasetLatestMetadataVersion(Int64 datasetId, bool includeCheckouts = false)
        {
            if (includeCheckouts) // the working copy version of checked out dataset is also included
            {
                var q1 = DatasetVersionRepo.Query(p =>
                                (p.Dataset.Status == DatasetStatus.CheckedIn || p.Dataset.Status == DatasetStatus.CheckedOut)
                            && (p.Status == DatasetVersionStatus.CheckedIn || p.Status == DatasetVersionStatus.CheckedOut)
                            && (p.Dataset.Id == datasetId)
                        ).Select(p => p.Metadata);
                return (q1.FirstOrDefault());
            }
            else //just latest checked in version of the checked in dataset
            {
                var q1 = DatasetVersionRepo.Query(p =>
                                (p.Dataset.Status == DatasetStatus.CheckedIn || p.Dataset.Status == DatasetStatus.CheckedOut) // include checked in (latest) versions of currently checked out datasets
                            && (p.Status == DatasetVersionStatus.CheckedIn)
                            && (p.Dataset.Id == datasetId)
                        ).Select(p => p.Metadata);
                return (q1.FirstOrDefault());
            }

            //// it works using the timestamp technique
            //var qu = (from dsv in DatasetVersionRepo.Get(p => p.Dataset.Status != DatasetStatus.Deleted)
            //         group dsv by dsv.Dataset.Id into grp
            //         let maxTimestamp = grp.Max(p => p.Timestamp)
            //         select grp.Single(p => p.Timestamp >= maxTimestamp).Metadata);

            //return (qu.ToList());
        }

        /// <summary>
        /// reports what changes have been done by the version specified by <paramref name="versionId"/>. Deletions, updates, new records, and changes in the dataset attributes are among the reported items.
        /// </summary>
        /// <param name="versionId"></param>
        /// <returns>Not clearly defined it</returns>
        public object GetDatasetVersionProfile(Int64 versionId)
        {
            /// get the latest version from the Versions property, or run a direct query on the db
            /// get the latest version by querying Tuples table for records with version <= latest version
            /// 
            return null;
        }

        /// <summary>
        /// Applies the submitted changes to the working copy and persists the changes but does <b>NOT</b> check-in the dataset.
        /// The changes are coming in the form of the tuples to be added, deleted, or editedVersion.
        /// The general procedure of making changes is CheckOut, Edit (one or more times), CheckIn or Rollback.
        /// there is no need to pass metadata, extendedPropertyValues, contentDescriptors .. as they can be assigned to the working copy version before sending it to the method.
        /// Just if they are null, they will not affect the version. 
        /// The general procedure is CheckOut, Edit*, CheckIn or Rollback
        /// While the dataset is checked out, all the changes go to the latest+1 version which acts like a working copy
        /// </summary>
        /// <param name="workingCopyDatasetVersion">The working copy version that accepts the changes.</param>
        /// <param name="createdTuples">The list of the new tuples to be added to the working copy.</param>
        /// <param name="editedTuples">The list of the tuples whose values have been changed and the changes should be considered in the working copy.</param>
        /// <param name="deletedTuples">The list of existing tuples to be deleted from the working copy.</param>
        /// <param name="unchangedTuples">to be removed</param>
        /// <returns>The working copy having the changes applied on it.</returns>
        public DatasetVersion EditDatasetVersion(DatasetVersion workingCopyDatasetVersion,
            ICollection<DataTuple> createdTuples, ICollection<DataTuple> editedTuples, ICollection<DataTuple> deletedTuples, ICollection<DataTuple> unchangedTuples = null
            //,ICollection<ExtendedPropertyValue> extendedPropertyValues, ICollection<ContentDescriptor> contentDescriptors
            )
        {
            workingCopyDatasetVersion.Dematerialize(false);
            
            //preserve metadata and XmlExtendedPropertyValues for later use
            var workingCopyDatasetVersionId = workingCopyDatasetVersion.Id;
            var metadata = workingCopyDatasetVersion.Metadata;
            var xmlExtendedPropertyValues = workingCopyDatasetVersion.XmlExtendedPropertyValues;
            var contentDescriptors = workingCopyDatasetVersion.ContentDescriptors;

            // do not move them to editDatasetVersion function
            this.DatasetRepo.Evict();
            this.DatasetVersionRepo.Evict();
            this.DataTupleRepo.Evict();
            this.DataTupleVerionRepo.Evict();
            this.DatasetRepo.UnitOfWork.ClearCache();

            // maybe its better to use Merge function ...
            workingCopyDatasetVersion = this.DatasetVersionRepo.Get(workingCopyDatasetVersionId);
            if (metadata != null)
                workingCopyDatasetVersion.Metadata = metadata;
            if (xmlExtendedPropertyValues != null)
                workingCopyDatasetVersion.XmlExtendedPropertyValues = xmlExtendedPropertyValues;
            if (contentDescriptors != null)
                workingCopyDatasetVersion.ContentDescriptors = contentDescriptors;

            return editDatasetVersion(workingCopyDatasetVersion, createdTuples, editedTuples, deletedTuples, unchangedTuples);
        }

        #endregion

        #region Private Methods

        private DatasetVersion getDatasetLatestVersion(Int64 datasetId)
        {
            DatasetVersion dsVersion = DatasetVersionRepo.Query(p =>
                    p.Dataset.Id == datasetId
                    && p.Dataset.Status == DatasetStatus.CheckedIn
                    && p.Status == DatasetVersionStatus.CheckedIn)
                    .FirstOrDefault();//DatasetVersionRepo.Query(p => p.Dataset.Id == datasetId && p.Dataset.Status == DatasetStatus.CheckedIn).OrderByDescending(p => p.Timestamp).FirstOrDefault();
            if (dsVersion != null)
            {
                //dsVersion.Materialize();
                return (dsVersion);
            }
            try
            {
                Dataset dataset = DatasetRepo.Get(datasetId);
                if (dataset == null)
                    throw new Exception(string.Format("Dataset {0} does not exist!", datasetId));
                if (dataset.Status == DatasetStatus.Deleted)
                    throw new Exception(string.Format("Dataset {0} is deleted", datasetId));
                if (dataset.Status == DatasetStatus.CheckedOut)
                {
                    throw new Exception(string.Format("Dataset {0} is checked out.", datasetId));
                }
            }
            catch (Exception ex)
            {
              throw ex; // new Exception(string.Format("Dataset {0} does not exist or an  error occurred!", datasetId));
            }
            return (null);
        }

        private DatasetVersion getDatasetWorkingCopy(Int64 datasetId)
        {
            DatasetVersion dsVersion = DatasetVersionRepo.Get(p =>
                                       p.Dataset.Id == datasetId
                                       && p.Dataset.Status == DatasetStatus.CheckedOut
                                       && p.Status == DatasetVersionStatus.CheckedOut
                                       )
                                     .FirstOrDefault();
            if (dsVersion != null)
            {
                dsVersion.Materialize();
                return (dsVersion);
            }

            // else there is a problem, try to find and report it
            Dataset dataset = DatasetRepo.Get(datasetId); // it would be nice to not fetch the dataset!
            if (dataset.Status == DatasetStatus.Deleted)
                throw new Exception(string.Format("Dataset {0} is deleted", datasetId));
            if (dataset.Status == DatasetStatus.CheckedIn)
                throw new Exception(string.Format("Dataset {0} is in checked in state", datasetId));
            return null;
        }
        
        private List<AbstractTuple> getDatasetVersionEffectiveTuples(DatasetVersion datasetVersion)
        {
            List<AbstractTuple> tuples = new List<AbstractTuple>();
            Dataset dataset = datasetVersion.Dataset;
            if (dataset.Status == DatasetStatus.Deleted)
                throw new Exception(string.Format("Provided dataset version {0} belongs to deleted dataset {1}.", datasetVersion.Id, dataset.Id));
            Int64 latestVersionId = dataset.Versions.OrderByDescending(t => t.Timestamp).Where(p => p.Timestamp <= dataset.LastCheckIOTimestamp).First().Id; // no need to replace it with the STATUS version
            if (datasetVersion.Id > latestVersionId)
                throw new Exception(string.Format("Invalid version id. The dataset version id {0} is greater than the latest version number!", datasetVersion.Id));

            if (latestVersionId == datasetVersion.Id && dataset.Status == DatasetStatus.CheckedOut) // its a request for the working copy
            {
                tuples = getWorkingCopyTuples(datasetVersion).Cast<AbstractTuple>().ToList();
            }
            else if (latestVersionId == datasetVersion.Id && dataset.Status == DatasetStatus.CheckedIn) // its a request for the latest checked in version that should be served from the Tuples table
            {
                tuples = getPrimaryTuples(datasetVersion).Cast<AbstractTuple>().ToList();
            }
            else
            {
                tuples = getHistoricTuples(datasetVersion);
            }
            tuples.ForEach(p => p.Materialize());
            return (tuples);
        }

        private List<AbstractTuple> getDatasetVersionEffectiveTuples(DatasetVersion datasetVersion, int pageNumber, int pageSize)
        {
            List<AbstractTuple> tuples = new List<AbstractTuple>();
            Dataset dataset = datasetVersion.Dataset;
            if (dataset.Status == DatasetStatus.Deleted)
                throw new Exception(string.Format("Provided dataset version {0} belongs to deleted dataset {1}.", datasetVersion.Id, dataset.Id));
            Int64 latestVersionId = dataset.Versions.OrderByDescending(t => t.Timestamp).Where(p => p.Timestamp <= dataset.LastCheckIOTimestamp).First().Id; // no need to replace it with the STATUS version
            if (datasetVersion.Id > latestVersionId)
                throw new Exception(string.Format("Invalid version id. The dataset version id {0} is greater than the latest version number!", datasetVersion.Id));

            if (latestVersionId == datasetVersion.Id && dataset.Status == DatasetStatus.CheckedOut) // its a request for the working copy
            {
                tuples = getWorkingCopyTuples(datasetVersion, pageNumber, pageSize).Cast<AbstractTuple>().ToList();
            }
            else if (latestVersionId == datasetVersion.Id && dataset.Status == DatasetStatus.CheckedIn) // its a request for the latest checked-in version that should be served from the Tuples table
            {
                tuples = getPrimaryTuples(datasetVersion, pageNumber, pageSize).Cast<AbstractTuple>().ToList();
            }
            else
            {
                tuples = getHistoricTuples(datasetVersion, pageNumber, pageSize).Cast<AbstractTuple>().ToList(); // its a request for version earlier than the current version, whether the latest version is check-out or in.
            }
            //Parallel.ForEach(tuples, p => p.Materialize()); // causes object serialization issues
            tuples .ForEach(p => p.Materialize());
            return (tuples);
        }

        private List<Int64> getDatasetVersionEffectiveTupleIds(DatasetVersion datasetVersion)
        {
            List<Int64> tuples = new List<Int64>();
            Dataset dataset = datasetVersion.Dataset;
            if (dataset.Status == DatasetStatus.Deleted)
                throw new Exception(string.Format("Provided dataset version {0} belongs to deleted dataset {1}.", datasetVersion.Id, dataset.Id));
            Int64 latestVersionId = dataset.Versions.OrderByDescending(t => t.Timestamp).Where(p => p.Timestamp <= dataset.LastCheckIOTimestamp).First().Id; // no need to replace it with the STATUS version
            if (datasetVersion.Id > latestVersionId)
                throw new Exception(string.Format("Invalid version id. The dataset version id {0} is greater than the latest version number!", datasetVersion.Id));

            if (latestVersionId == datasetVersion.Id && dataset.Status == DatasetStatus.CheckedOut) // its a request for the working copy
            {
                tuples = getWorkingCopyTupleIds(datasetVersion);
            }
            else if (latestVersionId == datasetVersion.Id && dataset.Status == DatasetStatus.CheckedIn) // its a request for the latest checked in version that should be served from the Tuples table
            {
                tuples = getPrimaryTupleIds(datasetVersion);
            }
            else
            {
                throw new NotSupportedException(string.Format("Invalid version id. The dataset version id {0} is not referring to the latest or working versions. This function is able to access historical versions, use GetDatasetVersionEffectiveTuples function instead!", datasetVersion.Id));
            }
            return (tuples);
        }

        private Int32 getDatasetVersionEffectiveTupleCount(DatasetVersion datasetVersion)
        {
            Int32 tuplesCount = 0;
            Dataset dataset = datasetVersion.Dataset;
            if (dataset.Status == DatasetStatus.Deleted)
                throw new Exception(string.Format("Provided dataset version {0} belongs to deleted dataset {1}.", datasetVersion.Id, dataset.Id));
            Int64 latestVersionId = dataset.Versions.OrderByDescending(t => t.Timestamp).Where(p => p.Timestamp <= dataset.LastCheckIOTimestamp).First().Id; // no need to replace it with the STATUS version
            if (datasetVersion.Id > latestVersionId)
                throw new Exception(string.Format("Invalid version id. The dataset version id {0} is greater than the latest version number!", datasetVersion.Id));

            if (latestVersionId == datasetVersion.Id && dataset.Status == DatasetStatus.CheckedOut) // its a request for the working copy
            {
                tuplesCount = getWorkingCopyTupleCount(datasetVersion);
            }
            else if (latestVersionId == datasetVersion.Id && dataset.Status == DatasetStatus.CheckedIn) // its a request for the latest checked in version that should be served from the Tuples table
            {
                tuplesCount = getPrimaryTupleCount(datasetVersion);
            }
            else
            {
                throw new NotSupportedException(string.Format("Invalid version id. The dataset version id {0} is not referring to the latest or working versions. This function is able to access historical versions, use GetDatasetVersionEffectiveTuples function instead!", datasetVersion.Id));
            }
            return (tuplesCount);
        }

        //[MeasurePerformance]
        private DatasetVersion editDatasetVersion(DatasetVersion workingCopyDatasetVersion, ICollection<DataTuple> createdTuples, ICollection<DataTuple> editedTuples, ICollection<DataTuple> deletedTuples, ICollection<DataTuple> unchangedTuples)
        {
            Contract.Requires(workingCopyDatasetVersion.Dataset != null && workingCopyDatasetVersion.Dataset.Id >= 0);
            Contract.Requires(workingCopyDatasetVersion.Dataset.Status == DatasetStatus.CheckedOut);

            Contract.Ensures(Contract.Result<DatasetVersion>() != null && Contract.Result<DatasetVersion>().Id >= 0);

            // be sure you are working on the latest version (working copy). applyTupleChanges takes the working copy from the DB            
            List<DataTupleVersion> tobeAdded = new List<DataTupleVersion>();
            List<DataTuple> tobeDeleted = new List<DataTuple>();
            List<DataTuple> tobeEdited = new List<DataTuple>();

            DatasetVersion editedVersion = applyTupleChanges(workingCopyDatasetVersion, ref tobeAdded, ref tobeDeleted, ref tobeEdited, createdTuples, editedTuples, deletedTuples, unchangedTuples);

            #region main code
            //using (IUnitOfWork uow = this.GetUnitOfWork())
            //{
            //    IRepository<DatasetVersion> repo = uow.GetRepository<DatasetVersion>();
            //    IRepository<DataTupleVersion> tupleVersionRepo = uow.GetRepository<DataTupleVersion>();
            //    IRepository<DataTuple> tupleRepo = uow.GetRepository<DataTuple>();

            //    // depends on how applyTupleChanges adds the tuples to its PriliminaryTuples!
            //    if (createdTuples != null)
            //    {
            //        foreach (DataTuple tuple in createdTuples)
            //        {
            //            tupleRepo.Put(tuple);
            //        }
            //    }

            //    if (tobeAdded != null)
            //    {
            //        foreach (DataTupleVersion dtv in tobeAdded)
            //        {
            //            tupleVersionRepo.Put(dtv);
            //        }
            //    }
            //    //foreach (var editedTuple in tobeEdited)
            //    //{
            //    //    editedTuple.VariableValues.ToList().ForEach(p => System.Diagnostics.Debug.Print(p.Value.ToString()));
            //    //    System.Diagnostics.Debug.Print(editedTuple.XmlVariableValues.AsString());
            //    //} 
            //    if (tobeDeleted != null)
            //    {
            //    foreach (DataTuple tuple in tobeDeleted)
            //    {
            //        tupleRepo.Delete(tuple);
            //    }
            //    }
            //    // check whether the changes to the latest version, which is changed in the applyTupleChanges , are committed too!
            //    repo.Put(editedVersion);
            //    uow.Commit();
            //}
            #endregion

            #region <<------ experimental code ------>>
            // Check the same scenario using stateless session/ BulkUnitOfWork
            using (IUnitOfWork uow = this.GetBulkUnitOfWork())
            {
                IRepository<DataTupleVersion> tupleVersionRepo = uow.GetRepository<DataTupleVersion>();
                IRepository<DataTuple> tupleRepo = uow.GetRepository<DataTuple>();

                // depends on how applyTupleChanges adds the tuples to its PriliminaryTuples!
                if (createdTuples != null && createdTuples.Count >0)
                {
                    int count = 0;
                    int batchSize = uow.PersistenceManager.PreferredPushSize;// int.Parse(uow.PersistenceManager.GetProperty("adonet.batch_size")); //.GetProperty("default_batch_size"));
                    List<DataTuple> processedTuples = new List<DataTuple>();
                    for (int i = 0; i < createdTuples.Count; i++)
                    {
                        DataTuple tuple = createdTuples.ElementAt(i);
                        tuple.Dematerialize();
                        tupleRepo.Put(tuple);
                        processedTuples.Add(tuple);
                        count++;
                        // flush and clear the session every BATCH_SIZE records
                        if (count % batchSize == 0)
                        {
                            uow.ClearCache(true); //flushes one batch of tuples 
                            processedTuples.ForEach(p => createdTuples.Remove(p));
                            processedTuples.Clear();
                            i = 0;
                            GC.Collect();
                        }
                    }
                }
                //if (tobeAdded != null)
                //{
                //    foreach (DataTupleVersion dtv in tobeAdded)
                //    {
                //        tupleVersionRepo.Put(dtv);
                //    }
                //}
                if (tobeAdded != null && tobeAdded.Count > 0)
                {
                    int count = 0;
                    int batchSize = uow.PersistenceManager.PreferredPushSize;// int.Parse(uow.PersistenceManager.GetProperty("adonet.batch_size")); //.GetProperty("default_batch_size"));
                    List<DataTupleVersion> processedTuples = new List<DataTupleVersion>();
                    for (int i = 0; i < tobeAdded.Count; i++)
                    {
                        DataTupleVersion tuple = tobeAdded.ElementAt(i);
                        tupleVersionRepo.Put(tobeAdded);
                        processedTuples.Add(tuple);
                        count++;
                        // flush and clear the session every BATCH_SIZE records
                        if (count % batchSize == 0)
                        {
                            uow.ClearCache(true); //flushes one batch of tuples 
                            processedTuples.ForEach(p => tobeAdded.Remove(p));
                            processedTuples.Clear();
                            i = 0;
                            GC.Collect();
                        }
                    }
                }
                //foreach (var editedTuple in tobeEdited)
                //{
                //    editedTuple.VariableValues.ToList().ForEach(p => System.Diagnostics.Debug.Print(p.Value.ToString()));
                //    System.Diagnostics.Debug.Print(editedTuple.XmlVariableValues.AsString());
                //} 
                //if (tobeDeleted != null)
                //{
                //    foreach (DataTuple tuple in tobeDeleted)
                //    {
                //        tupleRepo.Delete(tuple);
                //    }
                //}
                if (tobeDeleted != null && tobeDeleted.Count > 0)
                {
                    int count = 0;
                    int batchSize = uow.PersistenceManager.PreferredPushSize;// int.Parse(uow.PersistenceManager.GetProperty("adonet.batch_size")); //.GetProperty("default_batch_size"));
                    List<DataTuple> processedTuples = new List<DataTuple>();
                    for (int i = 0; i < tobeDeleted.Count; i++)
                    {
                        DataTuple tuple = tobeDeleted.ElementAt(i);
                        tupleRepo.Delete(tuple);
                        processedTuples.Add(tuple);
                        count++;
                        // flush and clear the session every BATCH_SIZE records
                        if (count % batchSize == 0)
                        {
                            uow.ClearCache(true); //flushes one batch of tuples 
                            processedTuples.ForEach(p => tobeDeleted.Remove(p));
                            processedTuples.Clear();
                            i = 0;
                            GC.Collect();
                        }
                    }
                }
                // check whether the changes to the latest version, which is changed in the applyTupleChanges , are committed too!
                uow.Commit();
            }
            #endregion

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<DatasetVersion> repo = uow.GetRepository<DatasetVersion>();
                repo.Put(editedVersion); // must be updated in a tracked session
                uow.Commit();
            }
            return (editedVersion);
        }

        private List<AbstractTuple> getHistoricTuples(DatasetVersion datasetVersion)
        {
            //get previous versions including the version specified, because  the data tuples belong to all versions greater or equal to their original versions.
            List<Int64> versionIds = getPreviousVersionIds(datasetVersion);            //get all tuples from the main tuples table belonging to one of the previous versions + the current version
            List<DataTuple> tuples = DataTupleRepo.Get(p => versionIds.Contains(p.DatasetVersion.Id)).ToList();

            // get those history tuples that represent editedVersion versions of data tuples changed from at least one of the effective versions and not committed to them (them: the effective versions).
            // any single data tuple can be editedVersion by a specific version once at most.
            // it is possible for a tuple to have beed changed many times between any given two versions v(x) and v(y), so it is required to group the tuples based on their original ID and then select the record corresponding to the max version
            var editedTupleVersionsGrouped = DataTupleVerionRepo.Query(p => (p.TupleAction == TupleAction.Edited) 
                                                                            && (versionIds.Contains(p.DatasetVersion.Id)) 
                                                                            && !(versionIds.Contains(p.ActingDatasetVersion.Id)))
                                                                .GroupBy(p => p.OriginalTuple.Id)
                                                                .Select(p => new { OriginalTupleId = p.Key, MaxVersionOfTheTuple = p.Max(l => l.DatasetVersion.Id) })
                                                                .ToList();


            IList<DataTupleVersion> editedTuples = new List<DataTupleVersion>();

            // having a list of original tuple id and related max version, now its time to build a proper query to fetch the actual data tuple versions from the database, the following block builds a dynamic predicate
            // to be passed to the where clause of the data retrieval method at: DataTupleVerionRepo.Query(...)
            if (editedTupleVersionsGrouped.Count >= 1)
            {
                var param1 = Expression.Parameter(typeof(DataTupleVersion), "p");
                var exp1 = 
                    Expression.AndAlso(
                    Expression.Equal(
                        Expression.Property(Expression.Property(param1, "OriginalTuple"), "Id"),
                        Expression.Constant(editedTupleVersionsGrouped.First().OriginalTupleId)
                    ),
                    Expression.Equal(
                        Expression.Property(Expression.Property(param1, "DatasetVersion"), "Id"),
                        Expression.Constant(editedTupleVersionsGrouped.First().MaxVersionOfTheTuple)
                    )
                    );
                if (editedTupleVersionsGrouped.Count > 1)
                {
                    foreach (var item in editedTupleVersionsGrouped.Skip(1))
                    {
                        //var param = Expression.Parameter(typeof(DataTupleVersion), "p");
                        var exp = 
                            Expression.AndAlso(
                            Expression.Equal(
                                Expression.Property(Expression.Property(param1, "OriginalTuple"), "Id"),
                                Expression.Constant(item.OriginalTupleId)
                            ),
                            Expression.Equal(
                                Expression.Property(Expression.Property(param1, "DatasetVersion"), "Id"),
                                Expression.Constant(item.MaxVersionOfTheTuple)
                            )
                            );
                       exp1 = Expression.OrElse(exp1, exp); ;

                    }
                }
                var typedExpression = Expression.Lambda<Func<DataTupleVersion, bool>>(exp1, new ParameterExpression[] { param1 });
                editedTuples = DataTupleVerionRepo.Query(typedExpression).ToList();
            }

          
            var deletedTuples = DataTupleVerionRepo.Get(p => (p.TupleAction == TupleAction.Deleted) 
                                                            && (versionIds.Contains(p.DatasetVersion.Id)) 
                                                            && !(versionIds.Contains(p.ActingDatasetVersion.Id)))
                                                   .Cast<AbstractTuple>()
                                                   .ToList();

            List<AbstractTuple> result = tuples
                
                .Union(editedTuples.Cast<AbstractTuple>())
                .Union(deletedTuples)
                // there is no guarantee that the overall list is ordered as its original order! because 1: OrderNo is not set yet. 2: OrderNo is not managed during the changes and so on, 
                // 3: The timestamp of the current tuples is indeed the timestamp of the change made by their latest acting version, but history record are carrying the original timestamp. but as there should be no overlap between the two table records
                // and history records have smaller timestamps, no side effect is expected. 4: I don't know why but ...
                .OrderBy(p => p.OrderNo).OrderBy(p => p.Timestamp)
                .ToList();
            return (result);
        }

        private List<DataTuple> getHistoricTuples(DatasetVersion datasetVersion, int pageNumber, int pageSize)
        {
            //get previous versions including the version specified
            List<Int64> versionIds = getPreviousVersionIds(datasetVersion);
            //get all tuples from the main tuples table belonging to one of the previous versions + the current version
            List<DataTuple> tuples = DataTupleRepo.Get(p => versionIds.Contains(p.DatasetVersion.Id)).ToList();

            List<DataTuple> editedTuples = DataTupleVerionRepo.Query(p => (p.TupleAction == TupleAction.Edited) 
                                                                        && (p.DatasetVersion.Id == datasetVersion.Id) 
                                                                        && !(versionIds.Contains(p.ActingDatasetVersion.Id)))
                                                            .Skip(pageNumber * pageSize).Take(pageSize)
                                                            .Cast<DataTuple>().ToList();
            List<DataTuple> deletedTuples = DataTupleVerionRepo.Query(p => (p.TupleAction == TupleAction.Deleted) 
                                                                    && (versionIds.Contains(p.DatasetVersion.Id)) 
                                                                    && !(versionIds.Contains(p.ActingDatasetVersion.Id)))
                                                               .Skip(pageNumber * pageSize).Take(pageSize)
                                                               .Cast<DataTuple>().ToList();
            // the resulting union-ned list is made by a page from editedVersion and a page from the deleted ones, so it is maximum 2 pages, but should be reduced to a page.
            // for this reason the union is sorted by timestamp and then the first page is taken.
            List<DataTuple> unioned = tuples.Union(editedTuples).Union(deletedTuples)
                .OrderBy(p => p.Timestamp)
                .Take(pageSize)
                .ToList();
            return (unioned);
        }

        private List<DataTuple> getPrimaryTuples(DatasetVersion datasetVersion)
        {
            // effective tuples of the latest checked in version are in DataTuples table but they belong to the latest and previous versions
            List<Int64> versionIds = getPreviousVersionIds(datasetVersion);
            List<DataTuple> tuples;
            // experimental code, the stateles session fails to allow fetching the object graphs later. tuple.value.variable...
            //using (IUnitOfWork uow = this.GetBulkUnitOfWork())
            //{
            //    IReadOnlyRepository<DataTuple> tuplesRepoTemp = uow.GetReadOnlyRepository<DataTuple>();
            //    tuples = (versionIds == null || versionIds.Count() <= 0) ? 
            //        new List<DataTuple>() : 
            //        tuplesRepoTemp.Get(p => versionIds.Contains(p.DatasetVersion.Id)).ToList();
            //}

            tuples = (versionIds == null || versionIds.Count() <= 0) ? 
                new List<DataTuple>() : 
                DataTupleRepo.Get(p => versionIds.Contains(p.DatasetVersion.Id)).ToList();
            ////Dictionary<string, object> parameters = new Dictionary<string, object>() { { "datasetVersionId", datasetVersion.Id } };
            ////List<DataTuple> tuples = DataTupleRepo.Get("getLatestCheckedInTuples", parameters).ToList();
            return (tuples);
        }

        private List<DataTuple> getPrimaryTuples(DatasetVersion datasetVersion, int pageNumber, int pageSize)
        {
            // effective tuples of the latest checked in version are in DataTuples table but they belong to the latest and previous versions
            List<Int64> versionIds = getPreviousVersionIds(datasetVersion);
            List<DataTuple> tuples;
            using (IUnitOfWork uow = this.GetBulkUnitOfWork())
            {
                IReadOnlyRepository<DataTuple> tuplesRepoTemp = uow.GetReadOnlyRepository<DataTuple>();
                tuples = (versionIds == null || versionIds.Count() <= 0) ?
                    new List<DataTuple>() :
                    tuplesRepoTemp.Query(p => versionIds.Contains(p.DatasetVersion.Id))
                            .Skip(pageNumber * pageSize)
                            .Take(pageSize)
                            .ToList();
            }

            //tuples = (versionIds == null || versionIds.Count() <= 0) ? new List<DataTuple>() :
            //                DataTupleRepo.Query(p => versionIds.Contains(p.DatasetVersion.Id))
            //                .Skip(pageNumber * pageSize).Take(pageSize)
            //                .ToList();

            //Dictionary<string, object> parameters = new Dictionary<string, object>() { { "datasetVersionId", datasetVersion.Id } };
            //List<DataTuple> tuples = DataTupleRepo.Get("getLatestCheckedInTuples", parameters).ToList();
            return (tuples);
        }

        private List<Int64> getPrimaryTupleIds(DatasetVersion datasetVersion)
        {
            // effective tuples of the latest checked in version are in DataTuples table but they belong to the latest and previous versions
            List<Int64> versionIds = getPreviousVersionIds(datasetVersion);
            List<Int64> tuples = (versionIds == null || versionIds.Count() <= 0) ? 
                                        new List<Int64>() 
                                        : DataTupleRepo.Get(p => versionIds.Contains(p.DatasetVersion.Id)).Select(p=> p.Id)
                                                       .ToList();
            return (tuples);
        }

        private Int32 getPrimaryTupleCount(DatasetVersion datasetVersion)
        {
            List<Int64> versionIds = getPreviousVersionIds(datasetVersion); 
            Int32 tuplesCount = (versionIds == null || versionIds.Count() <= 0) ?
                                        0
                                        : DataTupleRepo.Query(p => versionIds.Contains(p.DatasetVersion.Id)).Select(p => p.Id)
                                                       .Count();
            return (tuplesCount);
        }

        private List<Int64> getPreviousVersionIdsOrdered(DatasetVersion datasetVersion)
        {
            List<Int64> versionIds = datasetVersion.Dataset.Versions
                                        .Where(p => p.Timestamp <= datasetVersion.Timestamp)
                                        .OrderByDescending(t => t.Timestamp)
                                        .Select(p => p.Id)
                                        .ToList();
            return versionIds;
        }

        private List<Int64> getPreviousVersionIds(DatasetVersion datasetVersion)
        {
            List<Int64> versionIds = datasetVersion.Dataset.Versions
                                        .Where(p => p.Timestamp <= datasetVersion.Timestamp)
                                        .Select(p => p.Id)
                                        .ToList();
            return versionIds;
        }

        private List<DataTuple> getWorkingCopyTuples(DatasetVersion datasetVersion)
        {
            // effective tuples of the working copy are similar to latest checked in version. They are in DataTuples table but they belong to the latest and previous versions
            List<Int64> versionIds = getPreviousVersionIds(datasetVersion);
            List<DataTuple> tuples = (versionIds == null || versionIds.Count() <= 0) ? new List<DataTuple>() : DataTupleRepo.Get(p => versionIds.Contains(((DataTuple)p).DatasetVersion.Id)).ToList();
            return (tuples);
        }

        private List<DataTuple> getWorkingCopyTuples(DatasetVersion datasetVersion, int pageNumber, int pageSize)
        {
            // effective tuples of the working copy are similar to latest checked in version. They are in DataTuples table but they belong to the latest and previous versions
            List<Int64> versionIds = getPreviousVersionIds(datasetVersion);
            List<DataTuple> tuples = (versionIds == null || versionIds.Count() <= 0) ? new List<DataTuple>() :
                DataTupleRepo.Get(p => versionIds.Contains(((DataTuple)p).DatasetVersion.Id))
                        .Skip(pageNumber*pageSize).Take(pageSize)
                        .ToList();
            return (tuples);
        }

        private List<Int64> getWorkingCopyTupleIds(DatasetVersion datasetVersion)
        {
            // effective tuples of the working copy are similar to latest checked in version. They are in DataTuples table but they belong to the latest and previous versions
            List<Int64> versionIds = getPreviousVersionIds(datasetVersion);
            List<Int64> tuples = (versionIds == null || versionIds.Count() <= 0) ? new List<Int64>() : DataTupleRepo.Get(p => versionIds.Contains(((DataTuple)p).DatasetVersion.Id)).Select(p => p.Id).ToList();
            return (tuples);
        }

        private Int32 getWorkingCopyTupleCount(DatasetVersion datasetVersion)
        {
            List<Int64> versionIds = getPreviousVersionIds(datasetVersion);
            Int32 tupleCount = (versionIds == null || versionIds.Count() <= 0) ? 0 : DataTupleRepo.Query(p => versionIds.Contains(((DataTuple)p).DatasetVersion.Id)).Select(p => p.Id).Count();
            return (tupleCount);
        }

        private DatasetVersion getDatasetLatestVersion(Dataset dataset)
        {
            /// the latest checked in version should be returned.
            /// if dataset is checked out, exception
            /// If the dataset is marked as deleted its like that it is not there at all
           
            if (dataset == null)
                throw new Exception(string.Format("Provided dataset is null"));
            if (dataset.Status == DatasetStatus.Deleted)
                throw new Exception(string.Format("Dataset {0} is deleted", dataset.Id));
            if (dataset.Status == DatasetStatus.CheckedOut)
            {
                throw new Exception(string.Format("Dataset {0} is checked out.", dataset.Id));
            }
            if (dataset.Status == DatasetStatus.CheckedIn)
            {
                DatasetVersion dsVersion = dataset.Versions
                                                  .OrderByDescending(t => t.Timestamp)
                                                  .First(p => p.Status == DatasetVersionStatus.CheckedIn); // indeed the versions collection is ordered and there should be no need for ordering, but is just to prevent any side effects
                return (dsVersion);
            }
            return null;
        }

        private bool isDatasetCheckedOutFor(Int64 datasetId, string username)
        {
            return (DatasetRepo.Query(p => p.Status == DatasetStatus.CheckedOut && p.Id == datasetId && p.CheckOutUser == getUserIdentifier(username)).Count() == 1);
        }

        /// <summary>
        /// checks out the dataset and creates a new version on it. The new version acts like a working copy while it is not committed, hence editable.
        /// </summary>
        /// <param name="datasetId"></param>
        /// <param name="username"></param>
        private bool checkOutDataset(Int64 datasetId, string username, DateTime timestamp)
        {
            bool checkedOut = false;
            //XmlDocument doc = new XmlDocument();
            //doc.LoadXml(@"<Metadata>Empty</Metadata>");

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<Dataset> repo = uow.GetRepository<Dataset>();
                var q = repo.Query(p => p.Id == datasetId && p.Status == DatasetStatus.CheckedIn && (p.CheckOutUser.Equals(string.Empty) || p.CheckOutUser == null));
                Dataset ds = q.FirstOrDefault();
                if (ds != null)
                {
                    DatasetVersion dsNewVersion = new DatasetVersion()
                    {
                        Timestamp = timestamp,
                        //Metadata = doc,
                        //ExtendedPropertyValues = new List<ExtendedPropertyValue>(),
                        //ContentDescriptors = new List<ContentDescriptor>(),
                        Status = DatasetVersionStatus.CheckedOut,
                        Dataset = ds,
                    };
                    // if there is a previous version, copy its metadata, content descriptors and extended property values to the newly created version
                    if (ds.Versions.Count() > 0)
                    {
                        var previousCheckedInVersion = ds.Versions.Where(p => p.Status == DatasetVersionStatus.CheckedIn).First();
                        if (previousCheckedInVersion != null && previousCheckedInVersion.Timestamp >= timestamp) // it is an error
                        {
                            throw new Exception(string.Format("The provided timestamp {0} is earlier than the timestamp of the latest checked in version!", timestamp));
                        }
                        if (previousCheckedInVersion != null)
                        {
                            dsNewVersion.Metadata = previousCheckedInVersion.Metadata;
                            dsNewVersion.ExtendedPropertyValues = previousCheckedInVersion.ExtendedPropertyValues;
                            foreach (var item in previousCheckedInVersion.ContentDescriptors)
                            {
                                ContentDescriptor cd = new ContentDescriptor()
                                {
                                    MimeType = item.MimeType,
                                    Name = item.Name,
                                    OrderNo = item.OrderNo,
                                    URI = item.URI,
                                    DatasetVersion = dsNewVersion,
                                };
                                dsNewVersion.ContentDescriptors.Add(cd);
                            }                            
                        }
                    }
                    ds.Status = DatasetStatus.CheckedOut;
                    ds.LastCheckIOTimestamp = timestamp;
                    ds.CheckOutUser = getUserIdentifier(username);
                    ds.Versions.Add(dsNewVersion);
                    repo.Put(ds);
                    uow.Commit();
                    checkedOut = true;
                }
            }
            return (checkedOut);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="datasetId"></param>
        /// <param name="comment"></param>
        /// <param name="adminMode">if true, the check for current user is bypassed</param>
        private void checkInDataset(Int64 datasetId, string comment, string username, bool adminMode)
        {
            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<Dataset> repo = uow.GetRepository<Dataset>();
                Dataset ds = null;
                if (adminMode)
                    ds = repo.Get(p => p.Id == datasetId && p.Status == DatasetStatus.CheckedOut).FirstOrDefault();
                else
                    ds = repo.Get(p => p.Id == datasetId && p.Status == DatasetStatus.CheckedOut && p.CheckOutUser.Equals(getUserIdentifier(username))).FirstOrDefault();
                if (ds != null)
                {
                    DatasetVersion previousCheckIn = ds.Versions.FirstOrDefault(p => p.Status == DatasetVersionStatus.CheckedIn);
                    if (previousCheckIn != null)
                        previousCheckIn.Status = DatasetVersionStatus.Old;

                    DatasetVersion dsv = ds.Versions.OrderByDescending(t => t.Timestamp).First(p => p.Status == DatasetVersionStatus.CheckedOut);
                    dsv.ChangeDescription = comment;
                    dsv.Status = DatasetVersionStatus.CheckedIn;

                    ds.Status = DatasetStatus.CheckedIn;
                    ds.LastCheckIOTimestamp = DateTime.UtcNow;
                    ds.CheckOutUser = string.Empty;

                    repo.Put(ds);
                    uow.Commit();
                }
            }
        }

        // in some cases maybe another attribute of the user is used like its ID, email or the IP address
        private string getUserIdentifier(string username)
        {
            return (username);
        }

        /// <summary>
        /// Undo checkout, removes the checked out version of specified dataset and compensates all the tuples deleted, changed or created from the last check-in.
        /// It does not check-in the dataset meaning the caller should CheckInDataset after calling Undo
        /// </summary>
        /// <param name="datasetId"></param>
        /// <param name="username"></param>
        /// <param name="adminMode"></param>
        /// <param name="commit">in some cases, rollback is called on a set of datasets. In  these cases its better to not commit at each rollback, but at the end</param>
        private void undoCheckout(Int64 datasetId, string username, bool adminMode, bool commit = true)
        {
            // maybe its required to pass the caller's repo in order to the rollback changes to be visible to the caller function and be able to commit them
            // bring back the historical tuples. recover deleted ones/ editedVersion ones. throw away created ones.
            // remove the version after the processes are finished
            // take the dataset back to the checked in state
            // check for admin mode
            DateTime timestamp = DateTime.UtcNow;

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<Dataset> repo = uow.GetRepository<Dataset>();
                Dataset ds = null;
                if (adminMode)
                    ds = repo.Get(p => p.Id == datasetId && p.Status == DatasetStatus.CheckedOut).FirstOrDefault();
                else
                    ds = repo.Get(p => p.Id == datasetId && p.Status == DatasetStatus.CheckedOut && p.CheckOutUser.Equals(getUserIdentifier(username))).FirstOrDefault();

                if (ds != null)
                {
                    if (ds.Versions.Count() > 0)
                    {
                        //remove the version from the dataset, it should cause the version to be removed.
                        DatasetVersion dsv = ds.Versions.OrderByDescending(t => t.Timestamp).First(p => p.Status == DatasetVersionStatus.CheckedOut);
                        // handle tuples here
                        undoTupleChanges(dsv);
                        ds.Versions.Remove(dsv);
                    }

                    //// take the dataset back to the checked in status
                    //ds.Status = DatasetStatus.CheckedIn;
                    ////var previous = ds.Versions.OrderByDescending(t => t.Timestamp).FirstOrDefault(p => p.Status == DatasetVersionStatus.CheckedIn);
                    //ds.LastCheckIOTimestamp = timestamp;
                    //ds.CheckOutUser = string.Empty;

                    if (commit)
                    {
                        repo.Put(ds);
                        uow.Commit();
                    }
                }
            }
        }

        private DatasetVersion undoTupleChanges(DatasetVersion workingCopyVersion)
        {
            // delete newly created tuples
            // undo edit
            //undo delete
            return (workingCopyVersion);
        }

        private DatasetVersion applyTupleChanges(DatasetVersion workingCopyVersion
            , ref List<DataTupleVersion> tupleVersionsTobeAdded, ref List<DataTuple> tuplesTobeDeleted, ref List<DataTuple> tuplesTobeEdited
            , ICollection<DataTuple> createdTuples, ICollection<DataTuple> editedTuples, ICollection<DataTuple> deletedTuples, ICollection<DataTuple> unchangedTuples = null)
        {
#if DEBUG
            //measureVersionSize(workingCopyVersion.PriliminaryTuples == null ? 0 : workingCopyVersion.PriliminaryTuples.Count()
            //    , createdTuples == null ? 0 : createdTuples.Count()
            //    , deletedTuples == null ? 0 : deletedTuples.Count()
            //    , editedTuples  == null ? 0 : editedTuples.Count());
#endif
            // do nothing with unchanged for now
            #region Process Newly Created Tuples

            /// associate newly created tuples to the new version
            /// try using bulk copy or stateless sessions for large amount of new tuples. it should also apply on deleted tuples.
            /// Take care of automatic flushing and try to prevent or reduce it while the edit process is not finished.
            if (createdTuples != null && createdTuples.Count() > 0)
            {
                // is not working cause of the item.Dematerialize();
                //Parallel.ForEach(createdTuples, item =>
                //{
                //    item.Dematerialize();
                //    // commented for the performance testing purpose. see the efects and uncomment if needed-> workingCopyVersion.PriliminaryTuples.Add(item);
                //    item.DatasetVersion = workingCopyVersion;
                //    item.TupleAction = TupleAction.Created;
                //    item.Timestamp = workingCopyVersion.Timestamp;

                //});
                foreach (var item in createdTuples)
                {
                    item.Dematerialize();
                    // commented for the performance testing purpose. see the efects and uncomment if needed-> workingCopyVersion.PriliminaryTuples.Add(item);
                    item.DatasetVersion = workingCopyVersion;
                    item.TupleAction = TupleAction.Created;
                    item.Timestamp = workingCopyVersion.Timestamp;
                }
            }

            #endregion

            if ((editedTuples != null && editedTuples.Count() > 0) || (deletedTuples != null && deletedTuples.Count() > 0))
            {

                // latest version is the latest checked in version. it is the previous version in comparison to the working copy version.
                // the checks to see whether the dataset is checked out are considered to be done before
                DatasetVersion latestCheckedInVersion = workingCopyVersion.Dataset.Versions.OrderByDescending(p => p.Timestamp).FirstOrDefault(p => p.Status == DatasetVersionStatus.CheckedIn);
                if (latestCheckedInVersion == null) // there is no previous version, means its the first version. In this case there is no need to handle deleted and editedVersion items!
                    return (workingCopyVersion);

                // the edit and delete candiates know the exact ID of the target tuple, so no need to load the whole tuples in advance. in worst case the number of single tuple match queries will increase
                //List<DataTuple> latestVersionEffectiveTuples = getPrimaryTuples(workingCopyVersion); //latestVersionEffectiveTuples =  DataTupleRepo.Get(p=>p.DatasetVersion ==  null).ToList();
               
                #region Process Edited Tuples

                /// manage editedVersion tuples: 
                /// 1: create a DataTupleVersion based on its previous version
                /// 2: Remove the original from the original version
                /// 3: add them to the version
                /// 4: set timestamp for the editedVersion ones
                if (editedTuples != null && editedTuples.Count() > 0)
                {
                    // this part of the code loads the original tuples that are claimed edited. The edited collection is indeed one package of changes submitted in this round
                    // so its used to load only the relevant portion of the tuples. this avoid loading all the tuples asscoiated with the current version. also avoid N times querying the DB, each time to retreive one tuple
                    // Seems a moderate solution between the two extreme.
                    List<Int64> editedTupleIds = editedTuples.Select(t => t.Id).ToList(); // All the IDs of edited tuples of the current package
                    List<DataTuple> oraginalsOfEditedTuples = DataTupleRepo.Get(p => editedTupleIds.Contains(p.Id)).ToList(); // all the original tuples edited in the current package

                    //Parallel.ForEach(editedTuples, edited => // not able to use parallel for now, because the tuplesTobeEdited gets shared between threads for writting in it, which causes synch problems
                    foreach (var edited in editedTuples)
                    {
                        DataTuple orginalTuple = oraginalsOfEditedTuples.SingleOrDefault(p => p.Id == edited.Id); //DataTupleRepo.Get(edited.Id);// latestVersionEffectiveTuples.Where(p => p.Id == editedVersion.Id).Single();//maybe preliminary tuples are enough
                        if (orginalTuple == null || orginalTuple.Id <= 0) // maybe the tuple is in the edited list by a mistake!
                            continue;
                        //check if the history record for this data tuple has been created before. in cases of multiple edits in a single version for example
                        if (DataTupleVerionRepo.Query(p => p.OriginalTuple.Id == orginalTuple.Id && p.DatasetVersion.Id == orginalTuple.DatasetVersion.Id).Count() <= 0) // it is the first time the orginalTuple is getting editedVersion. so add a history record. the history record, keeps the tuple as was before the first edit!
                        {
                            DataTupleVersion tupleVersion = new DataTupleVersion()
                            {
                                TupleAction = TupleAction.Edited,
                                Extra = orginalTuple.Extra,
                                //Id = orginalTuple.Id,
                                OrderNo = orginalTuple.OrderNo,
                                Timestamp = orginalTuple.Timestamp,
                                XmlAmendments = orginalTuple.XmlAmendments,
                                XmlVariableValues = orginalTuple.XmlVariableValues,
                                OriginalTuple = orginalTuple,
                                DatasetVersion = orginalTuple.DatasetVersion, //latestCheckedInVersion,
                                ActingDatasetVersion = workingCopyVersion,
                            };
                            //DataTuple merged = 
                            orginalTuple.History.Add(tupleVersion);
                        }

                        //need a better way to preserve changes during the fetch of the original tuple. Maybe deep copy/ evict/ merge works
                        //XmlDocument xmlVariableValues = new XmlDocument();
                        //xmlVariableValues.LoadXml(editedVersion.XmlVariableValues.AsString());

                        // dematerialize just for the purpose of synching the xml fields with the object properties.
                        edited.Dematerialize();

                        orginalTuple.TupleAction = TupleAction.Edited;
                        orginalTuple.OrderNo = edited.OrderNo;
                        orginalTuple.XmlAmendments = null;
                        orginalTuple.XmlAmendments = edited.XmlAmendments;
                        orginalTuple.XmlVariableValues = null;
                        orginalTuple.XmlVariableValues = edited.XmlVariableValues;

                        //System.Diagnostics.Debug.Print(editedVersion.XmlVariableValues.AsString());                        
                        //editedVersion.VariableValues.ToList().ForEach(p => System.Diagnostics.Debug.Print(p.Value.ToString()));
                        //System.Diagnostics.Debug.Print(xmlVariableValues.AsString());

                        orginalTuple.DatasetVersion = workingCopyVersion;
                        orginalTuple.Timestamp = workingCopyVersion.Timestamp;
                        tuplesTobeEdited.Add(orginalTuple);
                        //workingCopyVersion.PriliminaryTuples.Add(detached);

                        //latestCheckedInVersion.PriliminaryTuples.Remove(orginalTuple);
                        //latestVersionEffectiveTuples.Remove(orginalTuple);
                    }
                    //); //parallel for each
                }

                #endregion

                #region Process Deleted Tuples

                /// manage deleted tuples: 
                /// 1: create a DataTupleVersion based on their previous version
                /// 2: Remove them from the latest version
                /// 3: DO NOT add them to the new version
                /// 4: DO NOT set timestamp for the deleted ones

                if (deletedTuples != null && deletedTuples.Count() > 0)
                {
                    //Parallel.ForEach(deletedTuples, deleted =>  // the tuplesTobeDeleted gets shared between the threads!
                    foreach (var deleted in deletedTuples)
                    {
                        DataTuple originalTuple = DataTupleRepo.Get(deleted.Id);// latestVersionEffectiveTuples.Where(p => p.Id == deleted.Id).Single();
                        // check if the tuple has a previous history record. for example may be it was first editedVersion and now is going to be deleted. in two different edits but in one version
                        DataTupleVersion tupleVersion = DataTupleVerionRepo.Query(p => p.OriginalTuple.Id == originalTuple.Id).FirstOrDefault();
                        if (tupleVersion != null)
                        {
                            // there is a previous history record, with tuple action equal to Edit or even Delete!
                            tupleVersion.TupleAction = TupleAction.Deleted;
                        }
                        else // there is no previous record, so create one
                        {
                            tupleVersion = new DataTupleVersion()
                            {
                                TupleAction = TupleAction.Deleted,
                                Extra = originalTuple.Extra,
                                //Id = orginalTuple.Id,
                                OrderNo = originalTuple.OrderNo,
                                Timestamp = originalTuple.Timestamp,
                                XmlAmendments = originalTuple.XmlAmendments,
                                XmlVariableValues = originalTuple.XmlVariableValues,
                                //OriginalTuple = orginalTuple,
                                DatasetVersion = originalTuple.DatasetVersion, // latestCheckedInVersion,
                                ActingDatasetVersion = workingCopyVersion,
                            };
                        }

                        tupleVersion.OriginalTuple = null;

                        // /////////////////////////////////////////
                        // try avoid accessing to the PriliminaryTuples, they cause loading all the tuples!!
                        // ////////////////////////////////////////

                        // -> latestCheckedInVersion.PriliminaryTuples.Remove(originalTuple);
                        // check whether the deleted tuples are removed from the datatuples table!!!!!
                        //latestVersionEffectiveTuples.Remove(originalTuple);
                        // -> workingCopyVersion.PriliminaryTuples.Remove(originalTuple);
                        try
                        {
                            originalTuple.History.ToList().ForEach(p => p.OriginalTuple = null);
                        }
                        catch { }
                        //try
                        //{
                        // ->   originalTuple.DatasetVersion.PriliminaryTuples.Remove(originalTuple);
                        //}
                        //catch { }

                        originalTuple.History.Clear();
                        originalTuple.DatasetVersion = null;

                        tuplesTobeDeleted.Add(originalTuple);
                        tupleVersionsTobeAdded.Add(tupleVersion);
                    }
                    //); // parralel for each loop
                }

                #endregion
            }
            return (workingCopyVersion);
        }

#if DEBUG
        //[Diagnose]
        private void measureVersionSize(int currentTuples, int tobeAdded, int tobeDelected, int tobeEdited)
        {
            // do nothing, this is a performance counting point, which is automatically recorded in the debug mode.
        }

        //[Diagnose]
        private void measureTupleSize(int p1, string p2)
        {
            // do nothing, 
        }

#endif
        #endregion

        #region DataTuple
        /// <summary>
        /// Using the provided values creates a data tuple, attaches it to the version and persists it in the database. 
        /// This method does not affect the status of the dataset version.
        /// </summary>
        /// <param name="orderNo">The order of the data tuple in the list of tuples</param>
        /// <param name="variableValues">The values to be considered as the data tuple. They must be attached to their corresponding variables according to the dataset's data structure.</param>
        /// <param name="amendments">Each data tuple can have amendments and if provided, the method attaches them to the data tuple.</param>
        /// <param name="datasetVersion">The version of the dataset the data tuple is attached to. The version must be checked-out.</param>
        /// <returns>the created data tuple</returns>
        /// <exception cref="Exception">throws and exception if the dataset version is not checked-out.</exception>
        public DataTuple CreateDataTuple(int orderNo, ICollection<VariableValue> variableValues, ICollection<Amendment> amendments, DatasetVersion datasetVersion)
        {
            //Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(datasetVersion != null);

            Contract.Ensures(Contract.Result<DataTuple>() != null && Contract.Result<DataTuple>().Id >= 0);
            if (datasetVersion.Status != DatasetVersionStatus.CheckedOut)
            {
                throw new Exception(string.Format("The dataset version {0} must be checked-out!", datasetVersion.Id));
            }

            DataTuple e = new DataTuple()
            {
                OrderNo = orderNo,
                DatasetVersion = datasetVersion,
                VariableValues = new List<VariableValue>(variableValues),
                Amendments = new List<Amendment>(amendments),
            };
            e.DatasetVersion.PriliminaryTuples.Add(e);
            e.Amendments.ToList().ForEach(ex => ex.Tuple = e);
            //e.VariableValues.ToList().ForEach(ex => ex.Tuple = e);

            // check to see if all variable values and their parameter values are defined in the data structure
            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<DataTuple> repo = uow.GetRepository<DataTuple>();
                repo.Put(e);
                uow.Commit();
            }
            return (e);
        }

        [Obsolete("Avoid using!")]
        public bool DeleteDataTuple(DataTuple entity)
        {
            Contract.Requires(entity != null);
            Contract.Requires(entity.Id >= 0);

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<DataTuple> repo = uow.GetRepository<DataTuple>();

                entity = repo.Reload(entity);
                entity.DatasetVersion = null;

                repo.Delete(entity);

                uow.Commit();
            }
            // if any problem was detected during the commit, an exception will be thrown!
            return (true);
        }

        [Obsolete("Avoid using!")]
        public bool DeleteDataTuple(IEnumerable<DataTuple> entities)
        {
            Contract.Requires(entities != null);
            Contract.Requires(Contract.ForAll(entities, (DataTuple e) => e != null));
            Contract.Requires(Contract.ForAll(entities, (DataTuple e) => e.Id >= 0));

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<DataTuple> repo = uow.GetRepository<DataTuple>();

                foreach (var entity in entities)
                {
                    var latest = repo.Reload(entity);
                    latest.DatasetVersion = null;

                    repo.Delete(latest);
                }
                uow.Commit();
            }
            return (true);
        }

        /// <summary>
        /// Provided that the data tuple entity contains some changes, the method persists the changes into the database.
        /// </summary>
        /// <param name="entity">The data tuple containing the changes.</param>
        /// <returns>The same data tuple having the changes applied.</returns>
        public DataTuple UpdateDataTuple(DataTuple entity)
        {
            Contract.Requires(entity != null, "provided entity can not be null.");
            Contract.Requires(entity.Id >= 0, "provided entity must have a permanent ID.");

            Contract.Ensures(Contract.Result<DataTuple>() != null && Contract.Result<DataTuple>().Id >= 0, "No entity is persisted!");

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<DataTuple> repo = uow.GetRepository<DataTuple>();
                repo.Put(entity); // Merge is required here!!!!
                uow.Commit();
            }
            return (entity);
        }
        
        #endregion

        // the Classes derived from DataValue are not independent persistence classes. They get persisted with their containers, So there is no need for Delete and update, 
        // e.g., tuple1.Amendments.First().Value = 10, UpdateTuple(tuple1);

        #region Extended Property Value

        /// <summary>
        /// An extended property is a custom property that is assigned to a dataset version in addition to the predefined properties. Then each dataset version owner/ accessor 
        /// can provide a value for the attached properties.
        /// </summary>
        /// <param name="extendedPropertyId">The identifier of the extended property.</param>
        /// <param name="value">The value to be assigned to the extended property of the dataset version.</param>
        /// <param name="note"><see cref="DataValue"/></param>
        /// <param name="samplingTime"><see cref="DataValue"/></param>
        /// <param name="resultTime"><see cref="DataValue"/></param>
        /// <param name="obtainingMethod"><see cref="DataValue"/></param>
        /// <param name="datasetVersion">The dataset version receiving the property value</param>
        /// <returns>The extended property value linked to its <see cref="ExtendedProperty"/> and the <see cref="DatasetVersion"/></returns>
        public ExtendedPropertyValue CreateExtendedPropertyValue(Int64 extendedPropertyId, string value, string note, DateTime samplingTime, DateTime resultTime, ObtainingMethod obtainingMethod,
             DatasetVersion datasetVersion)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(value));
            Contract.Requires(extendedPropertyId > 0);
            Contract.Requires(datasetVersion != null);

            Contract.Ensures(Contract.Result<ExtendedPropertyValue>() != null);
            ExtendedPropertyValue e = new ExtendedPropertyValue()
            {
                Value = value,
                Note = note,
                SamplingTime = samplingTime,
                ResultTime = resultTime,
                ObtainingMethod = obtainingMethod,
                ExtendedPropertyId = extendedPropertyId,
                DatasetVersion = datasetVersion, // subject to delete
            };
            e.DatasetVersion.ExtendedPropertyValues.Add(e);

            //using (IUnitOfWork uow = this.GetUnitOfWork())
            //{
            //    IRepository<ExtendedPropertyValue> repo = uow.GetRepository<ExtendedPropertyValue>();
            //    repo.Put(e);
            //    uow.Commit();
            //}
            return (e);
        }

        #endregion

        #region Amendments

        /// <summary>
        /// An amendment is like a variable value that is added to a data tuple. The difference is that the amendment does not need to be defined in the dataset's structure 
        /// and also not all the data tuples need to have the same amendments.
        /// This method creates and amendment object, attaches it to the data tuple but does <b>NOT</b> persist it.
        /// </summary>
        /// <param name="value"><see cref="DataValue"/></param>
        /// <param name="note"><see cref="DataValue"/></param>
        /// <param name="samplingTime"><see cref="DataValue"/></param>
        /// <param name="resultTime"><see cref="DataValue"/></param>
        /// <param name="obtainingMethod"><see cref="DataValue"/></param>
        /// <param name="parameterId">The identifier of the parameter that the amendment will be linked to. needs more clarification</param>
        /// <param name="tuple">The data tuple receiving the amendment.ku</param>
        /// <returns></returns>
        public Amendment CreateAmendment(string value, string note, DateTime samplingTime, DateTime resultTime, ObtainingMethod obtainingMethod, Int64 parameterId, DataTuple tuple)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(value));
            Contract.Requires(parameterId > 0);
            Contract.Requires(tuple != null);
            Contract.Ensures(Contract.Result<Amendment>() != null);

            Amendment e = new Amendment()
            {
                Value = value,
                Note = note,
                SamplingTime = samplingTime,
                ResultTime = resultTime,
                ObtainingMethod = obtainingMethod,
                ParameterId = parameterId,     
                Tuple = tuple,
            };

            //using (IUnitOfWork uow = this.GetUnitOfWork())
            //{
            //    IRepository<Amendment> repo = uow.GetRepository<Amendment>();
            //    repo.Put(e);
            //    uow.Commit();
            //}
            return (e);
        }

        #endregion

        #region Variable Value

        /// <summary>
        /// This method creates a variable value and returns it without persisting the object in the database. Usually the returned variable value should be added to a data tuple which in turn is belonging to a data set version.
        /// A value is a compound object holding information about a single event (sampling). The event can be a(n) measurement, observation, estimation, simulation, or computation of a feature of an entity.
        /// The value can be a assigned to a variable or a parameter based on the design of the data structure.
        /// </summary>
        /// <param name="value">The result of the event which can be a(n) measurement, observation, estimation, simulation, or computation</param>
        /// <param name="note">A free format, but short, description about the value</param>
        /// <param name="samplingTime">The exact time of the start of the event. It shows when the sampling is started.</param>
        /// <param name="resultTime">The sampling, or the processing may take time (like in the computation or simulation cases), also some devices/ sensors have a response time. The parameter captures the time when the result (value) is ready.
        /// The result time and its difference to sampling time are important for some analyses.
        /// </param>
        /// <param name="obtainingMethod">Determines how the values is obtained, which is one of the measurement, observation, estimation, simulation, or computation cases.</param>
        /// <param name="variableId">The identifier of the variable that the value is belonging to.</param>
        /// <param name="parameterValues">If the variable has parameters attached, the parameter values are passed alongside, so that the method links them to their corresponding variable value using <paramref name="variableId"/>.</param>
        /// <returns>A transient object of type <seealso cref="VariableValue"/>.</returns>
        public VariableValue CreateVariableValue(string value, string note, DateTime samplingTime, DateTime resultTime, ObtainingMethod obtainingMethod, Int64 variableId, ICollection<ParameterValue> parameterValues)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(value));
            Contract.Requires(variableId > 0);
            Contract.Ensures(Contract.Result<VariableValue>() != null);

            VariableValue e = new VariableValue()
            {
                Value = value,
                Note = note,
                SamplingTime = samplingTime,
                ResultTime = resultTime,
                ObtainingMethod = obtainingMethod,
                VariableId = variableId,
                ParameterValues = new List<ParameterValue>(parameterValues),
            };

            //using (IUnitOfWork uow = this.GetUnitOfWork())
            //{
            //    IRepository<VariableValue> repo = uow.GetRepository<VariableValue>();
            //    repo.Put(e);
            //    uow.Commit();
            //}
            return (e);
        }

        #endregion

        #region Parameter Value

        /// <summary>
        /// Creates a parameter value similar to <see cref="CreateVariableValue"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="note"></param>
        /// <param name="samplingTime"></param>
        /// <param name="resultTime"></param>
        /// <param name="obtainingMethod"></param>
        /// <param name="parameterId"></param>
        /// <returns>A transient object of type <seealso cref="ParameterValue"/>.</returns>
        public ParameterValue CreateParameterValue(string value, string note, DateTime samplingTime, DateTime resultTime, ObtainingMethod obtainingMethod, Int64 parameterId)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(value));
            Contract.Requires(parameterId > 0);
            Contract.Ensures(Contract.Result<ParameterValue>() != null);

            ParameterValue e = new ParameterValue()
            {
                Value = value,
                Note = note,
                SamplingTime = samplingTime,
                ResultTime = resultTime,
                ObtainingMethod = obtainingMethod,
                ParameterId = parameterId,
            };

            //using (IUnitOfWork uow = this.GetUnitOfWork())
            //{
            //    IRepository<ParameterValue> repo = uow.GetRepository<ParameterValue>();
            //    repo.Put(e);
            //    uow.Commit();
            //}
            return (e);
        }

        #endregion

        #region Content Descriptor

        /// <summary>
        /// Resource descriptors are the way to link resources to data set versions (and some other entity types, too).
        /// The resources can be persisted in the local or a remote file system or any other location reachable via a URL.
        /// The resource itself can be any type of file, service returning a resource, a normal webpage, and so on.
        /// The method creates a resource descriptor, links it to the provided data set version and persists the descriptor.
        /// </summary>
        /// <param name="name">A friendly name for the resource, mainly used in the UI</param>
        /// <param name="mimeType">The type of the resource. Used in the methods that transfer and/or process the resource</param>
        /// <param name="uri">The URI of the resource, may contain protocol, access method, authorization information , etc.</param>
        /// <param name="orderNo">The order of the resource in the list of all resources associated to the same dataset version.</param>
        /// <param name="datasetVersion"></param>
        /// <returns>A persisted <seealso cref="ContentDescriptor"/> object linked to the <paramref name="datasetVersion"/></returns>
        /// <remarks>The method does not have access to the resource itself, and does not persist it.</remarks>
        public ContentDescriptor CreateContentDescriptor(string name, string mimeType, string uri, Int32 orderNo, DatasetVersion datasetVersion)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(!string.IsNullOrWhiteSpace(mimeType));
            Contract.Requires(!string.IsNullOrWhiteSpace(uri));
            Contract.Requires(datasetVersion != null);
            // check whether is it needed that the dataset is checked out to add descriptor
            Contract.Ensures(Contract.Result<ContentDescriptor>() != null);

            ContentDescriptor e = new ContentDescriptor()
            {
                Name = name,
                MimeType = mimeType,
                OrderNo = orderNo,
                URI = uri,                
                DatasetVersion = datasetVersion,
            };
            e.DatasetVersion.ContentDescriptors.Add(e);

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<ContentDescriptor> repo = uow.GetRepository<ContentDescriptor>();
                repo.Put(e);
                uow.Commit();
            }
            return (e);
        }

        /// <summary>
        /// Detaches the content descriptor object from its corresponding dataset version, and then deletes the content descriptor.
        /// </summary>
        /// <param name="entity">The content descriptor object to be deleted.</param>
        /// <returns>True if successful, False otherwise.</returns>
        public bool DeleteContentDescriptor(ContentDescriptor entity)
        {
            Contract.Requires(entity != null);
            Contract.Requires(entity.Id >= 0);

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<ContentDescriptor> repo = uow.GetRepository<ContentDescriptor>();

                entity = repo.Reload(entity);
                entity.DatasetVersion = null;

                repo.Delete(entity);

                uow.Commit();
            }
            // if any problem was detected during the commit, an exception will be thrown!
            return (true);
        }

        /// <summary>
        /// Detaches a list of content descriptor objects from their, possibly different, corresponding dataset versions, and then deletes the content descriptors.
        /// </summary>
        /// <param name="entities">The content descriptor entities to be deleted.</param>
        /// <returns></returns>
        public bool DeleteContentDescriptor(IEnumerable<ContentDescriptor> entities)
        {
            Contract.Requires(entities != null);
            Contract.Requires(Contract.ForAll(entities, (ContentDescriptor e) => e != null));
            Contract.Requires(Contract.ForAll(entities, (ContentDescriptor e) => e.Id >= 0));

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<ContentDescriptor> repo = uow.GetRepository<ContentDescriptor>();

                foreach (var entity in entities)
                {
                    var latest = repo.Reload(entity);
                    latest.DatasetVersion = null;

                    repo.Delete(latest);
                }
                uow.Commit();
            }
            return (true);
        }

        /// <summary>
        /// Having a changed  content descriptor entity, the method applies the changes to the original entity and persists the changes.
        /// </summary>
        /// <param name="entity">The editedVersion version of the content descriptor entity.</param>
        /// <returns>The changed instance.</returns>
        /// <remarks>The entity should already exists in the database.</remarks>
        public ContentDescriptor UpdateContentDescriptor(ContentDescriptor entity)
        {
            Contract.Requires(entity != null, "provided entity can not be null");
            Contract.Requires(entity.Id >= 0, "provided entity must have a permanent ID");

            Contract.Ensures(Contract.Result<ContentDescriptor>() != null && Contract.Result<ContentDescriptor>().Id >= 0, "No entity is persisted!");

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<ContentDescriptor> repo = uow.GetRepository<ContentDescriptor>();
                repo.Put(entity); // Merge is required here!!!!
                uow.Commit();
            }
            return (entity);
        }
      
        #endregion

        #region Associations

        // there is no need for RemoveDataView as it is equal to DeleteDataView. DataView must be associated with a dataset or some data structures but not both
        // if you like to promote a view from a dataset to a data structure, set its Dataset property to null and send it to DataStructureManager.AddDataView

        /// <summary>
        /// Adds a data view to the designated dataset. This method does not execute the view, but only associates it with the dataset so that later its application can be requested.
        /// A data view is the specification of a set of criteria to filter a dataset vertically and horizontally in on demand.
        /// Applying a data view on a dataset version filters its data tuples and variables if the dataset is structured.
        /// For unstructured dataset the data view can be used to pass the filtering criteria to a proper processing tool.
        /// </summary>
        /// <param name="dataset">The dataset the view is linked to</param>
        /// <param name="view">The data view to be associated to the <paramref name="dataset"/>.</param>
        public void AddDataView(Dataset dataset, DataView view)
        {
            Contract.Requires(dataset != null);
            Contract.Requires(view != null && view.Id >= 0);
            Contract.Requires(view.Dataset == null);

            DatasetRepo.Reload(dataset);
            DatasetRepo.LoadIfNot(dataset.Views);
            int count = (from v in dataset.Views
                         where v.Id.Equals(view.Id)
                         select v
                        )
                        .Count();

            if (count > 0)
                throw new Exception(string.Format("There is a connection between dataset {0} and view {1}", dataset.Id, view.Id));

            dataset.Views.Add(view);
            view.Dataset = dataset;
            view.DataStructures.Clear();

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                // save the relation controller object which is the 1 side in 1:N relationships. in this case: View
                IRepository<DataView> repo = uow.GetRepository<DataView>();
                repo.Put(view);
                uow.Commit();
            }
        }

        /// <summary>
        /// Detaches the data view from its dataset and deletes the view from the database.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>True if successful, False otherwise.</returns>
        public bool DeleteDataView(DataView entity)
        {
            Contract.Requires(entity != null);
            Contract.Requires(entity.Id >= 0);

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<DataView> repo = uow.GetRepository<DataView>();

                entity = repo.Reload(entity);
                repo.Delete(entity);
                uow.Commit();
            }
            // if any problem was detected during the commit, an exception will be thrown!
            return (true);
        }

        #endregion
    }
}
