using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using Revit.Elements;

namespace DynamoMEP
{
    class MEP
    {
        /// <summary>
        /// Retrive all spaces in project
        /// </summary>
        /// <returns>List of spaces</returns>
        public static List<Space> AllSpaces()
        {
            // Get the active Document
            Autodesk.Revit.DB.Document document = DocumentManager.Instance.CurrentDBDocument;

            DB.FilteredElementCollector collector = new DB.FilteredElementCollector(document);
            IEnumerable<DB.Element> elements = collector.OfCategory(DB.BuiltInCategory.OST_MEPSpaces).ToElements();
            List<Space> spaces = new List<Space>();

            foreach (DB.Element element in elements)
            {
                spaces.Add(Space.FromExisting(element as DB.Mechanical.Space, true));
            }

            return spaces;

        }

        /// <summary>
        /// Retrive all room in project
        /// </summary>
        /// <returns>List of rooms</returns>
        public static List<Room> AllRooms()
        {
            // Get the active Document
            Autodesk.Revit.DB.Document document = DocumentManager.Instance.CurrentDBDocument;

            DB.FilteredElementCollector collector = new DB.FilteredElementCollector(document);
            IEnumerable<DB.Element> elements = collector.OfCategory(DB.BuiltInCategory.OST_Rooms).ToElements();
            List<Room> spaces = new List<Room>();

            foreach (DB.Element element in elements)
            {
                spaces.Add(Room.FromExisting(element as DB.Architecture.Room, true));
            }

            return spaces;

        }
    }
}
