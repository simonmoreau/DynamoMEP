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
    public class Space : Element
    {
        #region Internal Properties

        /// <summary>
        /// Internal reference to the Revit Element
        /// </summary>
        internal DB.Mechanical.Space InternalSpace
        {

            get;
            private set;
        }

        /// <summary>
        /// Reference to the Element
        /// </summary>
        public override DB.Element InternalElement
        {
            get { return InternalSpace; }
        }

        internal List<DB.BoundarySegment> InternalBoundarySegments = new List<DB.BoundarySegment>();

        #endregion

        #region Private constructors

        /// <summary>
        /// Create from an existing Revit Element
        /// </summary>
        /// <param name="space">An existing Revit space</param>
        private Space(DB.Mechanical.Space space)
        {
            SafeInit(() => InitSpace(space));
        }


        private Space(
            DB.Level level,
            DB.UV point)
        {
            SafeInit(() => InitSpace(level, point));
        }

        #endregion

        #region Helpers for private constructors

        /// <summary>
        /// Initialize a Space element
        /// </summary>
        /// <param name="room"></param>
        private void InitSpace(DB.Mechanical.Space room)
        {
            InternalSetSpace(room);
        }

        /// <summary>
        /// Transform of the Element
        /// </summary>
        internal DB.Transform InternalTransform
        {
            get;
            private set;
        }


        private void InitSpace(
            DB.Level level,
            DB.UV point)
        {
            DB.Document document = DocumentManager.Instance.CurrentDBDocument;

            // This creates a new wall and deletes the old one
            TransactionManager.Instance.EnsureInTransaction(document);

            //Phase 1 - Check to see if the object exists and should be rebound
            var roomElem = ElementBinder.GetElementFromTrace<DB.Mechanical.Space>(document);

            if (roomElem == null)
            {
                roomElem = document.Create.NewSpace(level, point);
            }

            InternalSetSpace(roomElem);

            TransactionManager.Instance.TransactionTaskDone();

            if (roomElem != null)
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
        /// <param name="space"></param>
        private void InternalSetSpace(DB.Mechanical.Space space)
        {
            InternalSpace = space;
            InternalElementId = space.Id;
            InternalUniqueId = space.UniqueId;
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
                //Find the revit instance where we find the room
                DB.FilteredElementCollector collector = new DB.FilteredElementCollector(DocumentManager.Instance.CurrentDBDocument);
                List<DB.RevitLinkInstance> linkInstances = collector.OfCategory(DB.BuiltInCategory.OST_RvtLinks).WhereElementIsNotElementType().ToElements().Cast<DB.RevitLinkInstance>().ToList();
                DB.RevitLinkInstance roomLinkInstance = linkInstances.FirstOrDefault();

                foreach (DB.RevitLinkInstance linkInstance in linkInstances)
                {
                    if (linkInstance.GetLinkDocument().GetHashCode() == InternalElement.Document.GetHashCode())
                    {
                        roomLinkInstance = linkInstance;
                        break;
                    }
                }

                return roomLinkInstance.GetTotalTransform();
            }
        }

        private List<DB.BoundarySegment> GetBoundarySegment()
        {
            List<DB.BoundarySegment> output = new List<DB.BoundarySegment>();
            DB.SpatialElementBoundaryOptions opt = new DB.SpatialElementBoundaryOptions();

            foreach (List<DB.BoundarySegment> segments in InternalSpace.GetBoundarySegments(opt))
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
        /// Create a MEP Space
        /// based on a location and a level
        /// </summary>
        /// <param name="point">Location point for the space</param>
        /// <param name="level">Level of the space</param>
        /// <returns></returns>
        public static Space ByPointAndLevel(Point point, Level level)
        {
            DB.Level revitLevel = level.InternalElement as DB.Level;
            DB.XYZ revitPoint = GeometryPrimitiveConverter.ToXyz(point);

            DB.UV uv = new DB.UV(revitPoint.X, revitPoint.Y);

            return new Space(revitLevel, uv);
        }

        /// <summary>
        /// Create a `MEP Space
        /// based on a location
        /// </summary>
        /// <param name="point">Location point for the space</param>
        /// <returns></returns>
        public static Space ByPoint(Point point)
        {
            DB.XYZ revitPoint = GeometryPrimitiveConverter.ToXyz(point);
            DB.Level revitLevel = GetNearestLevel(revitPoint);

            DB.UV uv = new DB.UV(revitPoint.X, revitPoint.Y);

            return new Space(revitLevel, uv);
        }

        /// <summary>
        /// Find the nearest level in the active document
        /// </summary>
        /// <param name="point">The reference point</param>
        /// <returns></returns>
        private static DB.Level GetNearestLevel(DB.XYZ point)
        {
            //find all level in the active document
            DB.Document doc = DocumentManager.Instance.CurrentDBDocument;

            DB.FilteredElementCollector collector = new DB.FilteredElementCollector(doc);
            List<DB.Level> activeLevels = collector.OfCategory(DB.BuiltInCategory.OST_Levels).WhereElementIsNotElementType().ToElements().Cast<DB.Level>().ToList();

            DB.Level nearestLevel = activeLevels.FirstOrDefault();
            double delta = Math.Abs(nearestLevel.ProjectElevation - point.Z);

            foreach (DB.Level currentLevel in activeLevels)
            {
                if (Math.Abs(currentLevel.ProjectElevation - point.Z) < delta)
                {
                    nearestLevel = currentLevel;
                    delta = Math.Abs(currentLevel.ProjectElevation - point.Z);
                }
            }

            return nearestLevel;
        }

        /// <summary>
        /// Create a MEP Space
        /// from an existing MEP Space
        /// </summary>
        /// <param name="element">The origin element</param>
        /// <returns></returns>
        public static Space FromElement(Element element)
        {
            if (element.InternalElement.GetType() == typeof(DB.Mechanical.Space))
            {
                return new Space(element.InternalElement as DB.Mechanical.Space);
            }
            else
            {
                throw new ArgumentException("The Element is not a MEP Space");
            }
        }

        #endregion

        #region public properties

        /// <summary>
        /// Retrive a set of properties 
        /// for the Space
        /// </summary>
        /// <returns name="Name">The MEPSpace Name</returns>
        /// <returns name="Number">The MEPSpace Number</returns>
        /// <returns name="CustomRoom Name">The associated room Name</returns>
        /// <returns name="CustomRoom Number">The associated room Number</returns>
        [MultiReturn(new[] { "Name", "Number", "CustomRoom Number", "CustomRoom Name" })]
        public Dictionary<string, string> GetIdentificationData()
        {
            string roomName = "Unoccupied";
            string roomNumber = "Unoccupied";
            if (InternalSpace.Room != null)
            {
                roomName = InternalSpace.Room.Name;
                roomNumber = InternalSpace.Room.Number;
            }
            return new Dictionary<string, string>()
                {
                    {"Name",InternalSpace.Name},
                    {"Number",InternalSpace.Number},
                    {"CustomRoom Name",roomName},
                    {"CustomRoom Number",roomNumber}
                };
        }

        /// <summary>
        /// Retrive space boundary elements
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
        /// Retrive the sapce location
        /// </summary>
        public Point LocationPoint
        {
            get
            {
                DB.LocationPoint locPoint = InternalElement.Location as DB.LocationPoint;
                return GeometryPrimitiveConverter.ToPoint(InternalTransform.OfPoint(locPoint.Point));
            }
        }

        /// <summary>
        /// Determine if an element lies
        /// within the volume of the Space
        /// </summary>
        public bool IsInSpace(Element element)
        {
            DB.FamilyInstance familyInstance = element.InternalElement as DB.FamilyInstance;
            if (familyInstance != null)
            {
                if (familyInstance.HasSpatialElementCalculationPoint)
                {
                    DB.XYZ insertionPoint = familyInstance.GetSpatialElementCalculationPoint();

                    if (InternalSpace.IsPointInSpace(insertionPoint))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            DB.LocationPoint insertionLocationPoint = element.InternalElement.Location as DB.LocationPoint;
            if (insertionLocationPoint != null)
            {
                DB.XYZ insertionPoint = insertionLocationPoint.Point;

                if (InternalSpace.IsPointInSpace(insertionPoint))
                {
                    return true;
                }
            }

            return false;
            
        }


        /// <summary>
        /// Return a grid of points in the space
        /// </summary>
        /// <param name="step">Lenght between two points</param>
        public List<Point> Grid(double step)
        {
            step = UnitConverter.DynamoToHostFactor(DB.UnitType.UT_Length) * step;
            List<Point> grid = new List<Point>();

            DB.BoundingBoxXYZ bb = InternalElement.get_BoundingBox(null);

            for (double x = bb.Min.X; x < bb.Max.X;)
            {
                for (double y = bb.Min.Y; y < bb.Max.Y;)
                {
                    DB.XYZ point = new DB.XYZ(x, y, bb.Min.Z);
                    if (InternalSpace.IsPointInSpace(point))
                    {
                        grid.Add(GeometryPrimitiveConverter.ToPoint(InternalTransform.OfPoint(point)));
                    }
                    y = y + step;
                }

                x = x + step;
            }

            return grid;
        }


        #endregion

        #region Internal static constructors

        /// <summary>
        /// Create a space from an existing reference
        /// </summary>
        /// <param name="space"></param>
        /// <param name="isRevitOwned"></param>
        /// <returns></returns>
        internal static Space FromExisting(DB.Mechanical.Space space, bool isRevitOwned)
        {
            return new Space(space)
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
            return string.Format("Space {1} - {0}", InternalSpace.Name, InternalSpace.Number);
        }

        #endregion

    }
}
