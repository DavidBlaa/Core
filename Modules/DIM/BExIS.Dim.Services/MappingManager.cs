﻿using BExIS.Dim.Entities.Mapping;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Vaiona.Persistence.Api;

namespace BExIS.Dim.Services
{
    public sealed class MappingManager
    {
        public MappingManager()
        {
            IUnitOfWork uow = this.GetUnitOfWork();
            this.LinkElementRepo = uow.GetReadOnlyRepository<LinkElement>();
            this.MappingRepo = uow.GetReadOnlyRepository<Mapping>();
            this.TransformationRuleRepo = uow.GetReadOnlyRepository<TransformationRule>();

        }

        #region Data Readers

        // provide read only repos for the whole aggregate area
        public IReadOnlyRepository<LinkElement> LinkElementRepo { get; private set; }
        public IReadOnlyRepository<Mapping> MappingRepo { get; private set; }
        public IReadOnlyRepository<TransformationRule> TransformationRuleRepo { get; private set; }

        #endregion

        #region LINK ELEMENT

        public IEnumerable<LinkElement> GetLinkElements()
        {
            return LinkElementRepo.Get();
        }

        public LinkElement GetLinkElement(long id)
        {
            return LinkElementRepo.Get().FirstOrDefault(le => le.Id.Equals(id));
        }

        public LinkElement GetLinkElement(LinkElementType type)
        {
            return LinkElementRepo.Get().FirstOrDefault(le => le.Type.Equals(type));
        }

        public LinkElement GetLinkElement(LinkElementType type, long parentid)
        {
            return LinkElementRepo.Get().FirstOrDefault(le => le.Type.Equals(type) && le.Parent.Id.Equals(parentid));
        }

        public LinkElement GetLinkElement(long elementid, LinkElementType type)
        {
            return LinkElementRepo.Get().FirstOrDefault(le => le.ElementId.Equals(elementid) && le.Type.Equals(type));
        }

        public LinkElement CreateLinkElement(long elementId, LinkElementType type, LinkElementComplexity complexity, string name, string xpath, string mask,
            bool isSequence = false, long parentId = -1)
        {
            Contract.Requires(elementId >= 0);

            LinkElement parent = this.GetLinkElement(parentId);

            LinkElement linkElement;

            linkElement = new LinkElement()
            {
                ElementId = elementId,
                Type = type,
                Name = name,
                XPath = xpath,
                IsSequence = isSequence,
                Parent = parent,
                Complexity = complexity,
                Mask = mask
            };

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<LinkElement> repo = uow.GetRepository<LinkElement>();
                repo.Put(linkElement);
                uow.Commit();
            }

            return (linkElement);
        }

        public bool DeleteLinkElement(LinkElement entity)
        {
            Contract.Requires(entity != null);
            Contract.Requires(entity.Id >= 0);

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<LinkElement> repo = uow.GetRepository<LinkElement>();

                repo.Delete(entity);

                uow.Commit();
            }
            // if any problem was detected during the commit, an exception will be thrown!
            return (true);
        }

        public LinkElement UpdateLinkElement(long id, string mask)
        {
            var linkElement = this.LinkElementRepo.Get(id);

            if (linkElement != null)
            {
                linkElement.Mask = mask;

                using (IUnitOfWork uow = this.GetUnitOfWork())
                {
                    IRepository<LinkElement> repo = uow.GetRepository<LinkElement>();
                    repo.Put(linkElement);
                    uow.Commit();
                }
            }

            return (linkElement);
        }

        #endregion

        #region Mapping

        public IEnumerable<Mapping> GetMappings()
        {
            return MappingRepo.Get();
        }

        public Mapping GetMappings(long id)
        {
            return MappingRepo.Get().FirstOrDefault(m => m.Id.Equals(id));
        }

        public Mapping CreateMapping(
            long source_elementId,
            LinkElementType source_type,
            LinkElementComplexity source_complexity,
            string source_name,
            string source_xpath,
            long target_elementId,
            LinkElementType target_type,
            LinkElementComplexity target_complexity,
            string target_name,
            string target_xpath,
            bool source_isSequence = false,
            long source_parentId = -1,
            bool target_isSequence = false,
            long target_parentId = -1,
            TransformationRule rule = null
            )
        {
            LinkElement source = CreateLinkElement(
                source_elementId,
                source_type,
                source_complexity,
                source_name,
                source_xpath,
                "",
                source_isSequence,
                source_parentId
                );

            LinkElement target = CreateLinkElement(
                target_elementId,
                target_type,
                target_complexity,
                target_name,
                target_xpath, "",
                target_isSequence,
                target_parentId
                );

            Mapping mapping = new Mapping();

            mapping.Source = source;
            mapping.Target = target;
            mapping.TransformationRule = rule;

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<Mapping> repo = uow.GetRepository<Mapping>();
                repo.Put(mapping);
                uow.Commit();
            }

            return (mapping);

        }

        public Mapping CreateMapping(LinkElement source, LinkElement target, long level, TransformationRule rule)
        {
            Mapping mapping = new Mapping();

            mapping.Source = source;
            mapping.Target = target;
            mapping.TransformationRule = rule;
            mapping.Level = level;

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<Mapping> repo = uow.GetRepository<Mapping>();
                repo.Put(mapping);
                uow.Commit();
            }

            return (mapping);

        }

        public Mapping UpdateMapping(Mapping mapping)
        {
            Contract.Requires(mapping != null);

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<Mapping> repo = uow.GetRepository<Mapping>();
                repo.Put(mapping);
                uow.Commit();
            }

            return (mapping);

        }

        //TODO add more complexity to the deleting function, also source and target need to delete if no mapping is using that link elements
        public bool DeleteMapping(Mapping entity)
        {
            Contract.Requires(entity != null);
            Contract.Requires(entity.Id >= 0);

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<Mapping> repo = uow.GetRepository<Mapping>();

                repo.Delete(entity);

                uow.Commit();
            }
            // if any problem was detected during the commit, an exception will be thrown!
            return (true);
        }

        public bool DeleteMapping(long id)
        {
            Contract.Requires(id >= 0);

            Mapping entity = MappingRepo.Get(id);
            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<Mapping> repo = uow.GetRepository<Mapping>();

                repo.Delete(entity);

                uow.Commit();
            }
            // if any problem was detected during the commit, an exception will be thrown!
            return (true);
        }

        #endregion

        #region Transformation Rule

        public TransformationRule CreateTransformationRule(string regex)
        {
            var transformationRule = new TransformationRule()
            {
                RegEx = regex,
            };

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<TransformationRule> repo = uow.GetRepository<TransformationRule>();
                repo.Put(transformationRule);
                uow.Commit();
            }

            return (transformationRule);
        }

        public TransformationRule UpdateTransformationRule(long id, string regex)
        {
            var transformationRule = this.TransformationRuleRepo.Get(id);

            if (transformationRule == null)
                transformationRule = CreateTransformationRule(regex);
            else
                transformationRule.RegEx = regex;

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<TransformationRule> repo = uow.GetRepository<TransformationRule>();
                repo.Put(transformationRule);
                uow.Commit();
            }

            return (transformationRule);
        }

        #endregion
    }
}
