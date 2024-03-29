﻿using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Text;

namespace PortalBusinessRulesCustomizations
{
    public class JavascriptGeneratorPlugin : IPlugin
    {
        const string PORTAL_BUSINESS_RULE_LOGICAL_NAME = "t365_portalbusinessrule";
        const string ENTITY_FORM_LOGICAL_NAME = "adx_entityform";
        const string WEB_FORM_STEP_LOGICAL_NAME = "adx_webformstep";
        const string CUSTOM_JS_FIELD_NAME = "adx_registerstartupscript";
        const string AUTO_JS_START_BLOCK = "//START AUTOJS (DON'T DELETE THIS LINE MANUALLY)\n";
        const string AUTO_JS_END_BLOCK = "//END AUTOJS (DON'T DELETE THIS LINE MANUALLY)\n";
        const string DOCUMENT_READY_START = "$(document).ready(function() {\n";
        const string DOCUMENT_READY_END = "});// end document ready\n";
        const string INJECTED_SCRIPT_CODE = "document.write(\"<script src='/portal-business-rules.js'></\"" + "+ \"script>\");\n";
        const int RULE_PUBLISHED_STATUS = 1;

        string pluginMessage = "";
        public void Execute(IServiceProvider serviceProvider)
        {

            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            EntityReference entityFormReference = null;
            EntityReference webFormStepReference = null;
            pluginMessage = context.MessageName.ToLower();
            Guid currentRecordId = Guid.Empty;
            string currentRecordLogicalName = "";
            string customJavascript = "";
            EntityCollection allBusinessRules = new EntityCollection();
            Entity entityForm = null;
            Entity webFormStep = null;


            if (pluginMessage == "delete")
            {
                var preImageEntity = (context.PreEntityImages != null && context.PreEntityImages.Contains("PreImage")) ? context.PreEntityImages["PreImage"] : null;
                entityFormReference = preImageEntity != null ? preImageEntity.GetAttributeValue<EntityReference>("t365_entityform") : null;
                webFormStepReference = preImageEntity != null ? preImageEntity.GetAttributeValue<EntityReference>("t365_webformstep") : null;
                currentRecordId = preImageEntity != null ? preImageEntity.Id : Guid.Empty;
                currentRecordLogicalName = preImageEntity != null ? preImageEntity.LogicalName : "";
            }
            else if (pluginMessage == "update")
            {
                var postImageEntity = (context.PostEntityImages != null && context.PostEntityImages.Contains("PostImage")) ? context.PostEntityImages["PostImage"] : null;
                entityFormReference = postImageEntity != null ? postImageEntity.GetAttributeValue<EntityReference>("t365_entityform") : null;
                webFormStepReference = postImageEntity != null ? postImageEntity.GetAttributeValue<EntityReference>("t365_webformstep") : null;
                currentRecordId = postImageEntity != null ? postImageEntity.Id : Guid.Empty;
                currentRecordLogicalName = postImageEntity != null ? postImageEntity.LogicalName : "";
            }
            else
            {
                tracingService.Trace("Invalid Message Type");
                return;
            }
            if (currentRecordLogicalName != PORTAL_BUSINESS_RULE_LOGICAL_NAME || currentRecordId == Guid.Empty)
            {
                tracingService.Trace($"Target entity is invalid:{currentRecordLogicalName}");
                return;
            }


            try
            {
                if (entityFormReference != null)
                {
                    entityForm = service.Retrieve(ENTITY_FORM_LOGICAL_NAME, entityFormReference.Id, new ColumnSet(CUSTOM_JS_FIELD_NAME));
                    customJavascript = entityForm.GetAttributeValue<string>(CUSTOM_JS_FIELD_NAME);
                    allBusinessRules = GetSiblingRules(service, tracingService, entityFormReference.Id, BusinessRuleParentType.EntityForm);
                }
                else if (webFormStepReference != null)
                {
                    webFormStep = service.Retrieve(WEB_FORM_STEP_LOGICAL_NAME, webFormStepReference.Id, new ColumnSet(CUSTOM_JS_FIELD_NAME));
                    customJavascript = webFormStep.GetAttributeValue<string>(CUSTOM_JS_FIELD_NAME);
                    allBusinessRules = GetSiblingRules(service, tracingService, webFormStepReference.Id, BusinessRuleParentType.WebFormStep);
                }
                else
                {
                    tracingService.Trace("No Entity form or Web form step exist");
                }

                // This is the compelte javascript generated from this rule and its sibling rules.
                // This is needed to modify that target EntityForm or WebFormStep Custom Javascript.
                string completeJS = GenerateJavascriptFromRules(service, tracingService, allBusinessRules, currentRecordId);

                if (string.IsNullOrEmpty(customJavascript)) customJavascript = "";
                if (string.IsNullOrEmpty(completeJS)) completeJS = "";

                // Update the entity form or web form step after cleaning up the old code 
                if (entityFormReference != null)
                {
                    ModifyTargetCustomJS(service, tracingService, customJavascript, entityForm, BusinessRuleParentType.EntityForm, completeJS);
                }
                else if (webFormStepReference != null)
                {
                    ModifyTargetCustomJS(service, tracingService, customJavascript, webFormStep, BusinessRuleParentType.WebFormStep, completeJS);
                }
                else
                {
                    tracingService.Trace($"No Entity form or Webform step were found");
                    return;
                }

            }
            catch (InvalidCastException castEx)
            {
                tracingService.Trace(castEx.Message);
                if (pluginMessage != "delete")
                {
                    string erroredRule = castEx.Data["RuleName"].ToString();
                    Entity thisEntity = new Entity(PORTAL_BUSINESS_RULE_LOGICAL_NAME, currentRecordId);
                    thisEntity.Attributes["t365_autogeneratedjavascript"] = $"Error in Rule \"{erroredRule}\":" + castEx.Message;
                    service.Update(thisEntity);
                }
                // I don't want to show an error message in this case but the error string will be shown instead of the generated JS
                //throw castEx;
            }
            catch (InvalidPluginExecutionException pex)
            {
                tracingService.Trace(pex.Message);
                throw pex;
            }
            catch (Exception ex)
            {
                tracingService.Trace(ex.Message);
                throw ex;
            }


        }

        private string GenerateJavascriptFromRules(IOrganizationService service, ITracingService tracingService, EntityCollection allBusinessRules, Guid currentBusinessRuleId)
        {

            tracingService.Trace("GenerateJavascriptFromRules");
            StringBuilder completeJS = new StringBuilder();
            StringBuilder rulesOnlyJS = new StringBuilder();

            foreach (Entity businessRule in allBusinessRules.Entities)
            {
                tracingService.Trace($"Processing Business Rule {businessRule.Id}");
                if (pluginMessage == "delete" && businessRule.Id == currentBusinessRuleId) continue;

                string ruleAutoJS = GenerateRuleJS(service, tracingService, businessRule);
                if (!string.IsNullOrEmpty(ruleAutoJS))
                {
                    Entity ruleEntity = new Entity(PORTAL_BUSINESS_RULE_LOGICAL_NAME, businessRule.Id);
                    ruleEntity.Attributes["t365_autogeneratedjavascript"] = ruleAutoJS;
                    service.Update(ruleEntity);
                }
                tracingService.Trace("Rule Generated:\n" + ruleAutoJS);

                rulesOnlyJS.Append(ruleAutoJS);
                // if this is the rule that triggered the plugin, update its automatic javascript
                if (businessRule.Id == currentBusinessRuleId && pluginMessage != "delete")
                {
                    Entity currentRule = new Entity(PORTAL_BUSINESS_RULE_LOGICAL_NAME, businessRule.Id);
                    currentRule.Attributes["t365_autogeneratedjavascript"] = ruleAutoJS;
                    service.Update(currentRule);
                    tracingService.Trace("Updating self");

                }
            }

            if (rulesOnlyJS.Length > 0)
            {
                completeJS.Append(AUTO_JS_START_BLOCK);
                completeJS.Append(INJECTED_SCRIPT_CODE);
                completeJS.Append(DOCUMENT_READY_START);
                completeJS.Append(rulesOnlyJS.ToString());
                completeJS.Append(DOCUMENT_READY_END);
                completeJS.Append(AUTO_JS_END_BLOCK);
            }
            return completeJS.ToString();

        }

        private void ModifyTargetCustomJS(IOrganizationService service, ITracingService tracingService, string cleanedCustomJS, Entity targetEntityFormOrWebFormStep, BusinessRuleParentType parentType, string documentJS)
        {
            if (targetEntityFormOrWebFormStep != null)
            {
                try
                {
                    tracingService.Trace("ModifyTargetCustomJS");
                    cleanedCustomJS = CleanExistingCustomJS(tracingService, cleanedCustomJS);
                    targetEntityFormOrWebFormStep.Attributes[CUSTOM_JS_FIELD_NAME] = cleanedCustomJS.TrimStart() + documentJS;
                    service.Update(targetEntityFormOrWebFormStep);
                }
                catch (InvalidPluginExecutionException pex)
                {
                    throw pex;
                }
            }
        }

        private string CleanExistingCustomJS(ITracingService tracingService, string existingCustomJS)
        {
            tracingService.Trace("CleanExistingCustomJS");

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
            int statusReason = businessRule.GetAttributeValue<OptionSetValue>("statuscode").Value;

            if (statusReason != RULE_PUBLISHED_STATUS) // if the rule is not published, don't generate its javascript
            {
                return "";
            }

            string operand1 = businessRule.GetAttributeValue<string>("t365_operand1");
            string operand2 = businessRule.GetAttributeValue<string>("t365_operand2");
            OptionSetValue operatorValue = businessRule.GetAttributeValue<OptionSetValue>("t365_operator");
            string entityName = businessRule.GetAttributeValue<string>("t365_entityname");
            string positiveJson = businessRule.GetAttributeValue<string>("t365_positiveactionsjson");
            string negativeJson = businessRule.GetAttributeValue<string>("t365_negativeactionsjson");
            string ruleName = businessRule.GetAttributeValue<string>("t365_name");
            Guid ruleId = businessRule.GetAttributeValue<Guid>("t365_portalbusinessruleid");
            positiveJson = string.IsNullOrEmpty(positiveJson) ? "[]" : positiveJson;
            negativeJson = string.IsNullOrEmpty(negativeJson) ? "[]" : negativeJson;

            RuleJSGenerator generator = new RuleJSGenerator(service, tracingService, entityName, businessRule.Id, ruleName);
            return generator.GenerateJavacript(ruleName, ruleId, operand1, operatorValue.Value, operand2, positiveJson, negativeJson);
        }

        private EntityCollection GetSiblingRules(IOrganizationService service, ITracingService tracingService, Guid parentId, BusinessRuleParentType parentType)
        {
            try
            {
                string targetEntityAttribute = (parentType == BusinessRuleParentType.EntityForm ? "t365_entityform" : "t365_webformstep");

                QueryExpression query = new QueryExpression(PORTAL_BUSINESS_RULE_LOGICAL_NAME);
                query.ColumnSet = new ColumnSet("statuscode", "t365_portalbusinessruleid", "t365_autogeneratedjavascript", "t365_operand1", "t365_operand2", "t365_operator", "t365_entityname", "t365_positiveactionsjson", "t365_negativeactionsjson", "t365_name");
                query.Criteria.AddCondition(targetEntityAttribute, ConditionOperator.Equal, parentId); // only sibling rules
                query.Criteria.AddCondition("statuscode", ConditionOperator.Equal, RULE_PUBLISHED_STATUS); // only published rules
                EntityCollection results = service.RetrieveMultiple(query);
                return results;
            }
            catch (InvalidPluginExecutionException pex)
            {
                throw pex;
            }
        }
    }

    public enum BusinessRuleParentType
    {
        EntityForm,
        WebFormStep
    }
}