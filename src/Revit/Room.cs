using System;
using DB = Autodesk.Revit.DB;
using DynamoServices;
using Autodesk.DesignScript.Runtime;
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Interfaces;
using Revit.GeometryConversion;
using RevitServices.Persistence;
using RevitServices.Transactions;
using System.Collections.Generic;
using System.Linq;

namespace Revit.Elements
{
    /// <summary>
    /// MEP Rooms
    /// </summary>
    [DynamoServices.RegisterForTrace]
    public class Room : Element, IGraphicItem
    {
        #region Internal Properties

        /// <summary>
        /// Internal reference to the Revit Element
        /// </summary>
        internal DB.Architecture.Room InternalRoom
        {
            get;
            private set;
        }

        /// <summary>
        /// Reference to the Element
        /// </summary>
        public override DB.Element InternalElement
        {
            get { return InternalRoom; }
        }

        internal List<DB.BoundarySegment> InternalBoundarySegments = new List<DB.BoundarySegment>();

        #endregion

        #region Private constructors

        /// <summary>
        /// Create from an existing Revit Element
        /// </summary>
        /// <param name="space">An existing Revit room</param>
        private Room(DB.Architecture.Room space)
        {
            SafeInit(() => InitSpace(space));
        }


        private Room(
            DB.Level level,
            DB.UV point)
        {
            SafeInit(() => InitSpace(level, point));
        }

        #endregion

        #region Helpers for private constructors

        /// <summary>
        /// Initialize a Room element
        /// </summary>
        /// <param name="room"></param>
        private void InitSpace(DB.Architecture.Room room)
        {
            InternalSetSpace(room);
        }


        private void InitSpace(DB.Level level, DB.UV point)
        {
            DB.Document document = DocumentManager.Instance.CurrentDBDocument;

            // This creates a new wall and deletes the old one
            TransactionManager.Instance.EnsureInTransaction(document);

            //Phase 1 - Check to see if the object exists and should be rebound
            var roomElem = ElementBinder.GetElementFromTrace<DB.Architecture.Room>(document);

            if (roomElem == null)
                roomElem = document.Create.NewRoom(level, point);

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
        private void InternalSetSpace(DB.Architecture.Room space)
        {
            InternalRoom = space;
            InternalElementId = space.Id;
            InternalUniqueId = space.UniqueId;
            GetBoundarySegment();
        }

        private void GetBoundarySegment()
        {
            List<DB.BoundarySegment> output = new List<DB.BoundarySegment>();
            DB.Document doc = DocumentManager.Instance.CurrentDBDocument;
            DB.SpatialElementBoundaryOptions opt = new DB.SpatialElementBoundaryOptions();

            foreach (List<DB.BoundarySegment> segments in InternalRoom.GetBoundarySegments(opt))
            {
                foreach (DB.BoundarySegment segment in segments)
                {
                    output.Add(segment);
                }
            }

            InternalBoundarySegments = output.Distinct().ToList();
        }

        #endregion

        #region Public static constructors

        /// <summary>
        /// Create a Room
        /// based on a location and a level
        /// </summary>
        /// <param name="point">Location point for the room</param>
        /// <param name="level">Level of the room</param>
        /// <returns></returns>
        public static Room ByPointAndLevel(Point point, Level level)
        {
            DB.Level revitLevel = level.InternalElement as DB.Level;

            DB.UV uv = new DB.UV(point.X, point.Y);

            return new Room(revitLevel, uv);
        }

        /// <summary>
        /// Create a Room
        /// from an existing Room
        /// </summary>
        /// <param name="element">The origin element</param>
        /// <returns></returns>
        public static Room FromElement(Element element)
        {
            if (element.InternalElement.GetType() == typeof(DB.Architecture.Room))
            {
                return new Room(element.InternalElement as DB.Architecture.Room);
            }
            else
            {
                throw new ArgumentException("The Element is not a Room");
            }
        }

        #endregion

        #region public properties

        /// <summary>
        /// Retrive a set of properties 
        /// for the Room
        /// </summary>
        /// <returns name="Name">The Room Name</returns>
        /// <returns name="Number">The Room Number</returns>
        [MultiReturn(new[] { "Name", "Number" })]
        public Dictionary<string, string> IdentificationData()
        {
            return new Dictionary<string, string>()
                {
                    {"Name",InternalRoom.Name},
                    {"Number",InternalRoom.Number}
                };
        }

        /// <summary>
        /// Retrive room boundary elements
        /// </summary>
        public List<Element> BoundaryElements
        {
            get
            {
                List<Element> output = new List<Element>();
                DB.Document doc = DocumentManager.Instance.CurrentDBDocument;

                foreach (DB.BoundarySegment segment in InternalBoundarySegments)
                {
                    DB.Element boundaryElement = doc.GetElement(segment.ElementId);
                    output.Add(ElementWrapper.ToDSType(boundaryElement, true));
                }

                output = output.Distinct().ToList();
                return output;
            }
        }

        /// <summary>
        /// Retrive Windows around the room
        /// </summary>
        public List<FamilyInstance> Windows
        {
            get
            {
                List<FamilyInstance> output = new List<FamilyInstance>();
                DB.Document doc = DocumentManager.Instance.CurrentDBDocument;

                // Find all Door instances in the document by using category filter
                DB.ElementCategoryFilter filter = new DB.ElementCategoryFilter(DB.BuiltInCategory.OST_Windows);

                // Apply the filter to the elements in the active document,
                // Use shortcut WhereElementIsNotElementType() to find doors instances
                DB.FilteredElementCollector collector = new DB.FilteredElementCollector(doc);
                IList<DB.FamilyInstance> windows = collector.WherePasses(filter).WhereElementIsNotElementType().ToElements().Cast<DB.FamilyInstance>().ToList();

                Dictionary<DB.ElementId, DB.FamilyInstance> boundaryWindows = windows.ToDictionary(x => x.Host.Id, x => x);

                foreach (DB.BoundarySegment segment in InternalBoundarySegments)
                {
                    if (boundaryWindows.ContainsKey(segment.ElementId))
                    {
                        DB.FamilyInstance boundaryWindow = boundaryWindows[segment.ElementId];
                        DB.Phase windowPhase = doc.GetElement(boundaryWindow.CreatedPhaseId) as DB.Phase;
                        if (boundaryWindow.get_FromRoom(windowPhase) != null)
                        {
                            if (boundaryWindow.get_FromRoom(windowPhase).Id == InternalRoom.Id)
                            {
                                output.Add(ElementWrapper.ToDSType(boundaryWindow, true) as FamilyInstance);
                                continue;
                            }
                        }

                        if (boundaryWindow.get_ToRoom(windowPhase) != null)
                        {
                            if (boundaryWindow.get_ToRoom(windowPhase).Id == InternalRoom.Id)
                            {
                                output.Add(ElementWrapper.ToDSType(boundaryWindow, true) as FamilyInstance);
                            }
                        }

                    }
                }

                output = output.Distinct().ToList();
                return output;
            }
        }

        /// <summary>
        /// Retrive Doors around the room
        /// </summary>
        public List<FamilyInstance> Doors
        {
            get
            {
                List<FamilyInstance> output = new List<FamilyInstance>();
                DB.Document doc = DocumentManager.Instance.CurrentDBDocument;

                // Find all Door instances in the document by using category filter
                DB.ElementCategoryFilter filter = new DB.ElementCategoryFilter(DB.BuiltInCategory.OST_Doors);

                // Apply the filter to the elements in the active document,
                // Use shortcut WhereElementIsNotElementType() to find doors instances
                DB.FilteredElementCollector collector = new DB.FilteredElementCollector(doc);
                IList<DB.FamilyInstance> doors = collector.WherePasses(filter).WhereElementIsNotElementType().ToElements().Cast<DB.FamilyInstance>().ToList();

                Dictionary<DB.ElementId, DB.FamilyInstance> boundaryDoors = doors.ToDictionary(x => x.Host.Id, x => x);

                foreach (DB.BoundarySegment segment in InternalBoundarySegments)
                {
                    if (boundaryDoors.ContainsKey(segment.ElementId))
                    {
                        DB.FamilyInstance boundaryDoor = boundaryDoors[segment.ElementId];
                        DB.Phase doorPhase = doc.GetElement(boundaryDoor.CreatedPhaseId) as DB.Phase;
                        if (boundaryDoor.get_FromRoom(doorPhase) != null)
                        {
                            if (boundaryDoor.get_FromRoom(doorPhase).Id == InternalRoom.Id)
                            {
                                output.Add(ElementWrapper.ToDSType(boundaryDoor, true) as FamilyInstance);
                                continue;
                            }
                        }

                        if (boundaryDoor.get_ToRoom(doorPhase) != null)
                        {
                            if (boundaryDoor.get_ToRoom(doorPhase).Id == InternalRoom.Id)
                            {
                                output.Add(ElementWrapper.ToDSType(boundaryDoor, true) as FamilyInstance);
                            }
                        }

                    }
                }

                output = output.Distinct().ToList();
                return output;
            }
        }

        private List<Curve> _boundaryCurves = new List<Curve>();

        /// <summary>
        /// Retrive space boundary curves
        /// </summary>
        public List<Curve> BoundaryCurves
        {
            get
            {
                return _boundaryCurves;
            }
        }



        #endregion

        #region Internal static constructors

        /// <summary>
        /// Create a space from an existing reference
        /// </summary>
        /// <param name="room"></param>
        /// <param name="isRevitOwned"></param>
        /// <returns></returns>
        internal static Room FromExisting(DB.Architecture.Room room, bool isRevitOwned)
        {
            return new Room(room)
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
            GeometryPrimitiveConverter.ToPoint(locPoint.Point).Tessellate(package, parameters);
            package.ApplyPointVertexColors(CreateColorByteArrayOfSize(package.PointVertexCount, 255, 0, 0, 0));

            //Boundaries
            foreach (DB.BoundarySegment segment in InternalBoundarySegments)
            {
                Curve crv = RevitToProtoCurve.ToProtoType(segment.GetCurve());
                _boundaryCurves.Add(crv);
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
            return string.Format("Room N°{1} - {0}", InternalRoom.Name, InternalRoom.Number);
        }

        #endregion

    }
}