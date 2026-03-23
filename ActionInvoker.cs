using System.Reflection;
using DMB.Core.Evaluator;

namespace DMB.Core.Actions
{
    public class ActionInvoker
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ExpressionEvaluator _evaluator;

        public ActionInvoker(IServiceProvider serviceProvider, ExpressionEvaluator expressionEvaluator)
        {
            _serviceProvider = serviceProvider;
            _evaluator = expressionEvaluator;
        }

        public async Task ExecuteAsync(
            ActionBinding actionBinding,
            object? successResult = null,
            Exception? failureException = null)
        {
            if (actionBinding == null)
                throw new ArgumentNullException(nameof(actionBinding));

            if (string.IsNullOrWhiteSpace(actionBinding.ActionExpression))
                throw new Exception("ActionExpression is not defined.");

            if (string.IsNullOrWhiteSpace(actionBinding.TargetTypeName))
                throw new Exception("TargetTypeName is not defined.");

            string actionExpression = ExpressionEvaluator.NormalizeExpression(actionBinding.ActionExpression);

            Type? targetType = ResolveType(actionBinding.TargetTypeName);
            if (targetType == null)
                throw new Exception($"Type '{actionBinding.TargetTypeName}' was not found.");

            object? service = _serviceProvider.GetService(targetType);
            if (service == null)
                throw new Exception($"Service '{actionBinding.TargetTypeName}' is not registered.");

            string methodName = ExtractMethodName(actionExpression);
            if (string.IsNullOrWhiteSpace(methodName))
                throw new Exception("Invalid ActionExpression format.");

            List<string> argExpressions = ExtractArgumentExpressions(actionExpression);
            object?[] inputParams = BuildInputParameters(argExpressions, successResult, failureException);

            MethodInfo? methodInfo = FindMethod(targetType, methodName, inputParams.Length);
            if (methodInfo == null)
                throw new Exception(
                    $"Method '{methodName}' with {inputParams.Length} parameter(s) was not found on '{targetType.FullName}'.");

            try
            {
                object? result = await InvokeMethodAsync(service, methodInfo, inputParams);

                if (actionBinding.OnSuccessAction != null)
                {
                    await ExecuteAsync(actionBinding.OnSuccessAction, result, null);
                }
            }
            catch (TargetInvocationException ex)
            {
                Exception actualEx = ex.InnerException ?? ex;

                if (actionBinding.OnFailureAction != null)
                {
                    await ExecuteAsync(actionBinding.OnFailureAction, null, actualEx);
                    return;
                }

                throw actualEx;
            }
            catch (Exception ex)
            {
                if (actionBinding.OnFailureAction != null)
                {
                    await ExecuteAsync(actionBinding.OnFailureAction, null, ex);
                    return;
                }

                throw;
            }
        }

        private async Task<object?> InvokeMethodAsync(object service, MethodInfo methodInfo, object?[] inputParams)
        {
            object? rawResult = methodInfo.Invoke(service, inputParams);

            if (rawResult == null)
                return null;

            if (rawResult is Task task)
            {
                await task;

                Type taskType = task.GetType();
                if (taskType.IsGenericType)
                {
                    PropertyInfo? resultProp = taskType.GetProperty("Result");
                    if (resultProp != null)
                        return resultProp.GetValue(task);
                }

                return null;
            }

            return rawResult;
        }

        private object?[] BuildInputParameters(
            List<string> argExpressions,
            object? successResult,
            Exception? failureException)
        {
            if (argExpressions.Count == 0)
                return Array.Empty<object?>();

            var contextVars = new Dictionary<string, object?>();

            if (successResult != null)
                contextVars["Result"] = successResult;

            if (failureException != null)
                contextVars["Exception"] = failureException;

            var values = new object?[argExpressions.Count];

            for (int i = 0; i < argExpressions.Count; i++)
            {
                values[i] = _evaluator.Evaluate(
                    argExpressions[i],
                    contextVars.Count > 0 ? contextVars : null);
            }

            return values;
        }

        private static Type? ResolveType(string targetTypeName)
        {
            Type? type = Type.GetType(targetTypeName, throwOnError: false, ignoreCase: true);
            if (type != null)
                return type;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(targetTypeName, throwOnError: false, ignoreCase: true);
                if (type != null)
                    return type;
            }

            return null;
        }

        private static MethodInfo? FindMethod(Type targetType, string methodName, int parameterCount)
        {
            return targetType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                    m.Name == methodName &&
                    m.GetParameters().Length == parameterCount);
        }

        private static string ExtractMethodName(string actionExpression)
        {
            int idx = actionExpression.IndexOf('(');
            if (idx <= 0)
                return string.Empty;

            return actionExpression.Substring(0, idx).Trim();
        }

        private static List<string> ExtractArgumentExpressions(string actionExpression)
        {
            var result = new List<string>();

            int start = actionExpression.IndexOf('(');
            int end = actionExpression.LastIndexOf(')');

            if (start < 0 || end <= start)
                return result;

            string argsText = actionExpression.Substring(start + 1, end - start - 1).Trim();
            if (string.IsNullOrWhiteSpace(argsText))
                return result;

            int depth = 0;
            bool inString = false;
            char stringChar = '\0';
            int argStart = 0;

            for (int i = 0; i < argsText.Length; i++)
            {
                char c = argsText[i];

                if (inString)
                {
                    if (c == stringChar && (i == 0 || argsText[i - 1] != '\\'))
                        inString = false;

                    continue;
                }

                if (c == '"' || c == '\'')
                {
                    inString = true;
                    stringChar = c;
                    continue;
                }

                if (c == '(' || c == '[' || c == '{')
                {
                    depth++;
                    continue;
                }

                if (c == ')' || c == ']' || c == '}')
                {
                    depth--;
                    continue;
                }

                if (c == ',' && depth == 0)
                {
                    string part = argsText.Substring(argStart, i - argStart).Trim();
                    if (!string.IsNullOrWhiteSpace(part))
                        result.Add(part);

                    argStart = i + 1;
                }
            }

            string last = argsText.Substring(argStart).Trim();
            if (!string.IsNullOrWhiteSpace(last))
                result.Add(last);

            return result;
        }
    }
}