using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Infrastructure;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Web.Controllers;
using Nop.Web.Models.Customer;
using Polydeck.Nop.Plugin.Misc.RequirePasswordChange.Domain;

namespace Polydeck.Nop.Plugin.Misc.RequirePasswordChange.ActionFilters
{
    /// <summary>
    /// Hides the "Require Password Change" checkbox in the customer-visible 
    /// info page (~/customer/info)
    /// </summary>
    /// <seealso cref="System.Web.Mvc.ActionFilterAttribute" />
    /// <seealso cref="System.Web.Mvc.IFilterProvider" />
    public class CustomerInfoActionFilter : ActionFilterAttribute, IFilterProvider
    {
        #region Fields

        private const string CustomerControllerName = "Customer";
        private const string InfoActionName = "Info";

        #endregion

        #region Methods

        public IEnumerable<Filter> GetFilters(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
        {
            var filters = new List<Filter>();

            if (IsCustomerInfoAction(actionDescriptor, controllerContext?.HttpContext))
                filters.Add(new Filter(this, FilterScope.Action, 0));

            return filters;
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);
            OnCustomerInfoActionExecuted(filterContext);
        }

        private void OnCustomerInfoActionExecuted(ActionExecutedContext filterContext)
        {
            var customerInfoModel = (filterContext.Result as ViewResult)?.Model as CustomerInfoModel;
            if (customerInfoModel == null)
                return;

            customerInfoModel.CustomerAttributes = customerInfoModel.CustomerAttributes
                .Where(attr => attr.Name != RequirePasswordChangePluginCustomerAttributeNames.RequiredPasswordChange)
                .ToList();
        }

        #endregion

        #region Utilities

        private bool IsCustomerInfoAction(ActionDescriptor actionDescriptor, HttpContextBase httpContext)
        {
            var controllerName = actionDescriptor.ControllerDescriptor.ControllerName;
            var actionName = actionDescriptor.ActionName;
            var method = httpContext?.Request?.HttpMethod;

            bool isCustomerController = String.Equals(controllerName, CustomerControllerName, StringComparison.InvariantCultureIgnoreCase);
            bool isInfoAction = String.Equals(actionName, InfoActionName, StringComparison.InvariantCultureIgnoreCase);
            bool isGet = String.Equals(method, "GET", StringComparison.InvariantCultureIgnoreCase);

            return isCustomerController && isInfoAction && isGet;
        }

        #endregion
    }
}