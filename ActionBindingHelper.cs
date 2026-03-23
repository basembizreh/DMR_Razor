using DMB.Core.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMR_Razor
{
    internal class ActionBindingHelper
    {
        public static bool IsDefined(ActionBinding? actionBinding)
        {
            return actionBinding != null
                && !string.IsNullOrWhiteSpace(actionBinding.TargetTypeName)
                && !string.IsNullOrWhiteSpace(actionBinding.ActionExpression);
        }
    }
}
