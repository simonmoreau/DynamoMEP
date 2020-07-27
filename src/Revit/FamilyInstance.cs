using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Revit.Elements;
using DB = Autodesk.Revit.DB;
using Autodesk.DesignScript.Geometry;
using RevitServices.Persistence;
using Revit.GeometryConversion;
using RevitServices.Transactions;
using Autodesk.DesignScript.Runtime;

namespace DynamoMEP
{
    /// <summary>
    /// Revit FamilyInstance
    /// </summary>
    public static class FamilyInstance
    {
        /// <summary>
        /// Return all planes aligned with the selected references
        /// </summary>
        /// <param name="familyInstance">The family instance</param>
        /// <param name="familyInstanceReferenceType"></param>
        public static List<Plane> GetReferencesPlanesByType(Revit.Elements.FamilyInstance familyInstance, string familyInstanceReferenceType)
        {
            List<Plane> planes = new List<Plane>();
            DB.Document activeDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            DB.FamilyInstance revitFamilyInstance = familyInstance.InternalElement as DB.FamilyInstance;

            if (familyInstanceReferenceType != null)
            {
                DB.FamilyInstanceReferenceType familyInstanceReferenceTypeEnum;

                if (Enum.TryParse<DB.FamilyInstanceReferenceType>(familyInstanceReferenceType, out familyInstanceReferenceTypeEnum))
                {
                    List<DB.Reference> references = revitFamilyInstance.GetReferences(familyInstanceReferenceTypeEnum).ToList();

                    // This creates a new wall and deletes the old one
                    TransactionManager.Instance.EnsureInTransaction(activeDocument);

                    using (DB.SubTransaction subTr = new DB.SubTransaction(activeDocument))
                    {
                        subTr.Start();

                        foreach (DB.Reference reference in references)
                        {
                            DB.SketchPlane sketchPlane = DB.SketchPlane.Create(activeDocument, reference);
                            planes.Add(sketchPlane.GetPlane().ToPlane());
                            sketchPlane.Dispose();
                        }

                        subTr.RollBack();
                    }

                    TransactionManager.Instance.TransactionTaskDone();

                }

            }

            return planes;
        }

        /// <summary>
        /// Return all references planes
        /// </summary>
        /// <param name="familyInstance">The family instance</param>
        public static List<Plane> GetReferencesPlanes(Revit.Elements.FamilyInstance familyInstance)
        {
            List<Plane> planes = new List<Plane>();
            List<string> names = new List<string>();
            DB.Document activeDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            DB.FamilyInstance revitFamilyInstance = familyInstance.InternalElement as DB.FamilyInstance;

            DB.FamilyInstanceReferenceType[] familyInstanceReferenceTypes = Enum.GetValues(typeof(DB.FamilyInstanceReferenceType)).Cast<DB.FamilyInstanceReferenceType>().ToArray();

            // This creates a new wall and deletes the old one
            TransactionManager.Instance.EnsureInTransaction(activeDocument);

            using (DB.SubTransaction subTr = new DB.SubTransaction(activeDocument))
            {
                subTr.Start();

                foreach (DB.FamilyInstanceReferenceType familyInstanceReferenceType in familyInstanceReferenceTypes)
                {
                    List<DB.Reference> references = revitFamilyInstance.GetReferences(familyInstanceReferenceType).ToList();

                    foreach (DB.Reference reference in references)
                    {
                        DB.SketchPlane sketchPlane = DB.SketchPlane.Create(activeDocument, reference);
                        planes.Add(sketchPlane.GetPlane().ToPlane());
                        sketchPlane.Dispose();
                    }
                }

                subTr.RollBack();
            }

            TransactionManager.Instance.TransactionTaskDone();


            return planes;
        }


        /// <summary>
        /// Return all references names
        /// </summary>
        /// <param name="familyInstance">The family instance</param>
        public static List<string> GetReferencesNames(Revit.Elements.FamilyInstance familyInstance)
        {
            List<string> names = new List<string>();
            DB.Document activeDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            DB.FamilyInstance revitFamilyInstance = familyInstance.InternalElement as DB.FamilyInstance;

            DB.FamilyInstanceReferenceType[] familyInstanceReferenceTypes = Enum.GetValues(typeof(DB.FamilyInstanceReferenceType)).Cast<DB.FamilyInstanceReferenceType>().ToArray();

            foreach (DB.FamilyInstanceReferenceType familyInstanceReferenceType in familyInstanceReferenceTypes)
            {
                List<DB.Reference> references = revitFamilyInstance.GetReferences(familyInstanceReferenceType).ToList();

                foreach (DB.Reference reference in references)
                {
                    names.Add(revitFamilyInstance.GetReferenceName(reference));
                }
            }

            return names;
        }



        /// <summary>
        /// Return a plane aligned with the named reference
        /// </summary>
        /// <param name="familyInstance">The family instance</param>
        /// <param name="name">The name of the reference</param>
        public static Plane GetReferencePlaneByName(Revit.Elements.FamilyInstance familyInstance, string name)
        {
            DB.Document activeDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            DB.FamilyInstance revitFamilyInstance = familyInstance.InternalElement as DB.FamilyInstance;

            DB.Reference reference = revitFamilyInstance.GetReferenceByName(name);

            if (reference != null)
            {
                Plane plane = null;
                // This creates a new wall and deletes the old one
                TransactionManager.Instance.EnsureInTransaction(activeDocument);

                using (DB.SubTransaction subTr = new DB.SubTransaction(activeDocument))
                {
                    subTr.Start();

                    DB.SketchPlane sketchPlane = DB.SketchPlane.Create(activeDocument, reference);

                    plane = sketchPlane.GetPlane().ToPlane();
                    sketchPlane.Dispose();

                    subTr.RollBack();
                }

                TransactionManager.Instance.TransactionTaskDone();

                return plane;
            }
            else
            {
                return null;
            }
        }
    }
}
