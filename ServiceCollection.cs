using DMB.Core.Elements;
using DMB.Core.Evaluator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DMR_Razor
{
    public static class ServiceCollection
    {
        public static IServiceCollection AddDmrServices(this IServiceCollection services)
        {
            services.TryAddScoped<DMB.Core.ModuleDocumentCore>();
            services.TryAddScoped<DMB.Core.Evaluator.ExpressionEvaluator>();
            services.TryAddScoped<DMB.Core.Dmf.DmfServiceCore<
                DMB.Core.Elements.DatasetModelCore<DMB.Core.Elements.DatasetFieldModelCore>,
                DMB.Core.Elements.DatasetFieldModelCore, DataGridColumnModelCore>>();

            return services;
        }

        public static object? Evaluate(this string expression, ExpressionEvaluator evaluator,
            DatasetRowModelCore? CurrentRow = null)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return expression;
            if (expression.StartsWith('='))
            {
                var expr = expression.Substring(1);
                if (CurrentRow != null)
                {
                    return evaluator.EvaluateForRow(expression, CurrentRow.Values);
                }
                return evaluator.Evaluate(expr);
            }
            return expression;
        }
    }
}
