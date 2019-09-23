using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using PortalBusinessRulesCustomizations.Util;
using System;
using System.Collections.Generic;
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

        public RuleJSGenerator(ITracingService tracingService,Guid ruleId,string ruleName)
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
        private string GenerateIfStatement(string operand1, string operatorValue, string operand2, AttributeTypeCode operand1Type)
        {

            string operatorSymbol = "";
            string ifStatement = "";
            bool nonStringOperand2Value = operand1Type == AttributeTypeCode.Double || operand1Type == AttributeTypeCode.BigInt || operand1Type == AttributeTypeCode.Boolean || operand1Type == AttributeTypeCode.Decimal || operand1Type == AttributeTypeCode.Integer || operand1Type == AttributeTypeCode.Picklist || operand1Type == AttributeTypeCode.Lookup;

            if (nonStringOperand2Value)
            {
                if (!IsValidNonStringOperand(operand1Type, operand2))
                {
                    throw new InvalidOprerandValueException($"Error: Operand2: ({operand2}) doesn't match operand1 type: ({operand1Type})");
                }
            }

            switch (operatorValue)
            {
                case "Equal" when nonStringOperand2Value:
                    operatorSymbol = "==";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                    break;
                case "Equal":
                    operatorSymbol = "==";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} \"{operand2}\")";
                    break;


                case "Not Equal" when nonStringOperand2Value:
                    operatorSymbol = "!=";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                    break;
                case "Not Equal":
                    operatorSymbol = "!=";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} \"{operand2}\")";
                    break;


                case "Less Than" when nonStringOperand2Value:
                    operatorSymbol = "<";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                    break;
                case "Less Than":
                    operatorSymbol = "<";
                    break;


                case "Less Than or Equal" when nonStringOperand2Value:
                    operatorSymbol = "<=";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                    break;
                case "Less Than or Equal":
                    operatorSymbol = "<=";
                    break;


                case "Greater Than" when nonStringOperand2Value:
                    operatorSymbol = ">";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                    break;
                case "Greater Than":
                    operatorSymbol = ">";
                    break;


                case "Greater Than or Equal" when nonStringOperand2Value:
                    operatorSymbol = ">=";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                    break;
                case "Greater Than or Equal":
                    operatorSymbol = ">=";
                    break;


                case "Contains Data":
                    ifStatement = $"if (Boolean(getFieldValue(\"{operand1}\")))";
                    break;

                case "Contains No Data":
                    ifStatement = $"if (!Boolean(getFieldValue(\"{operand1}\")))";
                    break;

                default:
                    throw new InvalidOpreratorException("The passed operator is not recognized.");
            }


            return ifStatement;
        }

        /// <summary>
        /// Determines if operand2 is of valid type based on operand1type. 
        /// </summary>
        /// <param name="operand1Type"></param>
        /// <param name="operand2"></param>
        /// <returns></returns>
        private bool IsValidNonStringOperand(AttributeTypeCode operand1Type, string operand2)
        {
            bool valid = false;

            switch (operand1Type)
            {
                case AttributeTypeCode.BigInt:
                    Int64 bigIntResult;
                    valid = Int64.TryParse(operand2, out bigIntResult);
                    break;
                case AttributeTypeCode.Boolean:
                    bool boolResult;
                    valid = bool.TryParse(operand2, out boolResult);
                    break;
                case AttributeTypeCode.Decimal:
                    decimal decimalResult;
                    valid = decimal.TryParse(operand2, out decimalResult);
                    break;
                case AttributeTypeCode.Double:
                    double doubleResult;
                    valid = double.TryParse(operand2, out doubleResult);
                    break;
                case AttributeTypeCode.Integer:
                    int intResult;
                    valid = int.TryParse(operand2, out intResult);
                    break;
                case AttributeTypeCode.Picklist:
                    int optionSetResult;
                    valid = int.TryParse(operand2, out optionSetResult);
                    break;
                case AttributeTypeCode.Lookup:
                    Guid idResult; ;
                    valid = Guid.TryParse(operand2, out idResult);
                    break;
            }

            return valid;
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
        public string GenerateJavacript(string operand1, string operatorValue, string operand2, AttributeTypeCode operand1Type, string positiveJson, string negativeJson)
        {

                string ifStatement = GenerateIfStatement(operand1, operatorValue, operand2, operand1Type);
                string ifTrueBody = GenerateIfElseBody(positiveJson);
                string ifFalseBody = GenerateIfElseBody(negativeJson);

                string finalOutput = ConstructFinalOutput(operand1, ifStatement, ifTrueBody, ifFalseBody);

                return finalOutput;
           
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
