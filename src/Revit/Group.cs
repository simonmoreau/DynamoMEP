using System;
using DB = Autodesk.Revit.DB;
using DynamoServices;
using Autodesk.DesignScript.Runtime;
using Revit.Elements;
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

        private Group(DB.XYZ point, DB.GroupType groupType)
        {
            SafeInit(() => InitGroup(point,groupType));
        }

        #endregion

        #region Helpers for private constructors

        /// <summary>
        /// Initialize a group element
        /// </summary>
        /// <param name="group">An existing Revit Group</param>
        private void InitGroup(DB.Group group)
        {
            InternalSetGroup(group);
        }

        /// <summary>
        /// Place a Group in the model
        /// </summary>
        /// <param name="point">The group instance location</param>
        /// <param name="groupType">The type of the group</param>
        private void InitGroup(DB.XYZ point,DB.GroupType groupType)
        {
            DB.Document document = DocumentManager.Instance.CurrentDBDocument;
            
            // This creates a new wall and deletes the old one
            TransactionManager.Instance.EnsureInTransaction(document);

            //Phase 1 - Check to see if the object exists and should be rebound
            var groupElem = ElementBinder.GetElementFromTrace<DB.Group>(document);

            if (groupElem == null)
                groupElem = document.Create.PlaceGroup(point, groupType);

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
        /// Place an instance of a Revit group
        /// </summary>
        /// <param name="point">Location point for the group instance</param>
        /// <param name="groupType">The type of the group</param>
        /// <returns></returns>
        public static Group PlaceGroupInstance(Point point,GroupType groupType)
        {
            DB.XYZ revitPoint = GeometryPrimitiveConverter.ToXyz(point);

            return new Group(revitPoint,groupType.InternalGroupType);
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
        ///// <returns name="CustomRoom Name">The associated room Name</returns>
        ///// <returns name="CustomRoom Number">The associated room Number</returns>
        //[MultiReturn(new[] { "Name", "Number", "CustomRoom Number", "CustomRoom Name" })]
        //public Dictionary<string, string> IdentificationData()
        //{
        //    string roomName = "Unoccupied";
        //    string roomNumber = "Unoccupied";
        //    if (InternalGroup.CustomRoom != null)
        //    {
        //        roomName = InternalGroup.CustomRoom.Name;
        //        roomNumber = InternalGroup.CustomRoom.Number;
        //    }
        //    return new Dictionary<string, string>()
        //        {
        //            {"Name",InternalGroup.Name},
        //            {"Number",InternalGroup.Number},
        //            {"CustomRoom Name",roomName},
        //            {"CustomRoom Number",roomNumber}
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
        /// Create a group from an existing reference
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
