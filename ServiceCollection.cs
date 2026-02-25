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
        private static IServiceCollection? _services;

        public static IServiceCollection AddDmrServices(this IServiceCollection services)
        {
            _services = services;
            services.TryAddSingleton<DMB.Core.ModuleStateCore>();
            services.TryAddSingleton<DMB.Core.Dmf.DmfServiceCore<DMB.Core.Elements.DatasetModelCore<DMB.Core.Elements.DatasetFieldModelCore>, DMB.Core.Elements.DatasetFieldModelCore>>();
            services.TryAddSingleton<DMB.Core.Evaluator.ExpressionEvaluator>();

            return services;
        }

        public static object? Evaluate(this string expression, ExpressionEvaluator evaluator)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return expression;
            if (expression.StartsWith("="))
            {
                var expr = expression.Substring(1);
                return evaluator.Evaluate(expr);
            }
            return expression;
        }
    }
}
