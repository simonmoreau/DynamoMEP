using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Revit.Elements;
using DB = Autodesk.Revit.DB;


namespace DynamoMEP
{
    /// <summary>
    /// Revit Rooms
    /// </summary>
    public static class Room
    {

        /// <summary>
        /// Retrive windows around the room
        /// </summary>
        public static List<FamilyInstance> Windows(this Revit.Elements.Room room)
        {
            return BoundaryFamilyInstance(DB.BuiltInCategory.OST_Windows, room.InternalElement);
        }

        /// <summary>
        /// Retrive Doors around the room
        /// </summary>
        public static List<FamilyInstance>Doors(this Revit.Elements.Room room)
        {
            return BoundaryFamilyInstance(DB.BuiltInCategory.OST_Doors, room.InternalElement);
        }

        /// <summary>
        /// Retrive family instance hosted in boundary elements
        /// This is the base function for Windows and Doors
        /// </summary>
        /// <param name="cat">The category of hosted elements</param>
        /// <param name="internalElement">The room Revit element</param>
        /// <returns></returns>
        private static List<FamilyInstance> BoundaryFamilyInstance(DB.BuiltInCategory cat, DB.Element internalElement)
        {
            List<FamilyInstance> output = new List<FamilyInstance>();

            //the document of the room
            DB.Document doc = internalElement.Document; // DocumentManager.Instance.CurrentDBDocument;

            //Find boundary elements and their associated document
            List<DB.ElementId> boundaryElements = new List<DB.ElementId>();
            List<DB.Document> boundaryDocuments = new List<DB.Document>();

            List<DB.BoundarySegment> boundarySegments = GetBoundarySegment(internalElement);

            foreach (DB.BoundarySegment segment in boundarySegments)
            {
                DB.Element boundaryElement = doc.GetElement(segment.ElementId);
                if (boundaryElement.GetType() == typeof(DB.RevitLinkInstance))
                {
                    DB.RevitLinkInstance linkInstance = boundaryElement as DB.RevitLinkInstance;
                    boundaryDocuments.Add(linkInstance.GetLinkDocument());
                    boundaryElements.Add(segment.LinkElementId);
                }
                else
                {
                    boundaryDocuments.Add(doc);
                    boundaryElements.Add(segment.ElementId);
                }
            }

            // Create a category filter
            DB.ElementCategoryFilter filter = new DB.ElementCategoryFilter(cat);
            // Apply the filter to the elements in these documents,
            // Use shortcut WhereElementIsNotElementType() to find family instances in all boundary documents
            boundaryDocuments = boundaryDocuments.Distinct().ToList();
            List<DB.FamilyInstance> familyInstances = new List<DB.FamilyInstance>();
            foreach (DB.Document boundaryDocument in boundaryDocuments)
            {
                DB.FilteredElementCollector collector = new DB.FilteredElementCollector(boundaryDocument);
                familyInstances.AddRange(collector.WherePasses(filter).WhereElementIsNotElementType().ToElements().Cast<DB.FamilyInstance>().ToList());
            }

            //Find all family instance hosted on a boundary element
            IEnumerable<DB.FamilyInstance> boundaryFamilyInstances = familyInstances.Where(s => boundaryElements.Contains(s.Host.Id));

            //loop on these boundary family instance to find to and from room
            foreach (DB.FamilyInstance boundaryFamilyInstance in boundaryFamilyInstances)
            {
                DB.Phase familyInstancePhase = boundaryFamilyInstance.Document.GetElement(boundaryFamilyInstance.CreatedPhaseId) as DB.Phase;
                if (boundaryFamilyInstance.get_FromRoom(familyInstancePhase) != null)
                {
                    if (boundaryFamilyInstance.get_FromRoom(familyInstancePhase).Id == internalElement.Id)
                    {
                        output.Add(ElementWrapper.ToDSType(boundaryFamilyInstance, true) as FamilyInstance);
                        continue;
                    }
                }

                if (boundaryFamilyInstance.get_ToRoom(familyInstancePhase) != null)
                {
                    if (boundaryFamilyInstance.get_ToRoom(familyInstancePhase).Id == internalElement.Id)
                    {
                        output.Add(ElementWrapper.ToDSType(boundaryFamilyInstance, true) as FamilyInstance);
                    }
                }
            }

            output = output.Distinct().ToList();
            return output;
        }

        private static List<DB.BoundarySegment> GetBoundarySegment(DB.Element InternalElement)
        {
            List<DB.BoundarySegment> output = new List<DB.BoundarySegment>();
            DB.SpatialElementBoundaryOptions opt = new DB.SpatialElementBoundaryOptions();

            DB.Architecture.Room room = InternalElement as DB.Architecture.Room;
            
            if (room != null)
            {
                foreach (List<DB.BoundarySegment> segments in room.GetBoundarySegments(opt))
                {
                    foreach (DB.BoundarySegment segment in segments)
                    {
                        output.Add(segment);
                    }
                }
            }

            return output.Distinct().ToList();
        }
    }
}
