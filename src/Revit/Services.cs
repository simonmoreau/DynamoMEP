using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Revit.GeometryConversion;
using RevitServices.Persistence;

namespace DynamoMEP
{
    public static class Services
    {
        public static double ConvertStepDeprecated(double step)
        {
            step = UnitConverter.DynamoToHostFactor(UnitType.UT_Length) * step;

            return step;
        }

        public static double ConvertStep( double step)
        {
            var unitTypeId =
    DocumentManager.Instance.CurrentDBDocument.GetUnits()
        .GetFormatOptions(SpecTypeId.Length)
        .GetUnitTypeId();

            step = UnitUtils.ConvertToInternalUnits(1, unitTypeId) * step;

            return step;
        }

    }
}
