using DMB.Core.Actions;
using DMB.Core.Evaluator;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ZXing;

namespace DMR_Razor
{
    public class ActionInvoker
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ExpressionEvaluator evaluator;

        public ActionInvoker(IServiceProvider serviceProvider, ExpressionEvaluator expressionEvaluator)
        {
            this._serviceProvider = serviceProvider;
            this.evaluator = expressionEvaluator;
        }

        public void Execute(ActionBinding actionBinding, object? successResult = null
            , Exception? failureException = null)
        {
            if (string.IsNullOrWhiteSpace(actionBinding.ActionExpression))
                throw new Exception("Expression is not defined!");

            if (string.IsNullOrWhiteSpace(actionBinding.ReferenceName))
                throw new Exception("Refernce is not defined!");

            var a = System.Reflection.Assembly.GetEntryAssembly();
            var type = a.GetType(actionBinding.ReferenceName);
            var service = _serviceProvider.GetService(type);

            string pattern = @"\[""?(.*?)""?\]";

            // 3. Extract and store in a List
            List<string> inputs = Regex.Matches(actionBinding.ActionExpression, pattern)
                                        .Cast<Match>()
                                        .Select(m => m.Groups[1].Value)
                                        .ToList();

            var inputParams = new List<object?>(inputs.Count);

            if (inputs.Count > 0)
            {
                foreach (var item in inputs)
                {
                    inputParams.Add(this.evaluator.Evaluate(item));
                }
            }

            pattern = @"^(\w+)(?=\()";

            Match match = Regex.Match(actionBinding.ActionExpression, pattern);

            if (!match.Success)
            {
                throw new Exception("Invalid expression format!");
            }

            string methodName = match.Groups[1].Value;
            var methodInfo = service.GetType().GetMethod(methodName);

            if (methodInfo == null) throw new Exception("Method not found");

            try
            {
                var result = methodInfo.Invoke(service, inputParams.ToArray());

                if (actionBinding.OnSuccessAction != null)
                {
                    // result should be consider somewhere
                    this.Execute(actionBinding.OnSuccessAction, result);
                }
            }
            catch (Exception ex)
            {
                if (actionBinding.OnFailureAction != null)
                {
                    this.Execute(actionBinding.OnFailureAction, null, ex);
                }
            }
        }
    }   
}
