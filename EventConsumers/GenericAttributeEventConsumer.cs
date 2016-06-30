using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Events;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Events;
using Polydeck.Nop.Plugin.Misc.RequirePasswordChange.Domain;

namespace Polydeck.Nop.Plugin.Misc.RequirePasswordChange.EventConsumers
{
    public class GenericAttributeEventConsumer :
        IConsumer<EntityInserted<GenericAttribute>>,
        IConsumer<EntityUpdated<GenericAttribute>>,
        IConsumer<EntityDeleted<GenericAttribute>>
    {
        private const string CustomerGenericAttributeKey = "Customer";
        private readonly ICustomerAttributeParser _customerAttributeParser;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ICustomerService _customerService;
        private readonly ICustomerAttributeService _customerAttributeService;

        public GenericAttributeEventConsumer(
            ICustomerService customerService,
            ICustomerAttributeService customerAttributeService,
            ICustomerAttributeParser customerAttributeParser,
            IGenericAttributeService genericAttributeService)
        {
            this._customerService = customerService;
            this._customerAttributeService = customerAttributeService;
            this._customerAttributeParser = customerAttributeParser;
            this._genericAttributeService = genericAttributeService;
        }

        #region IConsumer HandleEvent Mediators

        public void HandleEvent(EntityInserted<GenericAttribute> eventMessage)
        {
            if (IsCustomerCustomAttribute(eventMessage.Entity))
            {
                // get change type
                var reqPassChangeChangeType = GetReqPassChangeChangeType(eventMessage.Entity);

                // handle change
                if (reqPassChangeChangeType == ReqPassChangeChangeType.Added)
                    OnReqPassChangeAdded(eventMessage.Entity.EntityId);
                else if (reqPassChangeChangeType == ReqPassChangeChangeType.Removed)
                    OnReqPassChangeRemoved(eventMessage.Entity.EntityId);
            }
            else if (IsCustomerPasswordRecoveryTokenAttribute(eventMessage.Entity) &&
                 string.IsNullOrEmpty(eventMessage.Entity.Value))
            {
                OnCustomerPasswordRecoveryTokenAttributeRemoved(eventMessage.Entity.EntityId);
            }
        }

        public void HandleEvent(EntityUpdated<GenericAttribute> eventMessage)
        {
            if (IsCustomerCustomAttribute(eventMessage.Entity))
            {
                // get change type
                var reqPassChangeChangeType = GetReqPassChangeChangeType(eventMessage.Entity);

                // handle change
                if (reqPassChangeChangeType == ReqPassChangeChangeType.Added)
                    OnReqPassChangeAdded(eventMessage.Entity.EntityId);
                else if (reqPassChangeChangeType == ReqPassChangeChangeType.Removed)
                    OnReqPassChangeRemoved(eventMessage.Entity.EntityId);
            }
            else if (IsCustomerPasswordRecoveryTokenAttribute(eventMessage.Entity) &&
                string.IsNullOrEmpty(eventMessage.Entity.Value))
            {
                OnCustomerPasswordRecoveryTokenAttributeRemoved(eventMessage.Entity.EntityId);
            }
        }

        public void HandleEvent(EntityDeleted<GenericAttribute> eventMessage)
        {
            if (IsCustomerCustomAttribute(eventMessage.Entity))
            {
                // handle change
                OnReqPassChangeRemoved(eventMessage.Entity.EntityId);
            }
            else if (IsCustomerPasswordRecoveryTokenAttribute(eventMessage.Entity))
            {
                OnCustomerPasswordRecoveryTokenAttributeRemoved(eventMessage.Entity.EntityId);
            }

        }

        #endregion

        #region OnEvent Handlers

        private void OnReqPassChangeAdded(int customerId)
        {
            // Get customer
            var customer = _customerService.GetCustomerById(customerId);
            if (customer == null)
                throw new InvalidOperationException($"Coud not find customer with id \"{customerId}\"");

            // Get existing password recovery token
            var passwordRecoveryTokenExists = _genericAttributeService
                .GetAttributesForEntity(customerId, CustomerGenericAttributeKey)
                .Any(attr => attr.Key == SystemCustomerAttributeNames.PasswordRecoveryToken);

            // Create password recovery token if none exists.
            if (!passwordRecoveryTokenExists)
                _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.PasswordRecoveryToken, Guid.NewGuid().ToString());

            // Remove password recovery token valid date. Unlike normal password recovery tokens, this should not expire.
            DateTime? generatedDateTime = null;
            _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.PasswordRecoveryTokenDateGenerated, generatedDateTime);
        }

        private void OnReqPassChangeRemoved(int customerId)
        {
            // Get customer
            var customer = _customerService.GetCustomerById(customerId);
            if (customer == null)
                throw new InvalidOperationException($"Coud not find customer with id \"{customerId}\"");

            // Remove password recovery token 
            string passwordRecoveryToken = null;
            _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.PasswordRecoveryToken, passwordRecoveryToken);

            // Remove password recovery token valid date
            DateTime? generatedDateTime = null;
            _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.PasswordRecoveryTokenDateGenerated, generatedDateTime);
        }

        private void OnCustomerPasswordRecoveryTokenAttributeRemoved(int customerId)
        {
        }

        #endregion

        #region Utilities

        private ReqPassChangeChangeType GetReqPassChangeChangeType(GenericAttribute entity)
        {
            if (entity == null)
                return ReqPassChangeChangeType.Unknown;

            var customCustomerAttributesXml = entity.Value;

            var attrs = _customerAttributeParser.ParseCustomerAttributes(customCustomerAttributesXml);
            var attrValues = _customerAttributeParser.ParseCustomerAttributeValues(customCustomerAttributesXml);

            var reqPassChangeAttr = attrs.SingleOrDefault(x => x.Name.Equals(RequirePasswordChangePluginCustomerAttributeNames.RequiredPasswordChange, StringComparison.OrdinalIgnoreCase));
            if (reqPassChangeAttr == null)
                return ReqPassChangeChangeType.Removed;

            var reqPassChangeAttValue = attrValues.SingleOrDefault(x => x.CustomerAttributeId == reqPassChangeAttr.Id);
            if (reqPassChangeAttValue == null)
                // unsure how we landed in this block
                return ReqPassChangeChangeType.Removed;

            if (reqPassChangeAttValue.Name.Equals(RequirePasswordChangePluginCustomerAttributeValueNames.RequiredPasswordChangeYes, StringComparison.OrdinalIgnoreCase))
                return ReqPassChangeChangeType.Added;
            else if (reqPassChangeAttValue.Name.Equals(RequirePasswordChangePluginCustomerAttributeValueNames.RequiredPasswordChangeNo, StringComparison.OrdinalIgnoreCase))
                return ReqPassChangeChangeType.Removed;
            else
                return ReqPassChangeChangeType.Unknown;
        }

        private bool IsCustomerCustomAttribute(GenericAttribute entity)
        {
            if (entity == null)
                return false;

            return entity.KeyGroup == CustomerGenericAttributeKey &&
                entity.Key == SystemCustomerAttributeNames.CustomCustomerAttributes;
        }

        private bool IsCustomerPasswordRecoveryTokenAttribute(GenericAttribute entity)
        {
            if (entity == null)
                return false;

            return entity.KeyGroup == CustomerGenericAttributeKey &&
                entity.Key == SystemCustomerAttributeNames.PasswordRecoveryToken;
        }

        private enum ReqPassChangeChangeType { Unknown, Added, Removed }

        #endregion Utilities
    }
}
