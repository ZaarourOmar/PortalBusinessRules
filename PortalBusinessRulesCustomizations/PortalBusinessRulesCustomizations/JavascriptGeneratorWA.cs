using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalBusinessRulesCustomizations
{

    public class JavascriptGeneratorWA : CodeActivity
    {
        #region Inputs/Outputs
        //Define the properties
        [RequiredArgument]
        [Input("Operand1")]
        public InArgument<string> Operand1Input { get; set; }

        [Input("Operand1Type")]
        public InArgument<string> Operand1TypeInput { get; set; }
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
        [RequiredArgument]
        [Input("EFCustomJS")]
        public InArgument<string> EFCustomJSInput { get; set; }
        [Output("AutomaticJS")]
        public OutArgument<string> AutomaticJsOutput { get; set; }

        [Output("ModifiedEFCustomJS")]
        public OutArgument<string> ModifiedEFCustomJSOutput { get; set; }
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
            string operand1Type = Operand1TypeInput.Get<string>(executionContext);
            string operand2 = Operand2Input.Get<string>(executionContext);
            string operatorValue = OperatorInput.Get<string>(executionContext);
            string positiveJson = PositiveJsonInput.Get<string>(executionContext);
            string negativeJson = NegativeJsonInput.Get<string>(executionContext);
            string EFCustomJS = EFCustomJSInput.Get<string>(executionContext);
            string ruleId = context.PrimaryEntityId.ToString();
            string blockStart = $"//Start AutoJS({ruleId})\n";
            string blockEnd = $"//End AutoJS({ruleId})\n";
            RuleJSGenerator generator = new RuleJSGenerator(blockStart,blockEnd);
            string resultJs = generator.GenerateJavacript(operand1, operatorValue, operand2, "Text", positiveJson, negativeJson, ruleId);

           
            EFCustomJS = ReplaceRuleIfNeeded(blockStart, blockEnd, EFCustomJS);
            ModifiedEFCustomJSOutput.Set(executionContext, EFCustomJS);
            AutomaticJsOutput.Set(executionContext, resultJs);
        }

        private string ReplaceRuleIfNeeded(string blockStart, string blockEnd, string EFCustomJS)
        {
            if (!string.IsNullOrEmpty(EFCustomJS))
            {
                int startingIndex = EFCustomJS.IndexOf(blockStart);
                if (startingIndex > 0)
                {
                    int endIndex = EFCustomJS.IndexOf(blockEnd) + blockEnd.Length;
                    if (endIndex - startingIndex > 0)
                    {
                       return EFCustomJS.Remove(startingIndex, endIndex - startingIndex - 1);
                    }
                }
            }

            return EFCustomJS;
        }

     
    }


}
