using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Http.ModelBinding;

namespace OrderingCenterApi.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ValidateModelStateAttribute : ActionFilterAttribute
    {
        public ValidateModelStateAttribute()
        {
            this.EnforceIValidatable = false;
        }

        public bool EnforceIValidatable { get; set; }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (!actionContext.ModelState.IsValid)
            {
                if (EnforceIValidatable)
                {
                    RunIValidatable(actionContext);
                }

                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.BadRequest, actionContext.ModelState);
            }

            base.OnActionExecuting(actionContext);
        }

        private void RunIValidatable(HttpActionContext actionContext)
        {
            IValidatableObject validatableObject;

            ModelStateDictionary modelState = actionContext.ModelState;

            foreach (var item in actionContext.ActionArguments)
            {
                validatableObject = item.Value as IValidatableObject;

                if (validatableObject != null)
                {
                    IEnumerable<ValidationResult> results = validatableObject.Validate(new ValidationContext(validatableObject));

                    List<string> modelStateKeys = modelState.Keys.ToList();
                    List<ModelState> modelStateValues = modelState.Values.ToList();

                    foreach (ValidationResult result in results.Where(r => r != ValidationResult.Success))
                    {
                        List<string> errorMemberNames = result.MemberNames.ToList();

                        if (errorMemberNames.Count == 0)
                        {
                            errorMemberNames.Add(item.Key);
                        }

                        foreach (string memberName in errorMemberNames)
                        {
                            int index = modelStateKeys.IndexOf(memberName);

                            if (index == -1 || !modelStateValues[index].Errors.Any(_ => _.ErrorMessage == result.ErrorMessage))
                            {
                                modelState.AddModelError(memberName, result.ErrorMessage);
                            }
                        }
                    }
                }
            }
        }
    }
}