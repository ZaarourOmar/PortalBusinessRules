﻿using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalBusinessRulesCustomizations
{
    public enum OperandType
    {

    }
    public class JavascriptGeneratorWA : CodeActivity
    {
        #region Inputs/Outputs
        [RequiredArgument]
        [Input("EntityName")]
        public InArgument<string> EntityNameInput { get; set; }
        [RequiredArgument]
        [Input("Operand1")]
        public InArgument<string> Operand1Input { get; set; }
        [RequiredArgument]
        [Input("Operator")]
        public InArgument<string> OperatorInput { get; set; }
        [RequiredArgument]
        [Input("Operand2")]
        public InArgument<string> Operand2Input { get; set; }
        [RequiredArgument]
        [Input("Positive Actions Json")]
        public InArgument<string> PositiveJsonInput { get; set; }
        [RequiredArgument]
        [Input("Negative Actions Json")]
        public InArgument<string> NegativeJsonInput { get; set; }
        [RequiredArgument]
        [Input("EFCustomJS")]
        public InArgument<string> EFCustomJSInput { get; set; }
        [Output("AutomaticJS")]
        public OutArgument<string> AutomaticJsOutput { get; set; }

        [Output("ModifiedEFCustomJS")]
        public OutArgument<string> FormJavascriptOutput { get; set; }

        [Output("Error")]
        public OutArgument<string> ErrorOutput { get; set; }
        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            string entityName = EntityNameInput.Get<string>(executionContext);
            string operand1LogicalName = Operand1Input.Get<string>(executionContext);
            string operand2Value = Operand2Input.Get<string>(executionContext);
            string operatorValue = OperatorInput.Get<string>(executionContext);
            string positiveJson = PositiveJsonInput.Get<string>(executionContext);
            string negativeJson = NegativeJsonInput.Get<string>(executionContext);
            string formCustomJS = EFCustomJSInput.Get<string>(executionContext);

            string ruleId = context.PrimaryEntityId.ToString();
            try
            {
                RuleJSGenerator generator = new RuleJSGenerator(tracingService, ruleId);
                AttributeTypeCode operand1Type = GetAttributeType(service, entityName, operand1LogicalName);
                string resultJs = generator.GenerateJavacript(operand1LogicalName, operatorValue, operand2Value, operand1Type, positiveJson, negativeJson);
                formCustomJS = CleanFormJSFromExistingRule(tracingService,generator.StartBlock,generator.EndBlock, formCustomJS);
                FormJavascriptOutput.Set(executionContext, formCustomJS);
                AutomaticJsOutput.Set(executionContext, resultJs);
            }
            catch (InvalidOpreratorException operatorException)
            {
                tracingService.Trace(operatorException.Message);
                AutomaticJsOutput.Set(executionContext, "");
                ErrorOutput.Set(executionContext, operatorException.Message);
            }
            catch (InvalidOprerandValueException operandException)
            {
                tracingService.Trace(operandException.Message);
                AutomaticJsOutput.Set(executionContext, "");
                ErrorOutput.Set(executionContext, operandException.Message);
            }
            catch (Exception ex)
            {
                tracingService.Trace(ex.Message);
                AutomaticJsOutput.Set(executionContext, "");
                ErrorOutput.Set(executionContext, "An Error has happened, Please see the plugin trace for details");
                throw ex;
            }

        }

        private AttributeTypeCode GetAttributeType(IOrganizationService service, string entityName, string attributeName)
        {
            RetrieveAttributeRequest attributeRequest = new RetrieveAttributeRequest();
            attributeRequest.EntityLogicalName = entityName;
            attributeRequest.LogicalName = attributeName;
            attributeRequest.RetrieveAsIfPublished = false;
            RetrieveAttributeResponse attributeResponse = service.Execute(attributeRequest) as RetrieveAttributeResponse;
            if (attributeResponse != null)
            {
                return attributeResponse.AttributeMetadata.AttributeType.Value;
            }

            throw new InvalidOperationException($"Failed to get the type of :{attributeName}");
        }

        private string CleanFormJSFromExistingRule(ITracingService tracingService, string startBlock, string endBlock, string formCustomJS)
        {
            string injectedScriptString = "document.write(\"<script src='/portal-business-rules.js'></\"" + "+ \"script>\");";

            if (!string.IsNullOrEmpty(formCustomJS))
            {
                if(!formCustomJS.Contains(injectedScriptString))
                {
                    formCustomJS = injectedScriptString + formCustomJS;
                }

                int startingIndex = formCustomJS.IndexOf(startBlock);
                tracingService.Trace($"Starting Index={startingIndex}");
                if (startingIndex >= 0)
                {
                    int endIndex = formCustomJS.IndexOf(endBlock) + endBlock.Length;
                    tracingService.Trace($"End Index={endIndex}");

                    if (endIndex - startingIndex > 0)
                    {
                        return formCustomJS.Remove(startingIndex, endIndex - startingIndex - 1);
                    }
                }
            }

            return formCustomJS;
        }


    }


}
