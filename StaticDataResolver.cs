using DMB.Core;
using DMB.Core.Elements;
using DMB.Core.Evaluator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMR_Razor
{
	public static class StaticDataResolver
	{
		public static DatasetModelCore<DatasetFieldModelCore> CalculateData(string datasetId, ModuleStateCore state
			, ExpressionEvaluator evaluator)
		{
			var dataset = state.AllItems.OfType<DatasetModelCore<DatasetFieldModelCore>>()
		   .Cast<DatasetModelCore<DatasetFieldModelCore>>()
		   .FirstOrDefault(p => p.Id == datasetId);

			if (dataset == null)
				throw new Exception("Invalid dataset");

			CalculateData(dataset, evaluator);

			return dataset;
		}

		public static void CalculateData(DatasetModelCore<DatasetFieldModelCore> dataset
			, ExpressionEvaluator evaluator)
		{
			// Calculate calculated fieldsq
			var calculatedFields = dataset.Fields.OfType<DatasetFieldModelCore>()
			.Cast<DatasetFieldModelCore>()
			.Where(f => !string.IsNullOrWhiteSpace(f.Source) && f.Source.StartsWith("=")).ToList();

			foreach (var row in dataset.Rows)
			{
				foreach (var col in calculatedFields)
				{
					row.Set(col.Name, evaluator.EvaluateForRow(col.Source, row.Values));
				}
			}
		}
	}
}
