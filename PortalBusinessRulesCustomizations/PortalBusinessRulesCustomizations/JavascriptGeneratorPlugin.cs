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
            //triggered upon delete message

            //if (context.Depth > 2) return;

            Guid targetId = Guid.Empty;
            string targetLogicalName = "";
            if (context.InputParameters.Contains("Target"))
            {
                if (context.MessageName.ToLower() == "delete")
                {
                    message = "delete";
                    targetId = (context.InputParameters["Target"] as EntityReference).Id;
                    targetLogicalName = (context.InputParameters["Target"] as EntityReference).LogicalName;
                }
                else if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    targetId = (context.InputParameters["Target"] as Entity).Id;
                    targetLogicalName = (context.InputParameters["Target"] as Entity).LogicalName;
                }
                else
                {
                    tracingService.Trace("Not known entity");
                    return;
                }
            }



            if (targetLogicalName != PORTAL_BUSINESS_RULE_ENTITY)
            {
                tracingService.Trace($"Target entity is invalid:{targetLogicalName}");
                return;
            }
            try
            {
                // Find the Entity form Id or the Webform step id, in that order
                Guid recordId = targetId;
                string logicalName = targetLogicalName;
                string customJavascript = "";
                EntityCollection allBusinessRules = new EntityCollection();
                Entity form = service.Retrieve(logicalName, recordId, new ColumnSet("t365_entityform", "t365_webformstep"));
                EntityReference entityFormReference = form.GetAttributeValue<EntityReference>("t365_entityform");
                EntityReference webFormStepReference = form.GetAttributeValue<EntityReference>("t365_webformstep");
                Entity entityForm = null;
                Entity webFormStep = null;
                tracingService.Trace($"Entity Form Reference {entityFormReference} and Web form step reference {webFormStepReference}");
                if (entityFormReference != null)
                {
                    tracingService.Trace("Trying to get the EF entity");
                    entityForm = service.Retrieve(PORTAL_ENTITY_FORM_ENTITY, entityFormReference.Id, new ColumnSet(CUSTOM_JS_FIELD_NAME));
                    tracingService.Trace("Got the EF entity" + entityForm.Id);

                    customJavascript = entityForm.GetAttributeValue<string>(CUSTOM_JS_FIELD_NAME);
                    tracingService.Trace($"EF {customJavascript}");
                    allBusinessRules = GetBusinessRules(service, tracingService, entityFormReference.Id, ParentType.EntityForm);

                }
                else if (webFormStepReference != null)
                {
                    webFormStep = service.Retrieve(PORTAL_WEB_FORM_STEP_ENTITY, webFormStepReference.Id, new ColumnSet(CUSTOM_JS_FIELD_NAME));
                    customJavascript = webFormStep.GetAttributeValue<string>(CUSTOM_JS_FIELD_NAME);
                    allBusinessRules = GetBusinessRules(service, tracingService, webFormStepReference.Id, ParentType.WebFormStep);
                    tracingService.Trace($"WF {customJavascript}");

                }
                else
                {
                    tracingService.Trace("No Entity form or Web form step exist");
                }


                string allRulesJS = GenerateAllRulesJS(service, tracingService, allBusinessRules, targetId);
                tracingService.Trace("allRulesJS");

                if (!string.IsNullOrEmpty(allRulesJS))
                {
                    if (string.IsNullOrEmpty(customJavascript)) customJavascript = "";

                    // write to the new generated js to the rule itself
                    if (entityFormReference != null)
                    {
                        ModifyTargetCustomJS(service, tracingService, customJavascript, entityForm, ParentType.EntityForm, allRulesJS);

                    }
                    else if (webFormStepReference != null)
                    {
                        ModifyTargetCustomJS(service, tracingService, customJavascript, webFormStep, ParentType.WebFormStep, allRulesJS);
                    }
                    else
                    {
                        tracingService.Trace($"All Rules JS {allRulesJS}");
                    }
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

            StringBuilder newJSText = new StringBuilder();
            newJSText.Append(AUTO_JS_START_BLOCK);
            newJSText.Append(DOCUMENT_READY_START);

            foreach (Entity businessRule in allBusinessRules.Entities)
            {
                string ruleAutoJS = GenerateRuleJS(service, tracingService, businessRule);
                newJSText.Append(ruleAutoJS + "\n");
                // if this is the rule that triggered the plugin, update its automatic javascript
                tracingService.Trace("trybing to update");
                if (businessRule.Id == currentBusinessRuleId && message != "delete")
                {
                    Entity currentRule = new Entity("t365_portalbusinssrule", businessRule.Id);
                    currentRule.Attributes["t365_autogeneratedjavascript"] = ruleAutoJS;
                    service.Update(currentRule);
                }
                tracingService.Trace("finished update");

            }

            newJSText.Append(DOCUMENT_READY_END);
            newJSText.Append(AUTO_JS_END_BLOCK);

            return newJSText.ToString();
        }

        private void ModifyTargetCustomJS(IOrganizationService service, ITracingService tracingService, string cleanedCustomJS, Entity target, ParentType parentType, string allRulesJS)
        {
            if (target != null)
            {
                tracingService.Trace("ModifyTargetCustomJS");

                cleanedCustomJS = CleanExistingCustomJS(tracingService, cleanedCustomJS);
                target.Attributes[CUSTOM_JS_FIELD_NAME] = cleanedCustomJS + allRulesJS;
                service.Update(target);
            }
        }

        private string CleanExistingCustomJS(ITracingService tracingService, string formCustomJS)
        {
            tracingService.Trace("CleanExistingCustomJS");
            tracingService.Trace(formCustomJS);

            string injectedScriptString = "document.write(\"<script src='/portal-business-rules.js'></\"" + "+ \"script>\");\n";

            if (!formCustomJS.Contains(injectedScriptString))
            {
                formCustomJS = injectedScriptString + formCustomJS;
            }

            int startingIndex = formCustomJS.IndexOf(AUTO_JS_START_BLOCK);
            tracingService.Trace($"Starting Index={startingIndex}");
            if (startingIndex >= 0)
            {
                int endIndex = formCustomJS.IndexOf(AUTO_JS_END_BLOCK) + AUTO_JS_END_BLOCK.Length;
                tracingService.Trace($"End Index={endIndex}");

                if (endIndex - startingIndex > 0)
                {
                    formCustomJS = formCustomJS.Remove(startingIndex, endIndex - startingIndex - 1);
                    tracingService.Trace($"Starting Index={startingIndex}");
                }
            }

            return formCustomJS;
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