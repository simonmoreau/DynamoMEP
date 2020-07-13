using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.DesignScript.Geometry;
using Autodesk.Revit.DB;

using Revit.GeometryConversion;

using RevitServices.Persistence;
using RevitServices.Transactions;
using Curve = Autodesk.Revit.DB.Curve;
using Plane = Autodesk.Revit.DB.Plane;

namespace DynamoMEP
{
    /// <summary>
    /// A Revit ModelCurve
    /// </summary>
    [DynamoServices.RegisterForTrace]
    public class RoomSeparator : Revit.Elements.CurveElement
    {
        #region Private constructors

        /// <summary>
        /// Construct a model curve from the document.  The result is Dynamo owned
        /// </summary>
        /// <param name="curve"></param>
        private RoomSeparator(Autodesk.Revit.DB.ModelCurve curve)
        {
            SafeInit(() => InitModelCurve(curve));
        }

        // PB: This implementation borrows the somewhat risky notions from the original Dynamo
        // implementation.  In short, it has the ability to infer a sketch plane,
        // which might also mean deleting the original one.

        /// <summary>
        /// Internal constructor for ModelCurve
        /// </summary>
        /// <param name="crv"></param>
        /// <param name="makeReferenceCurve"></param>
        /// <param name="view"></param>
        private RoomSeparator(Autodesk.Revit.DB.Curve crv, bool makeReferenceCurve, View view)
        {
            SafeInit(() => InitModelCurve(crv, makeReferenceCurve, view));
        }

        #endregion

        #region Helpers for private constructors

        /// <summary>
        /// Initialize a ModelCurve element
        /// </summary>
        /// <param name="curve"></param>
        private void InitModelCurve(Autodesk.Revit.DB.ModelCurve curve)
        {
            InternalSetCurveElement(curve);
        }

        /// <summary>
        /// A reference to the current Document.
        /// </summary>
        internal static Document Document
        {
            get { return DocumentManager.Instance.CurrentDBDocument; }
        }

        // PB: This implementation borrows the somewhat risky notions from the original Dynamo
        // implementation.  In short, it has the ability to infer a sketch plane,
        // which might also mean deleting the original one.

        /// <summary>
        /// Initialize a ModelCurve element
        /// </summary>
        /// <param name="crv"></param>
        /// <param name="makeReferenceCurve"></param>
        /// <param name="view"></param>
        private void InitModelCurve(Autodesk.Revit.DB.Curve crv, bool makeReferenceCurve, View view)
        {
            if (Document.IsFamilyDocument)
            {
                throw new ArgumentException("You cannot create Room Separator in a familly document");
            }
            //Phase 1 - Check to see if the object exists and should be rebound
            var mc =
                ElementBinder.GetElementFromTrace<Autodesk.Revit.DB.ModelCurve>(Document);

            //There was a modelcurve, try and set sketch plane
            // if you can't, rebuild 
            if (mc != null)
            {
                InternalSetCurveElement(mc);
                InternalSetSketchPlaneFromCurve(crv);
                return;
            }


            ElementId oldId = (mc != null) ? mc.Id : ElementId.InvalidElementId;
            string oldUniqueId = (mc != null) ? mc.UniqueId : string.Empty;

            TransactionManager.Instance.EnsureInTransaction(Document);

            // (sic erat scriptum)
            SketchPlane sp = GetSketchPlaneFromCurve(crv);
            Plane plane = sp.GetPlane();


            if (CurveUtils.GetPlaneFromCurve(crv, true) == null)
            {
                
                Curve flattenCurve = Flatten3dCurveOnPlane(crv, plane);
                CurveArray curveArray = new CurveArray();
                curveArray.Append(flattenCurve);

                ModelCurveArray mca = Document.Create.NewRoomBoundaryLines(sp, curveArray, view);

                mc = mca.get_Item(0);

                setCurveMethod(mc, crv);
            }
            else
            {
                CurveArray curveArray = new CurveArray();
                curveArray.Append(crv);

                ModelCurveArray mca = Document.Create.NewRoomBoundaryLines(sp, curveArray, view);

                mc = mca.get_Item(0);
            }

            if (mc.SketchPlane.Id != sp.Id)
            {
                //THIS BIZARRE as Revit could use different existing SP, so if Revit had found better plane  this sketch plane has no use
                DocumentManager.Instance.DeleteElement(new ElementUUID(sp.UniqueId));
            }

            InternalSetCurveElement(mc);
            if (oldId != mc.Id && oldId != ElementId.InvalidElementId)
                DocumentManager.Instance.DeleteElement(new ElementUUID(oldUniqueId));
            if (makeReferenceCurve)
                mc.ChangeToReferenceLine();

            TransactionManager.Instance.TransactionTaskDone();

            ElementBinder.SetElementForTrace(this.InternalElement);

        }

        #endregion

        private readonly double tolerance = 0.01;

        /// <summary>
        /// Set the plane and the curve internally.
        /// </summary>
        private void InternalSetSketchPlaneFromCurve(Curve newCurve)
        {
            TransactionManager.Instance.EnsureInTransaction(Document);

            Plane newPlane = CurveUtils.GetPlaneFromCurve(newCurve, false);
            Plane oldPlane = (InternalElement as CurveElement).SketchPlane.GetPlane();

            var angleBetweenPlanes = newPlane.Normal.AngleTo(oldPlane.Normal);
            var distanceBetweenOrigins = newPlane.Origin.DistanceTo(oldPlane.Origin);

            Autodesk.Revit.DB.SketchPlane sp = null;

            // Planes are different.
            if (angleBetweenPlanes > tolerance || distanceBetweenOrigins > tolerance)
            {
                sp = GetSketchPlaneFromCurve(newCurve);
                (InternalElement as CurveElement).SetSketchPlaneAndCurve(sp, newCurve);
            }
            // Planes are the same.
            else
            {
                InternalSetCurve(newCurve);
            }

            string idSpUnused = String.Empty;
            if (sp != null && (InternalElement as CurveElement).SketchPlane.Id != sp.Id)
                idSpUnused = sp.UniqueId;

            // if we got a valid id, delete the old sketch plane
            if (idSpUnused != String.Empty)
            {
                DocumentManager.Instance.DeleteElement(new ElementUUID(idSpUnused));
            }

            TransactionManager.Instance.TransactionTaskDone();
        }

        #region Public constructor

        /// <summary>
        /// Construct a Revit Room Separator element from a Curve. The curve will be project onto the Revit active view
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public static RoomSeparator ByCurve(Autodesk.DesignScript.Geometry.Curve curve)
        {
            if (curve == null)
            {
                throw new ArgumentNullException("curve");
            }

            View currentView = Document.ActiveView;

            if (currentView.ViewType != ViewType.FloorPlan && currentView.ViewType != ViewType.EngineeringPlan)
            {
                throw new ArgumentException("Please open a floor plan view to create room separation lines");
            }

            Plane viewPlane = currentView.SketchPlane.GetPlane();
            Autodesk.DesignScript.Geometry.Curve projectedCurve = curve.PullOntoPlane(viewPlane.ToPlane());

            return new RoomSeparator(ExtractLegalRevitCurve(projectedCurve), false, currentView);
        }

        /// <summary>
        /// Construct a Revit Room Separator element from a Curve and a base view
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="view"></param>
        /// <returns></returns>
        public static RoomSeparator ByCurveAndView(Autodesk.DesignScript.Geometry.Curve curve, Revit.Elements.Views.View view)
        {
            if (curve == null)
            {
                throw new ArgumentNullException("curve");
            }

            if (view == null)
            {
                throw new ArgumentNullException("view");
            }

            View currentView = view.InternalElement as Autodesk.Revit.DB.View;

            if (currentView.ViewType != ViewType.FloorPlan && currentView.ViewType != ViewType.EngineeringPlan)
            {
                throw new ArgumentException("Please use a floor plan view to create room separation lines");
            }

            Plane viewPlane = currentView.SketchPlane.GetPlane();
            Autodesk.DesignScript.Geometry.Curve projectedCurve = curve.PullOntoPlane(viewPlane.ToPlane());

            return new RoomSeparator(ExtractLegalRevitCurve(projectedCurve), false, currentView);
        }

        #endregion

        #region Private static constructors
        /// <summary>
        /// Construct a Revit ModelCurve element from an existing element.  The result is Dynamo owned.
        /// </summary>
        /// <param name="modelCurve"></param>
        /// <param name="isRevitOwned"></param>
        /// <returns></returns>
        internal static RoomSeparator FromExisting(Autodesk.Revit.DB.ModelCurve modelCurve, bool isRevitOwned)
        {
            return new RoomSeparator(modelCurve);
        }

        #endregion

        #region Helper methods

        private static Autodesk.Revit.DB.Curve ExtractLegalRevitCurve(Autodesk.DesignScript.Geometry.Curve curve)
        {
            // PB:  PolyCurves may have discontinuities that prevent them from being turned into legal Revit ModelCurves.
            // Moreover, a single ModelCurve may not be a composite or multiple curves.

            // One could add a static method to ModelCurve that is an overload of ByCurve - ModelCurve[] ByCurve(PolyCurve).
            // But, this adds an unnecessary high level of complexity to the Element rebinding code.  Hence, we will go the
            // more straightforward route of just providing an informative error message to the user.
            if (curve is PolyCurve)
            {
                throw new Exception("Please explode Polycurve in Curves before using them");
            }

            return curve.ToRevitType();
        }

        private static Autodesk.Revit.DB.SketchPlane GetSketchPlaneFromCurve(Curve c)
        {
            Plane plane = CurveUtils.GetPlaneFromCurve(c, false);
            return Autodesk.Revit.DB.SketchPlane.Create(Document, plane);
        }

        private static Curve Flatten3dCurveOnPlane(Curve c, Plane plane)
        {
            if (c is Autodesk.Revit.DB.HermiteSpline)
            {
                var hs = c as Autodesk.Revit.DB.HermiteSpline;
                plane = CurveUtils.GetPlaneFromCurve(c, false);
                var projPoints = new List<XYZ>();
                foreach (var pt in hs.ControlPoints)
                {
                    var proj = pt - (pt - plane.Origin).DotProduct(plane.Normal) * plane.Normal;
                    projPoints.Add(proj);
                }

                return Autodesk.Revit.DB.HermiteSpline.Create(projPoints, false);
            }

            if (c is Autodesk.Revit.DB.NurbSpline)
            {
                var ns = c as Autodesk.Revit.DB.NurbSpline;
                if (plane == null)
                {
                    var bestFitPlane = Autodesk.DesignScript.Geometry.Plane.ByBestFitThroughPoints(
                        ns.CtrlPoints.ToList().ToPoints(false));

                    plane = bestFitPlane.ToPlane(false);
                }

                var projPoints = new List<XYZ>();
                foreach (var pt in ns.CtrlPoints)
                {
                    var proj = pt - (pt - plane.Origin).DotProduct(plane.Normal) * plane.Normal;
                    projPoints.Add(proj);
                }

                return Autodesk.Revit.DB.NurbSpline.CreateCurve(ns.Degree, ns.Knots.Cast<double>().ToList(), projPoints, ns.Weights.Cast<double>().ToList());
            }

            return c;
        }

        #endregion

        public override string ToString()
        {
            return "Room Separator";
        }
    }
}