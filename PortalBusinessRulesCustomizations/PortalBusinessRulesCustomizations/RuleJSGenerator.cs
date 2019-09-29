using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using PortalBusinessRulesCustomizations.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PortalBusinessRulesCustomizations
{
    public class RuleJSGenerator
    {
        public ITracingService TracingService { get; }
        public IOrganizationService Service { get; }

        public string EntityName { get; set; }
        /// <summary>
        /// StartBlock is a string that identifies the begining of the automatically generated Javascript
        /// </summary>
        public string CurrentRuleStartBlock { get; }
        /// <summary>
        /// EndBlock is a string that identifies the end of the automaticall generated Javascript.
        /// </summary>
        public string CurrentRuleEndBlock { get; }

        public RuleJSGenerator(IOrganizationService service, ITracingService tracingService, string entityName, Guid ruleId, string ruleName)
        {
            TracingService = tracingService;
            Service = service;
            EntityName = entityName;
            CurrentRuleStartBlock = $"//Start AutoJS({ruleName}-{ruleId.ToString()})\n";
            CurrentRuleEndBlock = $"//End AutoJS({ruleName}-{ruleId.ToString()})\n";
        }


        /// <summary>
        /// Based on the operand and operator types, generate the proper If statement.
        /// </summary>
        /// <param name="operand1"></param>
        /// <param name="operatorValue"></param>
        /// <param name="operand2"></param>
        /// <param name="operand1Type"></param>
        /// <returns></returns>
        private string GenerateIfStatement(string operand1, int operatorValue, string operand2, AttributeTypeCode operand1Type)
        {
            string operatorSymbol = "";
            string ifStatement = "";
            string operand2JsArray;
            switch (operatorValue)
            {
                case 497060000:
                    operatorSymbol = "==";
                    break;
                case 497060001:
                    operatorSymbol = "!=";
                    break;
                case 497060002:
                    operatorSymbol = "<";
                    break;
                case 497060006:
                    operatorSymbol = "<=";
                    break;
                case 497060003:
                    operatorSymbol = ">";
                    break;
                case 497060004:
                    operatorSymbol = ">=";
                    break;
                case 497060005: // contains
                    ifStatement = $"if (Boolean(getFieldValue(\"{operand1}\")) &&getFieldValue(\"{operand1}\")!=\"\" )";
                    return ifStatement;
                case 497060007: // doesn't contain
                    ifStatement = $"if (!Boolean(getFieldValue(\"{operand1}\")))";
                    return ifStatement;
                case 497060008: //in
                    operand2JsArray = GenerateJSArray(operand2, true);
                    ifStatement = $"if ({operand2JsArray}.includes(getFieldValue(\"{operand1}\")))";
                    return ifStatement;
                case 497060009: // not in
                    operand2JsArray = GenerateJSArray(operand2, true);
                    ifStatement = $"if (!{operand2JsArray}.includes(getFieldValue(\"{operand1}\")))";
                    return ifStatement;

                default:
                    throw new Exception("Operand 2 value is not formatted properly.");
            }

            TracingService.Trace(operatorValue.ToString());

            if (operand1Type == AttributeTypeCode.DateTime)
            {
                //make sure operand 2 is valid date
                DateTime result;
                if (DateTime.TryParse(operand2, out result))
                {
                    string date1 = $"new Date(getFieldValue(\"{operand1}\"))";
                    string date2 = $"new Date(\"{operand2}\")";
                    ifStatement = $"if ({date1} {operatorSymbol} {date2})";
                }
                else
                {
                    throw new Exception("Operand 2 Should be a formatted as a datetime");
                }
            }
            else if (operand1Type == AttributeTypeCode.Boolean)
            {
                bool result;
                if (bool.TryParse(operand2, out result) || operand2 == "1" || operand2 == "0")
                {
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                }
                else
                {
                    throw new Exception("Operand 2 Should be a boolean (true or false)");
                }
            }
            else if (operand1Type == AttributeTypeCode.Integer)
            {
                int result;
                if (int.TryParse(operand2, out result))
                {
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                }
                else
                {
                    throw new Exception("Operand 2 Should be an integer");
                }
            }
            else if (operand1Type == AttributeTypeCode.Double)
            {
                double result;
                if (double.TryParse(operand2, out result))
                {
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                }
                else
                {
                    throw new Exception("Operand 2 Should be an double");
                }
            }
            else if (operand1Type == AttributeTypeCode.Decimal)
            {
                decimal result;
                if (decimal.TryParse(operand2, out result))
                {
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                }
                else
                {
                    throw new Exception("Operand 2 Should be decimal");
                }
            }
            else if (operand1Type == AttributeTypeCode.Lookup)
            {
                Guid result;
                if (Guid.TryParse(operand2, out result))
                {
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} \"{operand2}\")";
                }
                else
                {
                    throw new Exception("Operand 2 Should be a GUID");
                }
            }
            else if (operand1Type == AttributeTypeCode.Picklist)
            {
                int result;
                if (int.TryParse(operand2, out result))
                {
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                }
                else
                {
                    throw new Exception("Operand 2 Should be the integer value of the option");
                }
            }
            else if (operand1Type == AttributeTypeCode.Memo || operand1Type == AttributeTypeCode.String)
            {
                ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} \"{operand2}\")";
            }



            return ifStatement;
        }

        private string GenerateJSArray(string operand2, bool textualValues)
        {
            string[] values = operand2.Split('^');
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            foreach (string s in values)
            {
                if (textualValues)
                {
                    sb.Append($"\"{s}\",");
                }
                else
                {
                    sb.Append($"{s},");
                }
            }
            sb.Append("]");
            return sb.ToString();

        }

        private bool IsValidNonString(string operand2)
        {
            return operand2.All(char.IsNumber) || operand2 == "true" || operand2 == "false";
        }



        /// <summary>
        /// The entry point of the Rule JS generator. This function constructs the If statement and wraps it with a documentready function.
        /// </summary>
        /// <param name="operand1"></param>
        /// <param name="operatorValue"></param>
        /// <param name="operand2"></param>
        /// <param name="operand1Type"></param>
        /// <param name="positiveJson"></param>
        /// <param name="negativeJson"></param>
        /// <returns></returns>
        public string GenerateJavacript(string operand1, int operatorValue, string operand2, string positiveJson, string negativeJson)
        {
            try
            {
                AttributeTypeCode operand1Type = GetAttributeType(Service, EntityName, operand1);
                string ifStatement = GenerateIfStatement(operand1, operatorValue, operand2, operand1Type);
                string ifTrueBody = GenerateIfElseBody(positiveJson, operand1Type);
                string ifFalseBody = GenerateIfElseBody(negativeJson, operand1Type);

                string finalOutput = ConstructFinalOutput(operand1, operand1Type, ifStatement, ifTrueBody, ifFalseBody);

                return finalOutput;
            }

            catch (Exception ex)
            {
                throw ex;
            }

        }


        /// <summary>
        /// This function injects the if/else statement inside a document ready Jquery function and adds an onchange trigger. 
        /// This will allow the logic to run on load and on change of the field (operand1)
        /// </summary>
        /// <param name="operand1"></param>
        /// <param name="ifStatement"></param>
        /// <param name="ifTrueBody"></param>
        /// <param name="ifFalseBody"></param>
        /// <param name="ruleId"></param>
        /// <returns></returns>
        private string ConstructFinalOutput(string operand1, AttributeTypeCode operand1Type, string ifStatement, string ifTrueBody, string ifFalseBody)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(CurrentRuleStartBlock);
            if (operand1Type == AttributeTypeCode.DateTime)
            {
                sb.Append($"$(\"#{operand1}\").parent().on(\"dp.change\",function(){{\n");

            }
            else
            {
                sb.Append($"$(\"#{operand1}\").change(function(){{\n");
            }

            sb.Append($"{ifStatement}{{ \n{ifTrueBody} \n }} \n else {{ \n{ifFalseBody} \n }}\n");
            sb.Append("});//end on change function\n");

            if (operand1Type == AttributeTypeCode.DateTime)
            {
                sb.Append($"$(\"#{operand1}\").parent().trigger(\"dp.change\");\n");
            }
            else
            {
                sb.Append($"$(\"#{operand1}\").change();\n");
            }
            sb.Append(CurrentRuleEndBlock);
            return sb.ToString();
        }


        /// <summary>
        /// Generate a string out of the list of actions stored in the actionJson string.
        /// Generates the proper javascript call based on the action type.
        /// </summary>
        /// <param name="actionsJson">A json array of the actions</param>
        /// <returns></returns>
        private string GenerateIfElseBody(string actionsJson, AttributeTypeCode operand1Type)
        {
            StringBuilder sb = new StringBuilder();
            JSONConverter<List<RuleAction>> converter = new JSONConverter<List<RuleAction>>();
            try
            {
                List<RuleAction> actions = converter.Deserialize(actionsJson);
                foreach (RuleAction action in actions)
                {
                    string targetName = action.target; // can be a section or a field
                    string message = action.message;
                    string value = action.value;

                    switch (action.type)
                    {
                        case "Show Field":
                            sb.Append($"setVisible(\"{targetName}\",true);\n");
                            break;
                        case "Hide Field":
                            sb.Append($"setVisible(\"{targetName}\",false);\n");
                            break;
                        case "Enable Field":
                            sb.Append($"setDisabled(\"{targetName}\",false);\n");
                            break;
                        case "Disable Field":
                            sb.Append($"setDisabled(\"{targetName}\",true);\n");
                            break;
                        case "Set Field Value":
                            AttributeTypeCode actionTargetType = GetAttributeType(Service, EntityName, targetName);
                            bool nonTextualValue = actionTargetType == AttributeTypeCode.Picklist || actionTargetType == AttributeTypeCode.Boolean || actionTargetType == AttributeTypeCode.Money || actionTargetType == AttributeTypeCode.Integer || actionTargetType == AttributeTypeCode.Double || actionTargetType == AttributeTypeCode.Decimal;
                            if (nonTextualValue)
                            {
                                sb.Append($"setValue(\"{targetName}\",{value});\n");
                            }
                            else
                            {
                                sb.Append($"setValue(\"{targetName}\",\"{value}\");\n");
                            }
                            break;

                        case "Make Required":
                            sb.Append($"setRequired(\"{targetName}\",true,\"{message}\");\n");
                            break;
                        case "Make not Required":
                            sb.Append($"setRequired(\"{targetName}\",false);\n");
                            break;
                        case "Prevent Past Date":
                            sb.Append($"blockPastDate(\"{targetName}\",\"{message}\");\n");
                            break;
                        case "Prevent Future Date":
                            sb.Append($"blockFutureDate(\"{targetName}\",\"{message}\");\n");
                            break;
                        case "Show Section":
                            sb.Append($"setSectionVisible(\"{targetName}\",true);\n");
                            break;
                        case "Hide Section":
                            sb.Append($"setSectionVisible(\"{targetName}\",false);\n");
                            break;
                        case "Show Tab":
                            sb.Append($"setTabVisible(\"{targetName}\",true);\n");
                            break;
                        case "Hide Tab":
                            sb.Append($"setTabVisible(\"{targetName}\",false);\n");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return sb.ToString();
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
    }


    public class RuleAction
    {
        public string type { get; set; }
        public string target { get; set; }
        public string value { get; set; }
        public string message { get; set; }
    }
}
