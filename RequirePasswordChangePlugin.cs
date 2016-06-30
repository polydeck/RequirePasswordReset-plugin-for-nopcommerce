using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Plugins;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Polydeck.Nop.Plugin.Misc.RequirePasswordChange.Domain;

namespace Polydeck.Nop.Plugin.Misc.RequirePasswordChange
{
    public class RequirePasswordChangePlugin : BasePlugin, IMiscPlugin
    {
        private readonly ICustomerAttributeService _customerAttributeService;
        private readonly IGenericAttributeService _genericAttributeService;

        public RequirePasswordChangePlugin(ICustomerAttributeService customerAttributeService, IGenericAttributeService genericAttributeService)
        {
            this._customerAttributeService = customerAttributeService;
            this._genericAttributeService = genericAttributeService;
        }

        public override void Install()
        {
            // create customer attributes
            var requiredPasswordChangeCustomerAttribute = GetRequiredPasswordChangeCustomerAttribute();
            if (requiredPasswordChangeCustomerAttribute == null)
            {
                var reqPassChangeCustAttr = new CustomerAttribute()
                {
                    Name = RequirePasswordChangePluginCustomerAttributeNames.RequiredPasswordChange,
                    AttributeControlType = AttributeControlType.RadioList,
                    AttributeControlTypeId = (int)AttributeControlType.RadioList,
                    IsRequired = false,
                };

                _customerAttributeService.InsertCustomerAttribute(reqPassChangeCustAttr);

                var reqPassChangeCustAttrValueYes = new CustomerAttributeValue()
                {
                    CustomerAttribute = reqPassChangeCustAttr,
                    CustomerAttributeId = reqPassChangeCustAttr.Id,
                    IsPreSelected = true,
                    DisplayOrder = int.MinValue,
                    Name = RequirePasswordChangePluginCustomerAttributeValueNames.RequiredPasswordChangeYes
                };

                var reqPassChangeCustAttrValueNo = new CustomerAttributeValue()
                {
                    CustomerAttribute = reqPassChangeCustAttr,
                    CustomerAttributeId = reqPassChangeCustAttr.Id,
                    IsPreSelected = false,
                    DisplayOrder = int.MaxValue,
                    Name = RequirePasswordChangePluginCustomerAttributeValueNames.RequiredPasswordChangeNo
                };

                _customerAttributeService.InsertCustomerAttributeValue(reqPassChangeCustAttrValueYes);
                _customerAttributeService.InsertCustomerAttributeValue(reqPassChangeCustAttrValueNo);
            }

            //locales
            foreach (var localeResourceKvp in GetLocaleResourceStrings())
                this.AddOrUpdatePluginLocaleResource(localeResourceKvp.Key, localeResourceKvp.Value);

            base.Install();
        }

        public override void Uninstall()
        {
            // remove customer attributes
            var requiredPasswordChangeCustomerAttribute = GetRequiredPasswordChangeCustomerAttribute();
            if (requiredPasswordChangeCustomerAttribute != null)
                _customerAttributeService.DeleteCustomerAttribute(requiredPasswordChangeCustomerAttribute);

            //locales
            foreach (var localeResourceKvp in GetLocaleResourceStrings())
                this.DeletePluginLocaleResource(localeResourceKvp.Key);

            base.Uninstall();
        }

        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            throw new NotImplementedException();
        }

        #region Utilities

        private static IDictionary<string, string> GetLocaleResourceStrings()
        {
            var localResourceStrings = new Dictionary<string, string>()
            {
                { "Plugins.Polydeck.Misc.RequirePasswordChange.ChangePassword", "Change Password" },
                { "Plugins.Polydeck.Misc.RequirePasswordChange.ChangePasswordButton", "Change Password" },
                { "Plugins.Polydeck.Misc.RequirePasswordChange.PageTitle.ChangePassword", "Change Password" },
                { "Plugins.Polydeck.Misc.RequirePasswordChange.PageTitle.ANewPasswordIsRequired", "A new password is required" },
                { "Plugins.Polydeck.Misc.RequirePasswordChange.Fields.RequirePasswordChange", "Require Password Change" },
                { "Plugins.Polydeck.Misc.RequirePasswordChange.Fields.PayflowProSandboxHost.Hint", "Requires the user to change his or her password upon next login." },
            };

            return localResourceStrings;
        }

        private CustomerAttribute GetRequiredPasswordChangeCustomerAttribute()
        {
            return _customerAttributeService.GetAllCustomerAttributes()
                .FirstOrDefault(x => x.Name == RequirePasswordChangePluginCustomerAttributeNames.RequiredPasswordChange);
        }

        #endregion
    }
}
