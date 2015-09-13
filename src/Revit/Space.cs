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

namespace Revit.Elements
{
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

        #endregion

        #region Private constructors

        /// <summary>
        /// Create from an existing Revit Element
        /// </summary>
        /// <param name="rebar"></param>
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
        /// <param name="rebar"></param>
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
                roomElem = document.Create.NewSpace(level,point);

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
        /// <param name="rebar"></param>
        private void InternalSetSpace(DB.Mechanical.Space space)
        {
            InternalSpace = space;
            InternalElementId = space.Id;
            InternalUniqueId = space.UniqueId;
        }

        #endregion

        #region Public static constructors

        /// <summary>
        /// Create an element based Tag
        /// </summary>
        /// <param name="element">Element to tag</param>
        /// <param name="horizontal">Horizontal alignment</param>
        /// <param name="addLeader">Add a leader</param>
        /// <returns></returns>
        public static Space ByPointAndLevel(Point point, Level level)
        {
            DB.Level revitLevel = level.InternalElement as DB.Level;

            DB.UV uv = new DB.UV(point.X, point.Y);

            return new Space(revitLevel, uv);
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
        /// OPTIONAL:
        /// Overriding ToString allows you to control what is
        /// displayed whenever the object's string representation
        /// is used. For example, ToString is called when the 
        /// object is displayed in a Watch node.
        /// </summary>
        /// <returns>The string representation of our object.</returns>
        public override string ToString()
        {
            return string.Format("Space {0} - {1} {2}", InternalSpace.Name, InternalSpace.Number, InternalSpace.Id.IntegerValue);
        }

        /// <summary>
        /// The Tessellate method in the IGraphicItem interface allows
        /// you to specify what is drawn when dynamo's visualization is
        /// updated.
        /// </summary>
        /// <param name="package">The IRenderPackage object into which you push your render data</param>
        /// <param name="param">Tessellation parameters</param>
        [IsVisibleInDynamoLibrary(false)]
        public void Tessellate(IRenderPackage package, TessellationParameters param)
        {
            //Location Point
            DB.LocationPoint locPoint = InternalSpace.Location as DB.LocationPoint;
            package.AddPointVertex(locPoint.Point.X, locPoint.Point.Y, locPoint.Point.Z);
            package.AddPointVertexColor(255, 0, 0, 255);

            //Boundaries
            DB.SpatialElementBoundaryOptions options = new DB.SpatialElementBoundaryOptions();
            options.SpatialElementBoundaryLocation = DB.SpatialElementBoundaryLocation.Finish;
            foreach (List<DB.BoundarySegment> segments in InternalSpace.GetBoundarySegments(options))
            {
                foreach (DB.BoundarySegment segment in segments)
                {
                    DB.XYZ endPoint0 = segment.GetCurve().GetEndPoint(0);
                    DB.XYZ endPoint1 = segment.GetCurve().GetEndPoint(1);
                    package.AddLineStripVertex(endPoint0.X, endPoint0.Y, endPoint0.Z);
                    package.AddLineStripVertex(endPoint1.X, endPoint1.Y, endPoint1.Z);

                    package.AddLineStripVertexColor(255, 0, 0, 255);
                    package.AddLineStripVertexColor(255, 0, 0, 255);

                    package.AddLineStripVertexCount(2);
                }
            }
        }

        #endregion

    }
}
