using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalBusinessRulesCustomizations.Util
{
    public class DataTypeValidator
    {
        public bool IsDate(params string[] operandValues)
        {
            DateTime result;

            foreach (string s in operandValues)
            {
                if (!DateTime.TryParse(s, out result))
                {
                    return false;
                }
            }
            return true;
        }
        public bool IsBoolean(params string[] operandValues)
        {
            bool result;

            foreach (string s in operandValues)
            {
                if (!bool.TryParse(s, out result))
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsInteger(params string[] operandValues)
        {

            int result;

            foreach (string s in operandValues)
            {
                if (!int.TryParse(s, out result))
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsDouble(params string[] operandValues)
        {
            double result;

            foreach (string s in operandValues)
            {
                if (!double.TryParse(s, out result))
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsDecimal(params string[] operandValues)
        {
            decimal result;

            foreach (string s in operandValues)
            {
                if (!decimal.TryParse(s, out result))
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsLookup(params string[] operandValues)
        {
            Guid result;

            foreach (string s in operandValues)
            {
                if (!Guid.TryParse(s, out result))
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsPiclist(params string[] operandValues)
        {
            return IsInteger(operandValues);
        }

        public bool IsSTring(params string[] operandValues)
        {
            return true; // any passed string should be a string.
        }
    }
}
