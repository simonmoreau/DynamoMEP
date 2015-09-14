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
    /// <summary>
    /// Architectural Rooms
    /// </summary>
    [DynamoServices.RegisterForTrace]
    public class Room : Element
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

        #endregion

        #region Private constructors

        /// <summary>
        /// Create from an existing Revit Element
        /// </summary>
        /// <param name="room">An existing Revit room</param>
        private Room(DB.Architecture.Room room)
        {
            SafeInit(() => InitRoom(room));
        }


        private Room(
            DB.Level level,
            DB.UV point)
        {
            SafeInit(() => InitRoom(level, point));
        }

        #endregion

        #region Helpers for private constructors

        /// <summary>
        /// Initialize a Rebar element
        /// </summary>
        /// <param name="room"></param>
        private void InitRoom(DB.Architecture.Room room)
        {
            InternalSetRoom(room);
        }


        private void InitRoom(
            DB.Level level,
            DB.UV point)
        {
            DB.Document document = DocumentManager.Instance.CurrentDBDocument;

            // This creates a new wall and deletes the old one
            TransactionManager.Instance.EnsureInTransaction(document);

            //Phase 1 - Check to see if the object exists and should be rebound
            var roomElem = ElementBinder.GetElementFromTrace<DB.Architecture.Room>(document);

            if (roomElem == null)
                roomElem = document.Create.NewRoom(level,point);

            InternalSetRoom(roomElem);

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
        /// <param name="room"></param>
        private void InternalSetRoom(DB.Architecture.Room room)
        {
            InternalRoom = room;
            InternalElementId = room.Id;
            InternalUniqueId = room.UniqueId;
        }

        #endregion

        #region Public static constructors

        /// <summary>
        /// Create an element based Tag
        /// </summary>
        /// <param name="point">Location point for the room</param>
        /// <param name="level">Level of the room</param>
        /// <returns></returns>
        public static Room ByPointAndLevel(Point point,Level level)
        {
            DB.Level revitLevel = level.InternalElement as DB.Level;

            DB.UV uv = new DB.UV(point.X, point.Y);

            return new Room(revitLevel, uv);
        }

        #endregion

        #region Internal static constructors

        /// <summary>
        /// Create a room from an existing reference
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="isRevitOwned"></param>
        /// <returns></returns>
        internal static Room FromExisting(DB.Architecture.Room tag, bool isRevitOwned)
        {
            return new Room(tag)
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
            return string.Format("Room {1} - {0}", InternalRoom.Name, InternalRoom.Number);
        }

        #endregion
    }
}
