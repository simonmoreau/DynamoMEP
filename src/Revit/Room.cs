using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Revit.Elements;
using DB = Autodesk.Revit.DB;
using Autodesk.DesignScript.Geometry;
using RevitServices.Persistence;
using Revit.GeometryConversion;


namespace DynamoMEP
{
    /// <summary>
    /// Revit Rooms
    /// </summary>
    public static class Room
    {
        /// <summary>
        /// Create a Room
        /// based on a location
        /// </summary>
        /// <param name="point">Location point for the room</param>
        /// <returns></returns>
        public static Revit.Elements.Room ByPoint(Point point)
        {
            DB.XYZ revitPoint = GeometryPrimitiveConverter.ToXyz(point);
            DB.Level revitLevel = GetNearestLevel(revitPoint);

            Revit.Elements.Level level = revitLevel.ToDSType(false) as Revit.Elements.Level;

            // return new Revit.Elements.Room(revitLevel, uv);
            return Revit.Elements.Room.ByLocation(level, point);
        }

        /// <summary>
        /// Return a grid of points in the room
        /// </summary>
        /// <param name="room">The room in which the grid is created</param>
        /// <param name="step">Lenght between two points</param>
        public static List<Point> Grid(this Revit.Elements.Room room, double step)
        {
            try
            {
                step = Services.ConvertStepDeprecated(step);
            }
            catch (Exception)
            {

                step = Services.ConvertStep(step);
            }
            
            List<Point> grid = new List<Point>();
            DB.Architecture.Room InternalRoom = room.InternalElement as DB.Architecture.Room;

            if (InternalRoom != null)
            {
                DB.BoundingBoxXYZ bb = room.InternalElement.get_BoundingBox(null);

                for (double x = bb.Min.X; x < bb.Max.X;)
                {
                    for (double y = bb.Min.Y; y < bb.Max.Y;)
                    {
                        DB.XYZ point = new DB.XYZ(x, y, bb.Min.Z);
                        if (InternalRoom.IsPointInRoom(point))
                        {
                            grid.Add(GeometryPrimitiveConverter.ToPoint(GetTransform(room.InternalElement).OfPoint(point)));
                        }
                        y = y + step;
                    }

                    x = x + step;
                }
            }


            return grid;
        }

        private static DB.Transform GetTransform(DB.Element InternalElement)
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
        /// Retrive windows around the room
        /// </summary>
        public static List<Revit.Elements.FamilyInstance> Windows(this Revit.Elements.Room room)
        {
            return BoundaryFamilyInstance(DB.BuiltInCategory.OST_Windows, room.InternalElement);
        }

        /// <summary>
        /// Retrive Doors around the room
        /// </summary>
        public static List<Revit.Elements.FamilyInstance>Doors(this Revit.Elements.Room room)
        {
            return BoundaryFamilyInstance(DB.BuiltInCategory.OST_Doors, room.InternalElement);
        }

        /// <summary>
        /// Retrive room boundary elements
        /// </summary>
        public static List<Element> BoundaryElements(this Revit.Elements.Room room)
        {
            List<Element> output = new List<Element>();
            DB.Document doc = room.InternalElement.Document;

            foreach (DB.BoundarySegment segment in GetBoundarySegment(room.InternalElement))
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

        /// <summary>
        /// Retrive family instance hosted in boundary elements
        /// This is the base function for Windows and Doors
        /// </summary>
        /// <param name="cat">The category of hosted elements</param>
        /// <param name="internalElement">The room Revit element</param>
        /// <returns></returns>
        private static List<Revit.Elements.FamilyInstance> BoundaryFamilyInstance(DB.BuiltInCategory cat, DB.Element internalElement)
        {
            List<Revit.Elements.FamilyInstance> output = new List<Revit.Elements.FamilyInstance>();

            //the document of the room
            DB.Document doc = internalElement.Document; // DocumentManager.Instance.CurrentDBDocument;

            //Find boundary elements and their associated document
            List<DB.ElementId> boundaryElements = new List<DB.ElementId>();
            List<DB.Document> boundaryDocuments = new List<DB.Document>();

            List<DB.BoundarySegment> boundarySegments = GetBoundarySegment(internalElement);

            foreach (DB.BoundarySegment segment in boundarySegments)
            {
                DB.Element boundaryElement = doc.GetElement(segment.ElementId);

                if (boundaryElement != null)
                {
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
                        output.Add(ElementWrapper.ToDSType(boundaryFamilyInstance, true) as Revit.Elements.FamilyInstance);
                        continue;
                    }
                }

                if (boundaryFamilyInstance.get_ToRoom(familyInstancePhase) != null)
                {
                    if (boundaryFamilyInstance.get_ToRoom(familyInstancePhase).Id == internalElement.Id)
                    {
                        output.Add(ElementWrapper.ToDSType(boundaryFamilyInstance, true) as Revit.Elements.FamilyInstance);
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
