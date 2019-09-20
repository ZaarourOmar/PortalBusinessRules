using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalBusinessRulesCustomizations
{
    public class RuleJSGenerator
    {
        public string BlockStart { get; }
        public string BlockEnd { get; }

        public RuleJSGenerator(string blockStart, string blockEnd)
        {
            BlockStart = blockStart;
            BlockEnd = blockEnd;
        }

        public string GenerateIfStatement(string operand1, string operatorValue, string operand2, string operandType)
        {

            string operatorSymbol = "";
            string ifStatement = "";
            bool nonStringValue = operandType == "WholeNumber" || operandType == "Decimal" || operandType == "Boolean" || operandType == "Optionset";
            switch (operatorValue)
            {
                case "Equal" when nonStringValue:
                    operatorSymbol = "==";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                    break;
                case "Equal":
                    operatorSymbol = "==";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} \"{operand2}\")";
                    break;


                case "Not Equal" when nonStringValue:
                    operatorSymbol = "!=";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                    break;
                case "Not Equal":
                    operatorSymbol = "!=";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} \"{operand2}\")";
                    break;


                case "Less Than" when nonStringValue:
                    operatorSymbol = "<";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                    break;
                case "Less Than":
                    operatorSymbol = "<";
                    break;


                case "Less Than or Equal" when nonStringValue:
                    operatorSymbol = "<=";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                    break;
                case "Less Than or Equal":
                    operatorSymbol = "<=";
                    break;


                case "Greater Than" when nonStringValue:
                    operatorSymbol = ">";
                    ifStatement = $"if (getFieldValue(\"{operand1}\") {operatorSymbol} {operand2})";
                    break;
                case "Greater Than":
                    operatorSymbol = ">";
                    break;


                case "Greater Than or Equal" when nonStringValue:
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
            }


            return ifStatement;
        }


        public string GenerateJavacript(string operand1, string operatorValue, string operand2, string operand1Type, string positiveJson, string negativeJson, string ruleId)
        {
            string ifStatement = GenerateIfStatement(operand1, operatorValue, operand2, operand1Type);
            string ifTrueBody = GenerateIfBody(positiveJson);
            string ifFalseBody = GenerateIfBody(negativeJson);

            string helperFunctions = StaticJavascriptHelpers.GetAllHelperFunctions();

            string finalOutput = ConstructFinalOutput(operand1, ifStatement, ifTrueBody, ifFalseBody, helperFunctions, ruleId);

            return finalOutput;

        }

        private string ConstructFinalOutput(string operand1, string ifStatement, string ifTrueBody, string ifFalseBody, string helperFunctions, string ruleId)
        {
            StringBuilder sb = new StringBuilder();
            //start of the document
            sb.Append(BlockStart);
            sb.Append("$(document).ready(function() {\n");



            sb.Append($"$(\"#{operand1}\").change(function(){{\n");
            sb.Append($"{ifStatement}{{ \n{ifTrueBody} \n }} \n else {{ \n{ifFalseBody} \n }}\n");
            sb.Append("});\n"); // end on change function
            sb.Append($"$(\"#{operand1}\").change();\n"); // trigger the change event


            //end of the document
            sb.Append("});// end document ready\n"); // end document ready

            sb.Append($"\n{StaticJavascriptHelpers.GetAllHelperFunctions()};\n");

            sb.Append(BlockEnd);



            return sb.ToString();
        }

        public string GenerateIfBody(string actionsJson)
        {
            StringBuilder sb = new StringBuilder();
            JSONConverter<List<RuleAction>> converter = new JSONConverter<List<RuleAction>>();
            try
            {
                List<RuleAction> actions = converter.DeSerialize(actionsJson);
                foreach (RuleAction action in actions)
                {
                    string fieldName = action.target;
                    switch (action.type)
                    {
                        case "Show Field":
                            sb.Append($"setVisible(\"{fieldName}\",true);\n");
                            break;
                        case "Hide Field":
                            sb.Append($"setVisible(\"{fieldName}\",false);\n");
                            break;
                        case "Make Required":
                            sb.Append($"setRequired(\"{fieldName}\",true);\n");
                            break;
                        case "Make not Required":
                            sb.Append($"setRequired(\"{fieldName}\",false);\n");
                            break;
                        case "Prevent Past Date":
                            sb.Append($"preventPastDate(\"{fieldName}\");\n");
                            break;
                        case "Prevent Future Date":
                            sb.Append($"preventFutureDate(\"{fieldName}\");\n");
                            break;
                        case "Show Section":
                            sb.Append($"setSectionVisible(\"{fieldName}\",true);\n");
                            break;
                        case "Hide Section":
                            sb.Append($"setSectionVisible(\"{fieldName}\",false);\n");
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
    }
}
