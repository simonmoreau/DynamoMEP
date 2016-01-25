using System;
using DB = Autodesk.Revit.DB;
using UI = Autodesk.Revit.UI;
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
    /// Revit Group
    /// </summary>
    [DynamoServices.RegisterForTrace]
    public class Group : Element, IGraphicItem
    {
        #region Internal Properties

        /// <summary>
        /// Internal reference to the Revit Element
        /// </summary>
        internal DB.Group InternalGroup
        {
            get;
            private set;
        }

        /// <summary>
        /// Reference to the Element
        /// </summary>
        public override DB.Element InternalElement
        {
            get { return InternalGroup; }
        }

        #endregion

        #region Private constructors

        /// <summary>
        /// Create from an existing Revit Element
        /// </summary>
        /// <param name="group">An existing Revit Group</param>
        private Group(DB.Group group)
        {
            SafeInit(() => InitGroup(group));
        }

        private Group(ICollection<DB.ElementId> ids)
        {
            SafeInit(() => InitGroup(ids));
        }

        #endregion

        #region Helpers for private constructors

        /// <summary>
        /// Initialize a Space element
        /// </summary>
        /// <param name="group">An existing Revit Group</param>
        private void InitGroup(DB.Group group)
        {
            InternalSetGroup(group);
        }

        /// <summary>
        /// Initialize a Group element from a set of objects
        /// </summary>
        /// <param name="level"></param>
        /// <param name="point"></param>
        private void InitGroup(ICollection<DB.ElementId> ids)
        {
            DB.Document document = DocumentManager.Instance.CurrentDBDocument;
            
            // This creates a new wall and deletes the old one
            TransactionManager.Instance.EnsureInTransaction(document);

            //Phase 1 - Check to see if the object exists and should be rebound
            var groupElem = ElementBinder.GetElementFromTrace<DB.Group>(document);

            if (groupElem == null)
                groupElem = document.Create.NewGroup(ids);

            InternalSetGroup(groupElem);

            TransactionManager.Instance.TransactionTaskDone();

            if (groupElem != null)
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
        /// <param name="group"></param>
        private void InternalSetGroup(DB.Group group)
        {
            InternalGroup = group;
            InternalElementId = group.Id;
            InternalUniqueId = group.UniqueId;
        }

        #endregion

        #region Public static constructors

        /// <summary>
        /// Create a Revit group
        /// from a set of elements
        /// </summary>
        /// <param name="elements">A set of elements which will be made into the new group.</param>
        /// <returns></returns>
        public static Group FromElements(List<Element> elements)
        {
            List<DB.ElementId> ids = new List<DB.ElementId>();
            foreach (Element elem in elements)
            {
                ids.Add(elem.InternalElement.Id);
            }

            return new Group(ids);
        }

        /// <summary>
        /// Create a group
        /// from an Revit group
        /// </summary>
        /// <param name="element">The origin element</param>
        /// <returns></returns>
        public static Group FromElement(Element element)
        {
            if (element.InternalElement.GetType() == typeof(DB.Group))
            {
                return new Group(element.InternalElement as DB.Group);
            }
            else
            {
                throw new ArgumentException("The Element is not a Revit Group");
            }
        }

        #endregion

        //#region public properties

        ///// <summary>
        ///// Retrive a set of properties 
        ///// for the Space
        ///// </summary>
        ///// <returns name="Name">The MEPSpace Name</returns>
        ///// <returns name="Number">The MEPSpace Number</returns>
        ///// <returns name="Room Name">The associated room Name</returns>
        ///// <returns name="Room Number">The associated room Number</returns>
        //[MultiReturn(new[] { "Name", "Number", "Room Number", "Room Name" })]
        //public Dictionary<string, string> IdentificationData()
        //{
        //    string roomName = "Unoccupied";
        //    string roomNumber = "Unoccupied";
        //    if (InternalGroup.Room != null)
        //    {
        //        roomName = InternalGroup.Room.Name;
        //        roomNumber = InternalGroup.Room.Number;
        //    }
        //    return new Dictionary<string, string>()
        //        {
        //            {"Name",InternalGroup.Name},
        //            {"Number",InternalGroup.Number},
        //            {"Room Name",roomName},
        //            {"Room Number",roomNumber}
        //        };
        //}

        ///// <summary>
        ///// Retrive space boundary elements
        ///// </summary>
        //public List<Element> BoundaryElements
        //{
        //    get
        //    {
        //        List<Element> output = new List<Element>();
        //        DB.Document doc = DocumentManager.Instance.CurrentDBDocument;
        //        DB.SpatialElementBoundaryOptions opt = new DB.SpatialElementBoundaryOptions();

        //        foreach (List<DB.BoundarySegment> segments in InternalGroup.GetBoundarySegments(opt))
        //        {
        //            foreach (DB.BoundarySegment segment in segments)
        //            {
        //                DB.Element boundaryElement = doc.GetElement(segment.ElementId);

        //                output.Add(ElementWrapper.ToDSType(boundaryElement, true));
        //            }

        //        }
        //        output = output.Distinct().ToList();
        //        return output;
        //    }
        //}

        //private List<Curve> _boundaryCurves = new List<Curve>();

        ///// <summary>
        ///// Retrive space boundary curves
        ///// </summary>
        //public List<Curve> BoundaryCurves
        //{
        //    get
        //    {
        //        return _boundaryCurves;
        //    }
        //}



        //#endregion

        #region Internal static constructors

        /// <summary>
        /// Create a space from an existing reference
        /// </summary>
        /// <param name="group"></param>
        /// <param name="isRevitOwned"></param>
        /// <returns></returns>
        internal static Group FromExisting(DB.Group group, bool isRevitOwned)
        {
            return new Group(group)
            {
                //IsRevitOwned = isRevitOwned
            };
        }

        #endregion

        #region Display Functions

        ///// <summary>
        ///// Display Spaces in the Dynamo interface
        ///// </summary>
        ///// <param name="package"></param>
        ///// <param name="parameters"></param>
        //[IsVisibleInDynamoLibrary(false)]
        //public new void Tessellate(IRenderPackage package, TessellationParameters parameters)
        //{
        //    //Ensure that the object is still alive
        //    if (!IsAlive) return;

        //    //Location Point
        //    DB.LocationPoint locPoint = InternalElement.Location as DB.LocationPoint;
        //    package.AddPointVertex(locPoint.Point.X, locPoint.Point.Y, locPoint.Point.Z);
        //    package.AddPointVertexColor(255, 0, 0, 255);

        //    //Boundaries
        //    DB.SpatialElementBoundaryOptions options = new DB.SpatialElementBoundaryOptions();
        //    options.SpatialElementBoundaryLocation = DB.SpatialElementBoundaryLocation.Finish;
        //    foreach (List<DB.BoundarySegment> segments in InternalGroup.GetBoundarySegments(options))
        //    {
        //        foreach (DB.BoundarySegment segment in segments)
        //        {
        //            Curve crv = RevitToProtoCurve.ToProtoType(segment.GetCurve());

        //            crv.Tessellate(package, parameters);

        //            _boundaryCurves.Add(crv);

        //            if (package.LineVertexCount > 0)
        //            {
        //                package.ApplyLineVertexColors(CreateColorByteArrayOfSize(package.LineVertexCount, 255, 0, 0, 0));
        //            }
        //        }
        //    }
        //}

        //private static byte[] CreateColorByteArrayOfSize(int size, byte red, byte green, byte blue, byte alpha)
        //{
        //    var arr = new byte[size * 4];
        //    for (var i = 0; i < arr.Length; i += 4)
        //    {
        //        arr[i] = red;
        //        arr[i + 1] = green;
        //        arr[i + 2] = blue;
        //        arr[i + 3] = alpha;
        //    }
        //    return arr;
        //}

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
            return string.Format("Model Group - {0}", InternalGroup.Name);
        }

        #endregion

    }
}
