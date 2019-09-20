using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalBusinessRulesCustomizations
{
    public class JavascriptGenerator
    {


       

        public  string GenerateIfStatement(string operand1, string operatorValue, string operand2,string operandType)
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
                    ifStatement = $"if (Boolean(getFieldValue(\"{operand1}\"))";
                    break;

                case "Contains No Data":
                    ifStatement = $"if (!Boolean(getFieldValue(\"{operand1}\"))";
                    break;
            }

          
            return ifStatement;
        }

   
        public string GenerateJavacript(string operand1, string operatorValue, string operand2,string operand1Type, string positiveJson, string negativeJson)
        {
            string ifStatement = GenerateIfStatement(operand1, operatorValue, operand2,operand1Type);
            string ifTrueBody = GenerateIfBody(positiveJson);
            string ifFalseBody = GenerateIfBody(negativeJson);

            string helperFunctions = StaticJavascriptHelpers.GetAllHelperFunctions();

            string finalOutput = $"{ifStatement}{{ \n {ifTrueBody} \n }} \n else {{ \n {ifFalseBody} \n }} \n\n ${helperFunctions}";

            return finalOutput;

        }

        public string GenerateIfBody(string actionsJson)
        {
            List < PortalRuleAction > actions = JsonConvert.DeserializeObject(actionsJson);
            return "//SomeJS here";
        }

    }

    public class PortalRuleAction { }
}
