using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalBusinessRulesCustomizations
{
    public class JavascriptGeneratorPlugin : IPlugin
    {

        const string PORTAL_BUSINESS_RULE_ENTITY = "t365_portalbusinessrule";
        const string PORTAL_ENTITY_FORM_ENTITY = "adx_entityform";
        const string PORTAL_WEB_FORM_STEP_ENTITY = "adx_webformstep";
        const string CUSTOM_JS_FIELD_NAME = "adx_registerstartupscript";

        const string AUTO_JS_START_BLOCK = "//START AUTOJS\n";
        const string AUTO_JS_END_BLOCK = "//END AUTOJS\n";
        const string DOCUMENT_READY_START = "$(document).ready(function() {\n";
        const string DOCUMENT_READY_END = "});// end document ready\n";
        string message = "";
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            EntityReference entityFormReference = null;
            EntityReference webFormStepReference = null;
            message = context.MessageName.ToLower();
            Guid currentRecordId = Guid.Empty;
            string currentRecordLogicalName = "";
            string customJavascript = "";
            EntityCollection allBusinessRules = new EntityCollection();
            Entity entityForm = null;
            Entity webFormStep = null;


            if (message == "delete")
            {
                var preImageEntity = (context.PreEntityImages != null && context.PreEntityImages.Contains("PreImage")) ? context.PreEntityImages["PreImage"] : null;
                entityFormReference = preImageEntity != null ? preImageEntity.GetAttributeValue<EntityReference>("t365_entityform") : null;
                webFormStepReference = preImageEntity != null ? preImageEntity.GetAttributeValue<EntityReference>("t365_webformstep") : null;
                currentRecordId = preImageEntity != null ? preImageEntity.Id : Guid.Empty;
                currentRecordLogicalName = preImageEntity != null ? preImageEntity.LogicalName : "";
            }
            else
            {
                var postImageEntity = (context.PostEntityImages != null && context.PostEntityImages.Contains("PostImage")) ? context.PostEntityImages["PostImage"] : null;
                entityFormReference = postImageEntity != null ? postImageEntity.GetAttributeValue<EntityReference>("t365_entityform") : null;
                webFormStepReference = postImageEntity != null ? postImageEntity.GetAttributeValue<EntityReference>("t365_webformstep") : null;
                currentRecordId = postImageEntity != null ? postImageEntity.Id : Guid.Empty;
                currentRecordLogicalName = postImageEntity != null ? postImageEntity.LogicalName : "";
            }

            if (currentRecordLogicalName != PORTAL_BUSINESS_RULE_ENTITY || currentRecordId == Guid.Empty)
            {
                tracingService.Trace($"Target entity is invalid:{currentRecordLogicalName}");
                return;
            }
            try
            {
                if (entityFormReference != null)
                {
                    entityForm = service.Retrieve(PORTAL_ENTITY_FORM_ENTITY, entityFormReference.Id, new ColumnSet(CUSTOM_JS_FIELD_NAME));
                    customJavascript = entityForm.GetAttributeValue<string>(CUSTOM_JS_FIELD_NAME);
                    allBusinessRules = GetBusinessRules(service, tracingService, entityFormReference.Id, ParentType.EntityForm);

                }
                else if (webFormStepReference != null)
                {
                    webFormStep = service.Retrieve(PORTAL_WEB_FORM_STEP_ENTITY, webFormStepReference.Id, new ColumnSet(CUSTOM_JS_FIELD_NAME));
                    customJavascript = webFormStep.GetAttributeValue<string>(CUSTOM_JS_FIELD_NAME);
                    allBusinessRules = GetBusinessRules(service, tracingService, webFormStepReference.Id, ParentType.WebFormStep);
                }
                else
                {
                    tracingService.Trace("No Entity form or Web form step exist");
                }

                string documentJS = GenerateAllRulesJS(service, tracingService, allBusinessRules, currentRecordId);

                if (string.IsNullOrEmpty(customJavascript)) customJavascript = "";
                if (string.IsNullOrEmpty(documentJS)) documentJS = "";

                if (entityFormReference != null)
                {
                    ModifyTargetCustomJS(service, tracingService, customJavascript, entityForm, ParentType.EntityForm, documentJS);
                }
                else if (webFormStepReference != null)
                {
                    ModifyTargetCustomJS(service, tracingService, customJavascript, webFormStep, ParentType.WebFormStep, documentJS);
                }
                else
                {
                    tracingService.Trace($"All Rules JS {documentJS}");
                }

            }
            catch (Exception ex)
            {
                tracingService.Trace(ex.Message);
                throw ex;
            }


        }

        private string GenerateAllRulesJS(IOrganizationService service, ITracingService tracingService, EntityCollection allBusinessRules, Guid currentBusinessRuleId)
        {
            tracingService.Trace("GenerateAllRulesJS");

            StringBuilder documentJS = new StringBuilder();

            StringBuilder allRulesJS = new StringBuilder();

            foreach (Entity businessRule in allBusinessRules.Entities)
            {
                if (message == "delete" && businessRule.Id == currentBusinessRuleId) continue;

                string ruleAutoJS = GenerateRuleJS(service, tracingService, businessRule);
                allRulesJS.Append(ruleAutoJS + "\n");
                // if this is the rule that triggered the plugin, update its automatic javascript
                tracingService.Trace("trybing to update");
                if (businessRule.Id == currentBusinessRuleId && message != "delete")
                {
                    Entity currentRule = new Entity("t365_portalbusinessrule", businessRule.Id);
                    currentRule.Attributes["t365_autogeneratedjavascript"] = ruleAutoJS;
                    service.Update(currentRule);
                }
                tracingService.Trace("finished update");

            }

            if (allRulesJS.Length > 0)
            {
                documentJS.Append(AUTO_JS_START_BLOCK);
                documentJS.Append(DOCUMENT_READY_START);
                documentJS.Append(allRulesJS.ToString());
                documentJS.Append(DOCUMENT_READY_END);
                documentJS.Append(AUTO_JS_END_BLOCK);
            }
            return documentJS.ToString();
        }

        private void ModifyTargetCustomJS(IOrganizationService service, ITracingService tracingService, string cleanedCustomJS, Entity targetEntityFormOrWebFormStep, ParentType parentType, string documentJS)
        {
            if (targetEntityFormOrWebFormStep != null)
            {
                tracingService.Trace("ModifyTargetCustomJS");
                cleanedCustomJS = CleanExistingCustomJS(tracingService, cleanedCustomJS, documentJS);
                targetEntityFormOrWebFormStep.Attributes[CUSTOM_JS_FIELD_NAME] = cleanedCustomJS + documentJS;
                service.Update(targetEntityFormOrWebFormStep);
            }
        }

        private string CleanExistingCustomJS(ITracingService tracingService, string existingCustomJS, string newDocumentJS)
        {
            tracingService.Trace("CleanExistingCustomJS");

            string injectedScriptString = "document.write(\"<script src='/portal-business-rules.js'></\"" + "+ \"script>\");\n";

            if (!existingCustomJS.Contains(injectedScriptString))
            {
                existingCustomJS = injectedScriptString + existingCustomJS;
            }
            else if (existingCustomJS.Contains(injectedScriptString) && string.IsNullOrEmpty(newDocumentJS))
            {
                existingCustomJS.Remove(existingCustomJS.IndexOf(injectedScriptString), injectedScriptString.Length - 1);
            }

            int startingIndex = existingCustomJS.IndexOf(AUTO_JS_START_BLOCK);
            tracingService.Trace($"Starting Index={startingIndex}");
            if (startingIndex >= 0)
            {
                int endIndex = existingCustomJS.IndexOf(AUTO_JS_END_BLOCK) + AUTO_JS_END_BLOCK.Length;
                tracingService.Trace($"End Index={endIndex}");

                if (endIndex - startingIndex > 0)
                {
                    existingCustomJS = existingCustomJS.Remove(startingIndex, endIndex - startingIndex - 1);
                    tracingService.Trace($"Starting Index={startingIndex}");
                }
            }

            return existingCustomJS;
        }

        private string GenerateRuleJS(IOrganizationService service, ITracingService tracingService, Entity businessRule)
        {
            string operand1 = businessRule.GetAttributeValue<string>("t365_operand1");
            string operand2 = businessRule.GetAttributeValue<string>("t365_operand2");
            OptionSetValue operatorValue = businessRule.GetAttributeValue<OptionSetValue>("t365_operator");
            string entityName = businessRule.GetAttributeValue<string>("t365_entityname");
            string positiveJson = businessRule.GetAttributeValue<string>("t365_positiveactionsjson");
            string negativeJson = businessRule.GetAttributeValue<string>("t365_negativeactionsjson");
            string ruleName = businessRule.GetAttributeValue<string>("t365_name");
            AttributeTypeCode operand1Type = GetAttributeType(service, tracingService, entityName, operand1);
            positiveJson = string.IsNullOrEmpty(positiveJson) ? "[]" : positiveJson;
            negativeJson = string.IsNullOrEmpty(negativeJson) ? "[]" : negativeJson;

            RuleJSGenerator generator = new RuleJSGenerator(service, tracingService, entityName, businessRule.Id, ruleName);
            string js = generator.GenerateJavacript(operand1, operatorValue.Value, operand2, operand1Type, positiveJson, negativeJson);
            return js;

        }

        private EntityCollection GetBusinessRules(IOrganizationService service, ITracingService tracingService, Guid parentId, ParentType parentType)
        {
            string targetEntityAttribute = (parentType == ParentType.EntityForm ? "t365_entityform" : "t365_webformstep");

            QueryExpression query = new QueryExpression(PORTAL_BUSINESS_RULE_ENTITY);
            query.ColumnSet = new ColumnSet(true);
            query.Criteria.AddCondition(targetEntityAttribute, ConditionOperator.Equal, parentId);
            EntityCollection results = service.RetrieveMultiple(query);
            return results;
        }
        private AttributeTypeCode GetAttributeType(IOrganizationService service, ITracingService tracingService, string entityName, string attributeName)
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
    }

    public enum ParentType
    {
        EntityForm,
        WebFormStep
    }
}