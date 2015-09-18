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
    /// MEP Spaces
    /// </summary>
    [DynamoServices.RegisterForTrace]
    public class Space : Element, IGraphicItem
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
                roomElem = document.Create.NewSpace(level, point);

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

            DB.UV uv = new DB.UV(point.X, point.Y);

            return new Space(revitLevel, uv);
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
        /// <returns name="Room Name">The associated room Name</returns>
        /// <returns name="Room Number">The associated room Number</returns>
        [MultiReturn(new[] { "Name", "Number", "Room Number", "Room Name" })]
        public Dictionary<string, string> IdentificationData()
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
                    {"Room Name",roomName},
                    {"Room Number",roomNumber}
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
                DB.Document doc = DocumentManager.Instance.CurrentDBDocument;
                DB.SpatialElementBoundaryOptions opt = new DB.SpatialElementBoundaryOptions();
                
                foreach (List<DB.BoundarySegment> segments in InternalSpace.GetBoundarySegments(opt))
                {
                    foreach (DB.BoundarySegment segment in segments)
                    {
                        DB.Element boundaryElement = doc.GetElement(segment.ElementId);

                        output.Add(ElementWrapper.ToDSType(boundaryElement, true));
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
            package.AddPointVertex(locPoint.Point.X, locPoint.Point.Y, locPoint.Point.Z);
            package.AddPointVertexColor(255, 0, 0, 255);

            //Boundaries
            DB.SpatialElementBoundaryOptions options = new DB.SpatialElementBoundaryOptions();
            options.SpatialElementBoundaryLocation = DB.SpatialElementBoundaryLocation.Finish;
            foreach (List<DB.BoundarySegment> segments in InternalSpace.GetBoundarySegments(options))
            {
                foreach (DB.BoundarySegment segment in segments)
                {
                    Curve crv = RevitToProtoCurve.ToProtoType(segment.GetCurve());

                    crv.Tessellate(package, parameters);
                    
                    _boundaryCurves.Add(crv);

                    if (package.LineVertexCount > 0)
                    {
                        package.ApplyLineVertexColors(CreateColorByteArrayOfSize(package.LineVertexCount, 255, 0, 0, 0));
                    }
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
