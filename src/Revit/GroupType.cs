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
    /// Revit GroupType
    /// </summary>
    [DynamoServices.RegisterForTrace]
    public class GroupType : Element, IGraphicItem
    {
        #region Internal Properties

        /// <summary>
        /// Internal reference to the Revit Element
        /// </summary>
        internal DB.GroupType InternalGroupType
        {
            get;
            private set;
        }

        /// <summary>
        /// Reference to the Element
        /// </summary>
        public override DB.Element InternalElement
        {
            get { return InternalGroupType; }
        }

        #endregion

        #region Private constructors

        /// <summary>
        /// Create from an existing Revit Element
        /// </summary>
        /// <param name="GroupType">An existing Revit GroupType</param>
        private GroupType(DB.GroupType GroupType)
        {
            SafeInit(() => InitGroupType(GroupType));
        }

        private GroupType(ICollection<DB.ElementId> ids)
        {
            SafeInit(() => InitGroupType(ids));
        }

        private GroupType(ICollection<DB.ElementId> ids, string name)
        {
            SafeInit(() => InitGroupType(ids, name));
        }

        #endregion

        #region Helpers for private constructors

        /// <summary>
        /// Initialize a GroupType element
        /// </summary>
        /// <param name="GroupType">An existing Revit GroupType</param>
        private void InitGroupType(DB.GroupType GroupType)
        {
            InternalSetGroupType(GroupType);
        }

        /// <summary>
        /// Initialize a GroupType element from a set of objects ids
        /// </summary>
        /// <param name="ids"></param>
        private void InitGroupType(ICollection<DB.ElementId> ids)
        {
            DB.Document document = DocumentManager.Instance.CurrentDBDocument;
            
            // This creates a new wall and deletes the old one
            TransactionManager.Instance.EnsureInTransaction(document);

            //Phase 1 - Check to see if the object exists and should be rebound
            var GroupTypeElem = ElementBinder.GetElementFromTrace<DB.GroupType>(document);

            if (GroupTypeElem == null)
                GroupTypeElem = document.Create.NewGroup(ids).GroupType;

            InternalSetGroupType(GroupTypeElem);

            TransactionManager.Instance.TransactionTaskDone();

            if (GroupTypeElem != null)
            {
                ElementBinder.CleanupAndSetElementForTrace(document, this.InternalElement);
            }
            else
            {
                ElementBinder.SetElementForTrace(this.InternalElement);
            }

        }

        /// <summary>
        /// Initialize a GroupType element from a set of objects ids and a name
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="name"></param>
        private void InitGroupType(ICollection<DB.ElementId> ids, string name)
        {
            DB.Document document = DocumentManager.Instance.CurrentDBDocument;

            //Find all groupType name in the document
            DB.FilteredElementCollector collector = new DB.FilteredElementCollector(document);
            List<string> groupTypeNames = collector.OfClass(typeof(DB.GroupType)).ToElements().Select(element => element.Name).ToList();

            // This creates a new wall and deletes the old one
            TransactionManager.Instance.EnsureInTransaction(document);

            //Phase 1 - Check to see if the object exists and should be rebound
            var GroupTypeElem = ElementBinder.GetElementFromTrace<DB.GroupType>(document);

            if (GroupTypeElem == null)
            {
                GroupTypeElem = document.Create.NewGroup(ids).GroupType;
                if (!groupTypeNames.Contains(name))
                {
                    GroupTypeElem.Name = name;
                }
                else
                {
                    GetNextFilename(name + " {0}", groupTypeNames);
                }
            }

            InternalSetGroupType(GroupTypeElem);

            TransactionManager.Instance.TransactionTaskDone();

            if (GroupTypeElem != null)
            {
                ElementBinder.CleanupAndSetElementForTrace(document, this.InternalElement);
            }
            else
            {
                ElementBinder.SetElementForTrace(this.InternalElement);
            }

        }

        private static string GetNextFilename(string pattern, List<string> names)
        {
            string tmp = string.Format(pattern, 1);
            if (tmp == pattern)
                throw new ArgumentException("The pattern must include an index place-holder", "pattern");

            if (!names.Contains(tmp))
                return tmp; // short-circuit if no matches

            int min = 1, max = 2; // min is inclusive, max is exclusive/untested

            while (names.Contains(string.Format(pattern, max)))
            {
                min = max;
                max *= 2;
            }

            while (max != min + 1)
            {
                int pivot = (max + min) / 2;
                if (names.Contains(string.Format(pattern, pivot)))
                    min = pivot;
                else
                    max = pivot;
            }

            return string.Format(pattern, max);
        }

        #endregion

        #region Private mutators

        /// <summary>
        /// Set the internal Element, ElementId, and UniqueId
        /// </summary>
        /// <param name="GroupType"></param>
        private void InternalSetGroupType(DB.GroupType GroupType)
        {
            InternalGroupType = GroupType;
            InternalElementId = GroupType.Id;
            InternalUniqueId = GroupType.UniqueId;
        }

        #endregion

        #region Public static constructors

        /// <summary>
        /// Create a Revit GroupType
        /// from a set of elements
        /// </summary>
        /// <param name="elements">A set of elements which will be made into the new GroupType.</param>
        /// <returns></returns>
        public static GroupType FromElements(List<Element> elements)
        {
            List<DB.ElementId> ids = new List<DB.ElementId>();
            foreach (Element elem in elements)
            {
                ids.Add(elem.InternalElement.Id);
            }

            return new GroupType(ids);
        }

        /// <summary>
        /// Create a Revit GroupType
        /// from a set of elements and a name
        /// </summary>
        /// <param name="elements">A set of elements which will be made into the new GroupType.</param>
        /// <param name="name">the name of the GroupType.</param>
        /// <returns></returns>
        public static GroupType FromElementsAndName(List<Element> elements, string name)
        {
            List<DB.ElementId> ids = new List<DB.ElementId>();
            foreach (Element elem in elements)
            {
                ids.Add(elem.InternalElement.Id);
            }

            return new GroupType(ids,name);
        }

        /// <summary>
        /// Create a GroupType
        /// from an Revit GroupType
        /// </summary>
        /// <param name="element">The origin element</param>
        /// <returns></returns>
        public static GroupType FromElement(Element element)
        {
            if (element.InternalElement.GetType() == typeof(DB.GroupType))
            {
                return new GroupType(element.InternalElement as DB.GroupType);
            }
            else
            {
                throw new ArgumentException("The Element is not a Revit GroupType");
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
        //    if (InternalGroupType.CustomRoom != null)
        //    {
        //        roomName = InternalGroupType.CustomRoom.Name;
        //        roomNumber = InternalGroupType.CustomRoom.Number;
        //    }
        //    return new Dictionary<string, string>()
        //        {
        //            {"Name",InternalGroupType.Name},
        //            {"Number",InternalGroupType.Number},
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

        //        foreach (List<DB.BoundarySegment> segments in InternalGroupType.GetBoundarySegments(opt))
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
        /// <param name="GroupType"></param>
        /// <param name="isRevitOwned"></param>
        /// <returns></returns>
        internal static GroupType FromExisting(DB.GroupType GroupType, bool isRevitOwned)
        {
            return new GroupType(GroupType)
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
        //    foreach (List<DB.BoundarySegment> segments in InternalGroupType.GetBoundarySegments(options))
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
            return string.Format("Model GroupType - {0}", InternalGroupType.Name);
        }

        #endregion

    }
}
