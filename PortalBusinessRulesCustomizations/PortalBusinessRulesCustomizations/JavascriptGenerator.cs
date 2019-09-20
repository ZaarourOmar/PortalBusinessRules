using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalBusinessRulesCustomizations
{
    public class JavascriptGenerator
    {


        public static string GenerateIfBody(string positiveJson)
        {
            return "//SomeJS here";
        }

        public static string GenerateIfStatement(string operand1, string operatorValue, string operand2)
        {
            string operatorSymbol = "";
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
                case "Less Thank or Equal":
                    operatorSymbol = "<=";
                    break;
            }

            return $"if (getFieldValue(\"{operand1}\") {operatorSymbol} \"{operand2}\")";
        }

        private static string GetFieldValueJS()
        {
            return $"function getFieldValue(e){{var l=document.getElementById(e),n=null===l||null===l.type||void 0===l.type?\"\":l.type;if(null===l)return null;var t=n.indexOf(\"text\")>-1&&-1===l.className.indexOf(\"money\"),a=n.indexOf(\"text\")>-1&&l.className.indexOf(\"money\")>-1,d=l.className.indexOf(\"lookup\")>-1&&n.indexOf(\"select\")>-1,u=null!==document.getElementById(e+\"_name\")&&document.getElementById(e+\"_name\").className.indexOf(\"lookup\")>-1,i=l.className.indexOf(\"picklist\")>-1&&n.indexOf(\"select\")>-1,o=l.className.indexOf(\"picklist\")>-1&&-1===n.indexOf(\"select\"),c=l.className.indexOf(\"boolean - dropdown\")>-1&&n.indexOf(\"select\")>-1,s=l.className.indexOf(\"boolean - radio\")>-1,m=l.className.indexOf(\"datetime\")>-1,r=n.indexOf(\"checkbox\")>-1;if(d)return\"\"===l.value?null:{{id:l.options[l.selectedIndex].value}};if(u)return\"\"===l.value?null:{{id:l.value,name:document.getElementById(e+\"_name\").value,logicalName:document.getElementById(e+\"_entityname\").value}};if(i||c)return c?\"1\"===l.options[l.selectedIndex].value:l.options[l.selectedIndex].value;if(o||s){{var f=null;if(\"function\"==typeof document.querySelector&&\"function\"==typeof document.querySelectorAll)f=document.querySelectorAll('*[id^=\"'+e+'_\"]');else for(var x=document.getElementsByTagName(\" * \"),v=0;v<x.length;v++)\"radio\"===x[v].type&&x[v].id.indexOf(e)&&f.push(x[v]);for(var y=0;y<f.length;y++)if(\"radio\"===f[y].type&&f[y].checked)return l.className.indexOf(\"boolean - radio\")>-1?\"1\"===f[y].value:f[y].value;return null}}return m?null===l.value||\"\"===l.value?null:new Date(l.value):r?l.checked:t&&!a?l.value:a?parseInt(l.value,10):null}}";
        }

        public string GenerateJavacript(string operand1, string operatorValue, string operand2, string positiveJson, string negativeJson)
        {
            string ifStatement = GenerateIfStatement(operand1, operatorValue, operand2);
            string ifTrueBody = GenerateIfBody(positiveJson);
            string ifFalseBody = GenerateIfBody(negativeJson);

            string helperFunctions = GetAllHelperFunctions();

            string finalOutput = $"{ifStatement}{{ \n {ifTrueBody} \n }} \n else {{ \n {ifFalseBody} \n }} \n\n ${helperFunctions}";

            return finalOutput;


        }

        private  string SetVisibleJS()
        {
            return "function setVisible(e,t){for(var l=getArrayOfFieldNames(e),a=0;a<l.length;a++){var n=l[a],s=document.getElementById(n),i=document.getElementById(n+\"_label\");if(null===s)return;t||\"none\"===s.style.display||(saveElemDisplayType(s),saveElemDisplayType(i)),s.parentElement.parentElement.style.display=t?getPreviousDisplayValue(s):\"none\",i.parentElement.parentElement.style.display=t?getPreviousDisplayValue(s):\"none\";var r=document.getElementById(n).parentElement.parentElement.parentElement;if(t)r.style.display=getPreviousDisplayValue(r,e+\"_parentrow\"),checkForLeftPadding(e,!1,r,r.getElementsByTagName(\"td\"));else{for(var m=!0,d=r.getElementsByTagName(\"td\"),y=d[0].className.indexOf(\"picklist\")>-1,p=0;p<d.length;p++)if(!(d[p].className.indexOf(\"zero - cell\")>-1||0===d[p].offsetWidth&&0===d[p].offsetHeight||y&&d[p].className.indexOf(\"clearfix cell\")>-1)){m=!1;break}checkForLeftPadding(e,m,r,d)}}}";
        }

        private  string SetDisabledJS()
        {
            return "function setDisabled(e, t) { for (var a = getArrayOfFieldNames(e), d = 0; d < a.length; d++) { var l = a[d]; document.getElementById(l).disabled = t} }";
        }

        private  string SetReadOnlyJS()
        {
            return "function setReadOnly(e,n){for(var a=getArrayOfFieldNames(e),r=0;r<a.length;r++){var t=a[r];document.getElementById(t).readOnly=n?\"readonly\":\"\"}}";
        }
        public  string GetAllHelperFunctions()
        {
            return "\n/*Start of Helper Functions*/" +
                "\n" + GetFieldValueJS() + "\n" +
                "\n" + SetVisibleJS() +"\n" +
                "\n" + SetDisabledJS() +"\n" +
                "\n" + SetReadOnlyJS() +"\n" +
                $"\n/*End of Helper Functions*/\n";
        }
    }
}
