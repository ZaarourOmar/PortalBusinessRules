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
            string nonTextLabel = "Non Textual Value";
            string textLabel = "Textual Value";
            string[] operand2Values = operand2.Split('^');
            string operand2JsArray = "[" + String.Join(",", operand2Values) + "]";

            switch (operatorValue)
            {
                case "Equal":
                    operatorSymbol = "==";
                    break;
                case "Not Equal":
                    operatorSymbol = "!=";
                    break;
                case "Less Than":
                    operatorSymbol = "<";
                    break;
                case "Less Than or Equal":
                    operatorSymbol = "<=";
                    break;
                case "Greater Than":
                    operatorSymbol = ">";
                    break;
                case "Greater Than or Equal":
                    operatorSymbol = ">=";
                    break;
                case "Contains Data":
                    ifStatement = $"if (Boolean(getFieldValue(\"{operand1}\")))";
                    return ifStatement;
                case "Contains No Data":
                    ifStatement = $"if (!Boolean(getFieldValue(\"{operand1}\")))";
                    return ifStatement;
                case "In":
                    operand2JsArray = GenerateJSArray(operand2, true);
                    ifStatement = $"if ({operand2JsArray}.includes(getFieldValue(\"{operand1}\")))";
                    return ifStatement;
                case "Not In":
                    operand2JsArray = GenerateJSArray(operand2, true);
                    ifStatement = $"if (!{operand2JsArray}.includes(getFieldValue(\"{operand1}\")))";
                    return ifStatement;

                default:
                    throw new InvalidOprerandValueException("Operand 2 value is not formatted properly.");
            }

            if (operand1Type == AttributeTypeCode.DateTime)
            {
                //make sure operand 2 is valid date
                DateTime result;
                if (DateTime.TryParse(operand2, out result))
                {

                }
            }
            else if (operand1Type == AttributeTypeCode.Boolean)
            {
                bool result;
                if (bool.TryParse(operand2, out result))
                {
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                }
                else
                {
                    throw new InvalidOprerandValueException("Operand 2 Should be a boolean (true or false)");
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
                    throw new InvalidOprerandValueException("Operand 2 Should be an integer");
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
                    throw new InvalidOprerandValueException("Operand 2 Should be an double");
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
                    throw new InvalidOprerandValueException("Operand 2 Should be decimal");
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
                    throw new InvalidOprerandValueException("Operand 2 Should be a GUID");
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
                    throw new InvalidOprerandValueException("Operand 2 Should be the integer value of the option");
                }
            }
            else if (operand1Type == AttributeTypeCode.Memo ||operand1Type== AttributeTypeCode.String)
            {
                ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} \"{operand2}\")";
            }



            return ifStatement;
        }

        private string GenerateIfStatementBasedOnType(string operand1, string operatorValue, string operand2)
        {
            throw new NotImplementedException();
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
