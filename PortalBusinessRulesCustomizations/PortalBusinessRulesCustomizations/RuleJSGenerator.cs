using Microsoft.Xrm.Sdk;
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

        /// <summary>
        /// StartBlock is a string that identifies the begining of the automatically generated Javascript
        /// </summary>
        public string StartBlock { get; }
        /// <summary>
        /// EndBlock is a string that identifies the end of the automaticall generated Javascript.
        /// </summary>
        public string EndBlock { get; }

        public RuleJSGenerator(ITracingService tracingService, Guid ruleId, string ruleName)
        {
            TracingService = tracingService;
            StartBlock = $"//Start AutoJS({ruleName}-{ruleId.ToString()})\n";
            EndBlock = $"//End AutoJS({ruleName}-{ruleId.ToString()})\n";
        }

        /// <summary>
        /// Based on the operand and operator types, generate the proper If statement.
        /// </summary>
        /// <param name="operand1"></param>
        /// <param name="operatorValue"></param>
        /// <param name="operand2"></param>
        /// <param name="operand1Type"></param>
        /// <returns></returns>
        private string GenerateIfStatement(string operand1, string operatorValue, string operand2, AttributeTypeCode operand1Type, string operand2ExpectedType)
        {
            string operatorSymbol = "";
            string ifStatement = "";
            switch (operatorValue)
            {
                case "Equal" when operand2ExpectedType == "Number" && IsValidNumber(operand2):
                    operatorSymbol = "==";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                    break;
                case "Equal" when operand2ExpectedType == "Text":
                    operatorSymbol = "==";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} \"{operand2}\")";
                    break;


                case "Not Equal" when operand2ExpectedType == "Number" && IsValidNumber(operand2):
                    operatorSymbol = "!=";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                    break;
                case "Not Equal" when operand2ExpectedType == "Text":
                    operatorSymbol = "!=";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} \"{operand2}\")";
                    break;


                case "Less Than" when operand2ExpectedType == "Number" && IsValidNumber(operand2):
                    operatorSymbol = "<";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                    break;
                case "Less Than" when operand2ExpectedType == "Text":
                    operatorSymbol = "<";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} \"{operand2}\")";
                    break;


                case "Less Than or Equal" when operand2ExpectedType == "Number" && IsValidNumber(operand2):
                    operatorSymbol = "<=";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                    break;
                case "Less Than or Equal" when operand2ExpectedType == "Text":
                    operatorSymbol = "<=";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} \"{operand2}\")";
                    break;


                case "Greater Than" when operand2ExpectedType == "Number" && IsValidNumber(operand2):
                    operatorSymbol = ">";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                    break;
                case "Greater Than" when operand2ExpectedType == "Text":
                    operatorSymbol = ">";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} \"{operand2}\")";
                    break;

                case "Greater Than or Equal" when operand2ExpectedType == "Number" && IsValidNumber(operand2):
                    operatorSymbol = ">=";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                    break;
                case "Greater Than or Equal" when operand2ExpectedType == "Text":
                    operatorSymbol = ">=";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} \"{operand2}\")";
                    break;


                case "Contains Data":
                    ifStatement = $"if (Boolean(getFieldValue(\"{operand1}\")))";
                    break;

                case "Contains No Data":
                    ifStatement = $"if (!Boolean(getFieldValue(\"{operand1}\")))";
                    break;

                default:
                    throw new InvalidOprerandValueException("Operand 2 value is not formatted properly.");
            }


            return ifStatement;
        }

        private bool IsValidNumber(string operand2)
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
        public string GenerateJavacript(string operand1, string operatorValue, string operand2, AttributeTypeCode operand1Type, string operand2ExpectedType, string positiveJson, string negativeJson)
        {
            try
            {

                string ifStatement = GenerateIfStatement(operand1, operatorValue, operand2, operand1Type, operand2ExpectedType);
                string ifTrueBody = GenerateIfElseBody(positiveJson);
                string ifFalseBody = GenerateIfElseBody(negativeJson);

                string finalOutput = ConstructFinalOutput(operand1, ifStatement, ifTrueBody, ifFalseBody);

                return finalOutput;
            }
            catch (InvalidOpreratorException operatorException)
            {
                throw operatorException;
            }
            catch (InvalidOprerandValueException operandException)
            {
                throw operandException;
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
        private string ConstructFinalOutput(string operand1, string ifStatement, string ifTrueBody, string ifFalseBody)
        {
            StringBuilder sb = new StringBuilder();
            //start of the document
            sb.Append(StartBlock);
            sb.Append("$(document).ready(function() {\n");

            sb.Append($"$(\"#{operand1}\").change(function(){{\n");
            sb.Append($"{ifStatement}{{ \n{ifTrueBody} \n }} \n else {{ \n{ifFalseBody} \n }}\n");
            sb.Append("});//end on change function\n");
            sb.Append($"$(\"#{operand1}\").change();\n");
            //end of the document
            sb.Append("});// end document ready\n");
            sb.Append(EndBlock);
            return sb.ToString();
        }


        /// <summary>
        /// Generate a string out of the list of actions stored in the actionJson string.
        /// Generates the proper javascript call based on the action type.
        /// </summary>
        /// <param name="actionsJson">A json array of the actions</param>
        /// <returns></returns>
        private string GenerateIfElseBody(string actionsJson)
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
                    switch (action.type)
                    {
                        case "Show Field":
                            sb.Append($"setVisible(\"{targetName}\",true);\n");
                            break;
                        case "Hide Field":
                            sb.Append($"setVisible(\"{targetName}\",false);\n");
                            break;
                        case "Enable Field":
                            sb.Append($"setDisabled(\"{targetName}\",true);\n");
                            break;
                        case "Disable Field":
                            sb.Append($"setDisabled(\"{targetName}\",false);\n");
                            break;
                        case "Set Field Value":
                            sb.Append($"setValue(\"{targetName}\",\"{message}\");\n");
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

    }

    public class RuleAction
    {
        public string type { get; set; }
        public string target { get; set; }
        public string message { get; set; }
    }
}
