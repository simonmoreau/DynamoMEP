using System;
using DB = Autodesk.Revit.DB;
using DynamoServices;
using Revit.Elements;
using Autodesk.DesignScript.Runtime;
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Interfaces;
using Revit.GeometryConversion;
using RevitServices.Persistence;
using RevitServices.Transactions;
using System.Collections.Generic;
using System.Linq;

namespace DynamoMEP
{
    /// <summary>
    /// MEP Spaces
    /// </summary>
    [DynamoServices.RegisterForTrace]
    public class Area : Element
    {
        #region Internal Properties

        /// <summary>
        /// Internal reference to the Revit Element
        /// </summary>
        internal DB.Area InternalArea
        {

            get;
            private set;
        }

        /// <summary>
        /// Reference to the Element
        /// </summary>
        public override DB.Element InternalElement
        {
            get { return InternalArea; }
        }

        internal List<DB.BoundarySegment> InternalBoundarySegments = new List<DB.BoundarySegment>();

        #endregion

        #region Private constructors

        /// <summary>
        /// Create from an existing Revit Element
        /// </summary>
        /// <param name="area">An existing Revit space</param>
        private Area(DB.Area area)
        {
            SafeInit(() => InitSpace(area));
        }


        private Area(
            DB.ViewPlan areaView,
            DB.UV point)
        {
            SafeInit(() => InitArea(areaView, point));
        }

        #endregion

        #region Helpers for private constructors

        /// <summary>
        /// Initialize a Space element
        /// </summary>
        /// <param name="area"></param>
        private void InitSpace(DB.Area area)
        {
            InternalSetSpace(area);
        }

        /// <summary>
        /// Transform of the Element
        /// </summary>
        internal DB.Transform InternalTransform
        {
            get;
            private set;
        }


        private void InitArea(
            DB.ViewPlan areaView,
            DB.UV point)
        {
            DB.Document document = DocumentManager.Instance.CurrentDBDocument;

            // This creates a new wall and deletes the old one
            TransactionManager.Instance.EnsureInTransaction(document);

            //Phase 1 - Check to see if the object exists and should be rebound
            var areaElement = ElementBinder.GetElementFromTrace<DB.Area>(document);

            if (areaElement == null)
            {
                areaElement = document.Create.NewArea(areaView, point);
            }

            InternalSetSpace(areaElement);

            TransactionManager.Instance.TransactionTaskDone();

            if (areaElement != null)
            {
                ElementBinder.CleanupAndSetElementForTrace(document, this.InternalElement);
            }
            else
            {
                ElementBinder.SetElementForTrace(this.InternalElement);
            }

        }

        #endregion

        #region Private mutators

        /// <summary>
        /// Set the internal Element, ElementId, and UniqueId
        /// </summary>
        /// <param name="area"></param>
        private void InternalSetSpace(DB.Area area)
        {
            InternalArea = area;
            InternalElementId = area.Id;
            InternalUniqueId = area.UniqueId;
            InternalBoundarySegments = GetBoundarySegment();
            InternalTransform = GetTransform();
        }

        private DB.Transform GetTransform()
        {
            if (InternalElement.Document.GetHashCode() == DocumentManager.Instance.CurrentDBDocument.GetHashCode())
            {
                return DB.Transform.Identity;
            }
            else
            {
                //Find the revit instance where we find the area
                DB.FilteredElementCollector collector = new DB.FilteredElementCollector(DocumentManager.Instance.CurrentDBDocument);
                List<DB.RevitLinkInstance> linkInstances = collector.OfCategory(DB.BuiltInCategory.OST_RvtLinks).WhereElementIsNotElementType().ToElements().Cast<DB.RevitLinkInstance>().ToList();
                DB.RevitLinkInstance areaLinkInstance = linkInstances.FirstOrDefault();

                foreach (DB.RevitLinkInstance linkInstance in linkInstances)
                {
                    if (linkInstance.GetLinkDocument().GetHashCode() == InternalElement.Document.GetHashCode())
                    {
                        areaLinkInstance = linkInstance;
                        break;
                    }
                }

                return areaLinkInstance.GetTotalTransform();
            }
        }

        private List<DB.BoundarySegment> GetBoundarySegment()
        {
            List<DB.BoundarySegment> output = new List<DB.BoundarySegment>();
            DB.SpatialElementBoundaryOptions boundaryOptions = new DB.SpatialElementBoundaryOptions
            {
                StoreFreeBoundaryFaces = true,
                SpatialElementBoundaryLocation = DB.SpatialElementBoundaryLocation.Center
            };

            foreach (List<DB.BoundarySegment> segments in InternalArea.GetBoundarySegments(boundaryOptions))
            {
                foreach (DB.BoundarySegment segment in segments)
                {
                    output.Add(segment);
                }
            }

            return output.Distinct().ToList();
        }

        #endregion

        #region Public static constructors

        /// <summary>
        /// Create a Revit Area
        /// based on a location and a view plan
        /// </summary>
        /// <param name="point">Location point for the area</param>
        /// <param name="areaView">The View plan of the area</param>
        /// <returns></returns>
        public static Area ByPointAndView(Point point, Revit.Elements.Views.AreaPlanView areaView)
        {
            DB.ViewPlan revitViewPlan = areaView.InternalElement as DB.ViewPlan;
            DB.XYZ revitPoint = GeometryPrimitiveConverter.ToXyz(point);

            DB.UV uv = new DB.UV(revitPoint.X, revitPoint.Y);

            return new Area(revitViewPlan, uv);
        }

        /// <summary>
        /// Create a Revit Area
        /// from an existing Revit Area
        /// </summary>
        /// <param name="element">The origin element</param>
        /// <returns></returns>
        public static Area FromElement(Element element)
        {
            if (element.InternalElement.GetType() == typeof(DB.Area))
            {
                return new Area(element.InternalElement as DB.Area);
            }
            else
            {
                throw new ArgumentException("The Element is not a Revit Area");
            }
        }

        #endregion

        #region public properties

        /// <summary>
        /// Retrive a set of properties 
        /// for the Area
        /// </summary>
        /// <returns name="Name">The Area Name</returns>
        /// <returns name="Number">The Area Number</returns>
        [MultiReturn(new[] { "Name", "Number" })]
        public Dictionary<string, string> GetIdentificationData()
        {
            return new Dictionary<string, string>()
                {
                    {"Name",InternalArea.Name},
                    {"Number",InternalArea.Number}
                };
        }

        /// <summary>
        /// Retrive area boundary elements
        /// </summary>
        public List<Element> BoundaryElements
        {
            get
            {
                List<Element> output = new List<Element>();
                DB.Document doc = InternalElement.Document;

                foreach (DB.BoundarySegment segment in InternalBoundarySegments)
                {
                    DB.Element boundaryElement = doc.GetElement(segment.ElementId);
                    if (boundaryElement.GetType() == typeof(DB.RevitLinkInstance))
                    {
                        DB.RevitLinkInstance linkInstance = boundaryElement as DB.RevitLinkInstance;
                        DB.Element linkBoundaryElement = linkInstance.GetLinkDocument().GetElement(segment.LinkElementId);
                        output.Add(ElementWrapper.ToDSType(linkBoundaryElement, true));
                    }
                    else
                    {
                        output.Add(ElementWrapper.ToDSType(boundaryElement, true));
                    }
                }

                output = output.Distinct().ToList();
                return output;
            }
        }

        /// <summary>
        /// Retrive the space associated level
        /// </summary>
        public Level Level
        {
            get
            {
                DB.Document doc = InternalElement.Document;
                DB.Element roomLevel = doc.GetElement(InternalElement.LevelId);

                return ElementWrapper.ToDSType(roomLevel, true) as Level;
            }
        }

        /// <summary>
        /// Retrive the area location
        /// </summary>
        public Point LocationPoint
        {
            get
            {
                DB.LocationPoint locPoint = InternalElement.Location as DB.LocationPoint;
                return GeometryPrimitiveConverter.ToPoint(InternalTransform.OfPoint(locPoint.Point));
            }
        }


        #endregion

        #region Internal static constructors

        /// <summary>
        /// Create a REvit area from an existing reference
        /// </summary>
        /// <param name="area"></param>
        /// <param name="isRevitOwned"></param>
        /// <returns></returns>
        internal static Area FromExisting(DB.Area area, bool isRevitOwned)
        {
            return new Area(area)
            {
                //IsRevitOwned = isRevitOwned
            };
        }

        #endregion

        #region Display Functions

        /// <summary>
        /// Display Spaces in the Dynamo interface
        /// </summary>
        /// <param name="package"></param>
        /// <param name="parameters"></param>
        [IsVisibleInDynamoLibrary(false)]
        public new void Tessellate(IRenderPackage package, TessellationParameters parameters)
        {
            //Ensure that the object is still alive
            if (!IsAlive) return;

            //Location Point
            DB.LocationPoint locPoint = InternalElement.Location as DB.LocationPoint;
            GeometryPrimitiveConverter.ToPoint(InternalTransform.OfPoint(locPoint.Point)).Tessellate(package, parameters);
            package.ApplyPointVertexColors(CreateColorByteArrayOfSize(package.LineVertexCount, 255, 0, 0, 0));

            //Boundaries
            foreach (DB.BoundarySegment segment in InternalBoundarySegments)
            {
                Curve crv = RevitToProtoCurve.ToProtoType(segment.GetCurve().CreateTransformed(InternalTransform));

                crv.Tessellate(package, parameters);

                if (package.LineVertexCount > 0)
                {
                    package.ApplyLineVertexColors(CreateColorByteArrayOfSize(package.LineVertexCount, 255, 0, 0, 0));
                }
            }
        }

        private static byte[] CreateColorByteArrayOfSize(int size, byte red, byte green, byte blue, byte alpha)
        {
            var arr = new byte[size * 4];
            for (var i = 0; i < arr.Length; i += 4)
            {
                arr[i] = red;
                arr[i + 1] = green;
                arr[i + 2] = blue;
                arr[i + 3] = alpha;
            }
            return arr;
        }

        /// <summary>
        /// OPTIONAL:
        /// Overriding ToString allows you to control what is
        /// displayed whenever the object's string representation
        /// is used. For example, ToString is called when the 
        /// object is displayed in a Watch node.
        /// </summary>
        /// <returns>The string representation of our object.</returns>
        public override string ToString()
        {
            return string.Format("Area {1} - {0}", InternalArea.Name, InternalArea.Number);
        }

        #endregion

    }
}
