﻿using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalBusinessRulesCustomizations
{
   public  class JavascriptGeneratorWA : CodeActivity
    {
        #region Inputs/Outputs
        //Define the properties
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
        [Output("AutomaticJS")]
        public OutArgument<string> AutomaticJsOutput { get; set; }
        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {
            //Create the tracing service
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();
            //Create the context
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            string operand1 = Operand1Input.Get<string>(executionContext);
            string operand2 = Operand2Input.Get<string>(executionContext);
            string operatorValue =  OperatorInput.Get<string>(executionContext);
            string positiveJson = PositiveJsonInput.Get<string>(executionContext);
            string negativeJson = NegativeJsonInput.Get<string>(executionContext);


            string ifStatement = GenerateIfStatement(operand1, operatorValue, operand2);
            string ifTrueBody = GenerateIfBody(positiveJson);
            string ifFalseBody = GenerateIfBody(negativeJson);
            string finalOutput = $"{ifStatement}{{ \n {ifTrueBody} \n }} \n else {{ \n {ifFalseBody} \n }} ";

            AutomaticJsOutput.Set(executionContext, finalOutput);

        }

        private string GenerateIfBody(string positiveJson)
        {
            return "SomeJS here";
        }

        private string GenerateIfStatement(string operand1, string operatorValue, string operand2)
        {
            string operatorSymbol="";
            switch(operatorValue)
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
                case "Less Thank or Equal":
                    operatorSymbol = "<=";
                    break;
            }
            return $"if ($(\"#{operand1}\").val() {operatorSymbol} {operand2})";
        }
    }
}
